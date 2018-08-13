﻿module UseNamedArgs.Tests.Support.DiagnosticResult

open Microsoft.CodeAnalysis

/// <summary>
/// Location where the diagnostic appears, as determined by path, line number, and column number.
/// </summary>
type DiagResultLocation = { 
    Path: string
    Line: uint32
    Col:  uint32 }

/// <summary>
/// Struct that stores information about a Diagnostic appearing in a source
/// </summary>
type DiagResult(location           : DiagResultLocation option,
                additionalLocations: DiagResultLocation list,
                severity           : DiagnosticSeverity,
                id                 : string,
                message            : string) = 
        member val Location            = location
        member val AdditionalLocations = additionalLocations
        member val Severity            = severity
        member val Id                  = id
        member val Message             = message
