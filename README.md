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
Below is an example of how you could, at the time of writing, write a [program to cycle through the NTSC background colors](./Samples/NtscBackgroundColorsSample.cs). 
See the [Features](#features) section below for a more complete list of features.

```csharp
using static VCSFramework.Registers;

namespace Samples
{
    public static class NtscBackgroundColorsSample
    {
        private static byte BackgroundColor; // Support for static fields.

        public static void Main()
        {
            // Processor and memory initialization code is automatically injected by the compiler into
            // the program's entry point, so there's no need to manually do it.
        MainLoop:
            // Perform vertical sync.
            // This is the same logic that would be used in 6502 assembly as well.
            VSync = 0b10; // TIA write-only registers implemented as setter-only properties.
            WSync(); // TIA strobe registers implemented as methods.
            WSync();
            WSync();
            Tim64T = 43;
            VSync = 0;

            // Actual logic to increment and set the background color every frame.
            // The least significant bit is unused, so incrementing by 1 instead of 2 slows the flashing down.
            BackgroundColor++;
            ColuBk = BackgroundColor;

            // Kill time until the vertical blank period is over.
            while (InTim != 0) ; // PIA read-only registers implemented as getter-only properties.

            WSync();
            VBlank = 0;

            // Visible image
            byte lines = 191; // Local variable support.
            while (lines != 0) // Support for while loops and some comparisons.
            {
                lines--;
                WSync();
            }
            
            WSync();
            VBlank = 0b10;

            // Overscan
            lines = 30;
            while (lines != 0)
            {
                lines--;
                WSync();
            }

            goto MainLoop; // goto support!
        }
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
* :x: Pointer Types
* :o: Custom Types
  * :o: Value Types
    * :heavy_check_mark: Single-byte types
	* :o: Multi-byte types
	* :x: Composite types (struct-in-struct) `(can't access member structs)`
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
  * :x: Implied address mode inline assembly (`TXS`, `SEI`, etc)
  * :heavy_check_mark: Write-only `A`/`X`/`Y` registers
* :o: Optimizations
  * :heavy_check_mark: Redundant `PHA`/`PLA` removal
  * :x: Reuse memory addresses
* :o: C#
  * :heavy_check_mark: `goto`
  * :heavy_check_mark: `unsafe`
  * :heavy_check_mark: `default`
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
	* :heavy_check_mark: Field (instance) (`ldfld`)
	  * :heavy_check_mark: Address (`ldflda`)
	* :x: Indirect (`ldind.i`, `ldind.i1`)
	* :heavy_check_mark: Local (`ldloc`, `ldloc.s`, `ldloc.0`, `ldloc.1`, `ldloc.2`, `ldloc.3`)
  * :o: Store
    * :heavy_check_mark: Argument (`starg`, `starg.s`)
	* :x: Element
	* :heavy_check_mark: Field (static) (`stsfld`)
	* :heavy_check_mark: Field (instance) (`stfld`)
	* :x: Indirect (`stind.i`, `stind.i1`)
	* :heavy_check_mark: Local (`stloc`, `stloc.s`, `stloc.0`, `stloc.1`, `stloc.2`, `stloc.3`)
  * :o: Miscellaneous Object Model
    * :heavy_check_mark: Initialize value type (`initobj`)

### Building
Load the solution into [Visual Studio Community 2017](https://www.visualstudio.com/) and it should build and run fine.

### Usage
The public interface is very rudimentary. You can either invoke it programmatically through `VCSCompiler.Compiler.CompileFromFiles()`, or through the command line program like so:

`dotnet VCSCompilerCLI.dll path_to_source_file path_to_vcsframework_dll path_to_dasm_executable`

### License
This project is licensed under the [MIT License](./LICENSE.txt).
