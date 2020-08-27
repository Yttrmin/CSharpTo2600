.cpu "6502"
.format "flat"
* = $F000

COLUBK = $09
INT_RESERVED_0 = $80
BackgroundColor = $81
TYPE_VCSFramework_V2_Nothing = 0
SIZE_VCSFramework_V2_Nothing = 0
TYPE_System_Byte = 100
SIZE_System_Byte = 1

.include "../../../VCSCompiler/vil.h"

// Repeatedly increments COLUBK without making any effort to handle scanlines, perform VSYNC,
// etc. Result is a rapidly scrolling screen that changes color multiple times mid-scanline.
// Only tested on emulator, not on TVs.
// This is not a proper VCS program. It's just meant to see how stripped down of a program
// we can make that still displays something on the screen.
Start 
	SEI
	CLD
	// Not doing any scanline/VSYNC/VBLANK/anything handling messes up the NTSC/PAL/SECAM
	// detection, so use $0E, which shows up as white for all of them.
	LDA #0
	STA BackgroundColor
MainLoop
	.let STACK_TYPEOF = [TYPE_VCSFramework_V2_Nothing, TYPE_VCSFramework_V2_Nothing]
	.let STACK_SIZEOF = [SIZE_VCSFramework_V2_Nothing, SIZE_VCSFramework_V2_Nothing]
	.pushGlobal BackgroundColor, TYPE_System_Byte, SIZE_System_Byte
	.let STACK_TYPEOF = [TYPE_System_Byte, STACK_TYPEOF[0]]
	.let STACK_SIZEOF = [SIZE_System_Byte, STACK_SIZEOF[0]]
	.pushConstant 1, TYPE_System_Byte, SIZE_System_Byte
	.let STACK_TYPEOF = [TYPE_System_Byte, STACK_TYPEOF[0]]
	.let STACK_SIZEOF = [SIZE_System_Byte, STACK_SIZEOF[0]]
	.addFromStack STACK_TYPEOF[0], STACK_SIZEOF[0], STACK_TYPEOF[1], STACK_SIZEOF[1]
	.let STACK_TYPEOF = [getAddResultType(STACK_TYPEOF[0], STACK_TYPEOF[1]), TYPE_VCSFramework_V2_Nothing]
	.let STACK_SIZEOF = [getSizeFromBuiltInType(STACK_TYPEOF[0]), SIZE_VCSFramework_V2_Nothing]
	PLA
	STA BackgroundColor
	STA COLUBK
	JMP MainLoop

// Special memory locations. Tells the 6502 where to go.
* = $FFFC
.word Start
.word Start