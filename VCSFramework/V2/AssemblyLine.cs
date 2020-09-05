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

    public partial record PushAddressOfGlobal : IStackPusher
    {
        // @TODO - While it may be byte-sized, we need to push an actual pointer type so we can track what it
        // is when we eventually dereference it.
        public void PerformStackPushOps(IStackTracker stackTracker, ImmutableArray<Label> parameters)
            => stackTracker.Push(new TypeLabel(BuiltInDefinitions.Byte), new SizeLabel(BuiltInDefinitions.Byte));
    }

    public partial record PushAddressOfField : IStackPusher
    {
        // @TODO - While it may be byte-sized, we need to push an actual pointer type so we can track what it
        // is when we eventually dereference it.
        public void PerformStackPushOps(IStackTracker stackTracker, ImmutableArray<Label> parameters)
            => stackTracker.Push(new TypeLabel(BuiltInDefinitions.Byte), new SizeLabel(BuiltInDefinitions.Byte));
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

    public partial record AddFromStack : IStackPusher
    {
        public void PerformStackPushOps(IStackTracker stackTracker, ImmutableArray<Label> parameters)
        {
            stackTracker.Push(
                new GetAddResultType((StackTypeArrayLabel)parameters[0], (StackTypeArrayLabel)parameters[2]),
                new GetSizeFromBuiltInType(new StackTypeArrayLabel(0)));
        }
    }

    public partial record PushLocal : IStackPusher
    {
        public void PerformStackPushOps(IStackTracker stackTracker, ImmutableArray<Label> parameters)
            => stackTracker.Push((TypeLabel)parameters[1], (SizeLabel)parameters[2]);
    }

    public partial record SubFromStack : IStackPusher
    {
        public void PerformStackPushOps(IStackTracker stackTracker, ImmutableArray<Label> parameters)
        {
            stackTracker.Push(
                new GetAddResultType((StackTypeArrayLabel)parameters[0], (StackTypeArrayLabel)parameters[2]),
                new GetSizeFromBuiltInType(new StackTypeArrayLabel(0)));
        }
    }

    public partial record OrFromStack : IStackPusher
    {
        public void PerformStackPushOps(IStackTracker stackTracker, ImmutableArray<Label> parameters)
            => stackTracker.Push(new GetBitOpResultType(new StackTypeArrayLabel(0), new StackTypeArrayLabel(1)), new GetSizeFromBuiltInType(new(0)));
    }

    public partial record CompareEqualToFromStack : IStackPusher
    {
        public void PerformStackPushOps(IStackTracker stackTracker, ImmutableArray<Label> parameters)
            => stackTracker.Push(new TypeLabel(BuiltInDefinitions.Bool), new SizeLabel(BuiltInDefinitions.Bool));
    }

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
        public static implicit operator string(AssemblyEntry entry) => entry.ToString();

        public abstract override string ToString();
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

        public override string ToString()
        {
            // Shouldn't really use this since only the first line will have indentation.
            return string.Join(Environment.NewLine, Entries.Select(e => e.ToString()));
        }
    }

    public abstract record PsuedoOp : AssemblyEntry
    {
    }

    public record BeginBlock : PsuedoOp
    {
        public override string ToString() => ".block";
    }

    public record EndBlock : PsuedoOp
    {
        public override string ToString() => ".endblock";
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

    // @TODO - Handle overloaded methods (same name).
    /// <summary>Label referring to the address of a local.</summary>
    public sealed record LocalLabel(MethodDefinition Method, int Index)
        : Label($"LOCAL_{Method.DeclaringType.NamespaceAndName()}_{Method.Name}_{Index}"), IEquatable<LocalLabel>
    {
        public bool Equals(LocalLabel? other) => ToString() == other?.ToString();

        public override int GetHashCode() => ToString().GetHashCode();
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
