#nullable enable
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace VCSFramework.V2
{
    /**
     * This file is all about using and heavily abusing the type system to squeeze
     * out as much compile-time safety as possible.
     * Note that most macro/function calls are auto-generated from the VIL header file.
     * If C# supported it, things like IExpression or ILabel would likely all just be
     * discriminated unions instead. Maybe in C# 10!
     */

    /// <summary>
    /// Represents a single logical unit in the assembly output.
    /// This can include things as simple as comments or labels, up to macro invocations with a parameter list and stack operations.
    /// </summary>
    public interface IAssemblyEntry
    {
    }

    public interface IMacroCall : IAssemblyEntry
    {
        string Name { get; }
        // Needed to lookup all referenced labels.
        ImmutableArray<IExpression> Parameters { get; }
        // Needed for source emitting.
        ImmutableArray<Inst> Instructions { get; }

        void PerformStackOperation(IStackTracker stackTracker);
    }

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

    public interface IFunctionCall : IExpression
    {
        /// <summary>Name of the function to be invoked.</summary>
        string Name { get; }
        ImmutableArray<IExpression> Parameters { get; }
    }

    public interface IExpression : IAssemblyEntry { }

    public sealed record Constant(object Value) : IExpression;
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

    public sealed record Comment(string Text) : IAssemblyEntry;

    public sealed record MultilineComment(ImmutableArray<string> Text) : IAssemblyEntry;

    public sealed record InlineAssembly(ImmutableArray<string> Assembly) : IAssemblyEntry;

    // @TODO - Some indication that these shouldn't exist at assembly time? Did that before
    // by making them macros, but that doesn't really make sense.
    public sealed record LoadString(Instruction SourceInstruction) : IAssemblyEntry;

    public sealed record InlineAssemblyCall(Instruction SourceInstruction) : IAssemblyEntry;

    public sealed record LabelAssignment(ILabel Label, string Value) : IAssemblyEntry;

    #region Labels
    public interface ILabel : IExpression { }

    // @TODO - Stack versions of these?
    public interface ISizeLabel : ILabel { }

    public interface ITypeLabel : ILabel { }

    /// <summary>
    /// Global labels refer to constant memory addresses and can be used from anywhere.
    /// </summary>
    public interface IGlobalLabel : ILabel { }

    public interface IBranchTargetLabel : ILabel { }

    public sealed record TypeSizeLabel(TypeRef Type) : ISizeLabel;

    public sealed record PointerSizeLabel(bool ZeroPage) : ISizeLabel;

    public sealed record TypeLabel(TypeRef Type) : ITypeLabel;

    public sealed record PointerTypeLabel(TypeRef ReferentType) : ITypeLabel;

    public sealed record GlobalFieldLabel(FieldRef Field) : IGlobalLabel;

    public sealed record ReservedGlobalLabel(int Index) : IGlobalLabel;

    /// <summary>A global whose label is defined elsewhere (e.g. COLUBK in a header).</summary>
    public sealed record PredefinedGlobalLabel(string Name) : IGlobalLabel;

    /// <summary>
    /// Label to a local variable that has been lifted to its own global address.
    /// These are far faster and easier to work with, and should always be preferred,
    /// but they are incompatible with any form of recursion.
    /// </summary>
    public sealed record LiftedLocalLabel(MethodDef Method, int Index) : IGlobalLabel;

    /// <summary>
    /// Label to a local variable that exists at an indeterminate address on the stack.
    /// This can only be used in the context of a frame pointer or some other offsetable value.
    /// </summary>
    public sealed record LocalLabel(MethodDef Method, int Index) : ILabel;

    public sealed record MethodLabel(MethodDef Method) : ILabel;

    public sealed record InstructionLabel(Inst Instruction) : IBranchTargetLabel;

    public sealed record BranchTargetLabel(string Name) : IBranchTargetLabel;
    #endregion

    public record ArrayAccessOp(string VariableName, int Index) : IExpression;

    public sealed record StackTypeArrayAccess(int Index) : ArrayAccessOp("STACK_TYPEOF", Index);

    public sealed record StackSizeArrayAccess(int Index) : ArrayAccessOp("STACK_SIZEOF", Index);
    
    public sealed record ArrayLetOp(string VariableName, ImmutableArray<IExpression> Elements) : IAssemblyEntry;

    public sealed record StackOperation(ArrayLetOp TypeOp, ArrayLetOp SizeOp);

    //
    public interface IPsuedoOp : IAssemblyEntry { }
    public sealed record BeginBlock() : IPsuedoOp;
    public sealed record EndBlock() : IPsuedoOp;
    public sealed record Blank() : IAssemblyEntry;
    public sealed record AssignLabel(ILabel Label, IExpression Value) : IPsuedoOp;
    public sealed record IncludeOp(string Filename) : IPsuedoOp;
    public sealed record CpuOp(string Architecture) : IPsuedoOp;
    public sealed record WordOp(ILabel Label) : IPsuedoOp;
    public sealed record ProgramCounterAssignOp(int Address) : IPsuedoOp;
    /// <summary>A marker that a method call was inlined here.</summary>
    public sealed record InlineFunction(Inst SourceInstruction, MethodDef Definition) : IAssemblyEntry;
    public sealed record Function(MethodDef Definition, ImmutableArray<IAssemblyEntry> Body);
    /// <summary>A marker that a function (inlined or otherwise) has ended.</summary>
    public sealed record EndFunction() : IAssemblyEntry;

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

        public MethodBody Body => Method.Body;

        public bool Equals(MethodDef? other)
            => Method.FullName == other?.Method.FullName;

        public override int GetHashCode()
            => Method.FullName.GetHashCode();
    }

    public sealed record FieldRef(FieldReference Field)
    {
        public static implicit operator FieldRef(FieldReference f) => new(f);
        public static implicit operator FieldReference(FieldRef f) => f.Field;

        public string Name => Field.Name;
        public TypeReference Type => Field.FieldType;

        public bool Equals(FieldRef? other)
            => Field.FullName == other?.Field.FullName;

        public override int GetHashCode()
            => Field.FullName.GetHashCode();

        public override string ToString() => $"{Field.DeclaringType.NamespaceAndName()}_{Field.Name}";
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

    /*internal interface IStackPusher
    {
        void PerformStackPushOps(IStackTracker stackTracker, ImmutableArray<Label> parameters);
    }

    internal interface ICallMacro
    {
        MethodDefinition Method { get; }
    }

    // @TODO - Since we're auto-generating most things, we can probably just stick with classes.
    public abstract record Macro : AssemblyEntry
    {
        public MacroLabel Label { get; }
        public ImmutableArray<Label> Params { get; }
        public ImmutableArray<MacroEffectAttribute> Effects { get; }
        /// <summary>What instructions this macro replaces.</summary>
        public ImmutableArray<Instruction> Instructions { get; init; } = ImmutableArray<Instruction>.Empty;
        public ImmutableArray<ArrayLetOp> StackLets { get; init; } = ImmutableArray<ArrayLetOp>.Empty;

        public Macro(MacroLabel label, params Label[] parameters)
        {
            Label = label;
            Params = parameters.ToImmutableArray();
            Effects = GetType().GetCustomAttributes(false).OfType<MacroEffectAttribute>().ToImmutableArray();
        }

        public Macro(Instruction instruction, MacroLabel label, params Label[] parameters)
        {
            Label = label;
            Params = parameters.ToImmutableArray();
            Effects = GetType().GetCustomAttributes(false).OfType<MacroEffectAttribute>().ToImmutableArray();
            Instructions = new[] { instruction }.ToImmutableArray();
        }

        public Macro(IEnumerable<Instruction> instructions, MacroLabel label, params Label[] parameters)
        {
            Label = label;
            Params = parameters.ToImmutableArray();
            Effects = GetType().GetCustomAttributes(false).OfType<MacroEffectAttribute>().ToImmutableArray();
            Instructions = instructions.ToImmutableArray();
        }

        public Macro WithStackLets(IStackTracker stackTracker, TypeLabel nothingType, SizeLabel nothingSize)
        {
            var stackPops = Effects.OfType<PopStackAttribute>().SingleOrDefault()?.Count;
            var stackPushes = Effects.OfType<PushStackAttribute>().SingleOrDefault()?.Count;

            if (stackPops == null && stackPushes == null)
            {
                // Doesn't touch the stack.
                return this;
            }

            if (stackPops != null)
            {
                stackTracker.Pop((int)stackPops);
            }
            if (stackPushes != null)
            {
                // @TODO - We can enforce this by including the interface with auto-gen
                // (if annotated to push stack), and thus requiring user to add a partial
                // to implement it.
                if (this is IStackPusher stackPusher)
                    stackPusher.PerformStackPushOps(stackTracker, Params);
                else
                    throw new InvalidOperationException($"{GetType().Name} is annotated with {nameof(PushStackAttribute)} but doesn't implement {nameof(IStackPusher)}");
            }

            return this with
            {
                StackLets = stackTracker.GenerateStackSetters()
            };
        }

        public override string Output
        {
            get
            {
                var paramString = string.Join(", ", Params.Select(p => p.Output));
                return $".{Label.Output} {paramString}";
            }
        }
    }

    public sealed record InlineAssemblyEntry(string Assembly) : AssemblyEntry
    {
        public override string Output
        {
            get
            {
                var builder = new StringBuilder();
                builder.AppendLine(new Comment("Begin inline assembly"));
                builder.AppendLine(Assembly);
                builder.AppendLine(new Comment("End inline assembly"));
                return builder.ToString();
            }
        }
    }

    public sealed record LoadString : Macro
    {
        public LoadString(Instruction instruction) : base(instruction, new("__LoadString__INTERNAL_ONLY_MUST_BE_OPTIMIZED_OUT")) { }

        public void Deconstruct(out ImmutableArray<Instruction> instructions)
        {
            instructions = Instructions;
        }
    }

    public sealed record InlineAssembly : Macro
    {
        public InlineAssembly(Instruction instruction) : base(instruction, new("__InlineAssembly__INTERNAL_ONLY_MUST_BE_OPTIMIZED_OUT")) { }

        public void Deconstruct(out ImmutableArray<Instruction> instructions)
        {
            instructions = Instructions;
        }
    }

    public sealed partial record PushConstant : IStackPusher
    {
        public void PerformStackPushOps(IStackTracker stackTracker, ImmutableArray<Label> parameters)
            => stackTracker.Push(Type, Size);
    }

    public partial record Duplicate : IStackPusher
    {
        public void PerformStackPushOps(IStackTracker stackTracker, ImmutableArray<Label> parameters)
            => stackTracker.Push(StackType, StackSize);
    }

    public partial record PushGlobal : IStackPusher
    {
        public void PerformStackPushOps(IStackTracker stackTracker, ImmutableArray<Label> parameters)
            => stackTracker.Push(Type, Size);
    }

    public partial record PushAddressOfGlobal : IStackPusher
    {
        public void PerformStackPushOps(IStackTracker stackTracker, ImmutableArray<Label> parameters)
            => stackTracker.Push(PointerType, PointerSize);
    }

    public partial record PushAddressOfLocal : IStackPusher
    {
        public void PerformStackPushOps(IStackTracker stackTracker, ImmutableArray<Label> parameters)
            => stackTracker.Push(PointerType, PointerSize);
    }

    public partial record PushAddressOfField : IStackPusher
    {
        public void PerformStackPushOps(IStackTracker stackTracker, ImmutableArray<Label> parameters)
            => stackTracker.Push(PointerType, PointerStackSize);
    }

    public partial record PushDereferenceFromStack : IStackPusher
    {
        public void PerformStackPushOps(IStackTracker stackTracker, ImmutableArray<Label> parameters)
            => stackTracker.Push(Type, Size);
    }

    public partial record PushFieldFromStack : IStackPusher
    {
        public void PerformStackPushOps(IStackTracker stackTracker, ImmutableArray<Label> parameters)
            => stackTracker.Push(FieldType, FieldSize);
    }

    public partial record AddFromGlobalAndConstant : IStackPusher
    {
        public void PerformStackPushOps(IStackTracker stackTracker, ImmutableArray<Label> parameters)
            => stackTracker.Push(
                new GetAddResultType(GlobalType, ConstantType),
                new GetSizeFromBuiltInType(new(0)));
    }

    public sealed record StoreTo : Macro
    {
        public StoreTo(Instruction instruction, GlobalLabel global)
            : base(instruction, new MacroLabel("storeTo"), global) { }

        public GlobalLabel Global => (GlobalLabel)Params[0];

        public void Deconstruct(out ImmutableArray<Instruction> instructions, out GlobalLabel global)
        {
            instructions = Instructions;
            global = Global;
        }
    }

    public partial record AddFromStack : IStackPusher
    {
        public void PerformStackPushOps(IStackTracker stackTracker, ImmutableArray<Label> parameters)
        {
            stackTracker.Push(
                new GetAddResultType(FirstOperandStackType, SecondOperandStackType),
                new GetSizeFromBuiltInType(new StackTypeArrayLabel(0)));
        }
    }

    public partial record PushLocal : IStackPusher
    {
        public void PerformStackPushOps(IStackTracker stackTracker, ImmutableArray<Label> parameters)
            => stackTracker.Push(Type, Size);
    }

    public partial record SubFromStack : IStackPusher
    {
        public void PerformStackPushOps(IStackTracker stackTracker, ImmutableArray<Label> parameters)
        {
            stackTracker.Push(
                new GetAddResultType(FirstOperandStackType, SecondOperandStackType),
                new GetSizeFromBuiltInType(new StackTypeArrayLabel(0)));
        }
    }

    public partial record OrFromStack : IStackPusher
    {
        public void PerformStackPushOps(IStackTracker stackTracker, ImmutableArray<Label> parameters)
            => stackTracker.Push(new GetBitOpResultType(FirstOperandStackType, SecondOperandStackType), new GetSizeFromBuiltInType(new(0)));
    }

    public partial record CompareEqualToFromStack : IStackPusher
    {
        public void PerformStackPushOps(IStackTracker stackTracker, ImmutableArray<Label> parameters)
            => stackTracker.Push(new TypeLabel(BuiltInDefinitions.Bool), new SizeLabel(BuiltInDefinitions.Bool));
    }

    public sealed record AssignConstantToGlobal : Macro
    {
        public AssignConstantToGlobal(IEnumerable<Instruction> instructions, ConstantLabel constant, GlobalLabel global, BaseSizeLabel size)
            : base(instructions, new MacroLabel("assignConstantToGlobal"), constant, global, size) { }
    }

    public partial record CallVoid : ICallMacro
    {
        MethodDefinition ICallMacro.Method => Method.Method;
    }

    public partial record CallNonVoid : IStackPusher, ICallMacro
    {
        MethodDefinition ICallMacro.Method => Method.Method;

        public void PerformStackPushOps(IStackTracker stackTracker, ImmutableArray<Label> parameters)
            => stackTracker.Push(ResultType, ResultSize);
    }

    public abstract record AssemblyEntry
    {
        public abstract string Output { get; }
    }

    public record InlineMethod : AssemblyEntry
    {
        public Instruction Instruction { get; init; }
        public MethodDefinition Method { get; }
        public ImmutableArray<AssemblyEntry> Entries { get; init; } = ImmutableArray<AssemblyEntry>.Empty;

        public InlineMethod(Instruction instruction, MethodDefinition method, ImmutableArray<AssemblyEntry> entries)
        {
            Instruction = instruction;
            Method = method;
            Entries = entries
                .Prepend(new Comment($"Begin inline method body of: '{Method.FullName}'"))
                .Append(new Comment($"End inline method body of: '{Method.FullName}'"))
                .ToImmutableArray();
        }

        public override string Output
        {
            get
            {
                // Shouldn't really use this since only the first line will have indentation.
                return string.Join(Environment.NewLine, Entries.Select(e => e.Output));
            }
        }
    }

    public abstract record PsuedoOp : AssemblyEntry
    {
    }

    public record BeginBlock : PsuedoOp
    {
        public override string Output => ".block";
    }

    public record EndBlock : PsuedoOp
    {
        public override string Output => ".endblock";
    }

    public record LetOp : PsuedoOp
    {
        protected Label VariableLabel { get; }
        protected Label ValueLabel { get; }

        internal LetOp(Label variableLabel, Label valueLabel)
        {
            VariableLabel = variableLabel;
            ValueLabel = valueLabel;
        }

        public override string Output
            => $".let {VariableLabel} = {ValueLabel}";
    }

    public record ArrayLetOp : PsuedoOp
    {
        private LetLabel VariableLabel { get; }
        // Can't use Label because a function invocation isn't a label
        private ImmutableArray<string> Values { get; }

        public ArrayLetOp(LetLabel variable, IEnumerable<string> values)
        {
            VariableLabel = variable;
            Values = values.ToImmutableArray();
        }

        public override string Output
            => $".let {VariableLabel.Output} = [{string.Join(',', Values)}]";
    }

    public sealed record Comment(string Text) : AssemblyEntry
    {
        public static implicit operator string(Comment comment) => comment.Output;

        // @TODO - Support multiline.
        public override string Output => $"// {Text}";

        public override string ToString() => Output;
    }

    public sealed class Bundle
    {

    }

    public abstract record Label(string Name) : AssemblyEntry
    {
        public override string Output => Name;
    }
    
    /// <summary>Label referring to a constant.</summary>
    public sealed record ConstantLabel(object Value) 
        : Label($"CONST_{Value}");

    public abstract record BaseSizeLabel(string Name) : Label(Name);

    /// <summary>Label referring to the size of a type.</summary>
    public sealed record SizeLabel(TypeReference Type)
        : BaseSizeLabel($"SIZE_{Type.ThrowIfPointer().NamespaceAndName()}"), IEquatable<SizeLabel>
    {
        // Some types are different but produce the same strings (e.g. Byte* and Byte&).
        // So compare the label name and not the actual types.
        public bool Equals(SizeLabel? other) => Output == other?.Output;

        public override int GetHashCode() => Output.GetHashCode();
    }

    public abstract record BaseTypeLabel(string Name) : Label(Name);

    // @TODO - This should probably throw on pointer like SizeLabel.
    // Can have MacroGenerator use BaseTypeLabel too so we can provide either this or PointerTypeLabel.
    public sealed record TypeLabel(TypeReference Type)
        : BaseTypeLabel($"TYPE_{Type.NamespaceAndName()}"), IEquatable<TypeLabel>
    {
        // Some types are different but produce the same strings (e.g. Byte* and Byte&).
        // So compare the label name and not the actual types.
        public bool Equals(TypeLabel? other) => Output == other?.Output;

        public override int GetHashCode() => Output.GetHashCode();
    }

    public sealed record PointerSizeLabel(bool ZeroPage)
        : BaseSizeLabel(ZeroPage ? "SIZE_SHORT_POINTER" : "SIZE_LONG_POINTER");

    public sealed record PointerTypeLabel(TypeReference Type)
        : BaseTypeLabel($"TYPE_{Type.NamespaceAndName()}_PTR"), IEquatable<PointerTypeLabel>
    {
        public bool Equals(PointerTypeLabel? other) => Output == other?.Output;

        public override int GetHashCode() => Output.GetHashCode();
    }

    /// <summary>Label referring to the address of a global.</summary>
    // @TODO - Could probably go the BaseGlobalLabel route and have a subclass for TypeReference vs predefined Name.
    public sealed record GlobalLabel(string Name, TypeReference Type, bool Predefined = false) : Label(Name), IEquatable<GlobalLabel>
    {
        public bool Equals(GlobalLabel? other) => Output == other?.Output;

        public override int GetHashCode() => Output.GetHashCode();
    }

    // @TODO - Handle overloaded methods (same name).
    /// <summary>Label referring to the address of a local.</summary>
    public sealed record LocalLabel(MethodDefinition Method, int Index)
        : Label($"LOCAL_{Method.DeclaringType.NamespaceAndName()}_{Method.Name}_{Index}"), IEquatable<LocalLabel>
    {
        public bool Equals(LocalLabel? other) => Output == other?.Output;

        public override int GetHashCode() => Output.GetHashCode();
    }

    /// <summary>Label referring to a defined macro.</summary>
    public sealed record MacroLabel(string Name) : Label(Name);

    public sealed record FunctionLabel(string Name) : Label(Name);

    /// <summary>Label referring to the left hand size of a `.let` psuedop.</summary>
    public sealed record LetLabel(string Name) : Label(Name);

    public sealed record InstructionLabel(string Name) : Label(Name);

    public sealed record MethodLabel(MethodDefinition Method, bool Inline) 
        : Label($"METHOD_{Method.DeclaringType.NamespaceAndName()}_{Method.Name}");

    public sealed record StackTypeArrayLabel(int Index)
        : Label($"STACK_TYPEOF[{Index}]");

    public sealed record StackSizeArrayLabel(int Index)
        : BaseSizeLabel($"STACK_SIZEOF[{Index}]");

    /// <summary>Type used to indicate a stack element is empty.</summary>
    public struct Nothing { }

    public abstract record Function : AssemblyEntry
    {
        public FunctionLabel Label { get; }
        public ImmutableArray<Label> Params { get; }

        public Function(FunctionLabel label, params Label[] parameters)
        {
            Label = label;
            Params = parameters.ToImmutableArray();
        }

        // Assume it has a return value
        public override string Output
        {
            get
            {
                var paramString = string.Join(", ", Params.Select(p => p.Output));
                return $"{Label.Output}({paramString})";
            }
        }
    }

    public sealed record GetAddResultType : Function
    {
        public GetAddResultType(StackTypeArrayLabel first, StackTypeArrayLabel second)
            : base(new FunctionLabel("getAddResultType"), first, second) { }

        public GetAddResultType(TypeLabel first, TypeLabel second)
            : base(new FunctionLabel("getAddResultType"), first, second) { }
    }

    public sealed record GetBitOpResultType : Function
    {
        public GetBitOpResultType(StackTypeArrayLabel first, StackTypeArrayLabel second)
            : base(new FunctionLabel("getBitOpResultType"), first, second) { }
    }

    public sealed record GetSizeFromBuiltInType : Function
    {
        public GetSizeFromBuiltInType(StackTypeArrayLabel type)
            : base(new FunctionLabel("getSizeFromBuiltInType"), type) { }
    }

    public sealed record GetTypeFromPointer : Function
    {
        public GetTypeFromPointer(StackTypeArrayLabel type)
            : base(new FunctionLabel("getTypeFromPointer"), type) { }
    }

    public sealed record GetSizeFromType : Function
    {
        public GetSizeFromType(StackTypeArrayLabel type)
            : base(new FunctionLabel("getSizeFromType"), type) { }
    }*/

    // [StackEffect(POP, 2)]
    // [StackEffect(PUSH, 1)]
    // [RegisterEffect(Accumulator, ResultLSB)]
    // public struct AddFromStackMacro

    // public Macro AddFromStackMacro => new Macro(addFromStack", new StackEffect(POP, 2), new StackEffect(PUSH, 1), new RegisterEffect(Accumulator, StackLSB));
    internal static class TypeReferenceStringExtensions
    {
        public static string NamespaceAndName(this TypeReference @this)
        {
            var formattedNamespace = @this.Namespace.Replace('.', '_');
            // & = managed pointers in CIL, versus unmanaged pointers.
            // Considering we have no garbage collector, or actual managed memory,
            // or similar, I feel the distinction isn't important here. Hopefully.
            var formattedName = @this.Name.Replace("*", "_PTR").Replace("&", "_PTR");
            return $"{formattedNamespace}_{formattedName}";
        }

        public static TypeReference ThrowIfPointer(this TypeReference @this)
        {
            if (@this.IsPointer || @this.IsPinned || @this.IsByReference)
                throw new InvalidOperationException($"Unexpected pointer type: '{@this.FullName}'");
            return @this;
        }
    }
}
