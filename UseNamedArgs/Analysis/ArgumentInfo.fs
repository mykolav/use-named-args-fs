[<AutoOpen>]
module UseNamedArgs.ArgumentInfo

open Microsoft.CodeAnalysis

type ArgumentInfo = {
    MethodOrProperty : ISymbol;
    Parameter : IParameterSymbol }