module UseNamedArgs.CodeFixProvider

open System.Composition
open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.CodeFixes
open System.Collections.Immutable
open UseNamedArgs.Analyzer
open FSharp.Control.Tasks.V2.ContextInsensitive
open System.Threading.Tasks

[<ExportCodeFixProvider(LanguageNames.CSharp, Name = "UseNamedArgsCodeFixProvider")>]
[<Shared>]
type public UseNamedArgsCodeFixProvider() = 
    inherit CodeFixProvider()

    static let title = "Use named args for params of same type"
    
    // This tells the infrastructure that this code-fix provider corresponds to
    // the `UseNamedArgsAnalyzer` analyzer.
    override val FixableDiagnosticIds = ImmutableArray.Create(UseNamedArgsAnalyzer.DiagnosticId)

    // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/FixAllProvider.md 
    // for more information on Fix All Providers
    override this.GetFixAllProvider() = WellKnownFixAllProviders.BatchFixer

    override this.RegisterCodeFixesAsync(context) = (task {
        let! root = context.Document.GetSyntaxRootAsync(context.CancellationToken)
        let diagnostic = context.Diagnostics |> Seq.head;
        let diagnosticSpan = diagnostic.Location.SourceSpan;

        return ()
    } :> Task)

(*
    [ExportCodeFixProvider(
        LanguageNames.CSharp, 
        Name = nameof(UseNamedArgsForParamsOfSameTypeCodeFixProvider))]
    [Shared]
    public class UseNamedArgsForParamsOfSameTypeCodeFixProvider : CodeFixProvider
    {
        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document
                .GetSyntaxRootAsync(context.CancellationToken)
                .ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var invocationExpressionSyntax = (InvocationExpressionSyntax)root.FindNode(diagnosticSpan);

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    title,
                    createChangedDocument: cancellationToken => 
                        UseNamedArgumentsAsync(
                            context.Document, 
                            root,
                            invocationExpressionSyntax, 
                            cancellationToken), 
                    equivalenceKey: title),
                diagnostic);
        }

        private async Task<Document> UseNamedArgumentsAsync(
            Document document,
            SyntaxNode root,
            InvocationExpressionSyntax invocationExpressionSyntax,
            CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync();

            // Figure out which exactly arguments should be converted from positional to named.
            var invocationExpressionSyntaxInfo = InvocationExpressionSyntaxInfo.From(
                semanticModel,
                invocationExpressionSyntax);

            // In case we have a diagnostic to get fixed, we still 
            // don't want to force all the invocation's arguments to be named --
            // it's up to the coder to decide on that.
            // What we do is finding the leftmost argument that should be named
            // and start named arguments from there.
            var ordinalOfFirstNamedArgument = invocationExpressionSyntaxInfo
                .ArgumentsWhichShouldBeNamed
                .SelectMany(argumentsByType => argumentsByType.arguments)
                .Min(argAndParam => argAndParam.Parameter.Ordinal);

            var originalArgumentList = invocationExpressionSyntax.ArgumentList;
            var newArgumentSyntaxes = new List<ArgumentSyntax>();
            foreach (var originalArgument in originalArgumentList.Arguments)
            {
                var newArgument = originalArgument;

                var argumentInfo = semanticModel.GetArgumentInfoOrThrow(originalArgument);
                // Any argument to the right of the first named argument,
                // should be named too -- otherwise the code won't compile.
                if (argumentInfo.Parameter.Ordinal >= ordinalOfFirstNamedArgument)
                { 
                    newArgument = originalArgument
                        .WithNameColon(
                            SyntaxFactory.NameColon(
                                argumentInfo.Parameter.Name.ToIdentifierName()
                            )
                        )
                        // Preserve whitespaces, etc. from the original code.
                        .WithTriviaFrom(originalArgument);
                }

                newArgumentSyntaxes.Add(newArgument);
            }

            var newArguments = SyntaxFactory.SeparatedList(
                newArgumentSyntaxes,
                originalArgumentList.Arguments.GetSeparators());

            var newArgumentList = originalArgumentList.WithArguments(newArguments);
            // An argument list is an "addressable" syntax element, that we can directly
            // replace in the document's root.
            var newRoot = root.ReplaceNode(originalArgumentList, newArgumentList);

            return document.WithSyntaxRoot(newRoot);
        }
    }
*)