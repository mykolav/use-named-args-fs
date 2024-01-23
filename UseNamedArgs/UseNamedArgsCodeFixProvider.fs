namespace UseNamedArgs.CodeFixProvider


open System.Collections.Immutable
open System.Composition
open System.Threading
open System.Threading.Tasks
open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.CodeActions
open Microsoft.CodeAnalysis.CodeFixes
open Microsoft.CodeAnalysis.CSharp.Syntax
open FSharp.Control.Tasks.V2.ContextInsensitive
open UseNamedArgs.Analyzer
open UseNamedArgs.Analysis
open UseNamedArgs.Analysis.SemanticModelParameterInfoExtensions
open UseNamedArgs.Analysis.SyntaxNodeArgumentExtensions


type private UseNamedArgsCodeFixContext =
    { Document:   Document
      SyntaxRoot: SyntaxNode
      Sema:       SemanticModel
      Ct:         CancellationToken }


[<ExportCodeFixProvider(LanguageNames.CSharp, Name = "UseNamedArgsCodeFixProvider")>]
[<Shared>]
type public UseNamedArgsCodeFixProvider() =
    inherit CodeFixProvider()


    static let Title = "Use named arguments"


    // This tells the infrastructure that this code-fix provider corresponds to
    // the `UseNamedArgsAnalyzer` analyzer.
    override val FixableDiagnosticIds = ImmutableArray.Create(DiagnosticDescriptors.NamedArgumentsSuggested.Id)


    // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/FixAllProvider.md
    // for more information on Fix All Providers
    override this.GetFixAllProvider() = WellKnownFixAllProviders.BatchFixer


    override this.RegisterCodeFixesAsync(context: CodeFixContext) = (task {
        let! syntaxRoot = context.Document.GetSyntaxRootAsync(context.CancellationToken)

        let diagnostic = Seq.head context.Diagnostics
        let syntaxNode = syntaxRoot.FindNode(diagnostic.Location.SourceSpan)

        let! sema = context.Document.GetSemanticModelAsync(context.CancellationToken)

        // Register a code action that will invoke the fix.
        context.RegisterCodeFix(
            CodeAction.Create(
                Title,
                createChangedDocument = fun cancellationToken -> task {
                    return! this.UseNamedArguments(
                        { Document   = context.Document
                          SyntaxRoot = syntaxRoot
                          Sema       = sema
                          Ct         = cancellationToken  },
                        syntaxNode)
                },
                equivalenceKey = Title),
            diagnostic)
        return ()
    } :> Task)


    member private this.UseNamedArguments(context: UseNamedArgsCodeFixContext,
                                          currentSyntaxNode: SyntaxNode)
                                         : Task<Document> = task {
        let invocationAnalysis = InvocationAnalysis.Of(context.Sema, currentSyntaxNode)
        match invocationAnalysis with
        | StopAnalysis ->
            return context.Document

        | OK invocationAnalysis ->
            let suggestedNamedArguments = invocationAnalysis.SuggestNamedArgument()
            match suggestedNamedArguments with
            | StopAnalysis
            | OK [||]      ->
                return context.Document

            | OK suggestedNamedArguments ->
                return! this.ReplaceWithNamedArguments(context,
                                                       // `syntaxNode.ArgumentList` must be present
                                                       // otherwise we wouldn't get to this point in code.
                                                       currentSyntaxNode.ArgumentList.Value,
                                                       suggestedNamedArguments)
    }


    member private this.ReplaceWithNamedArguments(context: UseNamedArgsCodeFixContext,
                                                  argumentList: IArgumentListSyntax,
                                                  argumentsToReplace: ArgumentInfo[])
                                                 : Task<Document> = task {
        match argumentList with
        | :? IArgumentListSyntax<ArgumentSyntax> as list ->
            return! this.DoReplaceWithNamedArguments(context, list, argumentsToReplace)

        | :? IArgumentListSyntax<AttributeArgumentSyntax> as list ->
            return! this.DoReplaceWithNamedArguments(context, list, argumentsToReplace)

        | _ ->
            return context.Document
    }


    member private this.DoReplaceWithNamedArguments<'T when 'T :> SyntaxNode>(
        context: UseNamedArgsCodeFixContext,
        list: IArgumentListSyntax<'T>,
        argumentsToReplace: ArgumentInfo[])
        : Task<Document> = task {

        // We are replacing some positional arguments with named.
        // But we still don't want to force all the arguments to be named.
        // It's up to the user whether they want all the arguments named or only some.
        //
        // Therefore we don't touch any arguments to
        // the left of the first argument that should be named.
        let ordinalOfFirstNamed = argumentsToReplace[0].ParameterSymbol.Ordinal
        let parentSymbol = context.Sema.GetSymbolInfo(list.Parent, context.Ct).Symbol

        let withName(position: int, argument: IArgumentSyntax<'T>): 'T =
            match context.Sema.GetParameterInfo(parentSymbol, position, argument.NameColon) with
            | None    -> argument.Syntax
            | Some pi ->
                if pi.Symbol.Ordinal < ordinalOfFirstNamed
                then
                    argument.Syntax
                else
                    argument.WithNameColon(pi.Symbol.Name)
                            .WithTriviaFrom(argument.Syntax)

        let argumentWithNames =
            list.Arguments
            |> Seq.mapi (fun position argument -> withName(position, argument))

        let listWithNamedArguments = list.WithArguments(argumentWithNames)

        // An argument list is an "addressable" syntax element, that we can directly
        // replace in the document's root.
        return context.Document.WithSyntaxRoot(
            context.SyntaxRoot.ReplaceNode(list.Syntax, listWithNamedArguments.Syntax))
    }
