#nullable enable
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace VCSFramework.V2
{
    // @TODO - Should we deconstruct instructions in the first position instead of last? This
    // would match how the Macro subclasses are constructed (we can't move them to the end
    // for construction since we need the params arg for parameters).

    internal interface IStackPusher
    {
        void PerformStackPushOps(IStackTracker stackTracker, ImmutableArray<Label> parameters);
    }

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

        public override string ToString()
        {
            var paramString = string.Join(", ", Params);
            return $".{Label} {paramString}";
        }
    }

    public sealed partial record PushConstant : IStackPusher
    {
        public void PerformStackPushOps(IStackTracker stackTracker, ImmutableArray<Label> parameters)
            => stackTracker.Push((TypeLabel)parameters[1], (SizeLabel)parameters[2]);
    }

    public partial record Duplicate : IStackPusher
    {
        public void PerformStackPushOps(IStackTracker stackTracker, ImmutableArray<Label> parameters)
            => stackTracker.Push((StackTypeArrayLabel)parameters[0], (StackSizeArrayLabel)parameters[1]);
    }

    public partial record PushGlobal : IStackPusher
    {
        public void PerformStackPushOps(IStackTracker stackTracker, ImmutableArray<Label> parameters)
            => stackTracker.Push((TypeLabel)parameters[1], (SizeLabel)parameters[2]);
    }

    public partial record AddFromGlobalAndConstant : IStackPusher
    {
        public void PerformStackPushOps(IStackTracker stackTracker, ImmutableArray<Label> parameters)
            => stackTracker.Push(
                new GetAddResultType((TypeLabel)parameters[1], (TypeLabel)parameters[4]),
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

    /*record Bar
    {
        public Bar(string q) { qqq = q; }
        public string qqq;
    }

    partial record Foo : Bar
    {
        public Foo() : base("zzz") { }
        void A() { qqq = null; }
    }

    abstract partial record Foo
    {
        void B() { this.A(); this.qqq = null; }
    }*/

    public partial record AddFromStack : IStackPusher
    {
        public void PerformStackPushOps(IStackTracker stackTracker, ImmutableArray<Label> parameters)
        {
            stackTracker.Push(
                new GetAddResultType((StackTypeArrayLabel)parameters[0], (StackTypeArrayLabel)parameters[2]),
                new GetSizeFromBuiltInType(new StackTypeArrayLabel(0)));
        }
    }

    /*public sealed record AddFromStack : Macro, IStackPusher
    {
        public AddFromStack(Instruction instruction, StackTypeArrayLabel firstOperandType, StackSizeArrayLabel firstOperandSize, StackTypeArrayLabel secondOperandType, StackSizeArrayLabel secondOperandSize)
            : base(instruction, new MacroLabel("addFromStack"), firstOperandType, firstOperandSize, secondOperandType, secondOperandSize) { }

        public StackTypeArrayLabel FirstOperandType => (StackTypeArrayLabel)Params[0];
        public StackSizeArrayLabel FirstOperandSize => (StackSizeArrayLabel)Params[1];
        public StackTypeArrayLabel SecondOperandType => (StackTypeArrayLabel)Params[2];
        public StackSizeArrayLabel SecondOperandSize => (StackSizeArrayLabel)Params[3];

        public void PerformStackPushOps(IStackTracker stackTracker)
        {
            stackTracker.Push(
                new GetAddResultType(FirstOperandType, SecondOperandType),
                new GetSizeFromBuiltInType(new StackTypeArrayLabel(0)));
        }

        public void Deconstruct(out ImmutableArray<Instruction> instructions, out StackTypeArrayLabel firstOperandType, out StackSizeArrayLabel firstOperandSize, out StackTypeArrayLabel secondOperandType, out StackSizeArrayLabel secondOperandSize)
        {
            instructions = Instructions;
            firstOperandType = FirstOperandType;
            firstOperandSize = FirstOperandSize;
            secondOperandType = SecondOperandType;
            secondOperandSize = SecondOperandSize;
        }
    }*/

    /*public sealed record AddFromGlobalAndConstant : Macro
    {
        public AddFromGlobalAndConstant(Instruction instruction, GlobalLabel global, TypeLabel globalType, SizeLabel globalSize, ConstantLabel constant, TypeLabel constantType, SizeLabel constantSize)
            : base(instruction, new MacroLabel("addFromGlobalAndConstant"), global, globalType, globalSize, constant, constantType, constantSize) { }

        public GlobalLabel Global => (GlobalLabel)Params[0];
        public TypeLabel GlobalType => (TypeLabel)Params[1];
        public SizeLabel GlobalSize => (SizeLabel)Params[2];
        public ConstantLabel Constant => (ConstantLabel)Params[3];
        public TypeLabel ConstantType => (TypeLabel)Params[4];
        public SizeLabel ConstantSize => (SizeLabel)Params[5];

        public void Deconstruct(out ImmutableArray<Instruction> instructions, out GlobalLabel global, out TypeLabel globalType, out SizeLabel globalSize, out ConstantLabel constant, out TypeLabel constantType, out SizeLabel constantSize)
        {
            instructions = Instructions;
            global = Global;
            globalType = GlobalType;
            globalSize = GlobalSize;
            constant = Constant;
            constantType = ConstantType;
            constantSize = ConstantSize;
        }
    }*/

    public sealed record AssignConstantToGlobal : Macro
    {
        public AssignConstantToGlobal(IEnumerable<Instruction> instructions, ConstantLabel constant, GlobalLabel global, SizeLabel size)
            : base(instructions, new MacroLabel("assignConstantToGlobal"), constant, global, size) { }
    }

    /**
     public record PushGlobal(Label GlobalLabel) : Macro
     */

    public abstract record AssemblyEntry
    {
        //public string? SourceText { get; init; }
        //public string? CilText { get; init; }

        public static implicit operator string(AssemblyEntry entry) => entry.ToString();

        public abstract override string ToString();
    }

    public abstract record PsuedoOp : AssemblyEntry
    {
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

        public override string ToString()
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

        public override string ToString()
            => $".let {VariableLabel} = [{string.Join(',', Values)}]";
    }

    public sealed record Comment(string Text) : AssemblyEntry
    {
        // @TODO - Support multiline.
        public override string ToString() => $"// {Text}";
    }

    public sealed class Bundle
    {

    }

    public abstract record Label(string Name) : AssemblyEntry
    {
        public static implicit operator string(Label label) => label.Name;
        public override string ToString() => Name;
    }
    
    /// <summary>Label referring to a constant.</summary>
    public sealed record ConstantLabel(object Value) 
        : Label($"CONST_{Value}");

    /// <summary>Label referring to the size of a type.</summary>
    public sealed record SizeLabel(TypeReference Type) 
        : Label($"SIZE_{Type.NamespaceAndName()}"), IEquatable<SizeLabel>
    {
        public bool Equals(SizeLabel? other)
        {
            return Type.FullName == other?.Type.FullName;
        }

        public override int GetHashCode()
        {
            return Type.FullName.GetHashCode();
        }
    }

    public sealed record TypeLabel(TypeReference Type)
        : Label($"TYPE_{Type.NamespaceAndName()}"), IEquatable<TypeLabel>
    {
        public bool Equals(TypeLabel? other)
        {
            return Type.FullName == other?.Type.FullName;
        }

        public override int GetHashCode()
        {
            return Type.FullName.GetHashCode();
        }
    }

    /// <summary>Label referring to the address of a global.</summary>
    public sealed record GlobalLabel(string Name, bool Predefined = false) : Label(Name);

    /// <summary>Label referring to the address of a local.</summary>
    public sealed record LocalLabel(string Name) : Label(Name);

    /// <summary>Label referring to a defined macro.</summary>
    public sealed record MacroLabel(string Name) : Label(Name);

    /// <summary>Label referring to the left hand size of a `.let` psuedop.</summary>
    public sealed record LetLabel(string Name) : Label(Name);

    public sealed record InstructionLabel(string Name) : Label(Name);

    public sealed record FunctionLabel(string Name) : Label(Name);

    public sealed record StackTypeArrayLabel(int Index)
        : Label($"STACK_TYPEOF[{Index}]");

    public sealed record StackSizeArrayLabel(int Index)
        : Label($"STACK_SIZEOF[{Index}]");

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
        public override string ToString()
        {
            var paramString = string.Join(", ", Params);
            return $"{Label}({paramString})";
        }
    }

    public sealed record GetAddResultType : Function
    {
        public GetAddResultType(StackTypeArrayLabel first, StackTypeArrayLabel second)
            : base(new FunctionLabel("getAddResultType"), first, second) { }

        public GetAddResultType(TypeLabel first, TypeLabel second)
            : base(new FunctionLabel("getAddResultType"), first, second) { }
    }

    public sealed record GetSizeFromBuiltInType : Function
    {
        public GetSizeFromBuiltInType(StackTypeArrayLabel type)
            : base(new FunctionLabel("getSizeFromBuiltInType"), type) { }
    }

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
            return $"{formattedNamespace}_{@this.Name}";
        }
    }
}
