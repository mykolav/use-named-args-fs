module UseNamedArgs.Tests.Support.UseNamedArgsDiagResult

[<RequireQualifiedAccess>]
module UseNamedArgsDiagResult =
    open System
    open System.Text
    open Microsoft.CodeAnalysis
    open DiagnosticResult
    open UseNamedArgs.Analyzer

    type Spec = {
        InvokedMethod: string
        ParamNamesByType: string seq seq
        FileName: string
        Line: uint32
        Column: uint32 }

    let create { InvokedMethod = invokedMethod
                 ParamNamesByType = paramNamesByType
                 FileName = fileName
                 Line = line
                 Column = column } =
        let sbParamsDescr = StringBuilder()
        let describeParamGroup (groupSeparator: string) 
                               (paramGroup: seq<string>) =
            sbParamsDescr.Append(groupSeparator)
                         .Append(String.Join(", ", paramGroup 
                                                   |> Seq.map (fun paramName -> "'" + paramName + "'")))
            |> ignore
            " and "
        paramNamesByType |> Seq.fold describeParamGroup "" |> ignore
        let paramsDescr = sbParamsDescr.ToString()
        let message = String.Format(UseNamedArgsAnalyzer.MessageFormat, 
                                    invokedMethod, 
                                    paramsDescr)
        let diagResult = DiagResult(id       = UseNamedArgsAnalyzer.DiagnosticId,
                                    message  = message,
                                    severity = DiagnosticSeverity.Warning,
                                    location = {Path=fileName; Line=line; Col=column})
        diagResult

