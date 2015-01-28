using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpTo2600.Framework.Assembly
{
    public abstract class AssemblyLine
    {
        public string Text { get; }
        public string Comment { get; }

        protected AssemblyLine(string Text, string Comment)
        {
            this.Text = Text;
            this.Comment = Comment;
        }

        protected AssemblyLine(string Text)
            : this(Text, String.Empty)
        {
        }

        public override string ToString()
        {
            if(String.IsNullOrEmpty(Comment))
            {
                return Text;
            }
            else
            {
                return $"{Text} ; {Comment}";
            }
        }
    }

    public sealed class Instruction : AssemblyLine
    {
        public string OpCode { get; }
        public string Argument { get; }
        public int Cycles { get; }

        private Instruction(string OpCode, string Argument, int Cycles)
            : base($"{OpCode} {Argument}")
        {
            this.OpCode = OpCode;
            this.Argument = Argument;
            this.Cycles = Cycles;
        }
    }

    public sealed class Comment : AssemblyLine
    {
        internal Comment(string Comment, int Indentation)
            : base($"{new string('\t', Indentation)}; {Comment}")
        {
        }
    }

    public sealed class Symbol : AssemblyLine
    {
        public string Name { get; }
        public byte? Value { get; }

        internal Symbol(string Name)
            : base(GetText(Name, null))
        {
            this.Name = Name;
            this.Value = null;
        }

        internal Symbol(string Name, byte Value)
            : base(GetText(Name, Value))
        {
            this.Name = Name;
            this.Value = Value;
        }

        private static string GetText(string Name, byte? Value)
        {
            if (Value.HasValue)
            {
                return $"{Name} = ${Value.Value.ToString("X2")}";
            }
            else
            {
                return $"{Name}:";
            }
        }
    }

    public sealed class PsuedoOp : AssemblyLine
    {
        internal PsuedoOp(string Text)
            : base(Text)
        {
            
        }
    }
}
