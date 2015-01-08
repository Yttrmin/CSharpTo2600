# CSharpTo2600
A compiler and framework for creating Atari 2600 games using C#. It uses the .NET Compiler Platform (Roslyn) to parse C# files and translate them into 6502 assembly instructions. The assembly code would then be passed into [DASM](http://dasm-dillon.sourceforge.net/) to produce the binary.
### Goal
When it comes to making Atari 2600 games there are only a couple languages to choose from. I want to add another option by supporting a subset of the C# language. My hope is to attract more people to try their hand at Atari 2600 development.
### What's Supported?
Not much, the project is very early in development. This will be updated as features are added.
### Building
Load the solution into [Visual Studio 2015 Preview](http://www.visualstudio.com/en-us/downloads/visual-studio-2015-downloads-vs) and it should build and run fine, there are no other dependencies.
### License
[MIT](https://github.com/Yttrmin/CSharpTo2600/blob/master/LICENSE.txt)
