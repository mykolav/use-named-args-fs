module UseNamedArgs.SemanticModelExtensions

open System
open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.CSharp.Syntax
open UseNamedArgs.ArgumentInfo
open UseNamedArgs.CSharpAdapters
open UseNamedArgs.MaybeBuilder

/// <summary>
/// To be able to convert positional arguments to named we need to find
/// corresponding <see cref="IParameterSymbol" /> for each argument.
/// </summary>
type SemanticModel with
    member semanticModel.GetArgumentInfo (argumentOrNull: ArgumentSyntax) =
        maybe {
            let! argument = Option.ofObj argumentOrNull
            let! argList = argument.Parent |> Option.ofType<ArgumentListSyntax>
            let! exprSyntax = argList.Parent  |> Option.ofType<ExpressionSyntax>
            let! methodOrProperty = semanticModel.GetSymbolInfo(exprSyntax).Symbol |> Option.ofType<ISymbol>
            let! parameters = methodOrProperty.GetParameters() |> Option.ofList
            if isNull argument.NameColon then
                // A positional argument.
                let index = argList.Arguments.IndexOf(argument)
                if index < 0 then return! None
                elif index < parameters.Length then return { MethodOrProperty = methodOrProperty;
                                                             Parameter = parameters.[index] }
                elif index >= parameters.Length && 
                    parameters.[parameters.Length - 1].IsParams then
                        return { MethodOrProperty = methodOrProperty;
                                 Parameter = parameters.[parameters.Length - 1] }
                else return! None
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
