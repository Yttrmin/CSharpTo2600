using System;

namespace VCSFramework
{
	public static class Registers
	{
		public static byte A { [OverrideWithLoadToRegister("A")] set { } }
		public static byte X { [OverrideWithLoadToRegister("X")] set { } }
		public static byte Y { [OverrideWithLoadToRegister("Y")] set { } }

		// TIA_REGISTERS_WRITE
		public static byte AudC0  { [OverrideWithStoreToSymbol("AUDC0")] set { } }
		public static byte AudC1  { [OverrideWithStoreToSymbol("AUDC1")] set { } }
		public static byte AudF0  { [OverrideWithStoreToSymbol("AUDF0")] set { } }
		public static byte AudF1  { [OverrideWithStoreToSymbol("AUDF1")] set { } }
		public static byte AudV0  { [OverrideWithStoreToSymbol("AUDV0")] set { } }
		public static byte AudV1  { [OverrideWithStoreToSymbol("AUDV1")] set { } }
		public static byte ColuBk { [OverrideWithStoreToSymbol("COLUBK")] set { } }
		public static byte ColuP0 { [OverrideWithStoreToSymbol("COLUP0")] set { } }
		public static byte ColuP1 { [OverrideWithStoreToSymbol("COLUP1")] set { } }
		public static byte ColuPf { [OverrideWithStoreToSymbol("COLUPF")] set { } }
		public static byte CtrlPf { [OverrideWithStoreToSymbol("CTRLPF")] set { } }
		[OverrideWithStoreToSymbol("CXCLR", true)]
		public static void CxClr() { }
		public static byte EnaM0  { [OverrideWithStoreToSymbol("ENAM0")] set { } }
		public static byte EnaM1  { [OverrideWithStoreToSymbol("ENAM1")] set { } }
		public static byte EnaBl  { [OverrideWithStoreToSymbol("ENABL")] set { } }
		public static byte GrP0   { [OverrideWithStoreToSymbol("GRP0")] set { } }
		public static byte GrP1   { [OverrideWithStoreToSymbol("GRP1")] set { } }
		public static byte HMBl   { [OverrideWithStoreToSymbol("HMBL")] set { } }
		[OverrideWithStoreToSymbol("HMCLR", true)]
		public static void HmClr() { }
		public static byte HMP0   { [OverrideWithStoreToSymbol("HMP0")] set { } }
		public static byte HMP1   { [OverrideWithStoreToSymbol("HMP1")] set { } }
		public static byte HMM0   { [OverrideWithStoreToSymbol("HMM0")] set { } }
		public static byte HMM1   { [OverrideWithStoreToSymbol("HMM1")] set { } }
		[OverrideWithStoreToSymbol("HMOVE", true)]
		public static void HMove() { }
		public static byte NuSiz0 { [OverrideWithStoreToSymbol("NUSIZ0")] set { } }
		public static byte NuSiz1 { [OverrideWithStoreToSymbol("NUSIZ1")] set { } }
		public static byte Pf0    { [OverrideWithStoreToSymbol("PF0")] set { } }
		public static byte Pf1    { [OverrideWithStoreToSymbol("PF1")] set { } }
		public static byte Pf2    { [OverrideWithStoreToSymbol("PF2")] set { } }
		public static byte RefP0  { [OverrideWithStoreToSymbol("REFP0")] set { } }
		public static byte RefP1  { [OverrideWithStoreToSymbol("REFP1")] set { } }
		[OverrideWithStoreToSymbol("RESBL", true)]
		public static void ResBl() { }
		[OverrideWithStoreToSymbol("RESM0", true)]
		public static void ResM0() { }
		[OverrideWithStoreToSymbol("RESM1", true)]
		public static void ResM1() { }
		[OverrideWithStoreToSymbol("RESP0", true)]
		public static void ResP0() { }
		[OverrideWithStoreToSymbol("RESP1", true)]
		public static void ResP1() { }
		public static byte ResMP0 { [OverrideWithStoreToSymbol("RESMP0")] set { } }
		public static byte ResMP1 { [OverrideWithStoreToSymbol("RESMP1")] set { } }
		[OverrideWithStoreToSymbol("RSYNC", true)]
		public static void RSync() { }
		public static byte VBlank { [OverrideWithStoreToSymbol("VBLANK")] set { } }
		public static byte VDelP0 { [OverrideWithStoreToSymbol("VDELP0")] set { } }
		public static byte VDelP1 { [OverrideWithStoreToSymbol("VDELP1")] set { } }
		public static byte VDelBl { [OverrideWithStoreToSymbol("VDELBl")] set { } }
		public static byte VSync  { [OverrideWithStoreToSymbol("VSYNC")] set { } }
		[OverrideWithStoreToSymbol("WSYNC", true)]
		public static void WSync() { }
		
		// TIA_REGISTERS_READ

		public static byte CxM0P { [OverrideWithLoadFromSymbol("CXM0P")] get { throw new NotImplementedException(); } }
		public static byte CxM1P { [OverrideWithLoadFromSymbol("CXM1P")] get { throw new NotImplementedException(); } }
		public static byte CxP0FB { [OverrideWithLoadFromSymbol("CXP0FB")] get { throw new NotImplementedException(); } }
		public static byte CxP1FB { [OverrideWithLoadFromSymbol("CXP1FB")] get { throw new NotImplementedException(); } }
		public static byte CxM0FB { [OverrideWithLoadFromSymbol("CXM0FB")] get { throw new NotImplementedException(); } }
		public static byte CxM1FB { [OverrideWithLoadFromSymbol("CXM1FB")] get { throw new NotImplementedException(); } }
		public static byte CxBlPf { [OverrideWithLoadFromSymbol("CXBLPF")] get { throw new NotImplementedException(); } }
		public static byte CxPPMM { [OverrideWithLoadFromSymbol("CXPPMM")] get { throw new NotImplementedException(); } }

		//

		public static byte InTim  { [OverrideWithLoadFromSymbol("INTIM")] get { throw new NotImplementedException(); } }
		public static byte TimInt { [OverrideWithLoadFromSymbol("TIMINT")] get { throw new NotImplementedException(); } }
		public static byte Tim64T { [OverrideWithStoreToSymbol("TIM64T")] set { } }
	}
}
