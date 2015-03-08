namespace CSharpTo2600.Framework.Assembly
{
    public static class ReservedSymbols
    {
        public static readonly Symbol VSYNC = new Symbol(nameof(VSYNC), 0x00);
        public static readonly Symbol VBLANK = new Symbol(nameof(VBLANK), 0x01);
        public static readonly Symbol WSYNC = new Symbol(nameof(WSYNC), 0x02);
        public static readonly Symbol RSYNC = new Symbol(nameof(RSYNC), 0x03);
        public static readonly Symbol NUSIZ0 = new Symbol(nameof(NUSIZ0), 0x04);
        public static readonly Symbol NUSIZ1 = new Symbol(nameof(NUSIZ1), 0x05);
        public static readonly Symbol COLUP0 = new Symbol(nameof(COLUP0), 0x06);
        public static readonly Symbol COLUP1 = new Symbol(nameof(COLUP1), 0x07);
        public static readonly Symbol COLUPF = new Symbol(nameof(COLUPF), 0x08);
        public static readonly Symbol COLUBK = new Symbol(nameof(COLUBK), 0x09);
        public static readonly Symbol CTRLPF = new Symbol(nameof(CTRLPF), 0x0A);
        public static readonly Symbol REFP0 = new Symbol(nameof(REFP0), 0x0B);
        public static readonly Symbol REFP1 = new Symbol(nameof(REFP1), 0x0C);
        public static readonly Symbol PF0 = new Symbol(nameof(PF0), 0x0D);
        public static readonly Symbol PF1 = new Symbol(nameof(PF1), 0x0E);
        public static readonly Symbol PF2 = new Symbol(nameof(PF2), 0x0F);
        public static readonly Symbol RESP0 = new Symbol(nameof(RESP0), 0x10);
        public static readonly Symbol RESP1 = new Symbol(nameof(RESP1), 0x11);
        public static readonly Symbol RESM0 = new Symbol(nameof(RESM0), 0x12);
        public static readonly Symbol RESM1 = new Symbol(nameof(RESM1), 0x13);
        public static readonly Symbol RESBL = new Symbol(nameof(RESBL), 0x14);
        public static readonly Symbol AUDC0 = new Symbol(nameof(AUDC0), 0x15);
        public static readonly Symbol AUDC1 = new Symbol(nameof(AUDC1), 0x16);
        public static readonly Symbol AUDF0 = new Symbol(nameof(AUDF0), 0x17);
        public static readonly Symbol AUDF1 = new Symbol(nameof(AUDF1), 0x18);
        public static readonly Symbol GRP0 = new Symbol(nameof(GRP0), 0x19);
        public static readonly Symbol GRP1 = new Symbol(nameof(GRP1), 0x1A);
        public static readonly Symbol ENAM0 = new Symbol(nameof(ENAM0), 0x1B);
        public static readonly Symbol ENAM1 = new Symbol(nameof(ENAM1), 0x1C);
        public static readonly Symbol ENABL = new Symbol(nameof(ENABL), 0x1D);
        public static readonly Symbol HMP0 = new Symbol(nameof(HMP0), 0x1E);
        public static readonly Symbol HMP1 = new Symbol(nameof(HMP1), 0x1F);
        public static readonly Symbol HMM0 = new Symbol(nameof(HMM0), 0x20);
        public static readonly Symbol HMM1 = new Symbol(nameof(HMM1), 0x21);
        public static readonly Symbol HMBL = new Symbol(nameof(HMBL), 0x22);
        public static readonly Symbol VDELP0 = new Symbol(nameof(VDELP0), 0x23);
        public static readonly Symbol VDELP1 = new Symbol(nameof(VDELP1), 0x24);
        public static readonly Symbol VDELBL = new Symbol(nameof(VDELBL), 0x25);
        public static readonly Symbol RESMP0 = new Symbol(nameof(RESMP0), 0x26);
        public static readonly Symbol RESMP1 = new Symbol(nameof(RESMP1), 0x27);
        public static readonly Symbol HMOVE = new Symbol(nameof(HMOVE), 0x28);
        public static readonly Symbol HMCLR = new Symbol(nameof(HMCLR), 0x29);
        public static readonly Symbol CXCLR = new Symbol(nameof(CXCLR), 0x2A);
        //
        public static readonly Symbol CXM0P = new Symbol(nameof(CXM0P), 0x00);
        public static readonly Symbol CXM1P = new Symbol(nameof(CXM1P), 0x01);
        public static readonly Symbol CXP0FB = new Symbol(nameof(CXP0FB), 0x02);
        public static readonly Symbol CXP1FB = new Symbol(nameof(CXP1FB), 0x03);
        public static readonly Symbol CXM0FB = new Symbol(nameof(CXM0FB), 0x04);
        public static readonly Symbol CXM1FB = new Symbol(nameof(CXM1FB), 0x05);
        public static readonly Symbol CXBLPF = new Symbol(nameof(CXBLPF), 0x06);
        public static readonly Symbol CXPPMM = new Symbol(nameof(CXPPMM), 0x07);
        public static readonly Symbol INPT0 = new Symbol(nameof(INPT0), 0x08);
        public static readonly Symbol INPT1 = new Symbol(nameof(INPT1), 0x09);
        public static readonly Symbol INPT2 = new Symbol(nameof(INPT2), 0x0A);
        public static readonly Symbol INPT3 = new Symbol(nameof(INPT3), 0x0B);
        public static readonly Symbol INPT4 = new Symbol(nameof(INPT4), 0x0C);
        public static readonly Symbol INPT5 = new Symbol(nameof(INPT5), 0x0D);
        //
        public static readonly Symbol SWCHA = new Symbol(nameof(SWCHA), 0x280);
        public static readonly Symbol SWACNT = new Symbol(nameof(SWACNT), 0x281);
        public static readonly Symbol SWCHB = new Symbol(nameof(SWCHB), 0x282);
        public static readonly Symbol SWBCNT = new Symbol(nameof(SWBCNT), 0x283);
        public static readonly Symbol INTIM = new Symbol(nameof(INTIM), 0x284);
        public static readonly Symbol TIMINT = new Symbol(nameof(TIMINT), 0x285);
        //
        public static readonly Symbol TIM1T = new Symbol(nameof(TIM1T), 0x294);
        public static readonly Symbol TIM8T = new Symbol(nameof(TIM8T), 0x295);
        public static readonly Symbol TIM64T = new Symbol(nameof(TIM64T), 0x296);
        public static readonly Symbol T1024T = new Symbol(nameof(T1024T), 0x297);
    }
}
