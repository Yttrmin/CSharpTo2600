# CSharpTo2600
A compiler and framework for creating Atari 2600 games using C#. It uses the .NET Compiler Platform (Roslyn) to compile C# files, and Mono.Cecil to compile the resulting CIL into 6502 assembly.

### Current Status
Initially this project was written to compile C# straight to 6502 assembly. It has since been rewritten to compile C# to CIL, and then compile the CIL to 6502 assembly.
The rewrite quickly surpassed the original implementation in both features and development speed.
Development will continue towards the goal of porting [my 2600 game](https://gist.github.com/Yttrmin/18ecc3d2d68b407b4be1) to C#.

### Current Goal
The current goal is to add all the features needed for me to port my [attempt at a 2600 game](https://gist.github.com/Yttrmin/18ecc3d2d68b407b4be1) to C#.

### Features
An incomplete list of supported features in no particular order. 

* :x: Primitive Types
  * :x: `bool`
  * :heavy_check_mark: `byte`
  * :x: `sbyte`
* :x: Array types
* :x: Custom Types
  * :x: Value Types
  * :x: Reference Types
    * :heavy_check_mark: Static reference types
    * :x: Instance reference types
* :x: Static Members
  * :heavy_check_mark: Fields
  * :x: Properties
  * :x: Methods
	* :heavy_check_mark: 0-parameter
	* :heavy_check_mark: >0-parameter
	* :heavy_check_mark: `void` return
	* :x: Non-`void` return
* :x: Inline Assembly
  * :heavy_check_mark: Implied mode inline assembly (`TXS`, `SEI`, etc)
* :x: Optimizations
  * :heavy_check_mark: Redundant `PHA`/`PLA` removal
* :x: C#
  * :heavy_check_mark: `goto`
* :x: CIL OpCodes
  * :x: Arithmetic
    * :heavy_check_mark: Addition (`add`, no overflow check)
	* :heavy_check_mark: Subtraction (`sub`, no overflow check)
	* :x: Division
	* :x: Multiplication
  * :x: Branching
    * :heavy_check_mark: Branch if true (`brtrue`, `brtrue.s`)
	* :x: Branch if false
	* :heavy_check_mark: Unconditional branch (`br`, `br.s`)
  * :x: Comparison
    * :x: Equal
    * :heavy_check_mark: Greater than (`cgt.un`)
	* :x: Less than
  * :x: Load
    * :heavy_check_mark: Argument (`ldarg`, `ldarg.s`, `ldarg.0`, `ldarg.1`, `ldarg.2`, `ldarg.3`)
	* :heavy_check_mark: Constant (`ldc.i4`, `ldc.i4.s`, `ldc.i4.0`, `ldc.i4.1`,`ldc.i4.2`,`ldc.i4.3`,`ldc.i4.4`,`ldc.i4.5`,`ldc.i4.6`,`ldc.i4.7`,`ldc.i4.8`)
	* :x: Element
	* :heavy_check_mark: Field (static) (`ldsfld`)
	* :heavy_check_mark: Local (`ldloc`, `ldloc.s`, `ldloc.0`, `ldloc.1`, `ldloc.2`, `ldloc.3`)
  * :x: Store
    * :heavy_check_mark: Argument (`starg`, `starg.s`)
	* :x: Element
	* :heavy_check_mark: Field (static) (`stsfld`)
	* :heavy_check_mark: Local (`stloc`, `stloc.s`, `stloc.0`, `stloc.1`, `stloc.2`, `stloc.3`)

### License
This project is licensed under the [MIT License](./LICENSE.txt).
