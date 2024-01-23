namespace UseNamedArgs.Analyzer


open Microsoft.CodeAnalysis


module DiagnosticDescriptors =


    let NamedArgumentsSuggested =
        DiagnosticDescriptor(
            id="UseNamedArgs",
            title="A method invocation can benefit from named arguments",
            messageFormat="Consider invoking `{0}` with named arguments",
            category="Code style",
            defaultSeverity=DiagnosticSeverity.Warning,
            isEnabledByDefault=true,
            description="Methods having successive parameters of the same type can benefit from named arguments",
            helpLinkUri=null)


    let InternalError =
        DiagnosticDescriptor(
            id="UseNamedArgs9999",
            title="Use named arguments analysis experienced an internal error",
            messageFormat="An internal error in `{0}`",
            category="Code style",
            defaultSeverity=DiagnosticSeverity.Hidden,
            description="Use named arguments analysis experienced an internal error",
            isEnabledByDefault=false,
            helpLinkUri=null)
