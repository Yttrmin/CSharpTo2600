.cpu "6502"
.format "flat"
* = $F000

INT_RESERVED_1 = $80
INT_RESERVED_2 = $81
BackgroundColor = $82
Increment = $84
INT_Byte_SIZE = 1

// When unoptimized, locals have the same behavior as globals: they occupy their own exclusive address.
LOCAL_Main_00 = $85
LOCAL_Main_01 = $86
LOCAL_Main_02 = $87
LOCAL_Main_03 = $88

SIZE_LOCAL_Main_00 = 1
SIZE_LOCAL_Main_01 = 1
SIZE_LOCAL_Main_02 = 1
SIZE_LOCAL_Main_03 = 1

VSYNC = $00
VBLANK = $01
WSYNC = $02
COLUBK = $09
TIM64T = $296
INTIM = $284

// Used for binary operations. In A - B for example, A = OPERAND_1 and B = OPERAND_2.
// Any operation that pushes will probably need to set these.
// This probably only works if there are no branches between pushing of operands to stack, and performing the operation.
.let OPERAND_1 = -1
.let OPERAND_2 = -1
.let OPERATORS = [-1, -1]

.include "../../../VCSCompiler/vil.h"

Start 
	.initialize
	.clearMemory

	.assignConstantToGlobal $1, Increment, INT_Byte_SIZE
Main__IL_0007
	.assignConstantToGlobal $2, VSYNC, INT_Byte_SIZE
	.storeTo WSYNC
	.storeTo WSYNC
	.storeTo WSYNC
	.assignConstantToGlobal 43, TIM64T, INT_Byte_SIZE
	.assignConstantToGlobal 0, VSYNC, INT_Byte_SIZE

	.addFromAddressesToAddress BackgroundColor, 1, Increment, 1, BackgroundColor, 1
	.storeTo COLUBK

Main__IL_004e
	.compareGreaterThanFromGlobalAndConstantToLocal INTIM, 0, LOCAL_Main_01
	.branchTrueFromLocal LOCAL_Main_01, Main__IL_004e

	.storeTo WSYNC
	.assignConstantToGlobal 0, VBLANK, INT_Byte_SIZE

	.assignConstantToLocal 192, LOCAL_Main_00, INT_Byte_SIZE
	.branch Main__IL_007d

Main__IL_0070
	.subLocalByConstant LOCAL_Main_00, 1
	.storeTo WSYNC

Main__IL_007d
	.compareGreaterThanFromLocalAndConstantToLocal LOCAL_Main_00, 0, LOCAL_Main_02 // SIZE PARAMS
	.branchTrueFromLocal LOCAL_Main_02, MAIN__IL_0070

	.storeTo WSYNC
	.assignConstantToGlobal 2, VBLANK, INT_Byte_SIZE
	.assignConstantToLocal 30, LOCAL_Main_00, INT_Byte_SIZE
	.branch Main__IL_00a4

Main__IL_0097
	.subLocalByConstant LOCAL_Main_00, 1
	.storeTo WSYNC

Main__IL_00a4
	.compareGreaterThanFromLocalAndConstantToLocal LOCAL_Main_00, 0, LOCAL_Main_03
	.pushLocal LOCAL_Main_03, SIZE_LOCAL_Main_03
	.branchTrueFromStack Main__IL_0097

	.branch Main__IL_0007

// Special memory locations. Tells the 6502 where to go.
* = $FFFC
.word Start
.word Start