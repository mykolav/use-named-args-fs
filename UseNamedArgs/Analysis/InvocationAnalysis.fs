namespace UseNamedArgs.Analysis


open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.CSharp.Syntax
open UseNamedArgs.Analysis.SyntaxNodeArgumentExtensions
open UseNamedArgs.Analysis.SemanticModelParameterInfoExtensions


[<RequireQualifiedAccess>]
module private Seq =
    let count predicate source =
        source |> Seq.fold (fun acc it -> if predicate it then acc + 1 else acc) 0


type ArgumentInfo = {
    Syntax: IArgumentSyntax;
    ParameterSymbol: IParameterSymbol }


[<Struct>]
type AnalysisResult<'T>
    = StopAnalysis
    | OK of 'T


type InvocationAnalysis private(_sema: SemanticModel,
                                _expressionSyntax: SyntaxNode,
                                _methodSymbol: IMethodSymbol) =


    static let NoMissingArgumentNames: ArgumentInfo[] = [||]


    static let isSupported (methodSymbol: IMethodSymbol): bool =
        // So far we only support analyzing the four kinds of methods listed below.
        match methodSymbol.MethodKind with
        | MethodKind.Ordinary
        | MethodKind.Constructor
        | MethodKind.LocalFunction
        | MethodKind.ReducedExtension -> true
        | _                           -> false


    static let resolveMethodSymbol (sema: SemanticModel)
                                   (analyzedSyntaxNode: SyntaxNode)
                                   : AnalysisResult<IMethodSymbol> =
        // We want to inspect a syntax node
        // if it's a method/ctor invocation.
        // We expect a method or ctor invocation corresponds to
        // an expression or an attribute syntax node (invoking the attribute ctor).
        if not (analyzedSyntaxNode :? ExpressionSyntax ||
                analyzedSyntaxNode :? AttributeSyntax)
        then
            // This syntax node we're looking at cannot be an invocation.
            StopAnalysis
        else

        let symbolInfo = sema.GetSymbolInfo(analyzedSyntaxNode)
        match symbolInfo.Symbol with
        | :? IMethodSymbol as methodSymbol ->
            OK methodSymbol

        | _                                ->
            // If the symbol that corresponds to
            // the supplied syntax node is not an `IMethodSymbol`,
            // we cannot be looking at an invocation.
            StopAnalysis


    static member Of(sema: SemanticModel,
                     analyzedSyntaxNode: SyntaxNode)
                     : AnalysisResult<InvocationAnalysis> =
        let result = resolveMethodSymbol sema analyzedSyntaxNode
        match result with
        | StopAnalysis            ->
            StopAnalysis

        | OK analyzedMethodSymbol ->
            if isSupported analyzedMethodSymbol
            then
                // OK, we're ready to analyze this method invocation/object creation.
                OK (InvocationAnalysis(sema, analyzedSyntaxNode, analyzedMethodSymbol))
            else
                // It is an invocation, but
                // - we don't supported analyzing invocations of methods of this kind
                // - or the invoked method doesn't require its arguments to be named.
                StopAnalysis


    member this.MethodName: string = sprintf "%s.%s"_methodSymbol.ContainingType.Name _methodSymbol.Name


    /// <summary>
    /// This method analyzes the invocation/object creation expression `_expressionSyntax`
    /// to see if it can benefit from named arguments.
    /// </summary>
    /// <returns>
    /// Either an array of arguments suggested to be named.
    /// Or `StopAnalysis` which, somewhat surprisingly, means we should
    /// stop the analysis of current expression.
    /// </returns>
    member this.SuggestNamedArgument(): AnalysisResult<ArgumentInfo[]> =
        let argumentSyntaxes = _expressionSyntax.Arguments

        if argumentSyntaxes.Length = 0
        then
            OK NoMissingArgumentNames
        else

        let lastArgumentAt = argumentSyntaxes.Length - 1
        let lastArgument = argumentSyntaxes[lastArgumentAt]

        let lastParameterInfo = _sema.GetParameterInfo(_methodSymbol,
                                                       lastArgumentAt,
                                                       lastArgument.NameColon)
        match lastParameterInfo with
        | None ->
            StopAnalysis

        | Some lastParameterInfo ->
            // If the last parameter is `params`
            // we don't require named arguments.
            // TODO: Is this limitation still present in C#?
            if lastParameterInfo.Symbol.IsParams
            then
                OK NoMissingArgumentNames
            else

            let argumentInfos = this.GetArgumentInfos(argumentSyntaxes)
            match argumentInfos with
            | StopAnalysis     ->
                StopAnalysis

            | OK argumentInfos ->
                let suggestedNamedArguments =
                    argumentInfos
                    |> Array.groupBy (_.ParameterSymbol.Type)
                    |> Array.where (fun (_, argumentGroup) -> this.ShouldSuggestNaming(argumentGroup))
                    |> Array.collect (fun (_, argumentGroup) -> argumentGroup)
                    |> Array.sortBy (_.ParameterSymbol.Ordinal)

                OK suggestedNamedArguments


    member private this.GetArgumentInfos(argumentSyntaxes: IArgumentSyntax[])
                                        : AnalysisResult<ArgumentInfo[]> =

        let parameterInfoResults =
            argumentSyntaxes
            |> Seq.mapi (fun at a -> _sema.GetParameterInfo(_methodSymbol, at, a.NameColon))

        if parameterInfoResults |> Seq.exists Option.isNone
        then
            StopAnalysis
        else

        let argumentInfos =
            parameterInfoResults
            |> Seq.mapi (fun i (Some parameterInfo) -> { Syntax = argumentSyntaxes[i]
                                                         ParameterSymbol = parameterInfo.Symbol })
            |> Array.ofSeq

        OK argumentInfos


    member private this.ShouldSuggestNaming(argumentGroup: ArgumentInfo[]): bool =
        let positionalCount = argumentGroup
                              |> Seq.count (fun it -> isNull it.Syntax.NameColon)

        // If the group has one or zero positional arguments,
        // it's impossible to accidentally mix up positions of arguments.
        // Therefore, we don't suggest the arguments should be named in such a case.
        positionalCount > 1 &&

        // This portion of the expression evaluates when
        // there are multiple positional arguments.
        //
        // We don't suggest named arguments for cases like the following.
        // And we do suggest named arguments in all other cases.
        //
        // void Foo(int x, int y) {}
        // ...
        // var x = 1; var y = 2;
        // Foo(x, y);
        let sameNameCount =
             argumentGroup
             |> Seq.count (fun argument -> match argument.Syntax.Expression with
                                           | :? IdentifierNameSyntax as ins ->
                                               argument.ParameterSymbol.Name = ins.Identifier.ValueText
                                           | _ -> false)

        sameNameCount <> argumentGroup.Length
