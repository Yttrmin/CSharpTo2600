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

	.pushConstant $1, INT_Byte_SIZE
	.popToGlobal Increment, INT_Byte_SIZE
Main__IL_0007
	.pushConstant $2, 1
	.popToGlobal VSYNC, INT_Byte_SIZE
	.storeTo WSYNC
	.storeTo WSYNC
	.storeTo WSYNC
	.pushConstant 43, 1
	.popToGlobal TIM64T, INT_Byte_SIZE
	.pushConstant 0, 1
	.popToGlobal VSYNC, INT_Byte_SIZE

	.pushGlobal BackgroundColor, 1
	.pushGlobal Increment, 1
	.addFromStack
	.popToGlobal BackgroundColor, 1
	.copyTo BackgroundColor, COLUBK, INT_Byte_SIZE

Main__IL_004e
	.pushGlobal INTIM, INT_Byte_SIZE
	.pushConstant 0, 1
	.compareGreaterThanFromStack //PARAMS
	.popToLocal LOCAL_Main_01, SIZE_LOCAL_Main_01
	.pushLocal LOCAL_Main_01, SIZE_LOCAL_Main_01
	.branchTrueFromStack Main__IL_004e

	.storeTo WSYNC
	.pushConstant 0, 1
	.popToGlobal VBLANK, INT_Byte_SIZE

	.pushConstant 192, 1
	.popToLocal LOCAL_Main_00, SIZE_LOCAL_Main_00
	.branch Main__IL_007d

Main__IL_0070
	.pushLocal LOCAL_Main_00, SIZE_LOCAL_Main_00
	.pushConstant 1, 1
	.subFromStack // PARAMS
	.convertToByte // PARAMS
	.popToLocal LOCAL_Main_00, SIZE_LOCAL_Main_00
	.storeTo WSYNC

Main__IL_007d
	.pushLocal LOCAL_Main_00, SIZE_LOCAL_Main_00
	.pushConstant 0, 1
	.compareGreaterThanFromStack // PARAMS
	.popToLocal LOCAL_Main_02, SIZE_LOCAL_Main_02
	.pushLocal LOCAL_Main_02, SIZE_LOCAL_Main_02
	.branchTrueFromStack Main__IL_0070

	.storeTo WSYNC
	.pushConstant 2, 1
	.popToGlobal VBLANK, INT_Byte_SIZE
	.pushConstant 30, 1
	.popToLocal LOCAL_Main_00, SIZE_LOCAL_Main_00
	.branchTo Main__IL_00a4

Main__IL_0097
	.pushLocal LOCAL_Main_00, SIZE_LOCAL_Main_00
	.pushConstant 1, 1
	.subFromStack // PARAMS
	.convertToByte // PARAMS
	.popToLocal LOCAL_Main_00, SIZE_LOCAL_Main_00
	.storeTo WSYNC

Main__IL_00a4
	.pushLocal LOCAL_Main_00, SIZE_LOCAL_Main_00
	.pushConstant 0, 1
	.compareGreaterThanFromStack // PARAMS
	.popToLocal LOCAL_Main_03, SIZE_LOCAL_Main_03
	.pushLocal LOCAL_Main_03, SIZE_LOCAL_Main_03
	.branchTrueFromStack Main__IL_0097

	.branch Main__IL_0007

// Special memory locations. Tells the 6502 where to go.
* = $FFFC
.word Start
.word Start