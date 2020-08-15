// Vcs Intermediate Language (WIP)
// A macro-based psuedo-intermediate language for making 2600 games.
// The intent is for the compiler to output only calls to these macros, instead of 6502 instructions.

/*

Primitives (generated on first compilation pass):
push
popTo
addFromStack
compareGreaterThan
branchTrueFromStack

Compositions (generated during optimization, never during first pass; replaces Primitives or other Compositions):
addFromAddresses = .push + .push + .addFromStack
addFromAddressesToAddress = .addFromAddresses + .popTo
storeTo = [will have multiple uses] .addFromAddressesToAddress + .copyTo (when .addFromAddressesToAddress:target == .copyTo::src) 
copyTo = .push + .popTo

--

Stack Operations:

push
popTo

Math Operations:

addFromStack
addFromAddresses
addFromAddressesToAddress

To Be Organized:

storeTo
copyTo
*/

// Pushes {size} bytes starting at {address} onto the stack.
// Effects: STACK+1, AccChange
pushGlobal .macro address, size
	.for i = \address, i <= \address + (\size - 1), i = i + 1
		LDA i
		PHA
	.next
.endmacro

// Pops {size} bytes off the stack and stores them at {address}.
// Effects: STACK-1, AccChange, MemChange
popToGlobal .macro address, size
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

// Primitive
addFromStack .macro
	//.assert OPERAND_1 > 0, "OPERAND_1 size not valid, this is very bad."
	//.assert OPERAND_2 > 0, "OPERAND_2 size not valid, this is very bad."
	//.errorif OPERAND_1 != OPERAND_2, "Differing operand sizes not yet supported for addFromStack."
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

// Primitive
subFromStack .macro // @TODO sizes
	PLA
	STA INT_RESERVED_1
	PLA
	SEC
	SBC INT_RESERVED_1
	PHA
.endmacro

// pushLocal + pushConstant + subFromStack
subFromLocalAndConstant .macro local, constant //@TODO SIZES
	// Load into X/Y and DEX/Y and PHA instad? Think it adds to same...
	LDA \local
	SEC
	SBC #\constant
	PHA
.endmacro

// subFromLocalAndConstant + [convertToByte](?) + popToLocal 
//  iff subFromLocalAndConstant and popToLocal use same local.
subLocalByConstant .macro local, constant // @TODO SIZES
	// 10 cycles to do full LDA/SEC/SBC/STA chain.
	// DEC = 5 cycles
	// For some reason assembler doesn't like this.
		//.repeat \constant
		//	DEC \local
		//.endrepeat
	// Also for some reason, a .elif chain ended up always evaluating the .else.
	.if \constant == 0
		NOP
	.endif
	.if \constant == 1
		DEC \local
	.endif
	.if \constant == 2
		DEC \local
		DEC \local
	.endif
	.if \constant > 2
		LDA \local
		SEC
		SBC #\constant
		STA \local
	.endif
.endmacro

// Primitive, also optimizable.
// addFromAddressesToAddress + copyTo = addFromAddressesToAddress + storeTo iff ToAddress target == copyTo source.
storeTo .macro address
	STA \address
.endmacro

//

// Primitive
branch .macro address
	JMP \address
.endmacro

convertToByte .macro
	// @TODO
.endmacro

// Primitive
compareGreaterThanFromStack .macro // @TODO SIZE PARAMS
	PLA
	STA INT_RESERVED_1
	PLA
	CMP INT_RESERVED_1
	// Carry=1 if A >= M, Zero=1 if A = M.
	// Therefore need to check Zero first, then Carry.
	BEQ _false
	BCS _true

_false
	LDA #0
	BEQ _end

_true
	LDA #1

_end
	PHA
.endmacro

// pushGlobal + pushConstant + compareGreaterThanFromStack
compareGreaterThanFromGlobalAndConstant .macro address, constant //@TODO SIZE PARAMS
	.if \constant == 0
		LDA \address
		BEQ +
		LDA #1
	+	PHA
	.else
		LDA \address
		CMP \constant

		BEQ _false
		BCS _true

	_false
		LDA #0
		BEQ _end

	_true
		LDA #1

	_end
		PHA
	.endif
.endmacro

// compareGreaterThanFromGlobalAndConstant + popToLocal
compareGreaterThanFromGlobalAndConstantToLocal .macro address, constant, local //@TODO SIZE PARAMS
	.if \constant == 0
		LDA \address
		BEQ +
		LDA #1
	+	STA \local
	.else
		LDA \address
		CMP \constant

		BEQ _false
		BCS _true

	_false
		LDA #0
		BEQ _end

	_true
		LDA #1

	_end
		STA \local
	.endif
.endmacro

// compareGreaterThanFromGlobalAndConstantToLocal + branchTrueFromLocal iff same locals
branchIfGreaterThanFromGlobalAndConstantToLocal .macro address, constant, local, branchTarget
	.compareGreaterThanFromGlobalAndConstantToLocal \address, \constant, \local
	// This saves 1 instruction, but more importantly, the presence of this macro could
	// mean that the local can be enregistered (if no other uses).
	BNE \branchTarget
.endmacro

// branchIfGreaterThanFromGlobalAndConstantToLocal and local is never read.
branchIfGreaterThanFromGlobalAndConstant .macro address, constant, branchTarget
	.if \constant == 0
		LDA \address
		BEQ +
		JMP \branchTarget
	+
	.else
		LDA \address
		CMP \constant
		BEQ +
		JMP \branchTarget
	+
	.endif
.endmacro

// pushLocal + pushConstant + compareGreaterThanFromStack
compareGreaterThanFromLocalAndConstant .macro local, constant //@TODO SIZE PARAMS
	.compareGreaterThanFromGlobalAndConstant \local, \constant
.endmacro

// compareGreaterThanFromLocalAndConstant + popToLocal
compareGreaterThanFromLocalAndConstantToLocal .macro local, constant, targetLocal //@TODO SIZE
	.if \constant == 0
		LDA \local
		BEQ +
		LDA #1
	+	STA \targetLocal
	.else
		LDA \local
		CMP \constant

		BEQ _false
		BCS _true

	_false
		LDA #0
		BEQ _end

	_true
		LDA #1

	_end
		STA \targetLocal
	.endif
.endmacro

// compareGreaterThanFromLocalAndConstantToLocal + branchTrueFromLocal iff same locals
branchIfGreaterThanFromLocalAndConstantToLocal .macro local, constant, targetLocal, branchTarget
	.compareGreaterThanFromLocalAndConstantToLocal \local, \constant, \targetLocal
	// This saves 1 instruction, but more importantly, the presence of this macro could
	// mean that the local can be enregistered (if no other uses).
	BNE \branchTarget
.endmacro

// Primitive
branchTrueFromStack .macro address
	PLA
	BNE \address
.endmacro

// pushLocal + branchTrueFromStack
branchTrueFromLocal .macro local, address
	LDA \local
	BNE \address
.endmacro

//@TODO Check if messed up endianness.
pushConstant .macro value, size
	.errorif \size > 2, "REPORTME: Bitshifting values that are >16-bit produces unexpected results."
	.for i = 0, i < \size, i = i + 1
		.let byte = (\value >> (i * 8)) & $FF
		LDA #byte
		PHA
	.next
.endmacro

// Primitive
pushLocal .macro address, size
	.pushGlobal \address, \size
.endmacro

// Primitive
popToLocal .macro address, size
	.popToGlobal \address, \size
.endmacro

// pushConstant + popToGlobal
assignConstantToGlobal .macro value, address, size
	//@TODO SIZE
	.errorif \size > 1, "assignConstantToGlobal only supports 8-bit constants currently."
	LDA #\value
	STA \address
.endmacro

// pushConstant + popToLocal
assignConstantToLocal .macro value, address, size
	.assignConstantToGlobal \value, \address, \size
.endmacro

initialize .macro
	SEI
	CLD
	LDX #$FF
	TXS
	LDA #0
.endmacro

clearMemory .macro
-	STA 0,X
	DEX
	BNE -
.endmacro

entryPoint .macro
	.initialize
	.clearMemory
.endmacro