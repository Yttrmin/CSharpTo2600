.cpu "6502"
.format "flat"
* = $F000

COLUBK = $09

// Displays a constant white background.
// Only tested on emulator, not on TVs.
// This is not a proper VCS program. It's just meant to see how stripped down of a program
// we can make that still displays something on the screen.
Start 
	SEI
	CLD
	// Not doing any scanline/VSYNC/VBLANK/anything handling messes up the NTSC/PAL/SECAM
	// detection, so use $0E, which shows up as white for all of them.
	LDA #$0E
MainLoop
	STA COLUBK
	JMP MainLoop

// Special memory locations. Tells the 6502 where to go.
* = $FFFC
.word Start
.word Start