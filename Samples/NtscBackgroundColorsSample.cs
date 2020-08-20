using static VCSFramework.Registers;

namespace Samples
{
    public static class NtscBackgroundColorsSample
    {
		private static byte BackgroundColor; // Support for static fields.
		private static byte Increment; // @TODO @DELETEME A temporary contrivance to test globals a bit more.

		public static void Main()
		{
			// Processor and memory initialization code is automatically injected by the compiler into
			// the program's entry point, so there's no need to manually do it.
			Increment = 1;
		MainLoop:
			// Perform vertical sync.
			// This is the same logic that would be used in 6502 assembly as well.
			VSync = 0b10; // TIA write-only registers implemented as setter-only properties.
			WSync(); // TIA strobe registers implemented as methods.
			WSync();
			WSync();
			Tim64T = 43;
			VSync = 0;

			// Actual logic to increment and set the background color every frame.
			// The least significant bit is unused, so incrementing by 1 instead of 2 slows the flashing down.
			BackgroundColor += Increment;
			ColuBk = BackgroundColor;

			// Kill time until the vertical blank period is over.
			while (InTim != 0) ; // PIA read-only registers implemented as getter-only properties.

			WSync();
			VBlank = 0;

			// Visible image
			byte lines = 191; // Local variable support.
			while (lines != 0) // Support for while loops and some comparisons.
			{
				lines--;
				WSync();
			}
			
			WSync();
			VBlank = 0b10;

			// Overscan
			lines = 30;
			while (lines != 0)
			{
				lines--;
				WSync();
			}

			goto MainLoop; // goto support!
		}
    }
}
