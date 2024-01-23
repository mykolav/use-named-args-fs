namespace UseNamedArgs.Tests


open System
open System.Collections.Generic
open System.Threading
open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.CodeActions
open Microsoft.CodeAnalysis.CodeFixes
open UseNamedArgs.Analyzer
open UseNamedArgs.CodeFixProvider
open UseNamedArgs.Tests.CodeFix.Support
open UseNamedArgs.Tests.Support
open Xunit


[<Sealed; AbstractClass>]
type private CSharpProgram private () =


    static member private FixedFrom(originalSourceCode: string): string =
        let document = Document.SingleFrom(Document.Language.CSharp, originalSourceCode)

        let analyzer = UseNamedArgsAnalyzer()
        let analyzerDiagnostics = Array.ofSeq (analyzer.Analyze([ document ]))
        let analyzerDiagnostic = analyzerDiagnostics[0]

        let codeFixProvider = UseNamedArgsCodeFixProvider()
        let codeActions = CSharpProgram.CodeActionsFor(codeFixProvider, document, analyzerDiagnostic)
        let codeAction = codeActions[0]

        let sourceCodeWithAppliedCodeFix =
           document.WithApplied(codeAction)
                   .ToSourceCode()

        sourceCodeWithAppliedCodeFix


    static member private CodeActionsFor(codeFixProvider: CodeFixProvider,
                                         document: Document,
                                         diagnostic: Diagnostic)
                                         : CodeAction[] =
        let codeActions = List<CodeAction>()
        let context = CodeFixContext(document,
                                     diagnostic,
                                     registerCodeFix = Action<_, _>(fun codeAction _ -> codeActions.Add(codeAction)),
                                     cancellationToken = CancellationToken.None)
        codeFixProvider.RegisterCodeFixesAsync(context).Wait()

        Array.ofSeq codeActions


    static member FixedFromClasses(classes: string): string =
        CSharpProgram.FixedFrom(
            CSharpProgram.WithClasses(classes))


    static member FixedFromStatements(statements: string): string =
        CSharpProgram.FixedFrom(
            CSharpProgram.WithStatements(statements))


type CodeFixProviderTests() =


    // TODO: Delegate. class C { void M(System.Action<int, int> f) => f(1, 2);
    // TODO: Indexer. class C { int this[int arg1, int arg2] => this[1, 2]; }
    // TODO: `this` ctor initializer. class C { C(int arg1, int arg2) {} C() : this(1, 2) {} }
    // TODO: `base` ctor initializer. class C { public C(int arg1, int arg2) {} } class D : C { D() : base(1, 2) {} }


    [<Fact>]
    member _.``Method w/ same type params invocation w/ positional args is fixed to named args``() =
        let original = @"
            void Gork(string fileName, int line, int column) {}
            void Bork()
            {
                Gork(""Gizmo.cs"", 9000, 1);
            }
        "

        let expected = @"
            void Gork(string fileName, int line, int column) {}
            void Bork()
            {
                Gork(""Gizmo.cs"", line: 9000, column: 1);
            }
        "

        Assert.That(CSharpProgram.FixedFromStatements(original))
              .IsEqualIgnoringWhitespaceTo(CSharpProgram.WithStatements(expected))


    [<Fact>]
    member _.``Method w/ same type params invocation w/ positional args is fixed to named args preserving trivia``() =
        let original = @"
            void Gork(string fileName, int line, int column) {}
            void Bork()
            {
                Gork(
                    ""Gizmo.cs"",


                    9000,
                    1);
            }
        "

        let expected = @"
            void Gork(string fileName, int line, int column) {}
            void Bork()
            {
                Gork(
                    ""Gizmo.cs"",


                    line: 9000,
                    column: 1);
            }
        "

        Assert.That(CSharpProgram.FixedFromStatements(original))
              .IsEqualIgnoringWhitespaceTo(CSharpProgram.WithStatements(expected))


    [<Fact>]
    member _.``Method w/ first 2 params of same type and 3rd param of another type: invocation w/ positional args is fixed to named args``() =
        let original = @"
            void Gork(int line, int column, string fileName) {}
            void Bork()
            {
                Gork(9000, 1, ""Gizmo.cs"");
            }
        "

        let expected = @"
            void Gork(int line, int column, string fileName) {}
            void Bork()
            {
                Gork(line: 9000, column: 1, fileName: ""Gizmo.cs"");
            }
        "

        Assert.That(CSharpProgram.FixedFromStatements(original))
              .IsEqualIgnoringWhitespaceTo(CSharpProgram.WithStatements(expected))


    [<Fact>]
    member _.``Method w/ 1st and 3rd params of same type: and 2nd param of another type invocation w positional args is fixed to named args``() =
        let original = @"
            void Gork(int line, string fileName, int column) {}
            void Bork()
            {
                Gork(9000, ""Gizmo.cs"", 1);
            }
        "

        let expected = @"
            void Gork(int line, string fileName, int column) {}
            void Bork()
            {
                Gork(line: 9000, fileName: ""Gizmo.cs"", column: 1);
            }
        "

        Assert.That(CSharpProgram.FixedFromStatements(original))
              .IsEqualIgnoringWhitespaceTo(CSharpProgram.WithStatements(expected))


    [<Fact>]
    member _.``Method w/ 3 params of same type invocation w/ 1st arg named and 2 positional args is fixed to named args``() =
        let original = @"
            void Gork(string foo, string bar, string baz) {}
            void Bork()
            {
                Gork(foo: ""pupper"", ""doggo"", ""woofer"");
            }
        "

        let expected = @"
            void Gork(string foo, string bar, string baz) {}
            void Bork()
            {
                Gork(foo: ""pupper"", bar: ""doggo"", baz: ""woofer"");
            }
        "

        Assert.That(CSharpProgram.FixedFromStatements(original))
              .IsEqualIgnoringWhitespaceTo(CSharpProgram.WithStatements(expected))


    [<Fact>]
    member _.``Method w/ 3 params of same type invocation w/ 2nd arg named and 2 positional args is fixed to named args``() =
        let original = @"
            void Gork(string foo, string bar, string baz) {}
            void Bork()
            {
                Gork(""pupper"", bar: ""doggo"", ""woofer"");
            }
        "

        let expected = @"
            void Gork(string foo, string bar, string baz) {}
            void Bork()
            {
                Gork(foo: ""pupper"", bar: ""doggo"", baz: ""woofer"");
            }
        "

        Assert.That(CSharpProgram.FixedFromStatements(original))
              .IsEqualIgnoringWhitespaceTo(CSharpProgram.WithStatements(expected))


    [<Fact>]
    member _.``Method w/ 3 params of same type invocation w/ 3rd arg named and 2 positional args is fixed to named args``() =
        let original = @"
            void Gork(string foo, string bar, string baz) {}
            void Bork()
            {
                Gork(""pupper"", ""doggo"", baz: ""woofer"");
            }
        "

        let expected = @"
            void Gork(string foo, string bar, string baz) {}
            void Bork()
            {
                Gork(foo: ""pupper"", bar: ""doggo"", baz: ""woofer"");
            }
        "

        Assert.That(CSharpProgram.FixedFromStatements(original))
              .IsEqualIgnoringWhitespaceTo(CSharpProgram.WithStatements(expected))


    [<Fact>]
    member _.``Extension method w/ params of same type invoked w/ positional args is fixed to named args``() =
        let original = @"
            class Wombat
            {
                void Bork()
                {
                    new Wombat().Gork(""Gizmo.cs"", 9000, 1);
                }
            }

            static class WombatExtensions
            {
                public static void Gork(this Wombat wombat, string fileName, int line, int column) {}
            }
        "

        let expected = @"
            class Wombat
            {
                void Bork()
                {
                    new Wombat().Gork(""Gizmo.cs"", line: 9000, column: 1);
                }
            }

            static class WombatExtensions
            {
                public static void Gork(this Wombat wombat, string fileName, int line, int column) {}
            }
        "

        Assert.That(CSharpProgram.FixedFromClasses(original))
              .IsEqualIgnoringWhitespaceTo(CSharpProgram.WithClasses(expected))


    [<Fact>]
    member _.``Constructor w/ params of same type invoked w/ positional args triggers diagnostic``() =
        let original = @"
            class Wombat
            {
                Wombat(string fileName, int line, int column) {}

                void Bork()
                {
                    new Wombat(""Gizmo.cs"", 9000, 1);
                }
            }
        "

        let expected = @"
            class Wombat
            {
                Wombat(string fileName, int line, int column) {}

                void Bork()
                {
                    new Wombat(""Gizmo.cs"", line: 9000, column: 1);
                }
            }
        "

        Assert.That(CSharpProgram.FixedFromClasses(original))
              .IsEqualIgnoringWhitespaceTo(CSharpProgram.WithClasses(expected))


    [<Fact>]
    member _.``Primary constructor w/ params of same type invoked w/ positional args triggers diagnostic``() =
        let original = @"
            record Wombat(string fileName, int line, int column)
            {
                void Bork()
                {
                    new Wombat(""Gizmo.cs"", 9000, 1);
                }
            }
        "

        let expected = @"
            record Wombat(string fileName, int line, int column)
            {
                void Bork()
                {
                    new Wombat(""Gizmo.cs"", line: 9000, column: 1);
                }
            }
        "

        Assert.That(CSharpProgram.FixedFromClasses(original))
              .IsEqualIgnoringWhitespaceTo(CSharpProgram.WithClasses(expected))
