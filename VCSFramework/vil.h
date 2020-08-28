﻿// Vcs Intermediate Language (WIP)
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
pushGlobal .macro address, type, size
	.for i = \address, i <= \address + (\size - 1), i = i + 1
		LDA i
		PHA
	.next
.endmacro

// Pops {globalSize} bytes off the stack and stores them at {targetAddress}.
// Effects: STACK-1, AccChange, MemChange
popToGlobal .macro targetAddress, globalType, globalSize, stackType, stackSize
	.errorIf \globalType != \stackType, "popToGlobal to @{targetAddress}: type mismatch."
	.for i = \targetAddress + (\globalSize - 1), i >= \targetAddress, i = i - 1
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

getAddResultType .function firstOperandType, secondOperandType
	.if firstOperandType == TYPE_System_Byte && secondOperandType == TYPE_System_Byte
		.return TYPE_System_Byte
	.else
		.error "Unsupported add types"
	.endif
.endfunction

getSizeFromBuiltInType .function type
	.if type == TYPE_System_Byte
		.return SIZE_System_Byte
	.else
		.error "Unknown builtin type"
	.endif 
.endfunction

// @GENERATE @PUSH=1 @POP=2
// Primitive
addFromStack .macro firstOperandStackType, firstOperandStackSize, secondOperandStackType, secondOperandStackSize
	// @TODO Need to know if this is signed/unsigned addition (pass in arrays?)
	//.assert OPERAND_1 > 0, "OPERAND_1 size not valid, this is very bad."
	//.assert OPERAND_2 > 0, "OPERAND_2 size not valid, this is very bad."
	//.errorif OPERAND_1 != OPERAND_2, "Differing operand sizes not yet supported for addFromStack."
	.invoke getAddResultType(\firstOperandStackType, \secondOperandStackType)
	.if \firstOperandStackSize == 1 && \secondOperandStackSize == 1
		PLA
		STA INTERNAL_RESERVED_0
		PLA
		CLC
		ADC INTERNAL_RESERVED_0
		PHA
	.else
		.error "Invalid addFromStack param sizes"
	.endif
.endmacro

// @GENERATE
// .pushGlobal + .pushConstant + .addFromStack
// OR
// .pushConstant + .pushGlobal + .addFromStack
addFromGlobalAndConstant .macro global, globalType, globalSize, constant, constantType, constantSize
	.errorif \globalSize != \constantSize, "Differing operand sizes not yet supported for addFromGlobalAndConstant."
	.errorif \globalSize != 1, ">1-byte addition not supported yet for addFromGlobalAndConstant"
	.if \globalSize == 1 && \constantSize == 1
		LDA \global
		CLC
		ADC \constant
		PHA
	.else
	.endif
.endmacro

// .addFromGlobalAndConstant + .popToGlobal
addFromGlobalAndConstantToGlobal .macro sourceGlobal, sourceGlobalType, sourceGlobalSize, constant, constantType, constantSize, targetGlobal, targetType, targetSize
	.errorif \sourceGlobalSize != \constantSize, "Differing operand sizes not yet supported for addFromGlobalAndConstantToGlobal."
	.errorif \sourceGlobalSize != 1, ">1-byte increment not supported yet for addFromGlobalAndConstantToGlobal"
	.errorif \targetSize != 1, ">1-byte increment not supported yet for addFromGlobalAndConstantToGlobal"
	.if \sourceGlobalSize == 1 && \constantSize == 1
		LDA \sourceGlobal
		CLC
		ADC \constant
		STA \targetGlobal
	.else
	.endif
.endmacro

// .addFromGlobalAndConstantToGlobal iff sourceGlobal==targetGlobal AND constant==1
incrementGlobal .macro global, globalType, globalSize
	.errorif \globalSize != 1, ">1-byte addition not supported yet for incrementGlobal"
	INC \global
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
	STA INTERNAL_RESERVED_0
	PLA
	SEC
	SBC INTERNAL_RESERVED_0
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
	STA INTERNAL_RESERVED_0
	PLA
	CMP INTERNAL_RESERVED_0
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
//@TODO - Delete type?
pushConstant .macro value, type, size
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

//@GENERATE @NOINSTPARAM
entryPoint .macro
	.initialize
	.clearMemory
.endmacro