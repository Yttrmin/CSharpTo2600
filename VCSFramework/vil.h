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

// @GENERATE @PUSH=type;size @OPTIONALINSTPARAM
// Pushes {size} bytes starting at {address} onto the stack.
// Effects: STACK+1, AccChange
pushGlobal .macro global, type, size
	.for i = \global, i <= \global + (\size - 1), i = i + 1
		LDA i
		PHA
	.next
.endmacro

// @GENERATE @PUSH=pointerType;pointerSize
pushAddressOfGlobal .macro global, pointerType, pointerSize
	.invoke assertIsPointer(\pointerType)
	.errorif \pointerSize != 1, "Only zero-page pointers are supported for pushAddressOfGlobal"
	LDA #\global
	PHA
.endmacro

// @GENERATE @PUSH=pointerType;pointerSize @DEPRECATED
pushAddressOfLocal .macro local, pointerType, pointerSize
	.invoke assertIsPointer(\pointerType)
	.errorif \pointerSize != 1, "Currently only zero-page pointers are supported for pushAddressOfLocal"
	LDA #\local
	PHA
.endmacro

// @GENERATE @POP=1 @PUSH=pointerType;pointerStackSize
pushAddressOfField .macro offsetConstant, pointerType, pointerStackSize
	.invoke assertIsPointer(\pointerType)
	.errorif \pointerStackSize != 1, "Currently only zero-page pointers are supported for pushAddressOfField"
	PLA
	CLC
	ADC \offsetConstant
	PHA
.endmacro

// @GENERATE @COMPOSITE @PUSH=getPointerFromType(referentType);longPtr
pushAddressOfRomDataElementFromConstant .macro romDataGlobal, referentType, referentTypeSize, indexConstant
	.let address = \romDataGlobal + (\referentTypeSize * \indexConstant)
	.let lsb = address & $FF
	.let msb = (address >> 8) & $FF
	LDA #msb
	PHA
	LDA #lsb
	PHA
.endmacro

// @GENERATE @COMPOSITE @POP=1 @PUSH=getPointerFromType(referentType);longPtr
pushAddressOfRomDataElementFromStack .macro romDataGlobal, referentType, referentTypeSize
	.if \referentTypeSize == 1
		.let address = \romDataGlobal
		.let lsb = address & $FF
		.let msb = (address >> 8) & $FF
		PLA
		CLC
		ADC #lsb
		TAX
		LDA #msb
		ADC #0
		PHA
		TXA
		PHA
	.endif
	.if \referentTypeSize != 1
		// Sizes >1 will cause problems due to the lack of any multiplication instructions.
		// Sizes that can be represented as powers of 2 are fairly easy (just ASL as needed), but still wastes cycles.
		// The optimal way to solve this would be with a strength reduction optimization (replace loop var logic with an increment by element size),
		// but that's a very different class of optimization than what we've been doing so far.
		.error "Only referentTypeSize of 1 is currently supported for pushAddressOfRomDataElementFromStack"
	.endif
.endmacro

// @GENERATE @RESERVED=2 @POP=1 @PUSH=type;size
pushDereferenceFromStack .macro pointerStackSize, type, size
	.if \pointerStackSize == 1
		PLA
		TAX
		.for i = 0, i < \size, i = i + 1
			LDA i,X
			PHA
		.next
	.endif
	// @TODO @BUG - Again, elif doesn't work as expected.
	.if \pointerStackSize == 2
		PLA
		STA INTERNAL_RESERVED_0
		PLA
		STA INTERNAL_RESERVED_1
		LDY #0
		.for i = 0, i < \size, i = i + 1
			LDA (INTERNAL_RESERVED_0),Y
			PHA
			INY
		.next
	.endif
.endmacro

// @GENERATE @RESERVED=2 @POP=1 @PUSH=fieldType;fieldSize
pushFieldFromStack .macro offsetConstant, fieldType, fieldSize, stackType, stackSize
	.if isPointer(\stackType) == true
		.errorif \fieldSize != 1, "Currently, only 1-byte types are allowed for pushFieldFromStack"
		.if \stackSize == 1
			PLA
			TAX
			LDA \offsetConstant,X
			PHA
		.endif
		.if \stackSize == 2
			PLA
			STA INTERNAL_RESERVED_0
			PLA
			STA INTERNAL_RESERVED_1
			LDY \offsetConstant
			LDA (INTERNAL_RESERVED_0),Y
			PHA
		.endif
	.else
		// Note even if we're only fetching a 1-byte field off a 10-byte instance, we still gotta clear the whole
		// thing off of the stack.
		// Could do some scary stuff like determine start of the field, pop the whole instance, then start pushing the
		// now-deallocated memory onto the stack. Making sure it's done in an order/direction that we don't overwrite
		// what we need to read. Would avoid the need of a temporary location to hold the field.
		TSX
		.for i = 0, i < \stackSize, i = i + 1
			PLA
		.next
		.for u = 0, u < \fieldSize, u = u + 1
			.let aaa = \stackSize
			.let bbb = \offsetConstant // @TODO @REPORTME - If we try do these directly in the LDA or a single .let it fails.
			LDA aaa - bbb - u,X
			PHA
		.next
	.endif
.endmacro

// @GENERATE @POP=2
popToFieldFromStack .macro offsetConstant, fieldType, fieldSize, pointerStackType, pointerStackSize
	.invoke assertIsPointer(\pointerStackType)
	.errorif \pointerStackSize != 1, "Only zero-page pointers are allowed for popToFieldFromStack"
	// Value is popped first, then address
	.if \fieldSize == 1 && \pointerStackSize == 1
		PLA
		TAY
		PLA
		TAX
		STY \offsetConstant,X
	.endif
	.if \fieldSize != 1 && \pointerStackSize == 1 // @TODO @REPORTME - Really doesn't like elif
		TSX // Use stack pointer to skip over value in stack. Remember SP points to NEXT stack location, not current (so add 1).
		LDA \fieldSize + 1,X
		TAX // Fetch pointer and store in X.
		.for i = \fieldSize - 1, i >= 0, i = i - 1
			PLA
			STA \offsetConstant + i,X
		.next
		PLA // Pop pointer.
	.endif
.endmacro

// @GENERATE @POP=1
popStack .macro stackSize
	.for i = 0, i < \stackSize, i = i + 1
		PLA
	.next
.endmacro

// @GENERATE @POP=1
initializeObject .macro size, pointerStackSize
	.errorif \pointerStackSize != 1, "Only zero-page pointers are allowed for initializeObject"
	PLA
	TAX
	LDA #0
	.for i = 0, i < \size, i = i + 1
		STA i,X
	.next
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
popToRegister .macro registerConstant, stackType
	.errorif \stackType != TYPE_System_Byte, "Only 'byte's can be directly popped to a register"
	// @TODO @REPORTME - if/elif doesn't work, have to use multiple if instead.
	.if \registerConstant == 0
		PLA
	.endif
	.if \registerConstant == 1
		PLA
		TAX
	.endif
	.if \registerConstant == 2
		PLA
		TAY
	.endif
	.if \registerConstant != 0 && \registerConstant != 1 && \registerConstant != 2
		.error format("Unknown register index: {0}", \registerConstant)
	.endif
.endmacro

// @GENERATE @POP=1 @OPTIONALINSTPARAM
// Pops {globalSize} bytes off the stack and stores them at {targetAddress}.
// Effects: STACK-1, AccChange, MemChange
popToGlobal .macro global, globalType, globalSize, stackType, stackSize
	// CIL may do things like push a byte and pop to a bool. If we're storing
	// to a built-in short int assume this is correct and just pop the
	// last x bytes.
	.if isPointer(\globalType) && isPointer(\stackType) && (\globalSize != \stackSize)
		// Instance methods could be called on both a RAM and ROM object. RAM would involve a short pointer, ROM a long pointer.
		// We only have 1 address for both, so there will be situations we have to pop a short pointer into a long pointer-sized global.
		.errorif \globalType < \stackType, "Attempted to pop a long pointer into a short pointer global."
		// @TODO - Shouldn't this be stored little-endian instead? May have an endian problem in another macro that this is masking.
		LDA #0
		STA \global
		PLA
		STA \global+1
	.else
		.errorif \globalSize != \stackSize, "Global/stack size mismatch for popToGlobal"
		.for i = \global + \globalSize - 1, i >= \global, i = i - 1
			PLA
			STA i
		.next
	.endif
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

// @GENERATE
getAddResultType .function firstOperandTypeExpression, secondOperandTypeExpression
	.if firstOperandTypeExpression == TYPE_System_Byte && secondOperandTypeExpression == TYPE_System_Byte
		.return TYPE_System_Byte
	.else
		.error "Unsupported add types"
	.endif
.endfunction

// @GENERATE
getBitOpResultType .function firstOperandTypeExpression, secondOperandTypeExpression
	.if firstOperandTypeExpression == TYPE_System_Boolean && secondOperandTypeExpression == TYPE_System_Boolean
		.return TYPE_System_Boolean
	.else
		.error "Unsupported bit op types"
	.endif
.endfunction

getTypeFromPointer .function pointerType
	.return pointerType - 1
.endfunction

// @GENERATE
getPointerFromType .function referentType
	.assert isPointer(referentType) == false, "Called getPointerFromType with a pointer"
	.return referentType + 1
.endfunction

assertIsPointer .function type
	.assert isPointer(type) == true, "assertIsPointer failed"
.endfunction

isPointer .function type
	.return (type & 1) == 1
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

// @GENERATE
// Could be either a label or a stack type array access. No discriminated unions, so just drop it to IExpression.
getSizeFromBuiltInType .function typeExpression
	// @TODO - When we accidentally fed a SIZE_foo value of size 1 here, the result was a bool for some reason. Some sort of assembler issue.
	.if typeExpression == TYPE_System_Byte
		.return SIZE_System_Byte
	.elseif typeExpression == TYPE_System_Boolean
		.return SIZE_System_Boolean
	.else
		.error "Unknown builtin type"
	.endif 
.endfunction

// @GENERATE @RESERVED=1 @PUSH=getAddResultType(firstOperandStackType,secondOperandStackType);getSizeFromBuiltInType(type[0]) @POP=2
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

// @GENERATE @COMPOSITE @PUSH=getAddResultType(globalType,constantType);getSizeFromBuiltInType(type[0])
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

// @GENERATE @RESERVED=1 @POP=2 @PUSH=getAddResultType(firstOperandStackType,secondOperandStackType);getSizeFromBuiltInType(type[0])
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

// @GENERATE
// Primitive, also optimizable.
// addFromAddressesToAddress + copyTo = addFromAddressesToAddress + storeTo iff ToAddress target == copyTo source.
storeTo .macro global
	STA \global
.endmacro

//

// @GENERATE
// Primitive
branch .macro branchTarget
	JMP \branchTarget
.endmacro

convertToByte .macro
	// @TODO
.endmacro

// @GENERATE @RESERVED=1 @POP=2 @PUSH=getBitOpResultType(firstOperandStackType,secondOperandStackType);getSizeFromBuiltInType(type[0])
orFromStack .macro firstOperandStackType, firstOperandStackSize, secondOperandStackType, secondOperandStackSize
	.errorIf \firstOperandStackType != \secondOperandStackType, "Currently types must be the same for orFromStack"
	.errorIf \firstOperandStackSize != 1, "Currently operands must be 1 byte in size for orFromStack"
	PLA
	STA INTERNAL_RESERVED_0
	PLA
	ORA INTERNAL_RESERVED_0
	PHA
.endmacro

// @GENERATE @RESERVED=1 @POP=2 @PUSH=bool;bool
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
	.else
		LDA \address
		CMP \constant
		BEQ +
		JMP \branchTarget
	.endif
+
.endmacro

// @GENERATE @POP=2 @RESERVED=1
branchIfLessThanFromStack .macro branchTarget
	// @TODO - Parameter sizes
	// Pop value2 into RESERVED. Pop value1 into A. Branch if value1 < value2.
	PLA
	STA INTERNAL_RESERVED_0
	PLA
	CMP INTERNAL_RESERVED_0

	BCC \branchTarget
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
branchFalseFromStack .macro branchTarget
	PLA
	JEQ \branchTarget
.endmacro

// @GENERATE @POP=1
// Primitive
branchTrueFromStack .macro branchTarget
	PLA
	JNE \branchTarget
.endmacro

// pushLocal + branchTrueFromStack
branchTrueFromLocal .macro local, branchTarget
	LDA \local
	BNE \branchTarget
.endmacro


// @GENERATE @PUSH=type;size
//@TODO Check if messed up endianness.
//@TODO - Delete type?
pushConstant .macro constant, type, size
	.errorif \size > 2, "REPORTME: Bitshifting constants that are >16-bit produces unexpected results."
	.for i = 0, i < \size, i = i + 1
		.let itrByte = (\constant >> (i * 8)) & $FF
		LDA #itrByte
		PHA
	.next
.endmacro

// @GENERATE @PUSH=type;size @DEPRECATED="Use pushGlobal"
// Primitive
pushLocal .macro local, type, size
	.pushGlobal \local, \type, \size
.endmacro

// @GENERATE @POP=1 @DEPRECATED="Use popToGlobal"
// Primitive
popToLocal .macro local, localType, localSize, stackType, stackSize
	.popToGlobal \local, \localType, \localSize, \stackType, \stackSize
.endmacro

// @GENERATE @COMPOSITE
// pushConstant + popToGlobal
assignConstantToGlobal .macro constant, global, size
	//@TODO SIZE
	.errorif \size > 1, "assignConstantToGlobal only supports 8-bit constants currently."
	LDA #\constant
	STA \global
.endmacro

// pushConstant + popToLocal
assignConstantToLocal .macro value, address, size
	.assignConstantToGlobal \value, \address, \size
.endmacro

// @GENERATE @PUSH=stackType;stackSize
duplicate .macro stackType, stackSize
	.errorif \stackSize != 1, "duplicate currently only supports 1-byte dups."
	// We pull first since it's not guaranteed that the last operation ended in a push (which
	// would mean the accumulator contains the pushed value).
	PLA
	PHA
	PHA
.endmacro

// @GENERATE
callMethod .macro method
	JSR \method
.endmacro

// @GENERATE @DEPRECATED
callVoid .macro method
	JSR \method
.endmacro

//callVoidTODO .macro method saveCallerFramePointerConstant, setupCalleeFramePointerConstant
//	.if setupCalleeFramePointerConstant == true
//		TSX
//		STX INTERNAL_FRAME_POINTER
//	.endif
//	.if saveCallerFramePointerConstant == true
//		LDA INTERNAL_FRAME_POINTER
//		PHA
//	.endif
//	JSR \method
//.endmacro

// @GENERATE @PUSH=resultType;resultSize @DEPRECATED
callNonVoid .macro method, resultType, resultSize
	// Allocate space for the return value.
	.for i = 0, i < \resultSize, i = i + 1
		PHA
	.next
	JSR \method
.endmacro

// @GENERATE
returnFromMethod .macro
	RTS
.endmacro

// @GENERATE @DEPRECATED
returnVoid .macro
	RTS
.endmacro

// @GENERATE @POP=1 @DEPRECATED
returnNonVoid .macro resultType, resultSize
	// Return address is 16-bit.
	.let returnValueStartOffset = 1 + \resultSize + 2
	TSX
	.for i = 0, i < \resultSize, i = i + 1
		PLA
		STA returnValueStartOffset + i,X
	.next
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