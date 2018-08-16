# Use named arguments for invocations of methods with multiple parameters of the same type

## Motivation

Quoting from this [comment](https://github.com/dotnet/roslyn-analyzers/issues/1216#issuecomment-304967649
) in a [new analyzer request](https://github.com/dotnet/roslyn-analyzers/issues/1216#issuecomment-304967649).

> [...] analyzer here would enforce used of named arguments 
> when you have successive parameters of the same type 
> (or convertible to same type with implicit conversions).

## Download and install

Install the [UseNamedArgs](https://www.nuget.org/packages/UseNamedArgs) nuget package.
For example, run the following command in the [NuGet Package Manager Console](https://docs.microsoft.com/en-us/nuget/tools/package-manager-console).

```powershell
Install-Package UseNamedArgs
```
   
This will download all the binaries, and add necessary analyzer references to your project.

## How to use it?

Once the nuget package is installed in a project it will analyze invocation expressions and emit a warning if the invocation should use named arguments.  
The rules to decide if an argumetn should be named are described in the [How does it work?](#how-does-it-work) section.

For example:
```csharp
public static void SetBookmark(string fileName, int line, int column) {}

// Elsewhere in your code:
// if `SetBookmark` method is called with positional arguments,
// the analyzer will emit a warning.
SetBookmark(fileName: "Program.cs", line: 9000, column: 1);
```

## <a name="how-does-it-work"></a> How does it work?

This analyzer looks at an invocation expression (e.g., a method call) and its arguments and suggests using named arguments according to the following rules:
- If a method or ctor has a number of parameters of the same type the invocation's corresponding arguments should be named.
- If named arguments are used for all but one parameter of the same type the analyzer doesn't emit a diagnostic. Two arguments of the same type cannot accidentally take each other's place in the described scenario. So the decision to have all the arguments named is a matter of code-style in this case and we leave it up to the developer.
- If the last parameter is `params`, the analyzer doesn't emit a diagnostic, as we cannot use named arguments in this case.

![The UseNamedArgs analyzer in action](./use-named-args-demo.gif)

## Technical details

The analyzer, code-fix provider, and tests are implemented in F#

# Thank you!

- [John Koerner](https://github.com/johnkoerner) for [Creating a Code Analyzer using F#](https://johnkoerner.com/code-analysis/creating-a-code-analyzer-using-f/)
- [Dustin Campbell](https://github.com/DustinCampbell) for [CSharpEssentials](https://github.com/DustinCampbell/CSharpEssentials)
- [Alireza Habibi](https://github.com/alrz) for [CSharpUseNamedArgumentsCodeRefactoringProvider](https://github.com/dotnet/roslyn/blob/master/src/Features/CSharp/Portable/UseNamedArguments/CSharpUseNamedArgumentsCodeRefactoringProvider.cs) which provided very useful code examples.

# License

The [UseNamedArgs](https://github.com/mykolav/use-named-args-fs) analyzer and code-fix provider are licensed under the MIT license.  
So they can be used freely in commercial applications.
