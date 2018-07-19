using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using UseNamedArgs.TestsSupport.Contract;

namespace UseNamedArguments.Tests.Support.CodeFix
{
    public static class UseNamedArgsCSharpCodeFixRunner
    {
        private const string Placeholder = "<PLACEHOLDER>";
        private static readonly string SourceTemplate = $@"
            namespace Frobnitz
            {{
                class Wombat
                {{
                    {Placeholder}
                }}
            }}
        ";

        /// <summary>
        /// Called to test a C# codefix when applied on the inputted string as a source
        /// </summary>
        /// <param name="originalSourceSnippet">A class in the form of a string before the CodeFix was applied to it</param>
        /// <param name="expectedSourceSnippet">A class in the form of a string after the CodeFix was applied to it</param>
        /// <param name="codeFixIndex">Index determining which codefix to apply if there are multiple</param>
        /// <param name="allowNewCompilerDiagnostics">A bool controlling whether or not the test will fail if the CodeFix introduces other warnings after being applied</param>
        public static void InvokeAndVerifyResult(
            this CodeFixProvider codeFixProvider,
            IAssert assert,
            DiagnosticAnalyzer diagnosticAnalyzer,
            string originalSourceSnippet, 
            string expectedSourceSnippet, 
            int? codeFixIndex = null, 
            bool allowNewCompilerDiagnostics = false)
        {
            var originalSource = FormatSource(originalSourceSnippet);
            var expectedSource = FormatSource(expectedSourceSnippet);

            CodeFixRunnerAndVerifier.InvokeAndVerifyCSharpFix(
                assert,
                codeFixProvider,
                diagnosticAnalyzer,
                originalSource,
                expectedSource,
                codeFixIndex,
                allowNewCompilerDiagnostics
            );
        }

        private static string FormatSource(string snippet)
            => SourceTemplate.Replace(Placeholder, snippet);
    }
}
