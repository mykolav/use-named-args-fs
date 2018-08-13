namespace UseNamedArgs.Analyzer

open System
open System.Collections.Immutable
open System.Text
open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.CSharp
open Microsoft.CodeAnalysis.CSharp.Syntax
open Microsoft.CodeAnalysis.Diagnostics
open UseNamedArgs.ArgumentAndParameter // Actually it's used
open UseNamedArgs.CSharpAdapters
open UseNamedArgs.InvocationExprSyntax
open UseNamedArgs.MaybeBuilder

[<DiagnosticAnalyzer(Microsoft.CodeAnalysis.LanguageNames.CSharp)>]
type public UseNamedArgsAnalyzer() = 
    inherit DiagnosticAnalyzer()

    static let diagnosticId = "UseNamedArgs"
    static let messageFormat = "'{0}' should be invoked with named arguments as parameters {1} have the same type"
    static let description = "Methods which have parameters of the same type should be invoked with named arguments."
    static let descriptor = 
        DiagnosticDescriptor(
            diagnosticId,
            "Method invocation with positional arguments." (*title*),
            messageFormat,
            "Naming" (*category*),
            DiagnosticSeverity.Warning (*defaultSeverity*), 
            true (*isEnabeledByDefault*), 
            description,
            null (*helpLinkUri*))

    static member DiagnosticId = diagnosticId
    static member MessageFormat = messageFormat

    override val SupportedDiagnostics = ImmutableArray.Create(descriptor)

    override this.Initialize (context: AnalysisContext) =
        // Register ourself to get invoked to analyze 
        //   - invocation expressions; e. g., calling a method. 
        //   - and object creation expressions; e. g., invoking a constructor.
        context.RegisterSyntaxNodeAction(
            (fun c -> this.Analyze c),
            SyntaxKind.InvocationExpression, 
            SyntaxKind.ObjectCreationExpression)

    member private this.filterSupported (methodSymbol: IMethodSymbol) = 
        match methodSymbol.MethodKind with
        // So far we only support analyzing of the three kinds of methods listed below.
        |     MethodKind.Ordinary
            | MethodKind.Constructor 
            | MethodKind.LocalFunction -> Some methodSymbol
        | _                            -> None

    member private this.formatDiagMessage argsWhichShouldBeNamed =
        let describeArgGroup 
            (groupDelim: string, sbDescr: StringBuilder) 
            (_, argAndParams: seq<_>) =
            let groupDescription = 
                String.Join(
                    ", ",
                    argAndParams |> Seq.map (fun it -> sprintf "'%s'" it.Parameter.Name))
            (" and ", sbDescr.Append(groupDelim)
                             .Append(groupDescription))

        argsWhichShouldBeNamed 
        |> Seq.fold describeArgGroup ("", StringBuilder()) 
        |> fun (_, sb) -> sb.ToString()

    member private this.Analyze(context: SyntaxNodeAnalysisContext) =
        maybe {
            let! sema = context.SemanticModel |> Option.ofObj
            let! invocationExprSyntax = context.Node |> Option.ofType<InvocationExpressionSyntax>
            let! methodSymbol = 
                sema.GetSymbolInfo(invocationExprSyntax).Symbol 
                |> Option.ofType<IMethodSymbol>
                >>= this.filterSupported
            // We got a supported kind of method.
            // Delegate heavy-lifting to the call below.
            let! argsWhichShouldBeNamed = getArgsWhichShouldBeNamed sema invocationExprSyntax

            // We inspected the arguments of invocation expression.
            if argsWhichShouldBeNamed |> Seq.any then
                // There are arguments that should be named -- emit the diagnostic.
                return context.ReportDiagnostic(
                    Diagnostic.Create(
                        descriptor, 
                        invocationExprSyntax.GetLocation(),
                        // messageArgs
                        methodSymbol.Name, 
                        this.formatDiagMessage argsWhichShouldBeNamed
                    )
                )
            // If none of them should be named or, maybe, they already are named,
            // we have nothing more to do.
            else return ()
        } |> ignore
