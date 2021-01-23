using VCSFramework;
using static VCSFramework.AssemblyUtilities;

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
