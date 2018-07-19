using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace UseNamedArguments.Tests.Support.Analyzer.Diagnostics
{
    public static class UseNamedArgumentsDiagnosticResult
    {
        public static readonly DiagnosticResult[] EmptyExpectedDiagnostics = {};

        public static DiagnosticResult Create(
            string diagnosticId,
            string analyzerMessageFormat,
            string invokedMethodName, 
            IEnumerable<IEnumerable<string>> parameterNamesByType, 
            params DiagnosticResultLocation[] locations)
        {
            var sbParametersDescriptions = new StringBuilder();
            var parametersDescriptionSeparator = "";
            foreach (var parameterNamesOfSameType in parameterNamesByType)
            {
                sbParametersDescriptions
                    .Append(parametersDescriptionSeparator)
                    .Append(string.Join(", ", parameterNamesOfSameType.Select(it => $"'{it}'")));

                parametersDescriptionSeparator = " and ";
            }

            var parametersDescriptions = sbParametersDescriptions.ToString();

            var message = string.Format(
                analyzerMessageFormat,
                invokedMethodName,
                parametersDescriptions);
            var diagnosticResult = new DiagnosticResult
            {
                Id = diagnosticId,
                Message = message,
                Severity = DiagnosticSeverity.Warning,
                Locations = locations
            };

            return diagnosticResult;
        }
    }
}
