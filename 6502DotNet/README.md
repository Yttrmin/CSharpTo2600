﻿# 6502.Net, A Simple .Net-Based 65xx and Z80 Cross-Assembler

Version 2.1.0

## Table of Contents

- [Introduction](#introduction)
- [Overview](#overview)
- [General Features](#general-features)
  - [Assembly Statements](#assembly-statements)
    - [Comments](#comments)
    - [Numbers and Strings](#numbers-and-strings)
- [Symbols](#symbols)
  - [Labels](#labels)
  - [Scoped Symbols](#scoped-symbols)
  - [Global Labels](#global-labels)
  - [Back and Forward References](#back-and-forward-references)
  - [Variables](#variables)
  - [Lists](#lists)
  - [Value types](#value-types)
- [Non-code (data) assembly](#non-code-%28data%29-assembly)
  - [Binary data](#binary-data)
  - [Uninitialized data](#uninitialized-data)
  - [Text data](#text-data)
  - [String Format function](#string-format-function)
  - [Encodings](#encodings)
  - [Escape sequences](#escape-sequences)
  - [File inclusions](#file-inclusions)
- [Mathematical and Conditional Operations](#mathematical-and-conditional-operations)
  - [Binary Operations](#binary-operations)
  - [Unary Operations](#unary-operations)
  - [Built-in functions](#built-in-functions)
  - [Math and Logical Constants](#math-and-logical-constants)
- [Addressing Model](#addressing-model)
- [Macros and Custom Functions](#macros-and-custom-functions)
  - [Macros](#macros)
  - [Functions](#functions)
- [Flow Control](#flow-control)
  - [Assembly Termination](#assembly-termination)
  - [Goto Statement](#goto-statement)
  - [Conditional Assembly](#conditional-assembly)
    - [If Statement Blocks](#if-statement-blocks)
    - [Switch Blocks](#switch-blocks)
  - [Loop Assembly](#loop-assembly)
    - [Simple Repetitions](#simple-repetitions)
    - [For/Next](#for-next)
    - [While](#while)
- [CPU-Specific Assembly](#cpu-specific-assembly)
  - [65xx Family generally](#65xx-family-generally)
  - [Addressing Mode Sizes](#addressing-mode-sizes)
  - [Pseudo Branch Instructions](#pseudo-branch-instructions)
  - [65816 Specific Directives](#65816-specific-directives)
  - [Z80 Implied Accumulator Instructions](#z80-implied-accumulator-instructions)
- [Reference](#reference)
  - [Pseudo-Ops](#pseudo-ops)
  - [Other Assembler Directives](#other-assembler-directives)
  - [Built-In functions](#built-in-functions)
  - [Command-line options](#command-line-options)
  - [6502 Illegal Mnemonics](#6502-illegal-mnemonics)  
  - [Reserved Words](#reserved-words)
- [Licensing and Legal](#licensing-and-legal)

## Introduction

The 6502.Net Macro Assembler is a simple cross-assembler targeting several CPUs from the 8-bit era of computing including the MOS 6502 and its variants, as well as the Zilog Z80. With the aim of cross-platform compatibility, it is targeted for .Net Core 3.1.

## Overview

The 6502.Net assembler is simple to use. Invoke it from a command line with the assembly source and (optionally) the output filename in the parameters. For instance, a `/6502.Net.exe myprg.asm` command will output assembly listing in `myprgm.asm` to binary output. To specify output file name use the `-o <file>` or `--output=<file>` option, otherwise the default output filename will be `a.out`.
You can specify as many source files as assembly input as needed. For instance, `6502.Net.exe mylib.asm myprg.asm` will assemble both the `mylib.asm` and `myprgm.asm` files sequentially to output. Be aware that if both files define the same symbol an assembler error will result.

### General Features

#### Assembly Statements

As with other assemblers, source statements are typically in the format:

```asm
label   mnemonic    operands
```

Source text is assumed to be ASCII- or Unicode-compliant. Normally, assembly statements are terminated by a new line:

```asm
        ldx #$00
```

However, a statement can span multiple lines if they comprise a single expression:

```asm
        .byte 1,2,3,4,5,
              6,7,8,9,10,
              11

        .word 39 +
              34
```

Multiple statements per line are separated by a colon (`:`) character:

```asm
        lda MESSAGE,x:jsr CHROUT:inx
```

Source is processed case insensitive by default, but this can be controlled using the `-C` option as described in the [command-line section](#Command-line%20options) below.

#### Comments

C/C++ style comments are recognized. Use `/* */` for block comments and `//` for line comments. For compatibility with traditional 6502 assemblers, semi-colons can be used to denote the beginning of line comments also. If a colon follows a semi-colon, it is considered a new line.

#### Numbers and Strings

Numeric constants can be expressed as decimal, real, hexadecimal, octal and binary, using the following notation schemes:

```asm
             65490 = 65490
           1.03E+5 = 0.0000103
           0177722 = 65490
          0o177722 = 65490
             $ffd2 = 65490
            0xffd2 = 65490
 %1111111111010010 = 65490
0b1111111111010010 = 65490
```

Negative numbers are assembled according to two's complement rules, with the highest bits set. Binary strings can alternatively be expressed as `.` for `0` and `#` for `1`, which is handy for laying out pixel data:

```asm
number1     .byte %...###..
            .byte %..####..
            .byte %.#####..
            .byte %...###..
            .byte %...###..
            .byte %...###..
            .byte %...###..
            .byte %.#######
```

Character literals are enclosed in single quotes, while string literals are enclosed in double quotes:

```asm
message     .string "HELLO", ' ', "WORLD"
```

### Symbols

#### Labels

Nearly every line of code can begin with a label, a named part of the program. Labels can also be assigned values to be used as constants.

```asm
            black   =   0

            lda #black      // load black into acc.
            beq setborder   // now set the border color
            ...
setborder:  sta $d020       // poke border color with acc.
```

In general, once labels are defined they cannot be re-defined or have their values re-assigned in other parts of code. But there are some exceptions. One is to make a label a "cheap local" by appending an underscore at the beginning:

```asm
routine1    lda message,x
            beq _done
            jsr $ffd2
            inx
            jmp routine1
_done       rts

routine2    ldy flag
            beq _done
            jmp dosomething
_done       rts
```

In the routine above, there are two labels called `_done` but the assembler will differentiate between them, since the second `_done` follows a different non-local label from the first.

#### Scoped Symbols

Additionally, all symbols can be scoped by wrapping code inside `.block` and `.endblock` directives:

```asm
            ...
endloop     lda #$ff
            rts

myblock     .block
            jsr endloop     // accumulator will be 0
            ...             // since endloop is local to myblock
endloop     lda #0
            rts
            .endblock
```

Labels inside a named scope block can be referenced with dot notation from other places outside of the scope:

```asm
kernal      .block

chrin       = $ffcf
chrout      = $ffd2

            .endblock

            jsr kernal.chrout   // call the subroutine whose label
                                // is defined in the kernal block
```

All symbols inside blocks not preceded by a label are only visible within that block:

```asm
            .block
            jsr increment
            ...
increment   inc mem
            beq done
            inc mem+1
done        rts
            .endblock

            jsr increment // will produce an assembler error
```

#### Global Labels

A label can be marked as global in scope regardless of which scope it is declared in:

```asm
someblock   .block

            // stuff
MYCONSTANT  .global 42
            .endblock

            .word MYCONSTANT // MYCONSTANT is not in someblock's scope.
```

#### Back and Forward References

Back and forward references allow one to do away with the need to think of unique label names altogether. Forward references are declared with a `+`, while back reference labels are declared using a `-`. They are forward or backward to the current assembly line and are referenced in the operand with one or more `+` or `-` symbols:

```asm
printmessage
            ldx #0
-           lda msg_ptr,x
            beq +               // jump to first forward reference from here
            jsr chrout
            inx
            bne -               // jump to first backward reference from here
+           rts
-           nop
            jmp --              // jump to the second backward reference from here
```

As you can see that, while convenient, back and forward references hinder readability if used too liberally. They are best for small branch jumps, though can be used in expressions:

```asm
-           .byte $01, $02, $03
            lda -,x
```

#### Variables

Variables are named symbols like labels, but whose values can be changed as often as required. A variable is declared with the `.let` directive, followed by an assignment expression. Variables and labels cannot share the same symbol name within the same scope.

```asm
            .let myvar = 34
            lda #myvar
            .let myvar = myvar + 1
            ldx #myvar
```

Unlike labels, variables cannot be referenced in other expressions before they are declared, since variables are not preserved between passes.

```asm
            .let y = x  
            .let x = 3
```

In the above example, the assembler would error assuming `x` has never been declared before.

#### Value types

Labels and variables can be assigned both numeric values as well as string literals:

```asm
WARNING     =   "DO NOT POKE THE LION!"

            .string WARNING
```

#### Lists

Labels and variables can also be declared as lists:

```asm
HIGHSCORES  = [ 10000, 9000, 8000, 7000, 5000 ]

initgame:
            ld hl,curr_hs
            ld bc,HIGHSCORES[1]
            ld (hl),c
            inc hl
            ld (hl),b
```

List variable elements can be updated accordingly using subscript notation:

```asm
            .let SCORES[0] = $0000
```

### Non-code (data) assembly

#### Binary data

In addition to 65xx and Z80 code, data can also be assembled. Expressions evaluate internally as double-precision floating point numbers before they are cast to an integral value of desired size. If the value given in the expression exceeds the data size, this will cause an illegal quantity error. The following pseudo-ops are available:

| Directive | Size                      |
| --------- | ------------------------- |
| `.byte`   | One byte unsigned         |
| `.sbyte`  | One byte signed           |
| `.addr`   | Two byte address          |
| `.sint`   | Two bytes signed          |
| `.word`   | Two bytes unsigned        |
| `.rta`    | Two byte return address   |
| `.lint`   | Three bytes signed        |
| `.long`   | Three bytes unsigned      |
| `.dint`   | Four bytes signed         |
| `.dword`  | Four bytes unsigned       |
| `.align`  | Zero or more bytes        |
| `.fill`   | One or more bytes         |

Data is assembled in little-endian order (the least significant byte first), since all targeted CPUs are little-endian architecture. Each value can be a constant or expression:

```asm
sprite      .byte %......##,%########,%##......
jump        .word sub1, sub2, sub3, sub4
```

The `.addr` and `.rta` directives are the same as `.word`, but `.rta` is the expression minus one. This is useful for doing an "rts jump" in 65xx-based code:

```asm
            lda #>jump  // high byte ($07)
            pha
            lda #<jump  // low byte ($ff)
            pha
            rts         // do the jump
jump        .rta $0800  // = $07ff
```

For `.fill` and `.align`, the assembler accepts either one or two arguments. The first is the quantity, while the second is the value. If the second is not given then it is assumed to be uninitialized data (see below). For `.fill`, quantity is number of bytes, for `.align` it is the number of bytes by which the program counter can be divided with no remainder:

```asm
unused      .fill 256,0 // Assemble 256 bytes with the value 0

atpage      .align 256  // The program counter is guaranteed to be at a page boundary
```

#### Uninitialized data

Sometimes it is desirable to direct the assembler to make a label reference an address, but without assembling bytes at that address. For instance, variables used by the program. Use the `?` instead of an expression:

```asm
highscore   .dword ?    // set the symbol highscore to the program counter,
                        // but do not output any bytes
```

Note that if uninitialized data is defined, but thereafter initialized data is defined, the output will fill bytes to the program counter from the occurrence of the uninitialized symbol:

```asm
highscore   .dword ?    // uninitialized highscore variables
            lda #0      // The output is now 6 bytes in size
```

#### Text data

Strings can be assembled in a few different ways, according to the needs of the programmer.

| Directive     | Meaning                                                                       |
| ------------- | ----------------------------------------------------------------------------- |
| `.string`     | A standard string literal                                                     |
| `.cstring`    | A C-style null-terminated string                                              |
| `.lstring`    | A string with output bytes left-shifted and the low bit set on its final byte |
| `.nstring`    | A string with the negative (high) bit set on its final byte                   |
| `.pstring`    | A Pascal-style string, its size in the first byte                             |

Since `.pstring` strings use a single byte to denote size, no string can be greater than 255 bytes. Since `.nstring` and `.lstring` make use of the high and low bits, bytes must not be greater in value than 127, nor less than 0.

##### String Format function

The string function `format()` allows you to convert non-string data to string data using a .Net format string:

```asm
stdout      = $ffd2
stdstring   .string format("The stdout routine is at ${0:X4}", stdout)
            /* will assemble to:
              "The stdout routine is at $FFD2 */

```

Many traditional assemblers allow programmers to use their character and value string pseudo-ops interchangeably, e.g. `.byte "HELLO"` and `.asc "HELLO"`. Note that 6502.Net treats character strings differently for `.byte` and similar directives. For these pseudo-ops string characters are packed into single values, and the pseudo-op length is still enforced:

```asm
            .byte "H"       // okay
            .dword "HELLO"  // also okay, .dword can accomodate 4 ASCII bytes
            .byte 'H','I'   // still ok--two different literals
            .byte "HELLO"   // will error out
```

Generally, therefore, it is best to use the string commands for processing character string literals.

##### Encodings

Assembly source text is assumed to be ASCII/UTF-8, and by default strings and character literals are encoded as such. You can change how text is output with the `.encoding` and `.map` directives. Use `.encoding` to select an encoding, either pre-defined or custom. There are four pre-defined encodings:

| Encoding      | Output bytes       |
| ------------- |--------------------|
| `none`        | UTF-8              |
| `atascreen`   | Atari screen codes |
| `cbmscreen`   | CBM screen codes   |
| `petscii`     | CBM PETSCII        |

The default encoding is `none`. It is worth noting that, for the Commodore-specific encodings, several of the glyphs in those platforms can be represented in Unicode counterparts. For instance, for Petscii encoding, ♥ outputs to `D3` as is expected.

Text encodings are modified using the `.map` and `.unmap` directives. After selecting an encoding, you can map a Unicode character to a custom output code as follows:

```asm
            /* select encoding */
            .encoding "myencoding"

            // map A to output 0
            .map "A", 0

            .string "ABC"
            // > 00 42 43

            // char literals are also affected
            lda #'A'    // a9 00

            ;; emoji will assemble too!
            .string "😁"    // f0 9f 98 81
```

The output for each text element can be one to four bytes. Entire character sets can also be mapped, with the re-mapped code treated as the first in the output range. The start and endpoints in the character set to be re-mapped can either be expressed as a two-character string literal or as expressions.

```asm
            // output lower-case chars as uppercase
            .map "az", "A"

            // output digits as actual integral values
            .map "0","9", 0

            // alternatively:
            .map 48, 48+9, 0

            // escape sequences are acceptable too:
            .map "\u21d4", $9f
```

**Caution:** Operand expressions containing a character literal mapped to a custom code will evaluate the character literal accordingly. This may produce unexpected results:

```asm
            .map 'A', 'a'

            .map 'a', 'A' // this is now the same as .map 'a', 'a'
```

Instead express character literals as one-character strings in double-quotes, which will resolve to UTF-8 values.

A further note about encodings and source files. As mentioned, source files are read and processed as UTF-8. While it is true that the .Net StreamReader class can auto-detect other encodings, this cannot be guaranteed (for instance if the BOM is lacking in a UTF-16-encoded source). If the source does not assemble as expected, consider converting it to UTF-8 or at least ASCII. [This article](https://www.joelonsoftware.com/2003/10/08/the-absolute-minimum-every-software-developer-absolutely-positively-must-know-about-unicode-and-character-sets-no-excuses/) offers a good overview on the issues concerning text encodings.

##### Escape sequences

Most [.Net escape sequences](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/strings/#string-escape-sequences) will also output, including Unicode.

```
            .string "He said, \"How are you?\""
            .byte '\t', '\''
```

Here are a few recognized escape sequences:

| Escape Sequence | ASCII/Unicode Representation |
| --------------- | ---------------------------- |
| `\n`            | Newline                      |
| `\r`            | Carriage return              |
| `\t`            | Tab                          |
| `\"`            | Double quotation mark        |
| `\unnnn`        | Unicode U+nnnn               |

#### File inclusions

Other files can be included in final assembly, either as 6502.Net-compatible source or as raw binary. Source files are included using the `.include` and `.binclude` directives. This is useful for libraries or other organized source you would not want to include in your main source file. The operand is the file name (and path) enclosed in quotes. `.include` simply inserts the source at the directive.

```asm
            // inside "../lib/library.s"

            .macro  inc16 mem
            inc \mem
            bne +
            inc \mem+1
+           .endmacro
            ...
```

This file called `"library.s"` inside the path `../lib` contains a macro definition called `inc16` (See the [section below](#macros) for more information about macros).

```asm
            .include "../lib/library.s"

            .inc16 $033c    // 16-bit increment value at $033c and $033d
```

If the included library file also contained its own symbols, caution would be required to ensure no symbol clashes. An alternative to `.include` is `.binclude`, which resolves this problem by enclosing the included source in its own scoped block.

```asm
lib         .binclude "../lib/library.s" /* all symbols in "library.s"
                                            are in the "lib" scope */

            jsr lib.memcopy
```

If no label is prefixed to the `.binclude` directive then the block is anonymous and labels are not visible to your code.

External files containing raw binary that will be needed to be included in your final output, such as `.sid` files or sprite data, can be assembled using the `.binary` directive.

```asm
            * = $1000

            .binary "../rsrc/sprites.raw"

            ...

            lda #64     // pointer to first sprite in "./rsrc/sprites.raw"
            sta 2040    // set first sprite to that sprite shape
```

You can also control how the binary will be included by specifying the offset (number of bytes from the start) and size to include.

```asm
            * = $1000

            .binary "../rsrc/music.sid", $7e    // skip first 126 bytes
                                                // (SID header)

            .binary "../lib/compiledlib.bin", 2, 256    // skip load header
                                                        // and take 256 bytes
```

### Mathematical and Conditional Operations

All non-string operands are treated as math or conditional expressions.The order of operation generally follows that the .Net languages for matching operators.

#### Binary Operations

| Operator      | Meaning                        |
| :-----------: | ------------------------------ |
| +             | Add                            |
| -             | Subtract                       |
| *             | Multiply                       |
| /             | Divide                         |
| %             | Modulo (remainder)             |
| ^^            | Raise to the power of          |
| &             | Bitwise AND                    |
| &#124;        | Bitwise OR                     |
| ^             | Bitwise XOR                    |
| <<            | Bitwise left shift             |
| >>            | Bitwise right shift            |
| <             | Less than                      |
| <=            | Less than or equal to          |
| ==            | Equal to                       |
| !=            | Not equal to                   |
| >=            | Greater than or equal to       |
| >             | Greater than                   |
| &&            | Logical AND                    |
| &#124;&#124;  | Logical OR                     |

```asm
            .addr   HIGHSCORE + 3 * 2 // the third address from HIGHSCORE
            .byte   * > $f000         // if program counter > $f000, assemble as 1
                                      // else 0

            // bounds check START_ADDR
            .assert START_ADDR >= MIN && START_ADDR <= MAX
```

#### Unary Operations

| Operator      | Meaning                                |
| :-----------: | -------------------------------------- |
| ~             | Bitwise complementary                  |
| <             | Extract significant byte               |
| >             | Extract Most significant (second) byte |
| &             | Extract Word (first two bytes) value   |
| ^             | Extract Bank (third) byte              |
| !             | Logical NOT                            |

```asm
            lda #>routine-1     // routine MSB
            pha
            lda #<routine-1     // routine LSB
            pha
            rts                 // RTS jump to "routine"

routine     lda &long_address   // load the absolute value of long_address
                                // (truncate bank byte) into accummulator
```

#### Built-in functions

Several built-in math functions that can also be called as part of the expressions.

```asm
            lda #sqrt(25)
```

See the section below on functions for a full list of available functions, as well as how to define custom funcctions.

#### Math and Logical Constants

The math constants π and _e_ are defined as `math_pi` and `math_e`, respectively, and can be referenced in expressions as follows:

```asm
            .dword sin(math_pi/3) * 10  // > 08
            .dword pow(math_e,2)        // > 07
```

The logical constants `true` and `false` are available to conditional expressions:

```asm
            .if (i > 3) == false
                .goto end
            .endif
```

Note that no labels or variables can share these names as they are considered reserved.

## Addressing Model

By default, programs start at address 0, but you can change this by setting the program counter before the first assembled byte. 6502.Net uses the `*` symbol for the program counter, the current address at which the instruction is evaluated. The assignment can be either a constant or expression:

```asm
            * = ZP + 1000       // program counter now 1000 bytes offset from
                                // the value of the constant ZP
```

As assembly continues, the program counter advances automatically. You can manually move the program counter forward, but keep in mind doing so will create a gap that will be filled if any bytes are added to the assembly from that point forward. For instance, consider:

```asm
            * = $1000
            ld a,0
            call $1234

            * = $2004
            rst $00
```

This will output 4096 bytes, with 4091 zeros. So this generally is not recommended unless this is the desired result.

To move the program counter forward for the purposes having the symbols use an address space that code will be relocated to later, you can use the `.relocate` directive:

```asm
            * = $0200
            newlocation = $a000

            lda #<torelocate
            sta $02
            lda #>torelocate
            sta $03
            lda #<newlocation
            sta $04
            lda #>newlocation
            sta $05
            ldy #0
            lda ($02),y
            sta ($04),y
            ....
torelocate:
            .relocate newlocation   // no gap created

            jsr relocatedsub    // now in the "newlocation" address space
            ...
relocatedsub    lda #0
                ...
```

To reset the program counter back to its regular position use the `.endrelocate` directive:

```asm
            jsr relocatedsub
            ...
            jmp finish
torelocate:
            relocate newlocation
            ...
            .endrelocate
            ;; done with movable code, do final cleanup
finish      rts
```

For better support for systems that can access memory outside the 64K space, the `.bank` directive can be used to specify the current program bank. See the 65816-specific section [below](#65816-specific-directives) for more information.

## Macros and Custom Functions

### Macros

One of the more powerful features of the 6502.Net cross assembler is the ability to re-use code segments in multiple places in your source. Macros are text substitutions that are defined once, then expanded wherever invoked. Macros can accept parameters, so that the final outputted code can be easily modified for different contexts. For instance, one of the more common operations in 6502 assembly is to do a 16-bit increment. You could use a macro for this purpose like this:

```asm
inc16       .macro  address
            inc \address
            bne +
            inc \address+1
+           .endmacro
```

The macro is called `inc16` and takes a parameter called `address`. The code inside the macro references the parameter with a backslash `\` followed by the parameter name. The parameter is a textual substitution; whatever you pass will be expanded at the reference point. Note the anonymous forward symbol at the branch instruction will be local to the block, as would any symbols inside the macro definition when expanded. To invoke a macro simply reference the name with a `.` in front, like any pseudo-op or directive:

```asm
myvariable  .word ?

            .inc16 myvariable
```

Once assembly on source happens, the line above expands to:

```asm
            inc myvariable
            bne +
            inc myvariable+1
+           ...
```

Parameter insertions also work in string literals, but the parameter formatting is more like a .Net string format with a `@` symbol in front:

```asm
hello       .macro name
            .string "Hello, @{name}."
            .endmacro
```

Parameters can be referenced by number in this way:

```asm
today       .macro
            .string "Today is @{1}"
            .endmacro

        // Expansion of ".today Tuesday""
            .string "Today is Tuesday"
```

If the string insertion contains whitespaces, enclose the expression in double quotes.

### Functions

Custom functions are similar to macros in that they are composed of several lines and accept parameters:

```asm
area            .function r=1
                    .errorif r < 0, "Radius cannot be negative."
                    .return math_pi * pow(r,2)
                .endfunction
```

Since the above function returns a value, it can be used as part of any math expression, just like any other function:

```asm
                .word area(32)+5
```

Functions can even be invoked stand-alone with the `.invoke` directive:

```asm
                .invoke myfunnyfunction()
```

All variables declared inside functions are considered local in scope to that function.

One critical difference between a function and a macro is that the pseudo ops and 65xx/Z80 mnemonics are not available to function definitions, since output of code this way is not allowed.

## Flow Control

In cases where you want to control the flow of assembly, either based on certain conditions (environmental or target architecture) or in certain iterations, 6502.Net provides certain directives to handle this.

### Assembly Termination

Assembly can be stopped on the current pass with the `.end` directive. But be careful not to use this directive inside a block such as `.if`, otherwise the `.endif` closure will never be reached, and the assembler will report an error.

### Goto Statement

The `.goto` directive will set the assembler to the labeled line specified in the operand:

```asm
        .goto code_end
        ... // stuff that won't be assembled
code_end    rts
```

### Conditional Assembly

#### If Statement Blocks

Conditional assembly is available using the `.if` and related directive.  Conditions can be nested. `.ifdef` and `.ifndef` variants are evaluated on first pass only, with their evaluations preserved in subsequent passes.

```asm
            ld a,$42
            .ifdef SPECCY   // is the SPECCY symbol defined?
                call $10a8
            .endif
```

#### Switch Blocks

Switch blocks can also be used for more compact conditional branching:

```asm
            .switch CPU_NAME
                .case "65816"
                    brl long
                    .break
                .case "65CE02"
                    bcs long
                    .break
                .default
                    bcc next
                    jmp long
                    .break
            .endswitch
```

Multiple cases can also fall through to the same branch:

```asm
            .switch *
                .case $100
                .case $200
                .case $300
                    .echo "At page break!"
                    .break
                .default
                    // do something else
                    .break
            .endswitch
```

### Loop Assembly

#### Simple Repetitions

On occasions where certain instructions will be repeatedly assembled, it is convenient to repeat their output in a loop. For instance, if you want to pad a series of `nop` instructions. The `.repeat` directive does just that.

```asm
            // will assemble $ea ten times
            .repeat 10
            nop
            .endrepeat
```

These repetitions can also be nested, as shown below.

```asm
            // print each letter of the alphabet 3 times
            * = $c000

            lda #$41
            .repeat 26
                .repeat 3
                    jsr $ffd2
                .endrepeat
                tax
                inx
                txa
            .endrepeat
            .repeat 3
               jsr $ffd2
            .endrepeat
            rts
```

#### For/Next

Repetitions can also be handled in while and for/next loops, where source can be emitted repeatedly until a condition is met.  iteration variable can optionally be initialized, with the advantage is the variable itself can be referenced inside the loop.

```asm
            lda #0
            .for i = $0400, i < $0800, i = i + 1
                sta i
            .next
```

Initialization nor conditional operands are not required, but the iteration is.

```asm
            .let c = 0;
            .let n = 1;
            .for , n < 10 , n = n + 1
                .if c == 3
                    .let n = n + 5;
                .endif
                .echo format("{0}",n);
            .next
            /*
            outputs:

            6
            11
            */
```

If required, loops can be broken out of using the `.break` directive, or continue to the next iteration using the `.continue` directive:

```asm
            .for i = 0, i < 256, i = i + 1
                .if * >= $1000
                    .break          // make sure assembly does not go past $1000
                .elseif * >= $900 && * <= $980
                    .continue       // skip lower 128 iterations.
                .endif
                lda #'A'
                jsr $ffd2
            .next
```

#### While

Similarly, `.while` loops continue until a condition is met:

```asm
            .while * < $100
                .byte 2
            .endwhile
```

## CPU-Specific Assembly

### 65xx Family generally

By default, 6502.Net "thinks" like a 6502 assembler, compiling only the published 56 mnemonics and 151 instructions of that microprocessor. 6502.Net can also compile illegal instructions as well as those of the successor WDC 65C02, CSG 65CE02, and W65C816S processors. The `.cpu` directive tells the assembler the type of source and instruction set it is to assemble.

```asm
            .cpu "6502i"    // enable illegal instructions

            ldx #0
            slo (zpvar,x)
```

There are eight options for the `.cpu` directive: `6502`, `6502i`, `65C02`, `65CS02`, `R65C02`, `W65C02`, `65CE02` and `65816`. `6502` is default. (NOTE: `Z80` is not an available option, nor will the `.cpu` directive work if the assembler is in Z80 mode.) You can also select the cpu in the command line by passing the `--cpu` option (detailed below). Note that only one CPU target can be selected at a time, though the 65C02 and 65CE02 are supersets of the 6502, and the 65816 and 65CE02 are in turn supersets of both the 65C02 and 6502, so those CPUs will recognize the base 6502 mnemonics.

### Addressing Mode Sizes

Because the 65xx architecture uses differing addressing modes for the same mnemonics, by default 6502.Net selects the appropriate instruction based on the minimum required size to express the operand. For instance `lda 42` can either be interpreted to be zero-page or absolute addressing, but 6502.Net will choose zero-page. Similarly, for the 65C816 `lda $c000` could either be an absolute or long address, but 6502.Net will again choose the shorter (and faster!) instruction to assemble. You can, however, force the assembler to choose the larger mode explicitly by pre-fixing the operand with the bit-size enclosed in square brackets.

```asm
            $c000

            // zero-page loadA
            lda 42          ; > .c000 a5 2a

            // absolute loadA
            lda [16] 42     ; > .c002 ad 2a 00

            // long jsr to bank 0 $ffd2
            jsr [24] $ffd2  ; > .c005 22 d2 ff 00
```

### Pseudo Branch Instructions

For convenience, long branch instructions are available to the programmer, where the distance is calculated to the offset relative to the current instruction before assembly, and if necessary an absolute jump is inserted. The pseudo-ops for these long branches are `jcc`, `jcs`, `jeq`, `jmi`, `jne`, `jpl`, `jvc`, and `jvs`:

```asm
            * = $c000
            lda ($19),y
            jne $c100
            ldx #0
            ...
            /* gets assembled as =>
            .C000  f0 03        beq $c005
            .c002  4c 00 c1     jmp $c100
            .c005  a2 00
            */
```

Naturally, these instructions break compatibility with nearly all other assemblers.

### 65816 Specific Directives

Immediate mode on the 65816 can emit different output based on the expected size. 6502.Net must be told which size to use for which register in order to assemble the correct number of bytes for immediate mode operations. Use `.m8` for 8-bit accumulator and `.m16` for 16-bit accumulator; `.x8` for 8-bit index registers and `.x16` for 16-bit index registers.

```asm
            rep #%00110000

            .m16
            lda #$c000
            ldx #$03

            .x16
            ldy #$1000
            jml $1012000
```

Eight-bit modes for registers are default.

You can also set all registers to the same size with `.mx8` and `.mx16` respectively.

```asm
            sep #%00110000

            .mx8

            lda #$00
            ldx #$01
            ldy #$02
```

As a convenience, the assembler can be told how to calculate the long address of symbols by their current bank, since the Program Counter itself cannot go past 65535. Use the `.bank` instruction for this:

```asm
            mvn here,there
            jsl bank1_addr  // > 22 00 10 01
            ...
            .bank 1
            *= $1000
bank1_addr  jmp bank1_addr  // > 4c 00 10
```

Note that if a symbol pointing to an address is in the same bank as the Program Counter, it will be treated like a "regular" zeropage/absolute address, but also that relative branches are not allowed if the symbol is in a different bank from the Program Counter.

### Z80 Implied Accumulator Instructions

All of the Z80 instructions that are implied to work on the accumulator, such as `sub b`, can also be written as `sub a,b`. Similarly, the logical operations, such as `xor a` can also be written as `xor a,a`.

## Reference

### Pseudo-Ops

Following is the detail of each of the 6502.Net pseudo operations. Optional arguments are in square brackets (`[` and `]`).

Note that every argument, unless specified, is a legal mathematical expression, and can include symbols such as labels (anonymous and named) and the program counter. If the expression evaluates to a value greater than the maximum value allowed by the pseudo-op, the assembler will issue an illegal quantity error.

<table>
<tr><td><b>Name</b></td><td><code>.addr</code></td></tr>
<tr><td><b>Alias</b></td><td><code>.word</code></td></tr>
<tr><td><b>Definition</b></td><td>Insert an unsigned 16-bit value or values between 0 and 65535 into the assembly. Multiple arguments can be passed as needed. If <code>?</code> is passed then the data is uninitialized.</td></tr>
<tr><td><b>Arguments</b></td><td><code>address[, address2[, ...]</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
        * = $c000
mysub   lda #13                 // output newline
        jsr chrout
        rts
        .addr mysub             // >c006 00 c0
</pre>
</td></tr></table>
<table>
<tr><td><b>Name</b></td><td><code>.align</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Set the program counter to a value divisible by the argument. If a second argument is specified, the
expressed bytes will be assembled until the point the program counter reaches its new value, otherwise is treated as uninitialized memory.</td></tr>
<tr><td><b>Arguments</b></td><td><code>amount[, fillvalue]</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
      * = $c023
      .align $10,$ff // >c023 ff ff ff ff ff ff ff ff
                     // >c02b ff ff ff ff ff
      .byte $23      // >c030 23
</pre>
</td></tr></table>
<table>
<tr><td><b>Name</b></td><td><code>.binary</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Insert a file as binary data into the assembly. Optional offset and file size arguments can be passed for greater flexibility.</td></tr>
<tr><td><b>Arguments</b></td><td><code>filename[, offset[, size]</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
      .binary     "subroutines.prg",2  // strip off start address
      .binary     "mybin.bin"          // include all of 'mybin.bin'
      .binary     "soundtrack.sid",$7e // skip SID-header
      .binary     "subroutines.prg",2,1000
                  // strip off start address, only take first
                  // 1000 bytes thereafter.
</pre>
</td></tr></table>
<table>
<tr><td><b>Name</b></td><td><code>.byte</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Insert an unsigned byte-sized value or values between 0 and 255 into the assembly. Multiple arguments can be passed as needed. If <code>?</code> is passed then the data is uninitialized.</td></tr>
<tr><td><b>Arguments</b></td><td><code>value[, value[, ...]</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
      * = $033c
      .byte $39, $38, $37, $36, $35, $34, $33, $32, $31
      // >033c 39 38 37 36 35 34 33 32
      // >0344 31
</pre>
</td></tr></table>
<table>
<tr><td><b>Name</b></td><td><code>.cstring</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Insert a C-style null-terminated string into the assembly. Multiple arguments can be passed, with a null only inserted at the end of the argument list. If <code>?</code> is passed then the data is an uninitialized byte. Enclosed text is assembled as string-literal while expressions are assembled to the minimum number of bytes required for storage, in little-endian byte order.</td></tr>
<tr><td><b>Arguments</b></td><td><code>value[, value[, ...]</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
        * = 1000
        .cstring "hello, world!"    // >1000 68 65 6c 6c 6f 2c 20 77
                                    // >1008 6f 72 6c 64 21 00
        .cstring $93,"ALL CLEAR"    // >100e 93 41 4c 4c 20 43 4c 45
                                    // >1016 41 52 00
        .cstring $ffd2              // >1019 d2 ff 00
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>.dint</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Insert a signed 32-bit value or values between −2147483648 and 2147483647 into the assembly, little-endian. Multiple arguments can be passed as needed. If <code>?</code> is passed then the data is uninitialized.</td></tr>
<tr><td><b>Arguments</b></td><td><code>value[, value[, ...]</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
        * = $0801
        .dint   18000000      // &gt;0801 80 a8 12 01
</pre>
</td></tr></table>
<table>
<tr><td><b>Name</b></td><td><code>.dword</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Insert a signed 32-bit value or values between 0 and 4294967295 into the assembly, little-endian. Multiple arguments can be passed as needed. If <code>?</code> is passed then the data is uninitialized.</td></tr>
<tr><td><b>Arguments</b></td><td><code>value[, value[, ...]</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
        * = $0801
        .dword  $deadfeed     // &gt;0801 ed fe ad de
</pre>
</td></tr></table>
<table>
<tr><td><b>Name</b></td><td><code>.fill</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Fill the assembly by the specified amount. Similar to align, that if only one argument is passed then space is merely reserved. Otherwise the optional second argument indicates the assembly should be filled with bytes making up the expression, in little-endian byte order.</td></tr>
<tr><td><b>Arguments</b></td><td><code>amount[, fillvalue]</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
        .fill   23  ; reserve 23 bytes
        * = $1000
        .fill 11,$ffd2 // >1000 d2 ff d2 ff d2 ff d2 ff
                       // >1008 d2 ff d2
</pre>
</td></tr></table>
<table>
<tr><td><b>Name</b></td><td><code>.lint</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Insert a signed 24-bit value or values between -8388608 and 8388607 into the assembly, little-endian. Multiple arguments can be passed as needed. If <code>?</code> is passed then the data is uninitialized.</td></tr>
<tr><td><b>Arguments</b></td><td><code>value[, value[, ...]</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
        * = $c100
        .lint   -80000    // >c100 80 c7 fe
</pre>
</td></tr></table>
<table>
<tr><td><b>Name</b></td><td><code>.long</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Insert a signed 24-bit value or values between 0 and 16777215 into the assembly, little-endian. Multiple arguments can be passed as needed. If <code>?</code> is passed then the data is uninitialized.</td></tr>
<tr><td><b>Arguments</b></td><td><code>value[, value[, ...]</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
        * = $c100
        .long   $ffdd22   // >c100 22 dd ff
</pre>
</td></tr></table>
<table>
<tr><td><b>Name</b></td><td><code>.lstring</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Insert a string into the assembly, each byte shifted to the left, with the lowest bit set on the final byte of the statement. See example of how this format can be used. If the highest bit of any output byte is set, the assembler will error. Multiple arguments can be passed, with a null only inserted at the end of the argument list. If <code>?</code> is passed then the data is an uninitialized byte. Enclosed text is assembled as string-literal while expressions are assembled to the minimum number of bytes required for storage, in little-endian byte order.</td></tr>
<tr><td><b>Arguments</b></td><td><code>value[, value[, ...]</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
        and a               // clear carry
        ld  de,screenbuf
        ld  hl,message
-       ld  a,(hl)          // next char
        rrca                // shift right
        ld  (de),a          // save in buffer
        jr  c,done          // carry set on shift? done
        inc hl              // else next char
        inc de              // and buff
        jr  -               // get next
done    ret
message .lstring "HELLO"   // >c100 90 8a 98 98 9f
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>.nstring</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Insert a string into the assembly, the negative (highest) bit set on the final byte of the statement. See example of how this format can be used. If the highest bit of the last byte is already set, the assembler will error. Multiple arguments can be passed, with a null only inserted at the end of the argument list. If <code>?</code> is passed then the data is an uninitialized byte. Enclosed text is assembled as string-literal while expressions are assembled to the minimum number of bytes required for storage, in little-endian byte order.</td></tr>
<tr><td><b>Arguments</b></td><td><code>value[, value[, ...]</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
        ld  de,screenbuf
        ld  hl,message
-       ld  a,(hl)          // next char
        ld  b,a             // copy to .b to test high bit
        and #%01111111      // turn off high bit...
        ld  (de),a          // and print
        rlc b               // high bit into carry
        jr  c,done          // if set we printed final char
        inc hl:inc de       // else increment pointers
        jr -                // get next
message .nstring "hello"    // >68 65 6c 6c ef
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>.pstring</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Insert a Pascal-style string into the assembly, the first byte of the statement indicating the full string size. Note this size includes all arguments in the expression. If the size is greater than 255, the assembler will error. If <code>?</code> is passed then the data is an uninitialized byte. Enclosed text is assembled as string-literal while expressions are assembled to the minimum number of bytes required for storage, in little-endian byte order.</td></tr>
<tr><td><b>Arguments</b></td><td><code>value[, value[, ...]</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
        * = $4000
        .pstring $23,$24,$25,$26,1024 // >4000 06 23 24 25 26 00 04
        .pstring "hello"              // >4007 05 68 65 6c 6c 6f
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>.rta</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Insert an unsigned 16-bit value or values between 0 and 65535 into the assembly. Similar to <code>.addr</code> and <code>.word</code>, except the value is decremented by one, yielding a return address. This is useful for building "rts jump" tables. Multiple arguments can be passed as needed. If <code>?</code> is passed then the data is uninitialized.</td></tr>
<tr><td><b>Arguments</b></td><td><code>address[, address2[, ...]</code></td></tr>
<tr><td><b>Example</b></td>
<td><pre>
chrin   = $ffcf
chrout  = $ffd2
        * = $c000
rtsjmp  txa                 // .x := index of jump
        asl a               // double it
        tax
        lda jumptable+1,x   // push high byte
        pha
        lda jumptable,x     // push low byte
        pha
        rts                 // do the jump
jumptable
        .rta chrout, chrin  // >c00b d1 ff ce ff
</pre></td>
</tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>.sbyte</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Insert an unsigned byte-sized value or values between -128 and 127 into the assembly. Multiple arguments can be passed as needed. If <code>?</code> is passed then the data is uninitialized.</td></tr>
<tr><td><b>Arguments</b></td><td><code>value[, value[, ...]</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
        * = $033c
        .sbyte 127, -3  // >033c 7f fd
</pre>
</td></tr></table>
<table>
<tr><td><b>Name</b></td><td><code>.sint</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Insert a signed 16-bit value or values between -32768 and 32767 into the assembly, little-endian. Multiple arguments can be passed as needed. If <code>?</code> is passed then the data is uninitialized.</td></tr>
<tr><td><b>Arguments</b></td><td><code>value[, value[, ...]</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
        * = $c000
mysub   lda #13             // output newline
        jsr chrout
        rts
        .sint -16384        // >c006 00 c0
</pre>
</td></tr></table>
<table>
<tr><td><b>Name</b></td><td><code>.string</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Insert a string into the assembly. Multiple arguments can be passed. If <code>?</code> is passed then the data is an uninitialized byte. Enclosed text is assembled as string-literal while expressions are assembled to the minimum number of bytes required for storage, in little-endian byte order.</td></tr>
<tr><td><b>Arguments</b></td><td><code>value[, value[, ...]</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
        * = 1000
        .string "hello, world!"   // >1000 68 65 6c 6c 6f 2c 20 77
                                  // >1008 6f 72 6c 64 21
</pre>
</td></tr>
</table>

### Other Assembler Directives

The directives below do not directly emit output but are responsible for other useful tasks, such as control of assembly flow execution and macro definitions.

<table>
<tr><td><b>Name</b></td><td><code>.assert</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Asserts the truth of a given expression. If the assertion fails, an error is logged. A custom error can optionally be specified.</td></tr>
<tr><td><b>Arguments</b></td><td><code>condition[, error]</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
        * = $0800
        nop
        .assert 5 == 6              // standard assertion error thrown
        .assert * < $0801, "Uh oh!" // custom error output
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>.bank</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Sets the current bank for calculating symbols in long address mode instructions. By default the current bank is 0. Note that this instruction will affect the assembler's behavior even for non-65816 CPUs.
</td></tr>
<tr><td><b>Arguments</b></td><td><code>bank</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
             .cpu "65816"
             jmp somewherefar  // > 5c 00 f0 ff 
             ...
             .bank $ff
             * = $f000  
somewherefar jmp somewherefar   // > 4c 00 f0
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>.binclude</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Include a source file and enclose the expanded source into a scoped block. The specified file is 6502.Net-compatible source. If no name is given in front of the directive then all symbols inside the included source will be inaccessible. Note that to prevent infinite recursion, a source file can only be included once in the entire source, including from other included files.</td></tr>
<tr><td><b>Arguments</b></td><td><code>filename</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
soundlib    .binclude "sound.s"
            jsr soundlib.play   // Invoke the
                                // play subroutine
                                // inside the
                                // sound.s source
            ;; whereas...
            .binclude "sound.s"
            jsr play            // will not assemble!
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>.block</code>/<code>.endblock</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Define a scoped block for symbols. Useful for preventing label definition clashes. Blocks can be nested as needed. Unnamed blocks are considered anonymous and all symbols defined within them are inaccessible outside the block. Otherwise symbols inside blocks can be accessed with dot-notation.</td></tr>
<tr><td><b>Arguments</b></td><td>None</td></tr>
<tr><td><b>Example</b></td><td>
<pre>
kernal .block
        chrout = $ffd2
        chrin  = $ffcf
        .endblock
        ...
chrout  lda message,x
        jsr kernal.chrout   // this is a different
                            // chrout!
done    rts                 // this is not the done
                            // below!
        .block
        beq done            // the done below!
        nop
        nop
done    rts
        .endblock
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>.break</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>In a for/next or while loop, it breaks out of the loop block. In a switch statement it sends control to the next or default case.</td></tr>
<tr><td><b>Arguments</b></td><td>None</td></tr>
<tr><td><b>Example</b></td><td>
<pre>
        .for n = 0, n < 1000, n = n + 1
            .if * > $7fff   // unless address >= $8000
                .break
            .endif
            nop             // do 1000 nops
        .next
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>.case</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Used in switch blocks for cases. A corresponding <code>.break</code> is required.</td></tr>
<tr><td><b>Arguments</b></td><td>Expression to compare.</td></tr>
<tr><td><b>Example</b></td><td>
<pre>
        .switch *
            .case $100
                .echo "Program Counter is now at $100"
                .break
            .case $1000
                .echo "Program Counter is now at $1000"
                .break
        .endswitch
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>.continue</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Continue the next iteration in a for-next or while loop.</td></tr>
<tr><td><b>Arguments</b></td><td>None</td></tr>
<tr><td><b>Example</b></td><td>
<pre>
        .for n = 0, n < 1000, n = n + 1
            .if (* % $100) == 0 // skip page poundaries
                .continue
            .endif
            .byte 42
        .next
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>.comment</code>/<code>.endcomment</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Set a multi-line comment block. NOTE: This directive is deprecated. /* and */ should be used for multi-line comments instead.</td></tr>
<tr><td><b>Arguments</b></td><td>None</td></tr>
<tr><td><b>Example</b></td><td>
<pre>
    .comment
    My code pre-amble
    .endcomment
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>.cpu</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Set the assembler to target the supported CPU. See the <code>--cpu</code> option in the command-line notes below for the available options. NOTE: If set in Z80 mode, this directive will error.</td></tr>
<tr><td><b>Arguments</b></td><td><code>cpu</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
      .cpu "65816"
      clc
      xce
      rep #%00110000
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>.default</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Used in switch blocks for the default case. A corresponding <code>.break</code> is required. Note that default cases are optional.</td></tr>
<tr><td><b>Arguments</b></td><td>None.</td></tr>
<tr><td><b>Example</b></td><td>
<pre>
        .switch *
            .case $100
                .echo "Program Counter is now at $100"
                .break
            .default
                .echo "Program Counter is some value other than $100"
                .break
        .endswitch
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>.echo</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Send a message to the console output. Note if the assembler
is in quiet mode, no output will be seen.</td></tr>
<tr><td><b>Arguments</b></td><td>message</td></tr>
<tr><td><b>Example</b></td><td>
<pre>
    .echo "hi there!"
    // console will output "hi there!"
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>.encoding</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Select the text encoding for assembly output. Four encodings are pre-defined:
<ul>
        <li><code>none</code>       - UTF-8 (default)</li>
        <li><code>atascreen</code>       - Atari screen codes</li>
        <li><code>cbmscreen</code>       - Commodore screen codes</li>
        <li><code>petscii</code>       - Commodore PETSCII</li>
</ul>
Note: <code>none</code> is default and will not be affected by <code>.map</code> and <code>.unmap</code> directives.
</td></tr>
<tr><td><b>Arguments</b></td><td><code>encoding</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
      .encoding "petscii"
      .string "hello"       // >> 45 48 4c 4c 4f
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>.end</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Terminate the assembly.</td></tr>
<tr><td><b>Arguments</b></td><td>None</td></tr>
<tr><td><b>Example</b></td><td>
<pre>
        jsr $ffd2
        beq done            // oops!
        rts
        .end                // stop everything
done    ...                 // assembly will never
                            // reach here!
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>.eor</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>XOR output with 8-bit value. Quick and dirty obfuscation trick.</td></tr>
<tr><td><b>Arguments</b></td><td><code>xormask</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
      .eor $ff
      .byte 0,1,2,3       // > ff fe fd fc
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>.equ</code></td></tr>
<tr><td><b>Alias</b></td><td><code>=</code></td></tr>
<tr><td><b>Definition</b></td><td>Assign the label, anonymous symbol, or program counter to the expression. Note that there is an implied version of this directive, such that if the directive and expression are ommitted altogether, the label or symbol is set to the program counter.</td></tr>
<tr><td><b>Arguments</b></td><td><code>value</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
chrin      .equ $ffcf
chrout      =   $ffd2
          * .equ $c000
-           =   255
start       ; same as start .equ *
            ldx #$00
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>.error</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Prints a custom error to the console. The error is treated like any assembler error and will cause failure of assembly.</td></tr>
<tr><td><b>Arguments</b></td><td><code>error</code></td></tr>
<tr><td><b>Example</b></td><td>
<code>.error "We haven't fixed this yet!" </code>
</td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>.errorif</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Prints a custom error to the console if the condition is met. Useful for sanity checks and assertions. The error is treated like any assembler error and will cause failure of assembly. The condition is any logical expression.</td></tr>
<tr><td><b>Arguments</b></td><td><code>condition, error</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
        * = $0800
        nop
        .errorif * > $0801, "Uh oh!" /*
                                       if program counter
                                       is greater than 2049,
                                       raise a custom error
                                     */
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>.[el]if[[n]def]</code>/<code>.endif</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>All source inside condition blocks are assembled if evaluated to true. The "non-definition" conditions are evaluated on each pass:
    <ul>
        <li><code>.if &lt;expression&gt;</code>   - Assemble if the expression is true</li>
        <li><code>.ifdef &lt;symbol&gt;</code>    - Assemble if the symbol is defined on first pass</li>
        <li><code>.ifndef &lt;symbol&gt;</code>   - Assemble if the symbol is not defined on first pass</li>
        <li><code>.elif &lt;expression&gt;</code> - Assemble if expression is true and previous conditions are false</li>
        <li><code>.elifdef &lt;symbol&gt;</code>  - Assemble if symbol is defined on first pass and previous conditions are false</li>
        <li><code>.elifndef &lt;symbol&gt;</code> - Assemble if symbol is not defined on first pass and previous conditions are false</li>
        <li><code>.else</code>                    - Assemble if previous conditions are false</li>
        <li><code>.endif</code>                   - End of condition block
    </ul>
</td></tr>
    <tr><td><b>Arguments</b></td><td><code>condition</code>/<code>symbol</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
        * = $0400
        cycles = 1
        .if cycles == 1
            nop
        .elif cycles == 2
            nop
            nop
        .endif
        /*
        will result as:
          nop
        */
</pre>
</td></tr></table>
<table>
<tr><td><b>Name</b></td><td><code>.for</code>/<code>.next</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Repeat until codition is met. The iteration variable can be used in source like any other variable. The initialization and conditional expression can both be blank, and multiple iteration expressions can be specified, but at least one is required.</td></tr>
<tr><td><b>Arguments</b></td><td><code>[init_expression[, condition, ],iteration_expression[, ...]</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
        .let x = 0
        .for , pages = $100, pages < $800, pages = pages + $100, x = x + 1
            ldx #x
            stx pages
        .next
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>.function</code>/<code>.endfunction</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Defines a custom function. Must be named. If the function is expected to be used in a math expression, a return value is required, otherwise it can only be used with <code>.invoke</code>. No assembly output is allowed inside a function. All symbols declared within a function are considered local to it, including its parameters. Parameters can be given default values to make them optional upon invocation.</td></tr>
<tr><td><b>Arguments</b></td><td><code>parameter[=default, parameter[, ...]</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
factorial   .function n
            .if n == 0
                .return 1
            .endif
                .return n * factorial(n - 1)
            .endfunction
            .word factorial(6) // > D0 02
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>.global</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Make the specified label global, irrespective of the current scope it in which it is defined.</td></tr>
<tr><td><b>Arguments</b></td><td><code>expression</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
                .block
GLOBAL_LABEL    .global 33
                .endblock
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>.goto</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Go to the specified label and resume assembly there.</td></tr>
<tr><td><b>Arguments</b></td><td><code>label</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
                .if V > 0
                    .goto INIT
                .endif
                pop bc     // will not assemble if V > 0
INIT            xor a,a
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>.include</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Include a source file into the assembly. The specified file is 6502.Net-compatible source. Note that to prevent infinite recursion, a source file can only be included once in the entire source, including from other included files.</td></tr>
<tr><td><b>Arguments</b></td><td><code>filename</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
      .include "mylib.s"
      // mylib is now part of source
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>.initmem</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Initialize the output memory. Gaps will assemble to the value.
</td></tr>
<tr><td><b>Arguments</b></td><td><code>value</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
      .initmem $ea
      .fill 10 
      lda #0   // 10 nops before here
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>.invoke</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Invokes a function. Useful for changing assembly run-time state, or accomplishing some repeated task.</td></tr>
<tr><td><b>Arguments</b></td><td><code>filename</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
boundscheck .function amount, bound
                .errorif amount > bound, "Out of bounds!"
            .endfunction
            .invoke boundscheck(*+variable, $1000)
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>.let</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Declares and assigns or re-assigns a variable to the given expression. Labels cannot be redefined as variables, and vice versa. In addition, variables cannot be forward-referenced, as the variable table is reset each pass.</td></tr>
<tr><td><b>Arguments</b></td><td><code>expression</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
            .let myvar =    $ffd2
            jsr myvar
            .let myvar =    myvar-$1000
            lda myvar
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>.m8</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Directs the assembler to treat immediate mode operations on the accumulator as 8-bit (one byte). Useful for when the assembler is in <code>65816</code> mode.</td></tr>
<tr><td><b>Arguments</b></td><td>None</td></tr>
<tr><td><b>Example</b></td><td>
<pre>
      sep #$20
      .m8
      lda #$13
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>.m16</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Directs the assembler to treat immediate mode operations on the accumulator as 16-bit (two bytes). Useful for when the assembler is in <code>65816</code> mode.</td></tr>
<tr><td><b>Arguments</b></td><td>None</td></tr>
<tr><td><b>Example</b></td><td>
<pre>
      rep #$20
      .m16
      lda #$1234
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>.macro</code>/<code>.endmacro</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Define a macro that when invoked will expand into source before assembly. Must be named. Optional arguments are treated as parameters to pass as text substitutions in the macro source where referenced, with a leading either a backslash <code>\</code> or as <code>@{param}</code> from within a string, either by name or by number in the parameter list. Parameters can be given default values to make them optional upon invocation. Macros are invoked with a leading <code>.</code>. All symbols in the macro definition are local, so macros can be re-used with no symbol clashes.</td></tr>
<tr><td><b>Arguments</b></td><td><code>parameter[=default, parameter[, ...]</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
inc16       .macro
            inc \1
            bne +
            inc \1+1
&#43;          .endmacro
            .inc16 $c000
            /* 
            expands to =>
            inc $c000
            bne +
            inc $c001
            */
&#43;
print       .macro  value = 13, printsub = $ffd2
            lda #\value     // or lda #\1
            jsr \printsub   // or jsr \2
            rts
            .endmacro
            .print
            /*
             expands to =>
             lda #$0d
             jsr $ffd2
             rts
            .print 'E',$fded
             expands to =>
             lda #$45
             jsr $fded
              rts
            */
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>.map</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Maps a character or range of characters to custom binary output in the selected encoding. Note: <code>none</code> is not affected by <code>.map</code> and <code>.unmap</code> directives. It is recommended to represent individual char literals as strings.
</td></tr>
<tr><td><b>Arguments</b></td><td><code>start[, end]</code>/<br>
<code>"&lt;start&gt;&lt;end&gt;"</code>,<code>code</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
      .encoding myencoding
      .map "A", "a"
      .map "π", $5e
      .byte 'A', 'π' ;; >> 61 5e
      .map "09", $00
      .string "2017" ;; >> 02 00 01 07
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>.mx8</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Directs the assembler to treat all immediate mode operations as 8-bit (one byte). Useful for when the assembler is in <code>65816</code> mode.</td></tr>
<tr><td><b>Arguments</b></td><td>None</td></tr>
<tr><td><b>Example</b></td><td>
<pre>
      sep #$30
      .mx8
      lda #$13
      ldx #$14
      ldy #$15
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>.mx16</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Directs the assembler to treat all immediate mode operations as 16-bit (two bytes). Useful for when the assembler is in <code>65816</code> mode.</td></tr>
<tr><td><b>Arguments</b></td><td>None</td></tr>
<tr><td><b>Example</b></td><td>
<pre>
      rep #$30
      .mx16
      lda #$1234
      ldx #$5678
      ldy #$9abc
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>.page</code>/<code>.endpage</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Ensures that the block of code resides within the same page. Useful for time-sensitive code. If code crosses the page an error is issued.
</td></tr>
<tr><td><b>Arguments</b></td><td>None</d></tr>
<tr><td><b>Example</b></td><td>
<pre>
      .page
-     ldx #0
      nop
      nop
      jmp - 
      .endpage
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>.relocate</code>/<code>.endrelocate</code></td></tr>
<tr><td><b>Alias</b></td><td><code>.pseudopc</code>/<code>.realpc</code></td></tr>
<tr><td><b>Definition</b></td><td>Sets the logical program counter to the specified address with the offset of the assembled output not changing. Useful for programs that relocate parts of themselves to different memory spaces.</td></tr>
<tr><td><b>Arguments</b></td><td><code>address</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
            * = $0801
            // create a Commodore BASIC stub
            // 10 SYS2061
SYS         = $9e
            .word eob, 10
            .cstring SYS, format("{0}", start)
eob         .word 0
start       ldx #0
-           lda highcode,x
            sta $c000,x
            inx
            bne -
            jmp $c000
highcode
            .relocate $c000
            ldx #0
printloop   lda message,x
            beq done
            jsr $ffd2
            inx
            jmp printloop
done        rts
message     .cstring "HELLO, HIGH CODE!"
            .endrelocate
            /* outputs the following =>
            &gt;0801 0b 08 0a 00
            &gt;0805 9e 32 30 36 31 00
            &gt;080b 00 00
            &gt;080d a2 00                 ldx #0
            &gt;080f bd 1b 08    -         lda highcode,x
            &gt;0812 9d 00 c0              sta $c000,x
            &gt;0815 e8                    inx
            &gt;0816 d0 f7                 bne -
            &gt;0818 4c 00 c0              jmp $c000
            &gt;081b a2 00                 ldx #$00
            &gt;081d bd 0f c0    printloop lda message,x
            &gt;0820 f0 07                 beq done
            &gt;0822 20 d2 ff              jsr $ffd2
            &gt;0825 e8                    inx
            &gt;0826 4c 02 c0              jmp printloop
            &gt;0829 60          done      rts
            ;; message
            &gt;082a 48 45 4c 4c 4f 2c 20 48
            &gt;0832 49 47 48 20 43 4f 44 45
            &gt;083a 21 00  
            */
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>.repeat</code>/<code>.endrepeat</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Repeat the specified source the specified number of times. Can be nested, but must be terminated with an <code>.endrepeat</code>.</td></tr>
<tr><td><b>Arguments</b></td><td><code>repeatvalue</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
        * = $0400
        ldx #$00
        .repeat 3
        inx
        .endrepeat
        rts
        /* will assemble as:
         ldx #$00
         inx
         inx
         inx
         rts
        */
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>.switch</code>/<code>.endswitch</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Perform a switch operation for program execution control, similar to <code>.if</code>/<code>.endif</code>. A switch must have at least one case or default specified.</td></tr>
<tr><td><b>Arguments</b></td><td><code>The symbol or constant expression to compare.</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
        .switch true
            .case 1 > 0
                .echo "One IS greater than zero!"
                .break
            .case 1 < 0
                .echo "One is less than zero?! #mindblown"
                .break
        .endswitch
</pre>
</td></tr></table>
<table>
<tr><td><b>Name</b></td><td><code>.target</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Set the target architecture for the assembly output. See the <code>--arch</code> option in the command-line notes below for the available architectures.</td></tr>
<tr><td><b>Arguments</b></td><td><code>architecture</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
      .target "apple2"
      // the output binary will have an Apple DOS header
      ...
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>.unmap</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Unmaps a custom code for a character or range of characters in the selected encoding and reverts to UTF-8. Note: <code>none</code> is not affected by <code>.map</code> and <code>.unmap</code> directives. It is recommended to represent individual char literals as strings.
</td></tr>
<tr><td><b>Arguments</b></td><td><code>start[, end]</code>/<br>
<code>"&lt;start&gt;&lt;end&gt;"</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
      .encoding "myencoding"
      .unmap "A"
      .unmap "π"        // revert to UTF-8 encoding
      .byte 'A', 'π'    // >> 41 cf 80
      .unmap "09"
      .string "2017"    // >> 32 30 31 37
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>.warn</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Prints a custom warning to the console. The warning is treated like any assembler warning, and if warnings are treated as errors it will cause failure of assembly.</td></tr>
<tr><td><b>Arguments</b></td><td><code>warning</code></td></tr>
<tr><td><b>Example</b></td><td>
<code>.warn "We haven't fixed this yet!" </code>
</td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>.warnif</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Prints a custom warning to the console if the condition is met. The warning is treated like any assembler warning, and if warnings are treated as errors it will cause failure of assembly The condition is any logical expression.</td></tr>
<tr><td><b>Arguments</b></td><td><code>condition, warning</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
    * = $0800
    nop
    .warnif   * > $0801, "Check bound"
    /* if program counter
       is greater than 2049,
       raise a custom warning */
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>.while</code>/<code>.endwhile</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Repeat until codition is met. Similar to <code>.for</code>/<code>.next</code>, but without explicit condition checking and loop variable iterations.</td></tr>
<tr><td><b>Arguments</b></td><td><code>condition</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
        .let n = 0
        .while n &lt; 256
            ld b,n
            ld (hl),b
            .let n = n +1
            inc hl
        .endwhile
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>.x8</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Directs the assembler to treat immediate mode operations on the index registers as 8-bit (one byte). Useful for when the assembler is in <code>65816</code> mode.</td></tr>
<tr><td><b>Arguments</b></td><td>None</td></tr>
<tr><td><b>Example</b></td><td>
<pre>
      sep #$10
      .x8
      ldx #$14
      ldy #$15
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>.x16</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Directs the assembler to treat immediate mode operations on the index registers as 8-bit (one byte). Useful for when the assembler is in <code>65816</code> mode.</td></tr>
<tr><td><b>Arguments</b></td><td>None</td></tr>
<tr><td><b>Example</b></td><td>
<pre>
      rep #$10
      .x16
      ldx #$5678
      ldy #$9abc
</pre>
</td></tr>
</table>

### Built-In functions

<table>
<tr><td><b>Name</b></td><td><code>abs</code></td></tr>
<tr><td><b>Definition</b></td><td>The absolute (positive sign) value of the expression.</td></tr>
<tr><td><b>Arguments</b></td><td><code>value</code></td></tr>
<tr><td><b>Example</b></td><td><code>.word abs(-2234)     // > ba 08</code></td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>acos</code></td></tr>
<tr><td><b>Definition</b></td><td>The arc cosine of the expression.</td></tr>
<tr><td><b>Arguments</b></td><td><code>value</code></td></tr>
<tr><td><b>Example</b></td><td><code>.byte acos(1.0)      // > 00</code></td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>atan</code></td></tr>
<tr><td><b>Definition</b></td><td>The arc tangent of the expression.</td></tr>
<tr><td><b>Arguments</b></td><td><code>value</code></td></tr>
<tr><td><b>Example</b></td><td><code>.byte atan(0.0)      // > 00</code></td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>cbrt</code></td></tr>
<tr><td><b>Definition</b></td><td>The cubed root of the expression.</td></tr>
<tr><td><b>Arguments</b></td><td><code>value</code></td></tr>
<tr><td><b>Example</b></td><td><code>.long cbrt(2048383)   // > 7f 00 00</code></td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>ceil</code></td></tr>
<tr><td><b>Definition</b></td><td>Round up expression.</td></tr>
<tr><td><b>Arguments</b></td><td><code>value</code></td></tr>
<tr><td><b>Example</b></td><td><code>.byte ceil(1.1)       // > 02</code></td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>cos</code></td></tr>
<tr><td><b>Definition</b></td><td>The cosine of the expression.</td></tr>
<tr><td><b>Arguments</b></td><td><code>value</code></td></tr>
<tr><td><b>Example</b></td><td><code>.byte cos(0.0)        // > 01</code></td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>cosh</code></td></tr>
<tr><td><b>Definition</b></td><td>The hyperbolic cosine of the expression.</td></tr>
<tr><td><b>Arguments</b></td><td><code>value</code></td></tr>
<tr><td><b>Example</b></td><td><code>.byte cosh(0.0)       // > 01</code></td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>deg</code></td></tr>
<tr><td><b>Definition</b></td><td>Degrees from radians.</td></tr>
<tr><td><b>Arguments</b></td><td><code>radian</code></td></tr>
<tr><td><b>Example</b></td><td><code>.byte deg(1.0)        // > 39</code></td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>exp</code></td></tr>
<tr><td><b>Definition</b></td><td>Exponential of <i>e</i>.</td></tr>
<tr><td><b>Arguments</b></td><td><code>power</code></td></tr>
<tr><td><b>Example</b></td><td><code>.dint exp(16.0)       // > 5e 97 87 00</code></td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>floor</code></td></tr>
<tr><td><b>Definition</b></td><td>Round down expression.</td></tr>
<tr><td><b>Arguments</b></td><td><code>value</code></td></tr>
<tr><td><b>Example</b></td><td><code>.sbyte floor(-4.8)     // > fb</code></td></tr>
</table>
<table>
<table>
<tr><td><b>Name</b></td><td><code>format</code></td></tr>
<tr><td><b>Definition</b></td><td>Converts objects to a string in the format specified. The format string must adhere to Microsoft .Net standards. Please see <a href="https://docs.microsoft.com/en-us/dotnet/standard/base-types/standard-numeric-format-strings">the documentation on standard .Net format strings</a> for more information.</td></tr>
<tr><td><b>Arguments</b></td><td><code>value</code></td></tr>
<tr><td><b>Example</b></td><td><code>.echo format("Program counter is ${0:x4}", *)</code></td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>frac</code></td></tr>
<tr><td><b>Definition</b></td><td>The fractional part.</td></tr>
<tr><td><b>Arguments</b></td><td><code>value</code></td></tr>
<tr><td><b>Example</b></td><td><code>.byte frac(5.18)*100  // > 12</code></td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>hypot</code></td></tr>
<tr><td><b>Definition</b></td><td>Polar distance.</td></tr>
<tr><td><b>Arguments</b></td><td><code>pole1, pole2</code></td></tr>
<tr><td><b>Example</b></td><td><code>.byte hypot(4.0, 3.0) // > 05</code></td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>ln</code></td></tr>
<tr><td><b>Definition</b></td><td>Natural logarithm.</td></tr>
<tr><td><b>Arguments</b></td><td><code>value</code></td></tr>
<tr><td><b>Example</b></td><td><code>.byte ln(2048.0)      // > 07</code></td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>log10</code></td></tr>
<tr><td><b>Definition</b></td><td>Common logarithm.</td></tr>
<tr><td><b>Arguments</b></td><td><code>value</code></td></tr>
<tr><td><b>Example</b></td><td><code>.byte log($7fffff)    // > 06</code></td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>peek</code></td></tr>
<tr><td><b>Definition</b></td><td>Lookup address. The address can only be in the range of assembled bytes.</td></tr>
<tr><td><b>Arguments</b></td><td><code>address</code></td></tr>
<tr><td><b>Example</b></td><td><code>.assert peek($c000) == $a2</code></td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>poke</code></td></tr>
<tr><td><b>Definition</b></td><td>Set address. The address can only be in the range of assembled bytes.</td></tr>
<tr><td><b>Arguments</b></td><td><code>address, value</code></td></tr>
<tr><td><b>Example</b></td><td><code>.invoke poke($c000,$a9)</td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>pow</code></td></tr>
<tr><td><b>Definition</b></td><td>Exponentiation.</td></tr>
<tr><td><b>Arguments</b></td><td><code>base, power</code></td></tr>
<tr><td><b>Example</b></td><td><code>.lint pow(2,16)       // > 00 00 01</code></td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>rad</code></td></tr>
<tr><td><b>Definition</b></td><td>Radians from degrees.</td></tr>
<tr><td><b>Arguments</b></td><td><code>degree</code></td></tr>
<tr><td><b>Example</b></td><td><code>.word rad(79999.9)    // > 74 05</code></td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>random</code></td></tr>
<tr><td><b>Definition</b></td><td>Generate a random number within the specified range of numbers. Both arguments can be negative or positive, but the second argument must be greater than the first, and the difference between them can be no greater than the maximum value of a signed 32-bit integer. This is a .Net limitation.</td></tr>
<tr><td><b>Arguments</b></td><td><code>range1, range2</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
 .word random(251,255)   // generate a random # between
                         // 251 and 255.
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>round</code></td></tr>
<tr><td><b>Definition</b></td><td>Round number.</td></tr>
<tr><td><b>Arguments</b></td><td><code>value</code></td></tr>
<tr><td><b>Example</b></td><td><code>.byte round(18.21) // > 12</code></td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>sgn</code></td></tr>
<tr><td><b>Definition</b></td><td>The sign of the expression, returned as -1 for negative, 1 for positive, and 0 for no sign.</td></tr>
<tr><td><b>Arguments</b></td><td><code>value</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
 .sbyte sgn(-8.0), sgn(14.0), sgn(0)
 // > ff 01 00
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>sin</code></td></tr>
<tr><td><b>Definition</b></td><td>The sine of the expression.</td></tr>
<tr><td><b>Arguments</b></td><td><code>value</code></td></tr>
<tr><td><b>Example</b></td><td><code>.sbyte sin(1003.9) * 14 // > f2</code></td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>sinh</code></td></tr>
<tr><td><b>Definition</b></td><td>The hyperbolic sine of the expression.</td></tr>
<tr><td><b>Arguments</b></td><td><code>value</code></td></tr>
<tr><td><b>Example</b></td><td><code>.byte sinh(0.0)        // > f2</code></td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>sqrt</code></td></tr>
<tr><td><b>Definition</b></td><td>The square root of the expression.</td></tr>
<tr><td><b>Arguments</b></td><td><code>value</code></td></tr>
<tr><td><b>Example</b></td><td><code>.byte sqrt(65536) - 1  // > ff</code></td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>tan</code></td></tr>
<tr><td><b>Definition</b></td><td>The tangent the expression.</td></tr>
<tr><td><b>Arguments</b></td><td><code>value</code></td></tr>
<tr><td><b>Example</b></td><td><code>.byte tan(444.0)*5.0   // > 08</code></td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>tanh</code></td></tr>
<tr><td><b>Definition</b></td><td>The hyperbolic tangent the expression.</td></tr>
<tr><td><b>Arguments</b></td><td><code>value</code></td></tr>
<tr><td><b>Example</b></td><td><code>.byte tanh(0.0)        // > 00</code></td></tr>
</table>

### Command-line options

6502.Net accepts several arguments, requiring at least one. If no option flag precedes the argument, it is considered an input file. Multiple input files can be assembled. If no output file is specified, source is assembled to `a.out` within the current working directory. Below are the available option flags and their parameters. Mono users note for the examples you must put `mono` in front of the executable.

<table>
<tr><td><b>Option</b></td><td><code>-o</code></td></tr>
<tr><td><b>Alias</b></td><td>--output</td></tr>
<tr><td><b>Definition</b></td><td>Output the assembly to the specified output file. A valid output filename is a required parameter.</td></tr>
<tr><td><b>Parameter</b></td><td><code>filename</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
6502.Net.exe myasm.asm -o myoutput
6502.Net.exe myasm.asm -output=myoutput
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Option</b></td><td><code>--arch</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Specify the target machine architecture of the binary output. Different options are available depending on the CPU being targeted. The default is <code>cbm</code> for 6502 and flat for Z80. The options:
    <ul>
        <li>65xx
            <ul>
                <li><code>apple2</code>    - Apple ][ binary with Apple DOS header</li>
                <li><code>atari-xex</code> - Atari 8-bit binary with XEX header</li>
                <li><code>cbm</code>       - Commodore DOS binary with load address header (default)</li>
                <li><code>d64</code>       - Commodore emulator D64 disk image format</li>
                <li><code>flat</code>      - Flat binary with no header</li>
            </ul>
        </li>
        <li>Z80
            <ul>
                <li><code>amsdos</code>  - Amstrad CPC DOS (disk)</li>
                <li><code>amstap</code>  - Amstrad CPC DOS (tape)</li>
                <li><code>msx</code> - MSX</li>
                <li><code>zx</code>       - ZX Spectrum</li>
                <li><code>flat</code>      - Flat binary with no header</li>
            </ul>
        </li>
    </ul>
</td></tr>
<tr><td><b>Parameter</b></td><td><code>architecture</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>6502.Net.exe myasm.asm -b --arch=flat flat.bin</pre>
</td></tr>
</table>
<table>
<tr><td><b>Option</b></td><td><code>-b</code></td></tr>
<tr><td><b>Alias</b></td><td><code>--big-endian</code></td></tr>
<tr><td><b>Definition</b></td><td>Assemble multi-byte values in big-endian order (highest order magnitude first).</td></tr>
<tr><td><b>Parameter</b></td><td>None</td></tr>
<tr><td><b>Example</b></td><td>
<pre>6502.Net.exe myasm.asm -b -o bigend.bin</pre>
</td></tr>
</table>
<table>
<tr><td><b>Option</b></td><td><code>-C</code></td></tr>
<tr><td><b>Alias</b></td><td><code>--case-sensitive</code></td></tr>
<tr><td><b>Definition</b></td><td>Set the assembly mode to case-sensitive. All tokens, including assembly mnemonics, directives, and symbols, are treated as case-sensitive. By default, 6502.Net is not case-sensitive.</td></tr>
<tr><td><b>Parameter</b></td><td>None</td></tr>
<tr><td><b>Example</b></td><td>
<pre>
6502.Net.exe mycsasm.asm -C
6502.Net.exe mycsasm.asm --case-sensitive
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Option</b></td><td><code>-c</code></td></tr>
<tr><td><b>Alias</b></td><td><code>--cpu</code></td></tr>
<tr><td><b>Definition</b></td><td>Direct the assembler to the given cpu target By default, 6502.Net targets only legal 6502 instructions. The following options are available:
<ul>
        <li><code>6502</code>        - Legal 6502 instructions only (default)</li>
        <li><code>6502i</code>       - Legal and illegal 6502 instructions</li>
        <li><code>65C02</code>       - Legal 6502 and 65C02 instructions</li>
        <li><code>65CS02</code>      - Legal 6502, 65C02 and W65C02 instructions</li>
        <li><code>R65C02</code>      - Legal 6502, 65C02 and RC6502 instructions</li>
        <li><code>W65C02</code>      - Legal 6502, 65C02, RC6502 and W65C02 instructions</li>
        <li><code>65CE02</code>      - Legal 6502, 65C02, RC6502 and 65CE02 instructions</li>
		<li><code>HuC6280</code>     - Legal 6502, 65C02, RC6502 and Hudson C6280 instructions</li>
        <li><code>65816</code>       - Legal 6502, 65C02 and W65C816 instructions</li>
        <li><code>z80</code>         - Z80 instructions</li>
</ul>
</td></tr>
<tr><td><b>Parameter</b></td><td><code>cpu</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
6502.Net.exe myspeccyproggy.asm --cpu=z80
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Option</b></td><td><code>--checksum</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Display SHA1 hash for output file after successful assembly.</td></tr>
<tr><td><b>Example</b></td><td>
<pre>6502.Net.exe myasm.asm --checksum</pre>
</td></tr>
</table>
<table>
<tr><td><b>Option</b></td><td><code>-D</code></td></tr>
<tr><td><b>Alias</b></td><td><code>--define</code></td></tr>
<tr><td><b>Definition</b></td><td>Assign a global label a value. Note that within the source the label cannot be redefined again. The value can be any expression 6502.Net can evaluate at assembly time. If no value is given the default value is 1.</td></tr>
<tr><td><b>Parameter</b></td><td><code>&lt;label&gt;=&lt;value&gt;</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>6502.Net.exe -D chrout=$ffd2 myasm.asm -o myoutput</pre>
</td></tr>
</table>
<table>
<tr><td><b>Option</b></td><td><code>-h</code></td></tr>
<tr><td><b>Alias</b></td><td><code>-?, --help</code></td></tr>
<tr><td><b>Definition</b></td><td>Print all command-line options to console output.</td></tr>
<tr><td><b>Parameter</b></td><td>None</td></tr>
<tr><td><b>Example</b></td><td>
<pre>
6502.Net.exe -h
6502.Net.exe --help
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Option</b></td><td><code>-q</code></td></tr>
<tr><td><b>Alias</b></td><td><code>--quiet</code></td></tr>
<tr><td><b>Definition</b></td><td>Assemble in quiet mode, with no messages sent to console output, including errors and warnings.</td></tr>
<tr><td><b>Parameter</b></td><td>None</td></tr>
<tr><td><b>Example</b></td><td>
<pre>
6502.Net.exe -q myasm.asm
6502.Net.exe --quiet myasm.asm
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Option</b></td><td><code>-w</code></td></tr>
<tr><td><b>Alias</b></td><td><code>--no-warn</code></td></tr>
<tr><td><b>Definition</b></td><td>Suppress the display of all warnings.</td></tr>
<tr><td><b>Parameter</b></td><td>None</td></tr>
<tr><td><b>Example</b></td><td>
<pre>
6502.Net.exe -w myasm.asm
6502.Net.exe --no-warn myasm.asm
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Option</b></td><td><code>--wnoleft</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Suppress warnings for lines where whitespaces precede labels.</td></tr>
<tr><td><b>Parameter</b></td><td>None</td></tr>
<tr><td><b>Example</b></td><td>
<pre>
6502.Net.exe --wnoleft myasm.asm
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Option</b></td><td><code>--werror</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Treat all warnings as errors.</td></tr>
<tr><td><b>Parameter</b></td><td>None</td></tr>
<tr><td><b>Example</b></td><td>
<pre>
6502.Net.exe --werror myasm.asm
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Option</b></td><td><code>-l</code></td></tr>
<tr><td><b>Alias</b></td><td><code>--labels</code></td></tr>
<tr><td><b>Definition</b></td><td>Dump all label definitions to listing.</td></tr>
<tr><td><b>Parameter</b></td><td><code>filename</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
6502.Net.exe myasm.asm -l labels.asm
6502.Net.exe myasm.asm --labels=labels.asm
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Option</b></td><td><code>-L</code></td></tr>
<tr><td><b>Alias</b></td><td><code>--list</code></td></tr>
<tr><td><b>Definition</b></td><td>Output the assembly listing to the specified file.</td></tr>
<tr><td><b>Parameter</b></td><td><code>filename</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
6502.Net.exe myasm.asm -L listing.asm
6502.Net.exe myasm.asm --list=listing.asm
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Option</b></td><td><code>-a</code></td></tr>
<tr><td><b>Alias</b></td><td><code>--no-assembly</code></td></tr>
<tr><td><b>Definition</b></td><td>Suppress assembled bytes from assembly listing.</td></tr>
<tr><td><b>Parameter</b></td><td>None</td></tr>
<tr><td><b>Example</b></td><td>
<pre>
6502.Net.exe myasm.asm -a -L mylist.asm
6502.Net.exe myasm.asm --no-assembly --list=mylist.asm
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Option</b></td><td><code>-d</code></td></tr>
<tr><td><b>Alias</b></td><td><code>--no-disassembly</code></td></tr>
<tr><td><b>Definition</b></td><td>Suppress disassembly from assembly listing.</td></tr>
<tr><td><b>Parameter</b></td><td>None</td></tr>
<tr><td><b>Example</b></td><td>
<pre>
6502.Net.exe myasm.asm -d -L mylist.asm
6502.Net.exe myasm.asm --no-disassembly --list=mylist.asm
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Option</b></td><td><code>-s</code></td></tr>
<tr><td><b>Alias</b></td><td><code>--no-source</code></td></tr>
<tr><td><b>Definition</b></td><td>Do not list original source in the assembly listing.</td></tr>
<tr><td><b>Parameter</b></td><td>None</td></tr>
<tr><td><b>Example</b></td><td>
<pre>
6502.Net.exe myasm.asm -s -L mylist.asm
6502.Net.exe myasm.asm --no-source --list=mylist.asm
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Option</b></td><td><code>--verbose-asm</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Make the assembly listing verbose. If the verbose option is set then all non-assembled lines are included, such as blocks and comment blocks.</td></tr>
<tr><td><b>Parameter</b></td><td>None</td></tr>
<tr><td><b>Example</b></td><td>
<pre>6502.Net.exe myasm.asm --verbose-asm -L myverboselist.asm</pre>
</td></tr>
</table>
<table>
<tr><td><b>Option</b></td><td><code>-V</code></td></tr>
<tr><td><b>Alias</b></td><td><code>--version</code></td></tr>
<tr><td><b>Definition</b></td><td>Print the current version of 6502.Net to console output.</td></tr>
<tr><td><b>Parameter</b></td><td>None</td></tr>
<tr><td><b>Example</b></td><td>
<pre>
6502.Net.exe -V
6502.Net.exe --version
</pre>
</td></tr>
</table>

### 6502 Illegal Mnemonics

When setting the `.cpu` directive to `"6502i"`, the various unpublished and so-called illegal instructions can be assembled. Since they are technically undocumented, mnemonics for illegal instructions vary among assemblers. 6502.Net closely follows those used by [VICE](http://vice-emu.sourceforge.net/), a popular Commodore 64 emulator. Illegal mnemonics, operations and opcodes are as follows:

<table>
<tr>
<td>
<table>
<tr><th>Mnemonic</th><th>Addressing Mode</th><th>Opcode</th></tr>
<tr><td>ANC</td><td>Immediate             </td><td>2B</td></tr>
<tr><td>ANE</td><td>Immediate             </td><td>8B</td></tr>
<tr><td>ARR</td><td>Immediate             </td><td>6B</td></tr>
<tr><td>ASR</td><td>Immediate             </td><td>4B</td></tr>
<tr><td>DCP</td><td>Indexed Indirect      </td><td>C3</td></tr>
<tr><td>DCP</td><td>Zero-Page             </td><td>C7</td></tr>
<tr><td>DCP</td><td>Absolute              </td><td>CF</td></tr>
<tr><td>DCP</td><td>Indirect Indexed      </td><td>D3</td></tr>
<tr><td>DCP</td><td>Zero-Page Indexed X   </td><td>D7</td></tr>
<tr><td>DCP</td><td>Absolute Indexed Y    </td><td>DB</td></tr>
<tr><td>DCP</td><td>Absolute Indexed X    </td><td>DF</td></tr>
<tr><td>DOP</td><td>Implied/Immediate     </td><td>80</td></tr>
<tr><td>ISB</td><td>Indexed Indirect      </td><td>E3</td></tr>
<tr><td>ISB</td><td>Zero-Page             </td><td>E7</td></tr>
<tr><td>ISB</td><td>Absolute              </td><td>EF</td></tr>
<tr><td>ISB</td><td>Indirect Indexed      </td><td>F3</td></tr>
<tr><td>ISB</td><td>Zero-Page Indexed X   </td><td>F7</td></tr>
<tr><td>ISB</td><td>Absolute Indexed Y    </td><td>FB</td></tr>
<tr><td>ISB</td><td>Absolute Indexed X    </td><td>FF</td></tr>
<tr><td>JAM*</td><td>Implied               </td><td>02</td></tr>
<tr><td>LAS</td><td>Absolute Indexed Y    </td><td>BB</td></tr>
<tr><td>LAX</td><td>Indexed Indirect      </td><td>A3</td></tr>
<tr><td>LAX</td><td>Zero-Page             </td><td>A7</td></tr>
<tr><td>LAX</td><td>Absolute              </td><td>AF</td></tr>
<tr><td>LAX</td><td>Indirect Indexed      </td><td>B3</td></tr>
<tr><td>LAX</td><td>Zero-Page Indexed X   </td><td>B7</td></tr>
<tr><td>LAX</td><td>Absolute Indexed Y    </td><td>BF</td></tr>  
<tr><td>RLA</td><td>Indexed Indirect      </td><td>23</td></tr>
<tr><td>RLA</td><td>Zero-Page             </td><td>27</td></tr>
<tr><td>RLA</td><td>Absolute              </td><td>2F</td></tr>
<tr><td>RLA</td><td>Indirect Indexed      </td><td>33</td></tr>
<tr><td>RLA</td><td>Zero-Page Indexed X   </td><td>37</td></tr>
<tr><td>RLA</td><td>Absolute Indexed Y    </td><td>3B</td></tr>
</table>
</td>
<td>
<table>
<tr><th>Mnemonic</th><th>Addressing Mode</th><th>Opcode</th></tr>
<tr><td>RLA</td><td>Absolute Indexed X    </td><td>3F</td></tr>
<tr><td>RRA</td><td>Indexed Indirect      </td><td>63</td></tr>
<tr><td>RRA</td><td>Zero-Page             </td><td>67</td></tr>
<tr><td>RRA</td><td>Absolute              </td><td>6F</td></tr>
<tr><td>RRA</td><td>Indirect Indexed      </td><td>73</td></tr>
<tr><td>RRA</td><td>Zero-Page Indexed X   </td><td>77</td></tr>
<tr><td>RRA</td><td>Absolute Indexed Y    </td><td>7B</td></tr>
<tr><td>RRA</td><td>Absolute Indexed X    </td><td>7F</td></tr>
<tr><td>SAX</td><td>Indexed Indirect      </td><td>83</td></tr>
<tr><td>SAX</td><td>Zero-Page             </td><td>87</td></tr>
<tr><td>SAX</td><td>Absolute              </td><td>8F</td></tr>
<tr><td>SAX</td><td>Zero-Page Indexed X   </td><td>97</td></tr>
<tr><td>SAX</td><td>Absolute Indexed Y    </td><td>9B</td></tr>
<tr><td>SHX</td><td>Absolute Indexed Y    </td><td>9E</td></tr>
<tr><td>SHY</td><td>Absolute Indexed X    </td><td>9C</td></tr>
<tr><td>SLO</td><td>Indexed Indirect      </td><td>03</td></tr>
<tr><td>SLO</td><td>Zero-Page             </td><td>07</td></tr>
<tr><td>SLO</td><td>Absolute              </td><td>0F</td></tr>
<tr><td>SLO</td><td>Indirect Indexed      </td><td>13</td></tr>
<tr><td>SLO</td><td>Zero-Page Indexed X   </td><td>17</td></tr>
<tr><td>SLO</td><td>Absolute Indexed Y    </td><td>1B</td></tr>
<tr><td>SLO</td><td>Absolute Indexed X    </td><td>1F</td></tr>
<tr><td>SRE</td><td>Indexed Indirect      </td><td>43</td></tr>
<tr><td>SRE</td><td>Zero-Page             </td><td>47</td></tr>
<tr><td>SRE</td><td>Absolute              </td><td>4F</td></tr>
<tr><td>SRE</td><td>Indirect Indexed      </td><td>53</td></tr>
<tr><td>SRE</td><td>Zero-Page Indexed X   </td><td>57</td></tr>
<tr><td>SRE</td><td>Absolute Indexed Y    </td><td>5B</td></tr>
<tr><td>SRE</td><td>Absolute Indexed X    </td><td>5F</td></tr>
<tr><td>STP*</td><td>Implied</td><td>12</td></tr>
<tr><td>TAS</td><td>Absolute Indexed Y    </td><td>9B</td></tr>
<tr><td>TOP</td><td>Immediate/Absolute    </td><td>0C</td></tr>
<tr><td>TOP</td><td>Absolute Indexed X    </td><td>1C</td></tr>
</table>
</td>
</tr>
</table>

*-`JAM` and `STP` are essentially the same command; they both halt the CPU.

### Reserved Words

All directives and mnemonics are reserved, and cannot be used for symbol names, such as variables or functions. In addition, the following are considered reserved if the assembler is in 6502 mode:

```
x,s,y
```

In Z80 mode, the following are reserved:

```
af,b,bc,c,d,de,e,h,hl,i,ix,ixh,ixl,iy,iyh,iyl,l,m,nc,nz,p,e,po,r
```

In either mode, the following are reserved:

```
a,false,int8_min,int8_max,int16_min,int16_max,int24_min,int24_max,int32_min,int32_max,math_e,
math_pi,sp,true,uint8_min,uint8_max,uint16_min,uint16_max,uint24_min,uint24_max,uint32_min,uint32_max,z
```

## Licensing and Legal

(C) 2017-2020 informedcitizenry <informedcitizenry@gmail.com>. Licensed under the MIT license. See LICENSE for full license information.