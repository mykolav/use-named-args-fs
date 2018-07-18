/// <summary>
/// This module contains code that looks at an invocation expression and its arguments
/// and decides whether the arguments should be named.
/// The rules are:
///   - If a method or ctor has a number of parameters of the same type 
///     the invocation's corresponding arguments should be named;
///   - If named arguments are used for all but one parameter of the same type
///     the analyzer doesn't emit the diagnostic;
///   - If the last parameter is <see langword="params" />, the analyzer
///     doesn't emit the diagnostic, as we cannot use named arguments in this case.
/// It's used by both 
///   - the <see cref="UseNamedArgsAnalyzer"/> class and
///   - the <see cref="UseNamedArgsCodeFixProvider"/> class.
/// </summary>
module UseNamedArgs.InvocationExprSyntax

open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.CSharp.Syntax
open UseNamedArgs.SemanticModelExtensions
open UseNamedArgs.MaybeBuilder
open UseNamedArgs.ArgumentAndParameter
open UseNamedArgs.CSharpAdapters
open UseNamedArgs.ArgumentInfo

let getArgumentAndParameters
    (sema: SemanticModel) 
    (argumentSyntaxes: SeparatedSyntaxList<ArgumentSyntax>) =
    let syntaxAndMaybeInfos = 
        argumentSyntaxes 
        |> Seq.map (fun it -> (it, sema.GetArgumentInfo it))
    let folder (syntax, maybeInfo) acc = maybe {
        let! argAndParams = acc 
        let! argInfo = maybeInfo
        return { Argument = syntax; Parameter = argInfo.Parameter }::argAndParams
    }
    Seq.foldBack folder syntaxAndMaybeInfos (Some [])

let getArgAndParamsGroupedByType 
    (sema: SemanticModel) 
    (argumentSyntaxes: SeparatedSyntaxList<ArgumentSyntax>) =
    maybe {
        let! argAndParams = getArgumentAndParameters sema argumentSyntaxes
        return argAndParams |> Seq.groupBy (fun it -> it.Parameter.Type)
    }

let filterArgsWhichShouldBeNamed (argAndParamsByType: seq<ITypeSymbol * seq<ArgumentAndParameter>>) =
    let shouldBeNamed (_, argAndParams: seq<ArgumentAndParameter>) =
        let argAndParamHaveSameName { Argument = arg; Parameter = param } =
            match arg.Expression with
            | :? IdentifierNameSyntax as idName -> 
                param.Name = idName.Identifier.ValueText
            | _ -> false
        let positionalArgsCount = argAndParams |> Seq.count (fun it -> isNull it.Argument.NameColon)
        // If among a group of args of the same type only one is positional,
        // it's impossible to accidentaly switch positions of two positional args.
        // Hence, we don't warn/require the arg should be named.
        if positionalArgsCount <= 1 then false else
        // Otherwise, there're multiple positional args.
        // In case the identifiers of the args are the same as the names of parameters
        // we don't warn/require the args should be named.
        let sameNameArgsAndParamsCount = argAndParams |> Seq.count argAndParamHaveSameName
        sameNameArgsAndParamsCount <> (Seq.length argAndParams)
    argAndParamsByType |> Seq.filter shouldBeNamed

/// <summary>
/// This method analyzes the supplied <paramref name="invocationExprSyntax" />
/// to see if any of the arguments need to be named.
/// </summary>
/// <param name="sema">The semantic model is necessary for the analysis</param>
/// <param name="invocationExprSyntax">The invocation to analyze</param>
/// <returns>
/// An option of list of arguments which should be named grouped by their types.
/// </returns>
let getArgumentsWhichShouldBeNamed 
    (sema: SemanticModel) 
    (invocationExprSyntax: InvocationExpressionSyntax) =
    let NoArgsShouldBeNamed = Seq.ofList []
    let argSyntaxes = invocationExprSyntax.ArgumentList.Arguments
    if argSyntaxes.Count = 0 then Some NoArgsShouldBeNamed else
    maybe {
        let! lastArgInfo = argSyntaxes |> Seq.last |> sema.GetArgumentInfo
        if lastArgInfo.Parameter.IsParams then return NoArgsShouldBeNamed else
        let! argAndParamsByType = getArgAndParamsGroupedByType sema argSyntaxes
        return filterArgsWhichShouldBeNamed argAndParamsByType
    }
