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

// @GENERATE @PUSH=1
// Pushes {size} bytes starting at {address} onto the stack.
// Effects: STACK+1, AccChange
pushGlobal .macro global, type, size
	.for i = \global, i <= \global + (\size - 1), i = i + 1
		LDA i
		PHA
	.next
.endmacro

// @GENERATE @PUSH=1
pushAddressOfGlobal .macro global, globalType
	LDA #\global
	PHA
.endmacro

// @GENERATE @POP=1 @PUSH=1
pushAddressOfField .macro offsetConstant, fieldType
	PLA
	CLC
	ADC \offsetConstant
	PHA
.endmacro

// @GENERATE @POP=1 @PUSH=1
pushDereferenceFromStack .macro type, size
	// Need to figure out the non-pointer version of what this pointer is pointing to.
	.errorif \size != 1, "Currently, only 1-byte sizes are supported for pushDereferenceFromStack"
	PLA
	TAX
	LDA 0,X
	PHA
.endmacro

// @GENERATE @POP=2
popToAddressFromStack .macro type, size
	// The value comes before the address when popping, which makes this WAY harder
	// than it has to be.
	.errorif \size != 1, "Currently, only 1-byte sizes are supported for popToAddressFromStack"
	.if \size == 1
		PLA
		TAY // Stick value in Y.
		PLA
		TAX // Stick address in X.
		STY 0,X
	.endif
.endmacro

// @GENERATE @POP=1
// Pops {globalSize} bytes off the stack and stores them at {targetAddress}.
// Effects: STACK-1, AccChange, MemChange
popToGlobal .macro global, globalType, globalSize, stackType, stackSize
	// CIL may do things like push a byte and pop to a bool. If we're storing
	// to a built-in short int assume this is correct and just pop the
	// last x bytes.
	.if isShortInteger(\globalType) == true
	.else
		.errorif \globalType != \stackType, "popToGlobal to @{global}: type mismatch."
	.endif
	.errorif \globalSize != 1, "size not 1"
	.errorif \stackSize != 1, "size not 1"
	.for i = \global + \globalSize - 1, i >= \global, i = i - 1
		PLA
		STA \global
	.next
.endmacro

// @GENERATE @COMPOSITE
// Can replace a consecutive ".push(...) .popTo(...)" as an optimization.
// Copies directly between 2 addresses without using PHA/PLA
copyGlobalToGlobal .macro fromGlobal, fromSize, toGlobal, toSize
	.errorif \fromSize != \toSize, "Sizes currently need to match for copyGlobalToGlobal."
	.for i = 0, i < \fromSize, i = i + 1
		.let source = \fromGlobal + i
		.let destination = \toGlobal + i
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

getBitOpResultType .function firstOperandType, secondOperandType
	.if firstOperandType == TYPE_System_Boolean && secondOperandType == TYPE_System_Boolean
		.return TYPE_System_Boolean
	.else
		.error "Unsupported bit op types"
	.endif
.endfunction

/*
Numeric data types
  Short integers
    Storing to integers, booleans, and characters (stloc, stfld, stind.i1, stelem.i2, etc.)
    truncates. Use the conv.ovf.* instructions to detect when this truncation results in a
    value that doesn’t correctly represent the original value.
	...
	Assignment to a local (stloc) or argument (starg) whose type is declared to be
    a short integer type automatically truncates to the size specified for the local
    or argument. 
*/
isShortInteger .function type
	.return type == TYPE_System_Byte || type == TYPE_System_Boolean
.endfunction

getSizeFromBuiltInType .function type
	.if type == TYPE_System_Byte
		.return SIZE_System_Byte
	.else if type == TYPE_System_Boolean
		.return SIZE_System_Boolean
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

// @GENERATE @COMPOSITE @PUSH=1
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

// @GENERATE @COMPOSITE
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
		// ^^ 10 cycles
	.else
	.endif
.endmacro

// @GENERATE @COMPOSITE
// .addFromGlobalAndConstantToGlobal iff sourceGlobal==targetGlobal AND constant==(1 OR 2)
incrementGlobal .macro global, globalType, globalSize
	.errorif \globalSize != 1, ">1-byte increment not supported yet for incrementGlobal"
	INC \global // 5 cycles
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

// @GENERATE @POP=2 @PUSH=1
// Primitive
subFromStack .macro firstOperandStackType, firstOperandStackSize, secondOperandStackType, secondOperandStackSize
	.invoke getAddResultType(\firstOperandStackType, \secondOperandStackType) // @TODO - Does this apply to add+sub?
	.if \firstOperandStackSize == 1 && \secondOperandStackSize == 1
		PLA
		STA INTERNAL_RESERVED_0
		PLA
		SEC
		SBC INTERNAL_RESERVED_0
		PHA
	.else
		.error "Invalid subFromStack param sizes"
	.endif
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

// @GENERATE
// Primitive
branch .macro instruction
	JMP \instruction
.endmacro

convertToByte .macro
	// @TODO
.endmacro

// @GENERATE @POP=2 @PUSH=1
orFromStack .macro firstOperandStackType, firstOperandStackSize, secondOperandStackType, secondOperandStackSize
	.errorIf \firstOperandStackType != \secondOperandStackType, "Currently types must be the same for orFromStack"
	.errorIf \firstOperandStackSize != 1, "Currently operands must be 1 byte in size for orFromStack"
	PLA
	STA INTERNAL_RESERVED_0
	PLA
	ORA INTERNAL_RESERVED_0
	PHA
.endmacro

// @GENERATE @POP=2 @PUSH=1
compareEqualToFromStack .macro firstOperandStackType, firstOperandStackSize, secondOperandStackType, secondOperandStackSize
	.errorIf \firstOperandStackType != \secondOperandStackType, "Currently types must be the same for compareEqualToFromStack"
	.errorIf \firstOperandStackSize != 1, "Currently operands must be 1 byte in size for compareEqualToFromStack"
	PLA
	STA INTERNAL_RESERVED_0
	PLA
	CMP INTERNAL_RESERVED_0
	BEQ _true
	LDA #0
	BEQ _end

_true
	LDA #1

_end
	PHA
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

// @GENERATE @POP=1
branchFalseFromStack .macro instruction
	PLA
	JEQ \instruction
.endmacro

// @GENERATE @POP=1
// Primitive
branchTrueFromStack .macro instruction
	PLA
	JNE \instruction
.endmacro

// pushLocal + branchTrueFromStack
branchTrueFromLocal .macro local, instruction
	LDA \local
	BNE \instruction
.endmacro


// @GENERATE @PUSH=1
//@TODO Check if messed up endianness.
//@TODO - Delete type?
pushConstant .macro constant, type, size
	.errorif \size > 2, "REPORTME: Bitshifting constants that are >16-bit produces unexpected results."
	.for i = 0, i < \size, i = i + 1
		.let byte = (\constant >> (i * 8)) & $FF
		LDA #byte
		PHA
	.next
.endmacro

// @GENERATE @PUSH=1
// Primitive
pushLocal .macro local, type, size
	.pushGlobal \local, \type, \size
.endmacro

// @GENERATE @POP=1
// Primitive
popToLocal .macro local, localType, localSize, stackType, stackSize
	.popToGlobal \local, \localType, \localSize, \stackType, \stackSize
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

// @GENERATE @PUSH=1
duplicate .macro stackType, stackSize
	.errorif \stackSize != 1, "duplicate currently only supports 1-byte dups."
	// We pull first since it's not guaranteed that the last operation ended in a push (which
	// would mean the accumulator contains the pushed value).
	PLA
	PHA
	PHA
.endmacro

// @GENERATE
callVoid .macro method
	JSR \method
.endmacro

// @GENERATE
returnFromCall .macro
	RTS
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