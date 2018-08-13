module UseNamedArgs.Tests.Support.AnalyzerExpectations

module Expect =
    open DiagnosticMatcher
    open DiagnosticProvider
    open DiagnosticResult
    open DocumentFactory
    open Microsoft.CodeAnalysis.Diagnostics

    let diagnosticsInCSharp (analyzer: DiagnosticAnalyzer) 
                            (sources: seq<string>) 
                            (expected: seq<DiagResult>) =
        expected |> Expect.diagnosticsToMatch analyzer 
                                              (analyzer.GetSortedDiagnostics(CSharp, sources))
