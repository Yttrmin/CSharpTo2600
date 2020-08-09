.cpu "6502"
.format "flat"
* = $F000

BackgroundColor = $80
INT_Byte_SIZE = 1

VSYNC = $00
VBLANK = $01
WSYNC = $02
COLUBK = $09
TIM64T = $296
INTIM = $284

push .macro address, size
	.for i = \address, i <= \address + (\size - 1), i = i + 1
		LDA i
		PHA
	.next
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
	.copyTo BackgroundColor, COLUBK, INT_Byte_SIZE

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