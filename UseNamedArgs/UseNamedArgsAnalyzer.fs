namespace UseNamedArgs.Analyzer


open System
open System.Collections.Immutable
open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.CSharp
open Microsoft.CodeAnalysis.Diagnostics
open UseNamedArgs.Analysis
open UseNamedArgs.Analyzer


[<DiagnosticAnalyzer(LanguageNames.CSharp)>]
type public UseNamedArgsAnalyzer() =
    inherit DiagnosticAnalyzer()


    let joinParameterNames (argumentWithMissingNames: seq<ArgumentInfo>): string =
        let parameterNames = argumentWithMissingNames
                             |> Seq.map (fun it -> "'" + it.ParameterSymbol.Name + "'")
        String.Join(", ", parameterNames)


    override val SupportedDiagnostics =
        ImmutableArray.Create(
            DiagnosticDescriptors.NamedArgumentsSuggested,
            DiagnosticDescriptors.InternalError)


    override this.Initialize (context: AnalysisContext) =
        // We don't want to suggest named args in generated code.
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None)
        // We can handle concurrent invocations.
        context.EnableConcurrentExecution()

        // Register ourself to get invoked to analyze
        //   - invocation expressions; e. g., calling a method.
        //   - and object creation expressions; e. g., invoking a constructor.
        context.RegisterSyntaxNodeAction(
            (fun c -> this.Analyze c),
            SyntaxKind.InvocationExpression,
            SyntaxKind.ObjectCreationExpression,
            SyntaxKind.ImplicitObjectCreationExpression,
            SyntaxKind.Attribute)


    member private this.Analyze(context: SyntaxNodeAnalysisContext) =
        try
            this.DoAnalyze(context)
        with
        | ex ->
            context.ReportDiagnostic(
                Diagnostic.Create(
                    DiagnosticDescriptors.InternalError,
                    context.Node.GetLocation(),
                    // messageArgs
                    ex.ToString()))


    member private this.DoAnalyze(context: SyntaxNodeAnalysisContext) =
        let invocationAnalysis = InvocationAnalysis.Of(context.SemanticModel, context.Node)
        match invocationAnalysis with
        | StopAnalysis ->
            ()

        | OK invocationAnalysis ->
            let argumentWithMissingNames = invocationAnalysis.SuggestNamedArgument()
            match argumentWithMissingNames with
            | StopAnalysis ->
                ()

            | OK argumentWithMissingNames ->
                if Array.isEmpty argumentWithMissingNames
                then
                    ()
                else

                // There are arguments we want to suggest be named.
                // Emit a corresponding diagnostic.
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DiagnosticDescriptors.NamedArgumentsSuggested,
                        context.Node.GetLocation(),
                        // messageArgs
                        invocationAnalysis.MethodName,
                        joinParameterNames argumentWithMissingNames))
