using VCSFramework.V2;
using static VCSFramework.V2.AssemblyUtilities;

namespace Samples
{
    static class InlineAssemblySample
    {
        private const string BackgroundColorAlias = "ALIAS_MyBackgroundColor";
        [InlineAssemblyAlias(BackgroundColorAlias)]
        private static byte BackgroundColor;

        public static void Main()
        {
            while (true)
            {
                InlineAssembly($@"
                    LDA {BackgroundColorAlias}
                    STA COLUBK");
                BackgroundColor++;
            }
        }
    }
}
