# CSharpTo2600

###### Iteration 3
---
A compiler and framework for creating Atari 2600 games using C#. It uses the .NET Compiler Platform (Roslyn) to compile C# files, and Mono.Cecil to compile the resulting CIL into 6502 assembler macros.

### Current Status
The first iteration of this project compiled C# directly to 6502 assembly.\
The second iteration of this project compiled CIL directly to 6502 assembly.\
The third (and current) iteration of this project instead compiles CIL to custom macros for the 6502 assembler. This sounds like a small change, but it offers a higher level of abstraction to compile to, and makes it easier to optimize the results. We're also now using [6502.Net](https://github.com/informedcitizenry/6502.Net) as the assembler instead of DASM, so every part of the compiler is now running on .NET.

### Current Goal
The current goal is to add all the features needed for me to port my [attempt at a 2600 game](https://gist.github.com/Yttrmin/18ecc3d2d68b407b4be1) to C#. Progress will likely be slow since I have other personal projects I'm also working on.

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

>:warning: This list is out of date due to starting work on the third iteration of the compiler. The goal is achieve as much feature parity as we can with the second iteration, so the list remains here. It will be updated prior to being merged into `master`.

* :o: Primitive Types
  * :x: `bool`
  * :heavy_check_mark: `byte`
  * :x: `sbyte`
* :x: Array types
* :x: Pointer Types
* :o: Custom Types
  * :heavy_check_mark: Value Types
    * :heavy_check_mark: Single-byte types
	* :heavy_check_mark: Multi-byte types
	* :heavy_check_mark: Composite types (struct-in-struct)
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
Load the solution into [Visual Studio Community 2019](https://www.visualstudio.com/) and it should build and run fine.

>:warning: This project is using .NET 5. At the time of writing, .NET 5 is in preview and scheduled to be released in November 2020. You will need to download the preview SDK to build prior to its release.

### Usage

TODO

### License
This project is licensed under the [MIT License](./LICENSE.txt).
