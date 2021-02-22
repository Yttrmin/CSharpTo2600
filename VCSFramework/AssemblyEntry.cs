#nullable enable
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Immutable;
using System.Diagnostics;

namespace VCSFramework
{
    /**
     * This file is all about using and heavily abusing the type system to squeeze
     * out as much compile-time safety as possible.
     * Note that most (all?) macro/function calls are auto-generated from the VIL header file.
     * If C# supported it, things like IExpression or ILabel would likely all just be
     * discriminated unions instead. Maybe in C# 10!
     */

    #region Core Interfaces
    /// <summary>
    /// Represents a single logical unit in the assembly output.
    /// This can include things as simple as comments or labels, up to macro invocations with a parameter list and stack operations.
    /// </summary>
    public interface IAssemblyEntry { }
    /// <summary>A call to a 6502.Net macro. Macros are the foundation of VIL.</summary>
    public interface IMacroCall : IAssemblyEntry
    {
        string Name { get; }
        // Needed to lookup all referenced labels.
        ImmutableArray<IExpression> Parameters { get; }
        // Needed for source emitting.
        ImmutableArray<Inst> Instructions { get; }

        void PerformStackOperation(IStackTracker stackTracker);
    }
    public interface IExpression : IAssemblyEntry { }
    /// <summary>
    /// A call to a 6502.Net function, not a runtime function or subroutine.
    /// Unlike macros, functions can return values, but can't execute runtime code.
    /// Even void functions are considered expressions for our purposes.
    /// </summary>
    public interface IFunctionCall : IExpression
    {
        string Name { get; }
        ImmutableArray<IExpression> Parameters { get; }
    }
    /// <summary>An entry that only exists to be consumed by a mandatory optimization. It's an error for this to get past the optimization stage.</summary>
    public interface IPreprocessedEntry : IAssemblyEntry { }
    #endregion

    #region Labels (are expressions)
    public interface ILabel : IExpression { }
    public interface ISizeLabel : ILabel { }
    public interface ITypeLabel : ILabel { }
    /// <summary>
    /// Global labels refer to constant memory addresses and can be used from anywhere.
    /// </summary>
    public interface IGlobalLabel : ILabel { }
    public interface IBranchTargetLabel : ILabel { }
    public sealed record ArgumentGlobalLabel(MethodDef Method, int Index) : IGlobalLabel;
    public sealed record BranchTargetLabel(string Name) : IBranchTargetLabel;
    public sealed record FunctionLabel(MethodDef Method) : ILabel;
    public sealed record GlobalFieldLabel(FieldRef Field) : IGlobalLabel;
    public sealed record InstructionLabel(Inst Instruction) : IBranchTargetLabel;
    public sealed record LocalGlobalLabel(MethodDef Method, int Index) : IGlobalLabel;
    /// <summary>Label for the size of a specific pointer global.</summary>
    public sealed record PointerGlobalSizeLabel(IGlobalLabel Global) : ISizeLabel;
    public sealed record PointerSizeLabel(bool ZeroPage) : ISizeLabel;
    public sealed record PointerTypeLabel(TypeRef ReferentType) : ITypeLabel;
    /// <summary>A global whose label is defined elsewhere (e.g. COLUBK in a header).</summary>
    public sealed record PredefinedGlobalLabel(string Name) : IGlobalLabel;
    public sealed record ReservedGlobalLabel(int Index) : IGlobalLabel;
    public sealed record ReturnValueGlobalLabel(MethodDef Method) : IGlobalLabel;
    /// <summary>Label to a readonly global located in ROM. May be a single value or the first element of multiple values.</summary>
    public sealed record RomDataGlobalLabel(MethodDef GeneratorMethod) : IGlobalLabel;
    public sealed record ThisPointerGlobalLabel(MethodDef Method) : IGlobalLabel;
    public sealed record TypeSizeLabel(TypeRef Type) : ISizeLabel;
    public sealed record TypeLabel(TypeRef Type) : ITypeLabel;
    #endregion

    #region PseudoOps
    public interface IPseudoOp : IAssemblyEntry { }
    public sealed record ArrayLetOp(string VariableName, ImmutableArray<IExpression> Elements) : IPseudoOp;
    public sealed record BeginBlock() : IPseudoOp;
    public sealed record ByteOp(ImmutableArray<byte> Bytes) : IPseudoOp;
    public sealed record EndBlock() : IPseudoOp;
    public sealed record IncludeOp(string Filename) : IPseudoOp;
    public sealed record CpuOp(string Architecture) : IPseudoOp;
    public sealed record WordOp(ILabel Label) : IPseudoOp;
    #endregion

    #region Preprocessed Entries
    /// <summary>Replaces a call to <see cref="VCSFramework.V2.AssemblyUtilities.InlineAssembly(string)"/>.</summary>
    public sealed record InlineAssemblyCall(Instruction SourceInstruction) : IPreprocessedEntry;
    public sealed record LoadString(Instruction SourceInstruction) : IPreprocessedEntry;
    [Obsolete]
    public sealed record RomDataGetByByteOffsetCall(Instruction SourceInstruction) : IPreprocessedEntry;
    public sealed record RomDataGetPointerCall(Instruction SourceInstruction) : IPreprocessedEntry;
    public sealed record RomDataGetterCall(Instruction SourceInstruction) : IPreprocessedEntry;
    public sealed record RomDataLengthCall(Instruction SourceInstruction) : IPreprocessedEntry;
    public sealed record RomDataStrideCall(Instruction SourceInstruction) : IPreprocessedEntry;
    #endregion

    #region Miscellaneous Entries
    public record ArrayAccess(string VariableName, int Index) : IExpression;
    public sealed record Blank() : IAssemblyEntry;
    public sealed record Comment(string Text) : IAssemblyEntry;
    public sealed record Constant(object Value) : IExpression;
    /// <summary>A marker that a function (inlined or otherwise) has ended.</summary>
    public sealed record EndFunction() : IAssemblyEntry;
    public sealed record InlineAssembly(ImmutableArray<string> Assembly) : IAssemblyEntry;
    /// <summary>A marker that a method call was inlined here.</summary>
    public sealed record InlineFunction(Inst? SourceInstruction, MethodDef Definition) : IAssemblyEntry;
    public sealed record LabelAssign(ILabel Label, IExpression Value) : IAssemblyEntry;
    public sealed record MultilineComment(ImmutableArray<string> Text) : IAssemblyEntry;
    public sealed record ProgramCounterAssign(int Address) : IAssemblyEntry;
    // We split this out and not IMacroCall.Instructions, which is also optional, since Instructions will
    // always be set at instantiation. StackOperation is never set at instantiation, it'll only be set 
    // with a `with` operation long after the fact.
    public sealed record StackMutatingMacroCall(IMacroCall MacroCall, StackOperation StackOperation) : IMacroCall
    {
        string IMacroCall.Name => MacroCall.Name;
        ImmutableArray<IExpression> IMacroCall.Parameters => MacroCall.Parameters;
        ImmutableArray<Inst> IMacroCall.Instructions => MacroCall.Instructions;
        void IMacroCall.PerformStackOperation(IStackTracker stackTracker) => MacroCall.PerformStackOperation(stackTracker);
    }
    public sealed record StackSizeArrayAccess(int Index) : ArrayAccess("STACK_SIZEOF", Index);
    public sealed record StackTypeArrayAccess(int Index) : ArrayAccess("STACK_TYPEOF", Index);
    #endregion

    #region Non-Entries, but used by them
    public enum ByteFormat { Decimal, Hex, Binary }
    public sealed record FormattedByte(byte Value, ByteFormat Format)
    {
        public override string ToString() => Format switch
        {
            ByteFormat.Decimal => Convert.ToString(Value),
            ByteFormat.Hex => $"${Value:X2}",
            ByteFormat.Binary => $"%{Convert.ToString(Value, 2)}",
            _ => throw new ArgumentException($"Unknown format: {Format}")
        };
    }
    public sealed record Function(MethodDef Definition, ImmutableArray<IAssemblyEntry> Body);
    public sealed record StackOperation(ArrayLetOp TypeOp, ArrayLetOp SizeOp);
    #endregion

    #region Value-equality wrappers for Cecil types
    // TypeReference only has referential equality, this reusable wrapper gives it value equality.
    public sealed record TypeRef(TypeReference Type)
    {
        public static implicit operator TypeRef(TypeReference t) => new(t);
        public static implicit operator TypeReference(TypeRef t) => t.Type;

        public bool Equals(TypeRef? other)
            => Type.FullName == other?.Type.FullName;

        public override int GetHashCode()
            => Type.FullName.GetHashCode();
    }

    public sealed record MethodDef(MethodDefinition Method)
    {
        public static implicit operator MethodDef(MethodDefinition m) => new(m);
        public static implicit operator MethodDefinition(MethodDef m) => m.Method;

        public string Name => Method.Name.Replace(".", "_");
        public string FullName => Method.FullName;
        public TypeDefinition DeclaringType => Method.DeclaringType;
        public MethodBody Body => Method.Body;
        public bool IsStatic => Method.IsStatic;

        public bool Equals(MethodDef? other)
            => Method.FullName == other?.Method.FullName;

        public override int GetHashCode()
            => Method.FullName.GetHashCode();
    }

    public sealed record ParameterDef(ParameterDefinition Parameter)
    {
        public static implicit operator ParameterDef(ParameterDefinition p) => new(p);
        public static implicit operator ParameterDefinition(ParameterDef p) => p.Parameter;
    }

    public sealed record FieldRef(FieldReference Field)
    {
        public static implicit operator FieldRef(FieldReference f) => new(f);
        public static implicit operator FieldReference(FieldRef f) => f.Field;

        public string Name => Field.Name;
        public TypeReference DeclaringType => Field.DeclaringType;
        public TypeReference Type => Field.FieldType;

        public bool Equals(FieldRef? other)
            => Field.FullName == other?.Field.FullName;

        public override int GetHashCode()
            => Field.FullName.GetHashCode();
    }

    [DebuggerDisplay("{Instruction.Offset:x4}")]
    public sealed record Inst(Instruction Instruction)
    {
        public static implicit operator Inst(Instruction i) => new(i);
        public static implicit operator Instruction(Inst i) => i.Instruction;

        public bool Equals(Inst? other)
            => Instruction.ToString() == other?.Instruction?.ToString();

        public override int GetHashCode()
            => Instruction.ToString().GetHashCode();
    }
    #endregion
}
