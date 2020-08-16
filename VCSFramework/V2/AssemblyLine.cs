#nullable enable
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace VCSFramework.V2
{
    public record Macro : AssemblyEntry
    {
        public MacroLabel Label { get; }
        public ImmutableArray<Label> Params { get; }
        public IEnumerable<MacroEffectAttribute> Effects => GetType().CustomAttributes.OfType<MacroEffectAttribute>();
        /// <summary>What instructions this macro replaces.</summary>
        public ImmutableArray<Instruction> Instructions { get; init; } = ImmutableArray<Instruction>.Empty;
        public ImmutableArray<StackLetOp> StackLets { get; init; } = ImmutableArray<StackLetOp>.Empty;

        public Macro(MacroLabel label, params Label[] parameters)
        {
            Label = label;
            Params = parameters.ToImmutableArray();
        }

        public Macro(Instruction instruction, MacroLabel label, params Label[] parameters)
        {
            Label = label;
            Params = parameters.ToImmutableArray();
            Instructions = new[] { instruction }.ToImmutableArray();
        }

        public virtual Macro WithStackLets(int depth)
        {
            return this;
        }

        /// <summary>
        /// Generates stack ops to push type/size up.
        /// For example, type[0] and size[0] will move to type[1] and size[1],
        /// type[1] and size[1] will move to type[2] and size[2], etc.
        /// </summary>\
        protected IEnumerable<StackLetOp> PercolateStackOps(int depth)
        {
            for (var i = depth - 1; i > 0; i++)
            {
                yield return new StackTypeOp(new StackTypeLabel(i), new StackTypeLabel(i - 1));
                yield return new StackSizeOp(new StackSizeLabel(i), new StackSizeLabel(i - 1));
            }
        }

        public override string ToString()
        {
            var paramString = string.Join(", ", Params);
            return $".{Label} {paramString}";
        }
    }

    public sealed record Branch : Macro
    {
        public Branch(Instruction instruction, InstructionLabel instructionLabel)
            : base(instruction, new MacroLabel("branch"), instructionLabel) { }

        public void Deconstruct(out InstructionLabel instruction)
            => instruction = (InstructionLabel)Params[0];
    }

    [PushStack(Count = 1)]
    public sealed record PushConstant : Macro
    {
        public PushConstant(Instruction instruction, ConstantLabel constant, TypeLabel constantType, SizeLabel constantSize) 
            : base(instruction, new MacroLabel("pushConstant"), constant, constantType, constantSize) { }

        public override Macro WithStackLets(int depth)
        {
            var stackLets = new StackLetOp[]
            {
                new StackTypeOp(new StackTypeLabel(0), (TypeLabel)Params[1]),
                new StackSizeOp(new StackSizeLabel(0), (SizeLabel)Params[2])
            };
            return this with { StackLets = PercolateStackOps(depth).Concat(stackLets).ToImmutableArray() };
        }

        public void Deconstruct(out ConstantLabel constant) => constant = (ConstantLabel)Params[0];
    }

    [PushStack(Count = 1)]
    public sealed record PushGlobal : Macro
    {
        public PushGlobal(Instruction instruction, GlobalLabel global, TypeLabel globalType, SizeLabel globalSize)
            : base(instruction, new MacroLabel("pushGlobal"), global, globalType, globalSize) { }

        public override Macro WithStackLets(int depth)
        {
            var stackLets = new StackLetOp[]
            {
                new StackTypeOp(new StackTypeLabel(0), (TypeLabel)Params[1]),
                new StackSizeOp(new StackSizeLabel(0), (SizeLabel)Params[2])
            };
            return this with { StackLets = PercolateStackOps(depth).Concat(stackLets).ToImmutableArray() };
        }

        public void Deconstruct(out GlobalLabel global) => global = (GlobalLabel)Params[0];
    }

    [PopStack(Count = 1)]
    public sealed record PopToGlobal : Macro
    {
        public PopToGlobal(Instruction instruction, GlobalLabel globalLabel, TypeLabel typeLabel, SizeLabel sizeLabel)
            : base(instruction, new MacroLabel("popToGlobal"), globalLabel, typeLabel, sizeLabel, new StackTypeLabel(0), new StackSizeLabel(0)) { }

        // @TODO - Invalidate stack since we popped?

        public void Deconstruct(out GlobalLabel global) => global = (GlobalLabel)Params[0];
    }

    public sealed record EntryPoint : Macro
    {
        public EntryPoint() : base(new MacroLabel("entryPoint")) { }
    }

    public sealed record Initialize : Macro
    {
        public Initialize() : base(new MacroLabel("initialize")) { }
    }

    public sealed record ClearMemory : Macro
    {
        public ClearMemory() : base(new MacroLabel("clearMemory")) { }
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

    public abstract record StackLetOp : LetOp
    {
        internal StackLetOp(Label variableLabel, Label valueLabel)
            : base(variableLabel, valueLabel) { }
    }
    
    public sealed record StackTypeOp : StackLetOp
    {
        public StackTypeLabel StackLabel => (StackTypeLabel)VariableLabel;
        //public TypeLabel TypeLabel => (TypeLabel)ValueLabel;

        public StackTypeOp(StackTypeLabel stackLabel, TypeLabel typeLabel)
            : base(stackLabel, typeLabel) { }

        public StackTypeOp(StackTypeLabel stackLabel, StackTypeLabel typeLabel)
            : base(stackLabel, typeLabel) { }
    }

    public sealed record StackSizeOp : StackLetOp
    {
        public StackSizeLabel StackLabel => (StackSizeLabel)VariableLabel;
        //public SizeLabel SizeLabel => (SizeLabel)ValueLabel;

        public StackSizeOp(StackSizeLabel stackLabel, SizeLabel sizeLabel)
            : base(stackLabel, sizeLabel) { }

        public StackSizeOp(StackSizeLabel stackLabel, StackSizeLabel sizeLabel)
            : base(stackLabel, sizeLabel) { }
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
        : Label($"SIZE_{Type.NamespaceAndName()}");

    public sealed record TypeLabel(TypeReference Type)
        : Label($"TYPE_{Type.NamespaceAndName()}");

    /// <summary>Label referring to the address of a global.</summary>
    public sealed record GlobalLabel(string Name, bool Predefined = false) : Label(Name);

    /// <summary>Label referring to the address of a local.</summary>
    public sealed record LocalLabel(string Name) : Label(Name);

    /// <summary>Label referring to a defined macro.</summary>
    public sealed record MacroLabel(string Name) : Label(Name);

    public sealed record InstructionLabel(string Name) : Label(Name);

    public sealed record FunctionLabel(string Name) : Label(Name);

    public sealed record StackTypeLabel(int Index) 
        : Label($"STACK_TYPEOF_{Index}");

    public sealed record StackSizeLabel(int Index)
        : Label($"STACK_SIZEOF_{Index}");

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
