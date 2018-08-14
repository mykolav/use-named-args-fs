module UseNamedArgs.Tests.Support.CodeFixExpectations

[<RequireQualifiedAccess>]
module Expect =
    open Microsoft.CodeAnalysis.CodeFixes
    open Microsoft.CodeAnalysis.Diagnostics
    open DiagnosticProvider
    open DocumentExtensions
    open DocumentFactory

    /// <summary>
    /// General verifier for codefixes.
    /// Creates a Document from the source string, then gets diagnostics on it and applies the relevant codefixes.
    /// Then gets the string after the codefix is applied and compares it with the expected result.
    /// Note: If any codefix causes new diagnostics to show up, the test fails unless allowNewCompilerDiagnostics is set to true.
    /// </summary>
    /// <param name="analyzer">The analyzer to be applied to the source code</param>
    /// <param name="codeFixProvider">The codefix to be applied to the code wherever the relevant Diagnostic is found</param>
    /// <param name="lang">The language the source code is in</param>
    /// <param name="originalSource">A class in the form of a string before the CodeFix was applied to it</param>
    /// <param name="expectedSource">A class in the form of a string after the CodeFix was applied to it</param>
    /// <param name="codeFixIndex">Index determining which codefix to apply if there are multiple</param>
    /// <param name="allowNewCompilerDiagnostics">A bool controlling whether or not the test will fail if the CodeFix introduces other warnings after being applied</param>
    let toMatchFixedCode (analyzer: DiagnosticAnalyzer) 
                         (codeFixProvider: CodeFixProvider) 
                         (lang: Langs) (originalSource: string) 
                         (codeFixIndex: int option) (allowNewCompilerDiags: bool)
                         (expectedSource: string) =
        let doc = mkDocument(originalSource, lang)
        let analyzerDiags = analyzer.GetSortedDiagnosticsFromDocs([doc])
        let compilerDiags = doc.GetCompilerDiags()
        let attempts = analyzerDiags |> Seq.length
        ()
        

//        var document = DocumentFactory.CreateDocument(oldSource, language);
//        var analyzerDiagnostics = analyzer.GetSortedDiagnosticsFromDocuments(new[] { document });
//        var compilerDiagnostics = document.GetCompilerDiagnostics();
//        var attempts = analyzerDiagnostics.Length;
            
//        for (var i = 0; i < attempts; ++i)
//        {
//            var actions = new List<CodeAction>();
//            var context = new CodeFixContext(
//                document, 
//                analyzerDiagnostics[0], 
//                registerCodeFix: (a, d) => actions.Add(a), 
//                cancellationToken: CancellationToken.None);

//            codeFixProvider.RegisterCodeFixesAsync(context).Wait();
//            if (!actions.Any())
//                break;

//            if (codeFixIndex != null)
//            {
//                document = document.ApplyFix(actions.ElementAt((int)codeFixIndex));
//                break;
//            }

//            document = document.ApplyFix(actions.ElementAt(0));
//            analyzerDiagnostics = analyzer.GetSortedDiagnosticsFromDocuments(new[] { document });

//            var newCompilerDiagnostics = DiagnosticComparer.GetNewDiagnostics(
//                compilerDiagnostics, 
//                document.GetCompilerDiagnostics());

//            //check if applying the code fix introduced any new compiler diagnostics
//            if (!allowNewCompilerDiagnostics && newCompilerDiagnostics.Any())
//            {
//                // Format and get the compiler diagnostics again so that the locations make sense in the output
//                document = document.WithSyntaxRoot(Formatter.Format(document.GetSyntaxRootAsync().Result, Formatter.Annotation, document.Project.Solution.Workspace));
//                newCompilerDiagnostics = DiagnosticComparer.GetNewDiagnostics(compilerDiagnostics, document.GetCompilerDiagnostics());

//                assert.True(false,
//                    string.Format("Fix introduced new compiler diagnostics:\r\n{0}\r\n\r\nNew document:\r\n{1}\r\n",
//                        string.Join("\r\n", newCompilerDiagnostics.Select(d => d.ToString())),
//                        document.GetSyntaxRootAsync().Result.ToFullString()));
//            }

//            //check if there are analyzer diagnostics left after the code fix
//            if (!analyzerDiagnostics.Any())
//                break;
//        }

//        //after applying all of the code fixes, compare the resulting string to the inputted one
//        var expectedSource = newSource.Replace("\r\n", "\n");
//        var actualSource = document.ToSourceCode().Replace("\r\n", "\n");
//        assert.Equal(expectedSource, actualSource);
//    }
//}
