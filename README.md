# CSharpTo2600
A compiler and framework for creating Atari 2600 games using C#. It uses the .NET Compiler Platform (Roslyn) to compile C# files, and Mono.Cecil to compile the resulting CIL into 6502 assembly.

### Current Status
This project is being actively rewritten to compile from CIL to 6502 instead of C# to 6502. This is being done to (hopefully) ease development and allow for more features.
The old C# to 6502 compiler can be found on the [master branch](https://github.com/Yttrmin/CSharpTo2600/tree/master) for now.

### Features
An incomplete list of supported features in no particular order. 

* - [ ] Primitive Types
  * - [ ] `bool`
  * - [x] `byte`
  * - [ ] `sbyte`
* - [ ] Array types
* - [ ] Custom Types
  * - [ ] Value Types
  * - [ ] Reference Types
    * - [x] Static reference types
    * - [ ] Instance reference types
* - [ ] Static Members
  * - [x] Fields
  * - [ ] Properties
  * - [ ] Methods
	* - [x] 0-parameter
	* - [x] >0-parameter
	* - [x] `void` return
	* - [ ] Non-`void` return
* - [ ] Inline Assembly
  * - [x] Implied mode inline assembly (`TXS`, `SEI`, etc)
* - [ ] Optimizations
  * - [x] Redundant PHA/PLA removal
* - [ ] C#
  * - [x] `goto`
* - [ ] CIL OpCodes
  * - [ ] Arithmetic
    * - [x] Addition (`add`, no overflow check)
	* - [x] Subtraction (`sub`, no overflow check)
	* - [ ] Division
	* - [ ] Multiplication
  * - [ ] Branching
    * - [x] Branch if true (`brtrue`, `brtrue.s`)
	* - [ ] Branch if false
	* - [x] Unconditional branch (`br`, `br.s`)
  * - [ ] Comparison
    * - [ ] Equal
    * - [x] Greater than (`cgt.un`)
	* - [ ] Less than
  * - [ ] Load
    * - [ ] Argument
	  * - [ ] `ldarg`, `ldarg.s`
	  * - [x] `ldarg.0`, `ldarg.1`, `ldarg.2`, `ldarg.3`
	* - [x] Constant (`ldc.i4`, `ldc.i4.s`, `ldc.i4.0`, `ldc.i4.1`,`ldc.i4.2`,`ldc.i4.3`,`ldc.i4.4`,`ldc.i4.5`,`ldc.i4.6`,`ldc.i4.7`,`ldc.i4.8`)
	* - [ ] Element
	* - [x] Field (static) (`ldsfld`)
	* - [x] Local (`ldloc`, `ldloc.s`, `ldloc.0`, `ldloc.1`, `ldloc.2`, `ldloc.3`)
  * - [ ] Store
    * - [x] Argument (`starg`, `starg.s`)
	* - [ ] Element
	* - [x] Field (static) (`stsfld`)
	* - [x] Local (`stloc`, `stloc.s`, `stloc.0`, `stloc.1`, `stloc.2`, `stloc.3`)

### License
This project is licensed under the [MIT License](./LICENSE.txt).
