module UseNamedArgs.ParameterInfo

open System
open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.CSharp.Syntax
open UseNamedArgs.CSharpAdapters
open UseNamedArgs.MaybeBuilder

type ParameterInfo = {
    MethodOrProperty : ISymbol;
    Parameter : IParameterSymbol }

/// <summary>
/// To be able to convert positional arguments to named we need to find
/// corresponding <see cref="IParameterSymbol" /> for each argument.
/// </summary>
type SemanticModel with
    member sema.GetParameterInfo (argumentOrNull: ArgumentSyntax) =
        maybe {
            let! argument = Option.ofObj argumentOrNull
            let! argList = argument.Parent |> Option.ofType<ArgumentListSyntax>
            let! exprSyntax = argList.Parent  |> Option.ofType<ExpressionSyntax>
            let! methodOrProperty = sema.GetSymbolInfo(exprSyntax).Symbol |> Option.ofType<ISymbol>
            let! parameters = methodOrProperty.GetParameters() |> Option.ofList
            if isNull argument.NameColon then
                // A positional argument.
                match argList.Arguments.IndexOf(argument) with
                | index when index >= 0 && index < parameters.Length -> 
                    return { MethodOrProperty = methodOrProperty;
                             Parameter = parameters.[index] }
                | index when index >= parameters.Length 
                             && parameters.[parameters.Length - 1].IsParams ->
                    return { MethodOrProperty = methodOrProperty;
                             Parameter = parameters.[parameters.Length - 1] }
                | _ -> return! None
            else 
                // Potentially, this is a named argument.
                let! name = argument.NameColon.Name |> Option.ofObj
                let! nameText = name.Identifier.ValueText |> Option.ofObj
                // Yes, it's a named argument.
                let! parameter = parameters |> Seq.tryFind (fun param -> 
                    String.Equals(param.Name, 
                                  nameText, 
                                  StringComparison.Ordinal))

                return { MethodOrProperty = methodOrProperty;
                         Parameter = parameter }
        }
