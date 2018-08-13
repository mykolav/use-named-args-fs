module UseNamedArgs.Tests.AnalyzerTests

open Expecto
open Support.AnalyzerExpectations
open Support.UseNamedArgsDiagResult
open UseNamedArgs.Analyzer

module private Expect =
    let toBeEmittedFrom code expectedDiags =
        Expect.diagnosticsInCSharp (UseNamedArgsAnalyzer()) [code] expectedDiags

    let emptyDiagnostics code = [||] |> toBeEmittedFrom code

// TODO: Delegate. class C { void M(System.Action<int, int> f) => f(1, 2);
// TODO: Indexer. class C { int this[int arg1, int arg2] => this[1, 2]; }
// TODO: `this` ctor initializer. class C { C(int arg1, int arg2) {} C() : this(1, 2) {} }
// TODO: `base` ctor initializer. class C { public C(int arg1, int arg2) {} } class D : C { D() : base(1, 2) {} }
// TODO: ctor. class C { C(int arg1, int arg2) { new C(1, 2); } }
// TODO: Attribute's parameters and properties?
[<Tests>]
let analyzerTests = 
    testList "The UseNamedArgs analyzer tests" [
        test "Empty code does not trigger diagnostics" {
            Expect.emptyDiagnostics @"";
        }
        test "Method w zero args does not trigger diagnostics" {
            Expect.emptyDiagnostics @"
                namespace Frobnitz
                {
                    class Wombat
                    {
                        void Gork() {}
                        void Bork()
                        {
                            Gork();
                        } } } 
            "
        }
        test "Method w/ one param does not trigger diagnostic" {
            Expect.emptyDiagnostics @"
                namespace Frobnitz
                {
                    class Wombat
                    {
                        void Gork(int powerLevel) {}
                        void Bork()
                        {
                            Gork(9001);
                        } } } 
            "
        }
        test "Method w/ variable number of params does not trigger diagnostic" {
            Expect.emptyDiagnostics @"
                namespace Frobnitz`
                {
                    class Wombat
                    {
                        void Gork(int line, int column, params string[] diagnosticMessages) {}
                        void Bork()
                        {
                            Gork(9000, 1, ""Goku"");
                        } } }
            "
        }
        test "Method w/ different type params invoked with positional args does not trigger diagnostic" {
            Expect.emptyDiagnostics @"
                namespace Frobnitz
                {
                    class Wombat
                    {
                        void Gork(string name, int powerLevel) {}
                        void Bork()
                        {
                            Gork(""Goku"", 9001);
                        } } }
            "
        }
        test "Method w/ same type params invoked with vars named same as params does not trigger diagnostic" {
            Expect.emptyDiagnostics @"
                namespace Frobnitz
                {
                    class Wombat
                    {
                        void Gork(string fileName, int line, int column) {}
                        void Bork()
                        {
                            var line = 9000;
                            var column = 1;
                            Gork(fileName: ""Gizmo.cs"", line, column);
                        } } }
            "
        }
        test "Method w/ same type params invoked with named args does not trigger diagnostic" {
            Expect.emptyDiagnostics @"
                namespace Frobnitz
                {
                    class Wombat
                    {
                        void Gork(string fileName, int line, int column) {}
                        void Bork()
                        {
                            Gork(fileName: ""Gizmo.cs"", line: 9000, column: 1);
                        } } }
            "
        }
        test "Method w/ same type params invoked with named args starting from same type params does not trigger diagnostic" {
            Expect.emptyDiagnostics @"
                namespace Frobnitz
                {
                    class Wombat
                    {
                        void Gork(string fileName, int line, int column) {}
                        void Bork()
                        {
                            Gork(""Gizmo.cs"", line: 9000, column: 1);
                        } } }
            "
        }
        test "Method w/ params of same type invoked w/ positional args triggers diagnostic" {
            let testCodeSnippet = @"
                namespace Frobnitz
                {
                    class Wombat
                    {
                        void Gork(string fileName, int line, int column) {}
                        void Bork()
                        {
                            Gork(""Gizmo.cs"", 9000, 1);
                        } } }
            "

            let expectedDiag = UseNamedArgsDiagResult.create {
                                   InvokedMethod="Gork"
                                   ParamNamesByType=[[ "line"; "column" ]]
                                   FileName="Test0.cs"; Line=9u; Column=29u }

            [|expectedDiag|] |> Expect.toBeEmittedFrom testCodeSnippet
        } ]
