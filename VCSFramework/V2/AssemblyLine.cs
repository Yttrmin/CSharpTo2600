#nullable enable
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
        public ImmutableArray<Instruction> Instructions { get; init; }

        public Macro(MacroLabel label, params Label[] parameters)
        {
            Label = label;
            Params = parameters.ToImmutableArray();
            Instructions = ImmutableArray<Instruction>.Empty;
        }

        public Macro(Instruction instruction, MacroLabel label, params Label[] parameters)
        {
            Label = label;
            Params = parameters.ToImmutableArray();
            Instructions = new[] { instruction }.ToImmutableArray();
        }

        public override string ToString()
        {
            var paramString = string.Join(", ", Params);
            return $".{Label} {paramString}";
        }
    }

    public sealed record Branch : Macro
    {
        public Branch(InstructionLabel instruction)
            : base(new MacroLabel("branch"), instruction) { }

        public void Deconstruct(out InstructionLabel instruction)
            => instruction = (InstructionLabel)Params[0];
    }

    [PushStack(Count = 1)]
    public sealed record PushConstant : Macro
    {
        public PushConstant(Instruction instruction, ConstantLabel constant, SizeLabel constantSize) 
            : base(instruction, new MacroLabel("pushConstant"), constant, constantSize) { }

        public void Deconstruct(out ConstantLabel constant) => constant = (ConstantLabel)Params[0];
    }

    [PushStack(Count = 1)]
    public sealed record PushGlobal : Macro
    {
        public PushGlobal(Instruction instruction, GlobalLabel global, SizeLabel globalSize)
            : base(instruction, new MacroLabel("pushGlobal"), global, globalSize) { }

        public void Deconstruct(out GlobalLabel global) => global = (GlobalLabel)Params[0];
    }

    [PopStack(Count = 1)]
    public sealed record PopToGlobal : Macro
    {
        public PopToGlobal(Instruction instruction, GlobalLabel label)
            : base(instruction, new MacroLabel("popToGlobal"), label) { }

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

    public sealed record LetOp : PsuedoOp
    {
        public LetOp() { }

        public override string ToString()
        {
            throw new NotImplementedException();
        }
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
    public sealed record ConstantLabel(string Name) : Label(Name);

    /// <summary>Label referring to the size of a type.</summary>
    public sealed record SizeLabel(string Name) : Label(Name);

    /// <summary>Label referring to the address of a global.</summary>
    public sealed record GlobalLabel(string Name) : Label(Name);

    /// <summary>Label referring to the address of a local.</summary>
    public sealed record LocalLabel(string Name) : Label(Name);

    /// <summary>Label referring to a defined macro.</summary>
    public sealed record MacroLabel(string Name) : Label(Name);

    public sealed record InstructionLabel(string Name) : Label(Name);

    public sealed record FunctionLabel(string Name) : Label(Name);

    // [StackEffect(POP, 2)]
    // [StackEffect(PUSH, 1)]
    // [RegisterEffect(Accumulator, ResultLSB)]
    // public struct AddFromStackMacro

    // public Macro AddFromStackMacro => new Macro(addFromStack", new StackEffect(POP, 2), new StackEffect(PUSH, 1), new RegisterEffect(Accumulator, StackLSB));
}
