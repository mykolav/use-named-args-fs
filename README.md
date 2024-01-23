# Suggest method calls that can benefit from using named arguments

[![Build status](https://ci.appveyor.com/api/projects/status/kr7293hw0oswn9fn?svg=true)](https://ci.appveyor.com/project/mykolav/use-named-args-fs)

This project contains a Roslyn code analyzer and an accompanying code-fix provider that suggest using named arguments when calling a method having successive parameters of the same type.

![The UseNamedArgs analyzer in action](./use-named-args-demo.gif)

## How to use it?

Just install the [nuget package](https://www.nuget.org/packages/UseNamedArgs/). The analyzer is going to look for method invocations that can benefit from named arguments across the project.

```csharp
public static void IntroduceCharacter(string name, string powerLevel) {}

// Elsewhere in your code:
// if `IntroduceCharacter` method is called with positional arguments,
// the analyzer emits a warning, as the the method has two parameters 
// of the same type following one another.
IntroduceCharacter(name: "Goku", powerLevel: "Over 9000!");
```

### Supported method kinds

The analyzer supports suggesting named arguments for the following method kinds  
- Regular instance and static methods
- Extension methods
- Regular constructors
- Attribute constructors
- Primary constructors 

## Download and install

Install the [UseNamedArgs](https://www.nuget.org/packages/UseNamedArgs) nuget package.
For example, run the following command in the [NuGet Package Manager Console](https://docs.microsoft.com/en-us/nuget/tools/package-manager-console).

```powershell
Install-Package UseNamedArgs
```

This will download all the binaries, and add necessary analyzer references to your project.

## Configuration

Starting in Visual Studio 2019 version 16.3, you can [configure the severity of analyzer rules, or diagnostics](https://learn.microsoft.com/en-us/visualstudio/code-quality/use-roslyn-analyzers?view=vs-2022#configure-severity-levels), in an EditorConfig file, from the light bulb menu, and the error list.

You can add the following to the `[*.cs]` section of your .editorconfig.

```ini
[*.cs]
dotnet_diagnostic.UseNamedArgs.severity = suggestion
```

The possible severity values are:
- `error`
- `warning`
- `suggestion`
- `silent`
- `none`
- `default` (in case of this analyzer, it's equal to `warning`)

Please take a look at [the documentation](https://learn.microsoft.com/en-us/visualstudio/code-quality/use-roslyn-analyzers?view=vs-2022#configure-severity-levels) for a detailed description.


# Thank you!

- [John Koerner](https://github.com/johnkoerner) for [Creating a Code Analyzer using F#](https://johnkoerner.com/code-analysis/creating-a-code-analyzer-using-f/)
- [Dustin Campbell](https://github.com/DustinCampbell) for [CSharpEssentials](https://github.com/DustinCampbell/CSharpEssentials)
- [Alireza Habibi](https://github.com/alrz) for [CSharpUseNamedArgumentsCodeRefactoringProvider](https://github.com/dotnet/roslyn/blob/master/src/Features/CSharp/Portable/UseNamedArguments/CSharpUseNamedArgumentsCodeRefactoringProvider.cs) which provided very useful code examples.

# License

The analyzer and code-fix provider are licensed under the MIT license.
