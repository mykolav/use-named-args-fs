namespace UseNamedArgs.Tests.Analysis.Support


open System
open Microsoft.CodeAnalysis
open UseNamedArgs.Analyzer


type ExpectedLocation = {
    Path:   string
    Line:   int32
    Column: int32 }


type ExpectedDiagnostic = {
    Severity:            DiagnosticSeverity
    Id:                  string
    Message:             string
    Location:            ExpectedLocation option
    AdditionalLocations: ExpectedLocation[] }
    with


    static member UseNamedArgs(invokedMethod: string,
                               fileName: string,
                               line: int32,
                               column: int32)
                              : ExpectedDiagnostic =

        let message = String.Format(
            DiagnosticDescriptors.NamedArgumentsSuggested.MessageFormat.ToString(),
            invokedMethod)

        let expectedDiagnostic =
            { Id                  = DiagnosticDescriptors.NamedArgumentsSuggested.Id
              Message             = message
              Severity            = DiagnosticSeverity.Warning
              Location            = Some { Path=fileName; Line=line; Column=column }
              AdditionalLocations = Array.empty }

        expectedDiagnostic
