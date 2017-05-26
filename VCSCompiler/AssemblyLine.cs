using System;

namespace VCSCompiler.Assembly
{
	internal abstract class AssemblyLine
    {
        public string Text { get; }
        public string Comment { get; }

        protected AssemblyLine(string Text, string Comment)
        {
            this.Text = Text;
            this.Comment = Comment;
        }

        protected AssemblyLine(string Text)
            : this(Text, null)
        {
        }

        protected abstract AssemblyLine WithCommentInternal(string Comment);

        protected string MergeComment(string NewComment)
        {
            if (Comment == null)
            {
                return NewComment;
            }
            else
            {
                return $"{Comment} ;; {NewComment}";
            }
        }

        public AssemblyLine WithComment(string Comment)
        {
            return WithCommentInternal(Comment);
        }

        public override string ToString()
        {
            if (String.IsNullOrEmpty(Comment))
            {
                return Text;
            }
            else
            {
                return $"{Text} ; {Comment}";
            }
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            var AsLine = obj as AssemblyLine;
            if (AsLine == null)
            {
                return false;
            }

            return Equals(AsLine);
        }

        public bool Equals(AssemblyLine Other)
        {
            if (Other == null)
            {
                return false;
            }

            return Text == Other.Text;
        }
    }

	internal sealed class AssemblyInstruction : AssemblyLine
    {
        public string OpCode { get; }
        public string Argument { get; }
        public int Cycles { get; }
        // http://homepage.ntlworld.com/cyborgsystems/CS_Main/6502/6502.htm
        // is a good resource for getting size (I-Len)
        public int Size { get; }

        internal AssemblyInstruction(string OpCode, string Argument, int Cycles, int Size, string Comment = null)
            : base($"\t{OpCode} {Argument}", Comment)
        {
            this.OpCode = OpCode;
            this.Argument = Argument;
            this.Cycles = Cycles;
            this.Size = Size;
        }

        internal AssemblyInstruction(string OpCode, int Cycles, int Size)
            : this(OpCode, null, Cycles, Size)
        {

        }

        protected override AssemblyLine WithCommentInternal(string Comment)
        {
            return new AssemblyInstruction(OpCode, Argument, Cycles, Size, MergeComment(Comment));
        }

        public new AssemblyInstruction WithComment(string Comment)
        {
            return (AssemblyInstruction)WithCommentInternal(Comment);
        }
    }

	internal sealed class Trivia : AssemblyLine
    {
        internal Trivia(string Text, string Comment = null)
            : base(Text, Comment)
        {

        }

        protected override AssemblyLine WithCommentInternal(string Comment)
        {
            return new Trivia(Text, MergeComment(Comment));
        }

        public new Trivia WithComment(string Comment)
        {
            return (Trivia)WithCommentInternal(Comment);
        }
    }

	internal sealed class Symbol : AssemblyLine
    {
        public string Name { get; }
        public ushort? Value { get; }

        internal Symbol(string Name, string Comment = null)
            : this(Name, null, Comment)
        {
        }

        internal Symbol(string Name, ushort? Value, string Comment = null)
            : base(GetText(Name, Value), Comment)
        {
            this.Name = Name;
            this.Value = Value;
        }

        protected override AssemblyLine WithCommentInternal(string Comment)
        {
            return new Symbol(Name, Value, MergeComment(Comment));
        }

        public new Symbol WithComment(string Comment)
        {
            return (Symbol)WithCommentInternal(Comment);
        }

        private static string GetText(string Name, ushort? Value)
        {
            if (Value.HasValue)
            {
                return $"{Name} = ${Value.Value.ToString("X4")}";
            }
            else
            {
                return $"{Name}:";
            }
        }
    }

	internal sealed class PsuedoOp : AssemblyLine
    {
        //@TODO
        internal PsuedoOp(string Text, string Comment = null)
            : base(Text, Comment)
        {

        }

        protected override AssemblyLine WithCommentInternal(string Comment)
        {
            return new PsuedoOp(Text, MergeComment(Comment));
        }

        public new PsuedoOp WithComment(string Comment)
        {
            return (PsuedoOp)WithCommentInternal(Comment);
        }
    }
}
