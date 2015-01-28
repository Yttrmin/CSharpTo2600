using System;

namespace CSharpTo2600.Framework.Assembly
{
    public static class AssemblyFactory
    {
        #region Miscellaneous
        public static Comment Comment(string Comment, int IndentationLevel = 1)
        {
            return new Comment(Comment, IndentationLevel);
        }
        public static AssemblyLine BlankLine()
        {
            throw new NotImplementedException();
        }
        #endregion

        #region Symbols
        public static Symbol Label(string Name)
        {
            return new Symbol(Name);
        }
        public static Symbol DefineSymbol(string Name, int Value)
        {
            if(Value < byte.MinValue || Value > byte.MaxValue)
            {
                throw new ArgumentException("Value must fit in a byte.");
            }
            return new Symbol(Name, (byte)Value);
        }
        #endregion

        #region PsuedoOps
        public static PsuedoOp Processor()
        {
            return new PsuedoOp("\tprocessor 6502");
        }

        public static PsuedoOp Org(int Address)
        {
            //@TODO - Min?
            if(Address > 0xFFFF)
            {
                throw new ArgumentException($"org address is out of range: 0x{Address.ToString("X4")}");
            }
            return new PsuedoOp($"\torg ${Address.ToString("X4")}");
        }

        public static PsuedoOp Include(string FileName)
        {
            return new PsuedoOp($"\tinclude \"{FileName}\"");
        }
        #endregion
    }
}
