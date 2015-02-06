# CSharpTo2600
A compiler and framework for creating Atari 2600 games using C#. It uses the .NET Compiler Platform (Roslyn) to parse C# files and translate them into 6502 assembly instructions. The assembly code would then be passed into [DASM](http://dasm-dillon.sourceforge.net/) to produce the binary.
### Goal
When it comes to making Atari 2600 games there are only a couple languages to choose from. I want to add another option by supporting a subset of the C# language. My hope is to attract more people to try their hand at Atari 2600 development.
### What's Supported?
Not much, the project is very early in development. This will be updated as features are added.
### Building
Load the solution into [Visual Studio 2015 CTP 5](https://support2.microsoft.com/kb/2967191) and it should build and run fine, there are no other dependencies.
### Usage
`Compiler DASMDirectory SourceFilePath`

for example:

`Compiler C:\Documents\DASM ..\..\..\FunctionalitySamples\Empty.cs`

The compiler will compile the .cs file into an assembly code file called `out.asm` in the compiler's directory. `out.asm` will then be passed into `dasm.exe` at the directory provided in the first argument. In the DASM directory, three files will be created: `output.bin` the ROM file, `output.lst` the list file, and `output.sym` the symbol dump file. The ROM file can then be loaded into an emulator, such as [Stella](http://stella.sourceforge.net/). The list and symbol file are used for debugging.
### Example
Small tests demonstrating specific features can be found in the [FunctionalityTests](https://github.com/Yttrmin/CSharpTo2600/tree/master/FunctionalitySamples) directory. Here is [MainLoopChangeBkColor.cs](https://github.com/Yttrmin/CSharpTo2600/blob/master/FunctionalitySamples/MainLoopChangeBkColor.cs), it changes the background color every other frame:
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
### License
[MIT](https://github.com/Yttrmin/CSharpTo2600/blob/master/LICENSE.txt)
