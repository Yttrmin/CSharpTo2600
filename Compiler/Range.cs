namespace CSharpTo2600.Compiler
{
    internal struct Range
    {
        public readonly int Start;
        public readonly int End;

        public Range(int Start, int End)
        {
            if (End < Start)
            {
                throw new System.ArgumentException("End must be greater than or equal to Start.");
            }
            this.Start = Start;
            this.End = End;
        }

        public bool Overlaps(Range Other)
        {
            // For example:
            // 0-7 overlaps 5-12
            // 5-12 overlaps 0-7
            // 0-7 overlaps 7-12
            // 0-0 overlaps 0-0
            return Other.Start >= this.Start && Other.Start <= this.End
                || Other.End >= this.Start && Other.End <= this.End;
        }

        public bool Contains(Range Other)
        {
            return Other.Start >= this.Start
                && Other.End <= this.End;
        }

        public bool Contains(int Other)
        {
            return Other >= this.Start
                && Other <= this.End;
        }

        public static bool operator ==(Range A, Range B)
        {
            return A.Equals(B);
        }

        public static bool operator !=(Range A, Range B)
        {
            return !A.Equals(B);
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            // Guidelines say to return null if its not a Range,
            // but something's wrong if that happens.
            return Equals((Range)obj);
        }

        public bool Equals(Range Other)
        {
            return this.Start == Other.Start
                && this.End == Other.End;
        }

        public override string ToString()
        {
            return $"0x{Start.ToString("X")} to 0x{End.ToString("X")}";
        }
    }
}
