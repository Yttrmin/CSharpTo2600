namespace CSharpTo2600.Framework
{
    public static class IOPorts
    {
        public static void WSync()
        {
            Instructions.STA(0x02);
        }
        public static byte BackgroundColor;
    }
}
