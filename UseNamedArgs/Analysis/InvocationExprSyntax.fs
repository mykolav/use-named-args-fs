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
open UseNamedArgs.MaybeBuilder
open UseNamedArgs.ArgumentAndParameter
open UseNamedArgs.ParameterInfo

let private getArgAndParams
    (sema: SemanticModel) 
    (argumentSyntaxes: SeparatedSyntaxList<ArgumentSyntax>) =
    let syntaxAndMaybeInfos = 
        argumentSyntaxes 
        |> Seq.map (fun it -> (it, sema.GetParameterInfo it))
    let folder (syntax, maybeInfo) acc = maybe {
        let! argAndParams = acc 
        let! { ParameterInfo.Parameter = param } = maybeInfo
        return { Argument = syntax; Parameter = param }::argAndParams
    }
    Seq.foldBack folder syntaxAndMaybeInfos (Some [])

let private argsShouldBeNamed (_, argAndParams: seq<ArgumentAndParameter>) =
    let argAndParamHaveSameName { Argument = arg; Parameter = param } =
        match arg.Expression with
        | :? IdentifierNameSyntax as idName -> 
            param.Name = idName.Identifier.ValueText
        | _ -> false
    let positionalArgsCount = argAndParams |> Seq.count (fun it -> isNull it.Argument.NameColon)
    // If among a group of args of the same type only one is positional,
    // it's impossible to accidentaly switch positions of two positional args.
    // Hence, we don't warn/require the arg should be named.
    positionalArgsCount > 1 &&
    // Otherwise, there're multiple positional args.
    // In case the identifiers of the args are the same as the names of parameters
    // we don't warn/require the args should be named.
    let sameNameArgsAndParamsCount = argAndParams |> Seq.count argAndParamHaveSameName
    sameNameArgsAndParamsCount <> (Seq.length argAndParams)

/// <summary>
/// This method analyzes the supplied <paramref name="invocationExprSyntax" />
/// to see if any of the arguments need to be named.
/// </summary>
/// <param name="sema">The semantic model is necessary for the analysis</param>
/// <param name="invocationExprSyntax">The invocation to analyze</param>
/// <returns>
/// An option of list of arguments which should be named grouped by their types.
/// </returns>
let getArgsWhichShouldBeNamed 
    (sema: SemanticModel) 
    (invocationExprSyntax: InvocationExpressionSyntax) =
    let NoArgsShouldBeNamed = Seq.ofList []
    let argSyntaxes = invocationExprSyntax.ArgumentList.Arguments
    if argSyntaxes.Count = 0 then Some NoArgsShouldBeNamed else
    maybe {
        let! { Parameter = lastParam } = argSyntaxes |> Seq.last |> sema.GetParameterInfo
        if lastParam.IsParams then return NoArgsShouldBeNamed else
        return! getArgAndParams sema argSyntaxes 
                |>> Seq.groupBy (fun it -> it.Parameter.Type) 
                |>> Seq.filter argsShouldBeNamed
    }
