# CSharpTo2600
A compiler and framework for creating Atari 2600 games using C#. It uses the .NET Compiler Platform (Roslyn) to compile C# files, and Mono.Cecil to compile the resulting CIL into 6502 assembly.

### Current Status
Initially this project was written to compile C# straight to 6502 assembly. It has since been rewritten to compile C# to CIL, and then compile the CIL to 6502 assembly.
The rewrite quickly surpassed the original implementation in both features and development speed.
Development will continue towards the goal of porting [my 2600 game](https://gist.github.com/Yttrmin/18ecc3d2d68b407b4be1) to C#.

### Current Goal
The current goal is to add all the features needed for me to port my [attempt at a 2600 game](https://gist.github.com/Yttrmin/18ecc3d2d68b407b4be1) to C#.

### Example
There's no collection of samples yet since they may quickly become obsolete. 
Below is an example of how you could, at the time of writing, write a program to cycle through the NTSC background colors. 
See the [Features](#features) section below for a more complete list of features.

```csharp
using static VCSFramework.Registers;
using static VCSFramework.Assembly.AssemblyFactory;
using static VCSFramework.Memory;

static class CycleBackgroundExample
{
    public static void Main()
    {
        SEI();                    // Inline assembly for implied addressing instructions.
        CLD();
        X = 0xFF;                 // Write access to A/X/Y registers.
        TXS();
        ClearMemory();            // Call methods implemented by the framework.
        byte backgroundColor = 0; // Local variable support.
    MainLoop:
        // Vertical blank.
        VSync = 0b10;
        WSync();                  // TIA strobe registers implemented as method calls.
        WSync();
        WSync();
        Tim64T = 43;
        VSync = 0;                // TIA write-only registers implemented as setter-only properties.

        backgroundColor += 2;
        ColuBk = backgroundColor;

        // Wait for VBlank end.
        while (InTim != 0) ;      // RIOT read-only registers implemented as getter-only properties.

        WSync();
        VBlank = 0;

        // Visible image.
        byte lines = 191;
        while (lines != 0)        // While loops and support for some comparisons.
        {
            lines--;
            WSync();
        }
        
        WSync();
        VBlank = 0b10;

        // Overscan.
        lines = 30;
        while (lines != 0)
        {
            lines--;
            WSync();
        }
        
        goto MainLoop;            // goto support!
    }
}
```

### Features
An incomplete list of supported features in no particular order. 

* :o: Primitive Types
  * :x: `bool`
  * :heavy_check_mark: `byte`
  * :x: `sbyte`
* :x: Array types
* :o: Custom Types
  * :x: Value Types
  * :o: Reference Types
    * :heavy_check_mark: Static reference types
    * :x: Instance reference types
* :o: Static Members
  * :heavy_check_mark: Fields
  * :x: Properties
  * :o: Methods
	* :heavy_check_mark: 0-parameter
	* :heavy_check_mark: >0-parameter
	* :heavy_check_mark: `void` return
	* :x: Non-`void` return
* :o: Inline Assembly
  * :heavy_check_mark: Implied address mode inline assembly (`TXS`, `SEI`, etc)
  * :heavy_check_mark: Write-only `A`/`X`/`Y` registers
* :o: Optimizations
  * :heavy_check_mark: Redundant `PHA`/`PLA` removal
  * :x: Reuse memory addresses
* :o: C#
  * :heavy_check_mark: `goto`
* :o: CIL OpCodes
  * :o: Arithmetic
    * :heavy_check_mark: Addition (`add`, no overflow check)
	* :heavy_check_mark: Subtraction (`sub`, no overflow check)
	* :x: Division
	* :x: Multiplication
  * :o: Branching
    * :heavy_check_mark: Branch if true (`brtrue`, `brtrue.s`)
	* :x: Branch if false
	* :heavy_check_mark: Unconditional branch (`br`, `br.s`)
  * :o: Comparison
    * :x: Equal
    * :heavy_check_mark: Greater than (`cgt.un`)
	* :x: Less than
  * :o: Load
    * :heavy_check_mark: Argument (`ldarg`, `ldarg.s`, `ldarg.0`, `ldarg.1`, `ldarg.2`, `ldarg.3`)
	* :heavy_check_mark: Constant (`ldc.i4`, `ldc.i4.s`, `ldc.i4.0`, `ldc.i4.1`,`ldc.i4.2`,`ldc.i4.3`,`ldc.i4.4`,`ldc.i4.5`,`ldc.i4.6`,`ldc.i4.7`,`ldc.i4.8`)
	* :x: Element
	* :heavy_check_mark: Field (static) (`ldsfld`)
	  * :heavy_check_mark: Address (`ldsflda`)
	* :heavy_check_mark: Local (`ldloc`, `ldloc.s`, `ldloc.0`, `ldloc.1`, `ldloc.2`, `ldloc.3`)
  * :o: Store
    * :heavy_check_mark: Argument (`starg`, `starg.s`)
	* :x: Element
	* :heavy_check_mark: Field (static) (`stsfld`)
	* :heavy_check_mark: Field (instance) (`stfld`)
	* :heavy_check_mark: Local (`stloc`, `stloc.s`, `stloc.0`, `stloc.1`, `stloc.2`, `stloc.3`)

### Building
Load the solution into [Visual Studio Community 2017](https://www.visualstudio.com/) and it should build and run fine.

### Usage
The public interface is very rudimentary. You can either invoke it programmatically through `VCSCompiler.Compiler.CompileFromFiles()`, or through the command line program like so:

`dotnet VCSCompilerCLI.dll path_to_source_file path_to_vcsframework_dll path_to_dasm_executable`

### License
This project is licensed under the [MIT License](./LICENSE.txt).
