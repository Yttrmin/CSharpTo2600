using System;
using System.Collections.Generic;
using System.Text;

namespace VCSFramework
{
	public static class Registers
	{
		public static byte A { [IgnoreImplementation][OverrideWithLoadToRegister("A")] set { } }
		public static byte X { [IgnoreImplementation][OverrideWithLoadToRegister("X")] set { } }
		public static byte Y { [IgnoreImplementation][OverrideWithLoadToRegister("Y")] set { } }

		// TIA_REGISTERS_WRITE
		public static byte AudC0  { [IgnoreImplementation][OverrideWithStoreToSymbol("AUDC0")] set { } }
		public static byte AudC1  { [IgnoreImplementation][OverrideWithStoreToSymbol("AUDC1")] set { } }
		public static byte AudF0  { [IgnoreImplementation][OverrideWithStoreToSymbol("AUDF0")] set { } }
		public static byte AudF1  { [IgnoreImplementation][OverrideWithStoreToSymbol("AUDF1")] set { } }
		public static byte AudV0  { [IgnoreImplementation][OverrideWithStoreToSymbol("AUDV0")] set { } }
		public static byte AudV1  { [IgnoreImplementation][OverrideWithStoreToSymbol("AUDV1")] set { } }
		public static byte ColuBk { [IgnoreImplementation][OverrideWithStoreToSymbol("COLUBK")] set { } }
		public static byte ColuP0 { [IgnoreImplementation][OverrideWithStoreToSymbol("COLUP0")] set { } }
		public static byte ColuP1 { [IgnoreImplementation][OverrideWithStoreToSymbol("COLUP1")] set { } }
		public static byte ColuPf { [IgnoreImplementation][OverrideWithStoreToSymbol("COLUPF")] set { } }
		public static byte CtrlPf { [IgnoreImplementation][OverrideWithStoreToSymbol("CTRLPF")] set { } }
		[IgnoreImplementation][OverrideWithStoreToSymbol("CXCLR", true)]
		public static void CxClr() { }
		public static byte EnaM0  { [IgnoreImplementation][OverrideWithStoreToSymbol("ENAM0")] set { } }
		public static byte EnaM1  { [IgnoreImplementation][OverrideWithStoreToSymbol("ENAM1")] set { } }
		public static byte EnaBl  { [IgnoreImplementation][OverrideWithStoreToSymbol("ENABL")] set { } }
		public static byte GrP0   { [IgnoreImplementation][OverrideWithStoreToSymbol("GRP0")] set { } }
		public static byte GrP1   { [IgnoreImplementation][OverrideWithStoreToSymbol("GRP1")] set { } }
		public static byte HMBl   { [IgnoreImplementation][OverrideWithStoreToSymbol("HMBL")] set { } }
		[IgnoreImplementation][OverrideWithStoreToSymbol("HMCLR", true)]
		public static void HmClr() { }
		public static byte HMP0   { [IgnoreImplementation][OverrideWithStoreToSymbol("HMP0")] set { } }
		public static byte HMP1   { [IgnoreImplementation][OverrideWithStoreToSymbol("HMP1")] set { } }
		public static byte HMM0   { [IgnoreImplementation][OverrideWithStoreToSymbol("HMM0")] set { } }
		public static byte HMM1   { [IgnoreImplementation][OverrideWithStoreToSymbol("HMM1")] set { } }
		[IgnoreImplementation][OverrideWithStoreToSymbol("HMOVE", true)]
		public static void HMove() { }
		public static byte NuSiz0 { [IgnoreImplementation][OverrideWithStoreToSymbol("NUSIZ0")] set { } }
		public static byte NuSiz1 { [IgnoreImplementation][OverrideWithStoreToSymbol("NUSIZ1")] set { } }
		public static byte Pf0    { [IgnoreImplementation][OverrideWithStoreToSymbol("PF0")] set { } }
		public static byte Pf1    { [IgnoreImplementation][OverrideWithStoreToSymbol("PF1")] set { } }
		public static byte Pf2    { [IgnoreImplementation][OverrideWithStoreToSymbol("PF2")] set { } }
		public static byte RefP0  { [IgnoreImplementation][OverrideWithStoreToSymbol("REFP0")] set { } }
		public static byte RefP1  { [IgnoreImplementation][OverrideWithStoreToSymbol("REFP1")] set { } }
		[IgnoreImplementation][OverrideWithStoreToSymbol("RESBL", true)]
		public static void ResBl() { }
		[IgnoreImplementation][OverrideWithStoreToSymbol("RESM0", true)]
		public static void ResM0() { }
		[IgnoreImplementation][OverrideWithStoreToSymbol("RESM1", true)]
		public static void ResM1() { }
		[IgnoreImplementation][OverrideWithStoreToSymbol("RESP0", true)]
		public static void ResP0() { }
		[IgnoreImplementation][OverrideWithStoreToSymbol("RESP1", true)]
		public static void ResP1() { }
		public static byte ResMP0 { [IgnoreImplementation][OverrideWithStoreToSymbol("RESMP0")] set { } }
		public static byte ResMP1 { [IgnoreImplementation][OverrideWithStoreToSymbol("RESMP1")] set { } }
		[IgnoreImplementation][OverrideWithStoreToSymbol("RSYNC", true)]
		public static void RSync() { }
		public static byte VBlank { [IgnoreImplementation][OverrideWithStoreToSymbol("VBLANK")] set { } }
		public static byte VDelP0 { [IgnoreImplementation][OverrideWithStoreToSymbol("VDELP0")] set { } }
		public static byte VDelP1 { [IgnoreImplementation][OverrideWithStoreToSymbol("VDELP1")] set { } }
		public static byte VDelBl { [IgnoreImplementation][OverrideWithStoreToSymbol("VDELBl")] set { } }
		public static byte VSync  { [IgnoreImplementation][OverrideWithStoreToSymbol("VSYNC")] set { } }
		[IgnoreImplementation][OverrideWithStoreToSymbol("WSYNC", true)]
		public static void WSync() { }
		
		// TIA_REGISTERS_READ

		public static byte CxM0P { [IgnoreImplementation][OverrideWithLoadFromSymbol("CXM0P")] get { throw new NotImplementedException(); } }
		public static byte CxM1P { [IgnoreImplementation][OverrideWithLoadFromSymbol("CXM1P")] get { throw new NotImplementedException(); } }
		public static byte CxP0FB { [IgnoreImplementation][OverrideWithLoadFromSymbol("CXP0FB")] get { throw new NotImplementedException(); } }
		public static byte CxP1FB { [IgnoreImplementation][OverrideWithLoadFromSymbol("CXP1FB")] get { throw new NotImplementedException(); } }
		public static byte CxM0FB { [IgnoreImplementation][OverrideWithLoadFromSymbol("CXM0FB")] get { throw new NotImplementedException(); } }
		public static byte CxM1FB { [IgnoreImplementation][OverrideWithLoadFromSymbol("CXM1FB")] get { throw new NotImplementedException(); } }
		public static byte CxBlPf { [IgnoreImplementation][OverrideWithLoadFromSymbol("CXBLPF")] get { throw new NotImplementedException(); } }
		public static byte CxPPMM { [IgnoreImplementation][OverrideWithLoadFromSymbol("CXPPMM")] get { throw new NotImplementedException(); } }

		//

		public static byte InTim  { [IgnoreImplementation][OverrideWithLoadFromSymbol("INTIM")] get { throw new NotImplementedException(); } }
		public static byte Tim64T { [IgnoreImplementation][OverrideWithStoreToSymbol("TIM64T")] set { } }
	}
}
