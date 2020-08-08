.cpu "6502"
.target "flat"
* = $F000

BackgroundColor = $80

VSYNC = $00
VBLANK = $01
WSYNC = $02
COLUBK = $09
TIM64T = $296
INTIM = $284

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
	LDA BackgroundColor
	STA COLUBK

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