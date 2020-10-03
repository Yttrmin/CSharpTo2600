using VCSFramework.V2;

namespace Samples
{
    static class InlineAssemblySample
    {
        public static void Main()
        {
            while (true)
            {
                AssemblyUtilities.InlineAssembly(@"
                LDA $80
                STA COLUBK
                INC $80");
            }
        }
    }
}
