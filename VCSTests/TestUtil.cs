using System.Threading.Tasks;
using VCSCompiler;

namespace VCSTests
{
	internal static class TestUtil
    {
		public static async Task<RomInfo> CompileFromText(string source)
		{
			return await Compiler.CompileFromText(source, "./VCSFramework.dll", "../../../../Dependencies/DASM/dasm.exe");
		}
    }
}
