# CSharpTo2600
A compiler and framework for creating Atari 2600 games using C#. It uses the .NET Compiler Platform (Roslyn) to parse C# files, translate them into 6502 assembly instructions, and pass them into an assembler.
### Goal
When it comes to making Atari 2600 games there are only a couple languages to choose from. I want to add another option by supporting a subset of the C# language. My hope is to attract more people to try their hand at Atari 2600 development.
### What's Supported?
Not much, the project is very early in development. This will be updated as features are added.

* Static classes
 * Static byte fields
 * Special-purpose static methods (recognized by the compiler, can't be invoked)
 * Static methods with 0 parameters and void return type (can be invoked)
* Multiple user-defined types and multiple source files

### Building
Load the solution into [Visual Studio 2015 RC](https://www.visualstudio.com/en-us/news/vs2015-vs) and it should build and run fine.
### Usage
The command line must be passed at least one file path. The order of the paths does not matter.

`Compiler <file>...`

for example:

`Compiler ..\..\..\FunctionalitySamples\Empty.cs`

The compiler will compile the .cs file into an assembly code file called `out.asm` in the compiler's directory. `out.asm` will then be passed into DASM. In the compiler directory, three files will be created: `output.bin` the ROM file, `output.lst` the list file, and `output.sym` the symbol dump file. The ROM file can then be loaded into an emulator, such as [Stella](http://stella.sourceforge.net/). The list and symbol file are used for debugging.
### Example
Small tests demonstrating specific features can be found in the [FunctionalitySamples](./FunctionalitySamples) directory. Here is [MainLoopChangeBkColor.cs](./FunctionalitySamples/MainLoopChangeBkColor.cs), it changes the background color every other frame:
```c#
using CSharpTo2600.Framework;
using static CSharpTo2600.Framework.TIARegisters;

namespace CSharpTo2600.FunctionalitySamples
{
    /// <summary>
    /// Compiles to an NTSC Atari 2600 ROM that cycles through all possible background colors.
    /// </summary>
    [Atari2600Game]
    static class MainLoopChangeBkColor
    {
        private static byte Color;

        // Tells the compiler that this code is to be executed every frame
        // during the vertical blank period.
        [SpecialMethod(MethodType.MainLoop)]
        static void Tick()
        {
            // COLUBK doesn't actually use the 0 bit (LSB), so the background
            // color changes every other frame.
            Color++;
            // BackgroundColor maps directly to the COLUBK TIA register.
            BackgroundColor = Color;
        }
    }
}
```
### Dependencies (included)
[6502Net](https://github.com/aaronmell/6502Net) ([BSD License](./Dependencies/6502Net/LICENSE)) - Used for testing generated code.

[DASM 2.20.11 (unmodified)](http://dasm-dillon.sourceforge.net/) ([GPLv2 License](./Dependencies/DASM/COPYING)) - Assembles the generated assembly file into an Atari 2600 ROM.

### License
This project is licensed under the [MIT License](./LICENSE.txt).
