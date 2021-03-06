﻿module UseNamedArgs.Tests.CodeFixProviderTests

open Expecto
open UseNamedArgs.Analyzer
open UseNamedArgs.CodeFixProvider
open UseNamedArgs.Tests.Support.CodeFixExpectations
open UseNamedArgs.Tests.Support.DocumentFactory

[<RequireQualifiedAccess>]
module private Expect =
    let formatSource (source: string) =
        sprintf
            "namespace Frobnitz
            {
                class Wombat
                {
                    %s
                }
            }" source

    let toBeFixedAndMatch expectedFixedSnippet originalSnippet =
        let expectedFixedSource = formatSource expectedFixedSnippet
        let originalSource = formatSource originalSnippet
        Expect.toMatchFixedCode (UseNamedArgsAnalyzer()) (UseNamedArgsCodeFixProvider())
                                CSharp originalSource
                                None false
                                expectedFixedSource

// TODO: Delegate. class C { void M(System.Action<int, int> f) => f(1, 2);
// TODO: Indexer. class C { int this[int arg1, int arg2] => this[1, 2]; }
// TODO: `this` ctor initializer. class C { C(int arg1, int arg2) {} C() : this(1, 2) {} }
// TODO: `base` ctor initializer. class C { public C(int arg1, int arg2) {} } class D : C { D() : base(1, 2) {} }
// TODO: ctor. class C { C(int arg1, int arg2) { new C(1, 2); } }
// TODO: Attribute's parameters and properties?
[<Tests>]
let codeFixProviderTests = 
    testList "The UseNamedCodeFixProvider code-fix provider tests" [
        testList "Method w/ same type params" [
            test "Invocation w/ positional args is fixed to named args" {
                let originalSnippet = @"
                    void Gork(string fileName, int line, int column) {}
                    void Bork()
                    {
                        Gork(""Gizmo.cs"", 9000, 1);
                    }
                "

                let expectedFixedSnippet = @"
                    void Gork(string fileName, int line, int column) {}
                    void Bork()
                    {
                        Gork(""Gizmo.cs"", line: 9000, column: 1);
                    }
                "

                originalSnippet |> Expect.toBeFixedAndMatch expectedFixedSnippet
            } 
            test "Method w/ same type params: invocation w/ positional args is fixed to named args preserving trivia" {
                let originalSnippet = @"
                    void Gork(string fileName, int line, int column) {}
                    void Bork()
                    {
                        Gork(
                            ""Gizmo.cs"",


                            9000,
                            1);
                    }
                "

                let expectedFixedSnippet = @"
                    void Gork(string fileName, int line, int column) {}
                    void Bork()
                    {
                        Gork(
                            ""Gizmo.cs"",


                            line: 9000,
                            column: 1);
                    }
                "

                originalSnippet |> Expect.toBeFixedAndMatch expectedFixedSnippet
        } ]
        test "Method w/ first 2 params of same type and 3rd param of another type: invocation w/ positional args is fixed to named args" {
            let originalSnippet = @"
                void Gork(int line, int column, string fileName) {}
                void Bork()
                {
                    Gork(9000, 1, ""Gizmo.cs"");
                }
            "

            let expectedFixedSnippet = @"
                void Gork(int line, int column, string fileName) {}
                void Bork()
                {
                    Gork(line: 9000, column: 1, fileName: ""Gizmo.cs"");
                }
            "

            originalSnippet |> Expect.toBeFixedAndMatch expectedFixedSnippet
        }
        test "Method w/ 1st and 3rd params of same type: and 2nd param of another type invocation w positional args is fixed to named args" {
            let originalSnippet = @"
                void Gork(int line, string fileName, int column) {}
                void Bork()
                {
                    Gork(9000, ""Gizmo.cs"", 1);
                }
            "

            let expectedFixedSnippet = @"
                void Gork(int line, string fileName, int column) {}
                void Bork()
                {
                    Gork(line: 9000, fileName: ""Gizmo.cs"", column: 1);
                }
            "

            originalSnippet |> Expect.toBeFixedAndMatch expectedFixedSnippet
        }
        testList "Method w/ 3 params of same type" [
            test "Invocation w/ 1st arg named and 2 positional args is fixed to named args" {
                let originalSnippet = @"
                    void Gork(string foo, string bar, string baz) {}
                    void Bork()
                    {
                        Gork(foo: ""pupper"", ""doggo"", ""woofer"");
                    }
                "

                let expectedFixedSnippet = @"
                    void Gork(string foo, string bar, string baz) {}
                    void Bork()
                    {
                        Gork(foo: ""pupper"", bar: ""doggo"", baz: ""woofer"");
                    }
                "

                originalSnippet |> Expect.toBeFixedAndMatch expectedFixedSnippet
            }
            test "Invocation w/ 2nd arg named and 2 positional args is fixed to named args" {
                let originalSnippet = @"
                    void Gork(string foo, string bar, string baz) {}
                    void Bork()
                    {
                        Gork(""pupper"", bar: ""doggo"", ""woofer"");
                    }
                "

                let expectedFixedSnippet = @"
                    void Gork(string foo, string bar, string baz) {}
                    void Bork()
                    {
                        Gork(foo: ""pupper"", bar: ""doggo"", baz: ""woofer"");
                    }
                "

                originalSnippet |> Expect.toBeFixedAndMatch expectedFixedSnippet
            }
            test "Invocation w/ 3rd arg named and 2 positional args is fixed to named args" {
                let originalSnippet = @"
                    void Gork(string foo, string bar, string baz) {}
                    void Bork()
                    {
                        Gork(""pupper"", ""doggo"", baz: ""woofer"");
                    }
                "

                let expectedFixedSnippet = @"
                    void Gork(string foo, string bar, string baz) {}
                    void Bork()
                    {
                        Gork(foo: ""pupper"", bar: ""doggo"", baz: ""woofer"");
                    }
                "

                originalSnippet |> Expect.toBeFixedAndMatch expectedFixedSnippet
            } ] ]
