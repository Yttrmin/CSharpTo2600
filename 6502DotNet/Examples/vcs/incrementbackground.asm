.cpu "6502"
.format "flat"
* = $F000

COLUBK = $09

// Repeatedly increments COLUBK without making any effort to handle scanlines, perform VSYNC,
// etc. Result is a rapidly scrolling screen that changes color multiple times mid-scanline.
// Only tested on emulator, not on TVs.
// This is not a proper VCS program. It's just meant to see how stripped down of a program
// we can make that still displays something on the screen.
Start 
	SEI
	CLD
	LDX #0
MainLoop
	STX COLUBK
	INX
	JMP MainLoop

// Special memory locations. Tells the 6502 where to go.
* = $FFFC
.word Start
.word Start