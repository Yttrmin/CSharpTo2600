namespace ScreenColors
{
    static class Simple
    {
        //static long ULongVar;
        //static int int1, int2, int3, int4, int5, int6, int7, int8, int9;
        //static byte Test;
        static byte b1, b2, b3;
        static int i1;
        static byte bp1 { get; set; }
        static void Initialize()
        {
            i1 = 0x12ABCDEF;
            b1 = 5;
            i1 = b1;
            b1 = (byte)i1;
            bp1 = b1;
        }
    }
}
