module UseNamedArgs.CodeFixProvider

open System.Collections.Immutable
open System.Composition
open System.Threading
open System.Threading.Tasks
open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.CodeActions
open Microsoft.CodeAnalysis.CodeFixes
open Microsoft.CodeAnalysis.CSharp
open Microsoft.CodeAnalysis.CSharp.Syntax
open FSharp.Control.Tasks.V2.ContextInsensitive
open UseNamedArgs.Analyzer
open UseNamedArgs.ParameterInfo
open UseNamedArgs.InvocationExprSyntax

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
        let invocationExprSyntax = root.FindNode(diagnosticSpan) :?> InvocationExpressionSyntax;

        // Register a code action that will invoke the fix.
        context.RegisterCodeFix(
            CodeAction.Create(
                title,
                createChangedDocument = fun cancellationToken -> task {
                    return! this.UseNamedArgumentsAsync(
                        context.Document, 
                        root,
                        invocationExprSyntax, 
                        cancellationToken) 
                }, 
                equivalenceKey = title),
            diagnostic)
        return ()
    } :> Task)

    member private this.UseNamedArgumentsAsync(document: Document,
                                               root: SyntaxNode,
                                               invocationExprSyntax: InvocationExpressionSyntax,
                                               _: CancellationToken) = task {
        let! sema = document.GetSemanticModelAsync()

        // Figure out which exactly arguments should be converted from positional to named.
        // As it's the named args code fix provider, there must be Some args which should be named,
        // hence we use use Option.get to unwrap them.
        let argsWhichShouldBeNamed = getArgsWhichShouldBeNamed sema invocationExprSyntax |> Option.get

        // In case we have a diagnostic to get fixed, we still 
        // don't want to force all the invocation's arguments to be named --
        // it's up to the coder to decide on that.
        // What we do is finding the leftmost argument that should be named
        // and start named arguments from there.
        let ordinalOfFirstNamedArg = argsWhichShouldBeNamed
                                     |> Seq.collect (fun (_, args) -> args)
                                     |> Seq.minBy (fun { Parameter = param } -> param.Ordinal)
                                     |> fun { Parameter = param } -> param.Ordinal

        let maybeNameArg (arg: ArgumentSyntax) =
            match sema.GetParameterInfo arg with
            // Any argument to the right of the first named argument,
            // should be named too -- otherwise the code won't compile.
            | Some { Parameter = param } when param.Ordinal >= ordinalOfFirstNamedArg 
                -> arg.WithNameColon(SyntaxFactory.NameColon(param.Name))
                       // Preserve whitespaces, etc. from the original code.
                      .WithTriviaFrom(arg)
            | _ -> arg
        
        let originalArgList = invocationExprSyntax.ArgumentList
        let maybeNamedArgSyntaxes = originalArgList.Arguments |> Seq.map maybeNameArg

        let newArgumentList = originalArgList.WithArguments(
                                 SyntaxFactory.SeparatedList(
                                     maybeNamedArgSyntaxes,
                                     originalArgList.Arguments.GetSeparators()))
        // An argument list is an "addressable" syntax element, that we can directly
        // replace in the document's root.
        return document.WithSyntaxRoot(root.ReplaceNode(originalArgList, newArgumentList))
    }
