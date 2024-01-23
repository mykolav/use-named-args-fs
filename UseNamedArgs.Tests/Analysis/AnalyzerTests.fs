namespace UseNamedArgs.Tests


open Microsoft.CodeAnalysis
open Xunit
open UseNamedArgs.Analyzer
open UseNamedArgs.Tests.Analysis.Support
open UseNamedArgs.Tests.Support


[<RequireQualifiedAccess>]
module private Diagnostics =


    let Of(program: string): Diagnostic[] =
        let analyzer = UseNamedArgsAnalyzer()
        analyzer.Analyze(Document.Language.CSharp, [program])


type AnalyzerTests() =


    // TODO: Delegate. class C { void M(System.Action<int, int> f) => f(1, 2);
    // TODO: Indexer. class C { int this[int arg1, int arg2] => this[1, 2]; }
    // TODO: `this` ctor initializer. class C { C(int arg1, int arg2) {} C() : this(1, 2) {} }
    // TODO: `base` ctor initializer. class C { public C(int arg1, int arg2) {} } class D : C { D() : base(1, 2) {} }


    [<Fact>]
    member _.``Empty code does not trigger diagnostics``() =
        let diagnostics = Diagnostics.Of(CSharpProgram.WithClasses(@""))
        Assert.That(diagnostics).AreEmpty()


    [<Fact>]
    member _.``Method w/ zero args does not trigger diagnostics``() =
        let diagnostics = Diagnostics.Of(CSharpProgram.WithClasses(@"
            class Wombat
            {
                void Gork() {}
                void Bork()
                {
                    Gork();
                } }
        "))

        Assert.That(diagnostics).AreEmpty()


    [<Fact>]
    member _.``Method w/ one param does not trigger diagnostic``() =
        let diagnostics = Diagnostics.Of(CSharpProgram.WithClasses(@"
            class Wombat
            {
                void Gork(int powerLevel) {}
                void Bork()
                {
                    Gork(9001);
                } }
        "))

        Assert.That(diagnostics).AreEmpty()


    [<Fact>]
    member _.``Method w/ variable number of params does not trigger diagnostic``() =
        let diagnostics = Diagnostics.Of(CSharpProgram.WithClasses(@"
            class Wombat
            {
                void Gork(int line, int column, params string[] diagnosticMessages) {}
                void Bork()
                {
                    Gork(9000, 1, ""Goku"");
                } }
        "))

        Assert.That(diagnostics).AreEmpty()


    [<Fact>]
    member _.``Method w/ different type params invoked with positional args does not trigger diagnostic``() =
        let diagnostics = Diagnostics.Of(CSharpProgram.WithClasses(@"
            class Wombat
            {
                void Gork(string name, int powerLevel) {}
                void Bork()
                {
                    Gork(""Goku"", 9001);
                } }
        "))

        Assert.That(diagnostics).AreEmpty()


    [<Fact>]
    member _.``Method w/ same type params invoked with vars named same as params does not trigger diagnostic``() =
        let diagnostics = Diagnostics.Of(CSharpProgram.WithClasses(@"
            class Wombat
            {
                void Gork(string fileName, int line, int column) {}
                void Bork()
                {
                    var line = 9000;
                    var column = 1;
                    Gork(fileName: ""Gizmo.cs"", line, column);
                } }
        "))

        Assert.That(diagnostics).AreEmpty()


    [<Fact>]
    member _.``Method w/ same type params invoked with named args does not trigger diagnostic``() =
        let diagnostics = Diagnostics.Of(CSharpProgram.WithClasses(@"
            class Wombat
            {
                void Gork(string fileName, int line, int column) {}
                void Bork()
                {
                    Gork(fileName: ""Gizmo.cs"", line: 9000, column: 1);
                } }
        "))

        Assert.That(diagnostics).AreEmpty()


    [<Fact>]
    member _.``Method w/ same type params invoked with named args starting from same type params does not trigger diagnostic``() =
        let diagnostics = Diagnostics.Of(CSharpProgram.WithClasses(@"
            class Wombat
            {
                void Gork(string fileName, int line, int column) {}
                void Bork()
                {
                    Gork(""Gizmo.cs"", line: 9000, column: 1);
                } }
        "))

        Assert.That(diagnostics).AreEmpty()


    [<Fact>]
    member _.``Method w/ params of same type invoked w/ positional args triggers diagnostic``() =
        let diagnostics = Diagnostics.Of(CSharpProgram.WithClasses(@"
            class Wombat
            {
                void Gork(string fileName, int line, int column) {}
                void Bork()
                {
                    Gork(""Gizmo.cs"", 9000, 1);
                } }
        "))

        let expectedDiagnostic = ExpectedDiagnostic.UseNamedArgs(
                                    invokedMethod="Wombat.Gork",
                                    fileName="Test0.cs",
                                    line=10,
                                    column=21)


        Assert.That(diagnostics).Match([ expectedDiagnostic ])
