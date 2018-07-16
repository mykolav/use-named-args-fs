module UseNamedArgs.CSharpAdapters

open System.Collections.Immutable
open Microsoft.CodeAnalysis

let toList (ia: ImmutableArray<'a>) =
    [for i in 0 .. (ia.Length - 1) -> ia.[i]]

type ISymbol with
    member symbol.GetParameters() =
        match symbol with
        | :? IMethodSymbol as s   -> s.Parameters
        | :? IPropertySymbol as s -> s.Parameters
        | _                       -> ImmutableArray<IParameterSymbol>.Empty
        |> toList

type Option<'a> with
    static member ofType<'Derived when 'Derived : null> (baseObj: obj) = 
        match baseObj with 
        | :? 'Derived as derivedObj -> Some derivedObj
        | _                         -> None
    static member ofList list =
        match list with
        | [] -> None
        | _  -> Some list

