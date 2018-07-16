[<AutoOpen>]
module UseNamedArgs.ArgumentSyntaxAndParameterSymbol

open Microsoft.CodeAnalysis.CSharp.Syntax
open Microsoft.CodeAnalysis

type ArgumentSyntaxAndParameterSymbol = {
    ArgumentSyntax: ArgumentSyntax;
    ParameterSymbol: IParameterSymbol }
