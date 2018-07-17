namespace UseNamedArgs.Analyzer

open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.Diagnostics
open Microsoft.CodeAnalysis.CSharp
open Microsoft.CodeAnalysis.CSharp.Syntax
open System.Collections.Immutable
open UseNamedArgs.MaybeBuilder
open UseNamedArgs.CSharpAdapters
open UseNamedArgs.Common

[<DiagnosticAnalyzer(Microsoft.CodeAnalysis.LanguageNames.CSharp)>]
type public UseNamedArgsAnalyzer() = 
    inherit DiagnosticAnalyzer()
    let descriptor = DiagnosticDescriptor("FSharpIsLowerCase", 
                            "Types cannot contain lowercase letters", 
                            "{0} contains lowercase letters" , 
                            "Naming", 
                            DiagnosticSeverity.Warning, 
                            true, 
                            "User declared types should not contain lowercase letters.", 
                            null)

    //override x.SupportedDiagnostics with get() = ImmutableArray.Create(descriptor)
    override val SupportedDiagnostics = ImmutableArray.Create(descriptor)

    override this.Initialize (context: AnalysisContext) =
        // Register ourself to get invoked to analyze 
        //   - invocation expressions; e. g., calling a method. 
        //   - and object creation expressions; e. g., invoking a constructor.
        context.RegisterSyntaxNodeAction(
            //new Action<_>(this.Analyze),
            (fun c -> this.Analyze c),
            SyntaxKind.InvocationExpression, 
            SyntaxKind.ObjectCreationExpression)

    member private this.Analyze(context: SyntaxNodeAnalysisContext) =
        (*
            var semanticModel = context.SemanticModel;
            var invocationExpressionSyntax = (InvocationExpressionSyntax)context.Node;

            var methodSymbol = semanticModel.GetSymbolInfo(invocationExpressionSyntax).Symbol as IMethodSymbol;
            if (methodSymbol == null)
                return;

            // So far we only support analyzing of the three kinds of methods listed below.
            if (!methodSymbol.MethodKind.In(
                    MethodKind.Ordinary, 
                    MethodKind.Constructor, 
                    MethodKind.LocalFunction))
            {
                return;
            }

            // We got a supported kind of method.
            // Delegate heavy-lifting to the call below.
            var invocationExpressionSyntaxInfo = InvocationExpressionSyntaxInfo.From(
                semanticModel,
                invocationExpressionSyntax);

            // We inspected the arguments of invocation expression.
            // If none of them should be named or, maybe, they already are named,
            // we have nothing more to do. Just return control to the calling code then.
            if (!invocationExpressionSyntaxInfo.ArgumentsWhichShouldBeNamed.Any())
                return;

            // There are arguments that should be named.
            // Prepare the diagnositc's message.
            var sbArgumentsOfSameTypeDescriptions = new StringBuilder();
            var argumentsOfSameTypeSeparator = "";
            foreach (var argumentsOfSameType in 
                invocationExpressionSyntaxInfo.ArgumentsWhichShouldBeNamed)
            {
                var argumentsOfSameTypeDescription = string.Join(
                    ", ", 
                    argumentsOfSameType.arguments.Select(it => $"'{it.Parameter.Name}'"));

                sbArgumentsOfSameTypeDescriptions
                    .Append(argumentsOfSameTypeSeparator)
                    .Append(argumentsOfSameTypeDescription);

                argumentsOfSameTypeSeparator = " and ";
            }

            // And finally, emit the diagnostic.
            context.ReportDiagnostic(
                Diagnostic.Create(
                    Rule, 
                    invocationExpressionSyntax.GetLocation(), 
                    messageArgs: new object[] {
                        methodSymbol.Name, 
                        sbArgumentsOfSameTypeDescriptions.ToString()
                    })
            );
        *)
        maybe {
            let! semanticModel = context.SemanticModel |> Option.ofObj
            let! invocationExprSyntax = context.Node |> Option.ofType<InvocationExpressionSyntax>
            let! methodSymbol = 
                semanticModel.GetSymbolInfo(invocationExprSyntax).Symbol 
                |> Option.ofType<IMethodSymbol>

            match methodSymbol.MethodKind with
            // So far we only support analyzing of the three kinds of methods listed below.
            | (  MethodKind.Ordinary
                | MethodKind.Constructor 
                | MethodKind.LocalFunction) -> return ()
            | _                             -> return ()

            // We've got a supported kind of method.
            // Delegate heavy-lifting to the call below.
            //var invocationExpressionSyntaxInfo = InvocationExpressionSyntaxInfo.From(
            //    semanticModel,
            //    invocationExpressionSyntax);
        } |> ignore
