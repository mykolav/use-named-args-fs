module UseNamedArgs.Tests.AnalyzerTests

open Expecto
open UseNamedArguments.Tests.Support.Analyzer.Diagnostics
open UseNamedArgs.Analyzer
open UseNamedArguments.Tests.Support
open UseNamedArgs.Assert

module private Expect =
    let diagnostics expectedDiags code =
        UseNamedArgsCSharpAnalyzerRunner.InvokeAndVerifyDiagnostic(
            UseNamedArgsAnalyzer(),
            Assert(),
            code, 
            expectedDiags);

    let emptyDiagnostics = diagnostics [||]

[<Tests>]
let analyzerTests = 
    testList "The UseNamedArgs analyzer tests" [
        test "Empty code does not trigger diagnostics" {
            let testCodeSnippet = @"";
            Expect.emptyDiagnostics testCodeSnippet
        }
        test "Method w zero args does not trigger diagnostics" {
            let testCodeSnippet = @"
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

            Expect.emptyDiagnostics testCodeSnippet
        }
        test "Method w/ one param does not trigger diagnostic" {
            let testCodeSnippet = @"
                namespace Frobnitz
                {
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

            Expect.emptyDiagnostics testCodeSnippet
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

            let expectedDiagnostic = 
                UseNamedArgumentsDiagnosticResult.Create(
                    UseNamedArgsAnalyzer.DiagnosticId,
                    UseNamedArgsAnalyzer.MessageFormat,
                    invokedMethodName="Gork",
                    parameterNamesByType=([ [ "line"; "column" ] :> seq<_>] :> seq<_>),
                    locations=[|DiagnosticResultLocation("Test0.cs", line=9, column=29)|]);

            Expect.diagnostics [|expectedDiagnostic|] testCodeSnippet
        }
]
