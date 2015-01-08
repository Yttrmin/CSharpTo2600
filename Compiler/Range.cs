namespace CSharpTo2600.Compiler
{
    internal struct Range
    {
        public readonly int Start;
        public readonly int End;

        public Range(int Start, int End)
        {
            this.Start = Start;
            this.End = End;
        }

        public override string ToString()
        {
            return "\{Start} to \{End}";
        }
    }
}
