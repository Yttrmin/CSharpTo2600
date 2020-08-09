.cpu "6502"
.format "flat"
* = $F000

INT_RESERVED_1 = $80
INT_RESERVED_2 = $81
BackgroundColor = $82
Increment = $84
INT_Byte_SIZE = 1

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

push .macro address, size
	.for i = \address, i <= \address + (\size - 1), i = i + 1
		LDA i
		PHA
		.let OPERAND_1 = INT_Byte_SIZE
	.next
	OPERAND_1 = 1
	OPERAND_2 = 1
.endmacro

popTo .macro address, size
	.for i = \address + (\size - 1), i >= \address, i = i - 1
		PLA
		STA i
	.next
.endmacro

// Can replace a consecutive ".push(...) .popTo(...)" as an optimization.
// Copies directly between 2 addresses without using PHA/PLA
copyTo .macro fromAddress, toAddress, size
	.for i = 0, i < \size, i = i + 1
		.let source = \fromAddress + i
		.let destination = \toAddress + i
		LDA source
		STA destination
	.next
.endmacro

addFromStack .macro
	.assert OPERAND_1 > 0, "OPERAND_1 size not valid, this is very bad."
	.assert OPERAND_2 > 0, "OPERAND_2 size not valid, this is very bad."
	.errorif OPERAND_1 != OPERAND_2, "Differing operand sizes not yet supported for addFromStack."
	.if OPERAND_1 == 1 && OPERAND_2 == 1
		PLA
		STA INT_RESERVED_1
		PLA
		CLC
		ADC INT_RESERVED_1
		PHA
	.else
	.endif
.endmacro

addFromAddresses .macro addressA, sizeA, addressB, sizeB
	.errorif \sizeA != \sizeB, "Differing operand sizes not yet supported for addFromAddresses."
	.if \sizeA == 1 && \sizeB == 1
		LDA \addressA
		CLC
		ADC \addressB
		PHA
	.else
	.endif
.endmacro

addFromAddressesToAddress .macro addressA, sizeA, addressB, sizeB, targetAddress, targetSize
	.errorif \sizeA != \sizeB, "Differing operand sizes not yet supported for addFromAddressesToAddress."
	.errorif \sizeA != \targetSize, "Differing target size not yet supported for addFromAddressesToAddress."
	.if \sizeA == 1 && \sizeB == 1
		LDA \addressA
		CLC
		ADC \addressB
		STA \targetAddress
	.else
	.endif
.endmacro

storeTo .macro address
	STA \address
.endmacro

Start 
	SEI
	CLD
	LDX #$FF
	TXS
	LDA #0
ClearMem
	STA 0,X
	DEX
	BNE ClearMem

	LDA #1
	STA Increment
MainLoop
	LDA #%00000010
	STA VSYNC
	STA WSYNC
	STA WSYNC
	STA WSYNC
	LDA #43
	STA TIM64T
	LDA #0
	STA VSYNC

	INC BackgroundColor
	//.push BackgroundColor, INT_Byte_SIZE
	//.popTo COLUBK, INT_Byte_SIZE
	// Compiler should optimize above to:
	//.copyTo BackgroundColor, COLUBK, INT_Byte_SIZE

	// Let's treat the increment as a variable so we can play around with multi-variable operations:

	// OPERAND_1/2 need to be updated out here. Doing it in the macro doesn't seem to work.
	/*.push BackgroundColor, 1
	OPERAND_1 = OPERAND_2
	OPERAND_2 = 1
	.push Increment, 1
	OPERAND_1 = OPERAND_2
	OPERAND_2 = 1
	.addFromStack
	.popTo BackgroundColor, 1
	.copyTo BackgroundColor, COLUBK, INT_Byte_SIZE*/

	// .push() + .push() + .addFromStack() = .addFromAddresses()
	/*.addFromAddresses BackgroundColor, 1, Increment, 1
	.popTo BackgroundColor, 1
	.copyTo BackgroundColor, COLUBK, INT_Byte_SIZE*/

	// .addFromAddresses() + .popTo() = .addFromAddressesToAddress()
	/*.addFromAddressesToAddress BackgroundColor, 1, Increment, 1, BackgroundColor, 1
	.copyTo BackgroundColor, COLUBK, INT_Byte_SIZE*/

	// .addFromAddressesToAddress() + .copyTo() = .addFromAddressesToAddresss() + .storeTo()
	// This saves an LDA since the result of the ADC is still in the accumulator.
	.addFromAddressesToAddress BackgroundColor, 1, Increment, 1, BackgroundColor, 1
	.storeTo COLUBK

WaitForVBlankEnd
	LDA INTIM
	BNE WaitForVBlankEnd
	STA WSYNC
	STA VBLANK
	LDY #192

ScanLoop
	STA WSYNC
	DEY
	BNE ScanLoop

	LDA #2
	STA WSYNC
	STA VBLANK
	LDY #30
OverScanWait
	STA WSYNC
	DEY
	BNE OverScanWait
	JMP MainLoop

// Special memory locations. Tells the 6502 where to go.
* = $FFFC
.word Start
.word Start