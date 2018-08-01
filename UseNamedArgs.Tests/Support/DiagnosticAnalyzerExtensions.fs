
module UseNamedArgs.Tests.Support.Analyzer

open System.Collections.Generic
open System.Collections.Immutable
open System.Linq
open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.Diagnostics
open UseNamedArgs.Tests.Support.DocumentFactory

/// <summary>
/// Extensions for turning strings into documents and getting the diagnostics on them
/// </summary>
    type DiagnosticAnalyzer() =
        /// <summary>
        /// Given classes in the form of strings, their language, and an IDiagnosticAnalyzer to apply to it, return the diagnostics found in the string after converting it to a document.
        /// </summary>
        /// <param name="sources">Classes in the form of strings</param>
        /// <param name="language">The language the source classes are in</param>
        /// <param name="analyzer">The analyzer to be run on the sources</param>
        /// <returns>An IEnumerable of Diagnostics that surfaced in the source code, sorted by Location</returns>
        member analyzer.GetSortedDiagnostics(lang: Langs, sources: string list) =
                analyzer.GetSortedDiagnosticsFromDocs(mkDocuments(sources, lang))

        /// <summary>
        /// Given an analyzer and a document to apply it to, run the analyzer and gather an array of diagnostics found in it.
        /// The returned diagnostics are then ordered by location in the source document.
        /// </summary>
        /// <param name="analyzer">The analyzer to run on the documents</param>
        /// <param name="documents">The Documents that the analyzer will be run on</param>
        /// <returns>An IEnumerable of Diagnostics that surfaced in the source code, sorted by Location</returns>
        member analyzer.GetSortedDiagnosticsFromDocs(documents: Document list) =
                let diags: Diagnostic list = []
                diags
                //var projects = new HashSet<Project>();
                //foreach (var document in documents)
                //{
                //    projects.Add(document.Project);
                //}

                //var diagnostics = new List<Diagnostic>();
                //foreach (var project in projects)
                //{
                //    var compilationWithAnalyzers = project.GetCompilationAsync().Result.WithAnalyzers(ImmutableArray.Create(analyzer));
                //    var diags = compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync().Result;
                //    foreach (var diag in diags)
                //    {
                //        if (diag.Location == Location.None || diag.Location.IsInMetadata)
                //        {
                //            diagnostics.Add(diag);
                //        }
                //        else
                //        {
                //            foreach (var document in documents)
                //            {
                //                var tree = document.GetSyntaxTreeAsync().Result;
                //                if (tree == diag.Location.SourceTree)
                //                {
                //                    diagnostics.Add(diag);
                //                }
                //            }
                //        }
                //    }
                //}

                //var results = SortDiagnostics(diagnostics);
                //diagnostics.Clear();
                //return results;

        /// <summary>
        /// Sort diagnostics by location in source document
        /// </summary>
        member analyzer.SortDiagnostics (diags: seq<Diagnostic>) =
            diags |> Seq.sortBy (fun d -> d.Location.SourceSpan.Start)
