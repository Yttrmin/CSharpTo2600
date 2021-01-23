/*
.method public hidebysig static void  Main() cil managed
{
  // Code size       140 (0x8c)
  .maxstack  2
  .locals init (uint8 V_0)
  IL_0000:  ldc.i4.1
  IL_0001:  stsfld     uint8 Samples.NtscBackgroundColorsSample::Increment
  IL_0006:  ldc.i4.2
  IL_0007:  call       void [VCSFramework]VCSFramework.Registers::set_VSync(uint8)
  IL_000c:  call       void [VCSFramework]VCSFramework.Registers::WSync()
  IL_0011:  call       void [VCSFramework]VCSFramework.Registers::WSync()
  IL_0016:  call       void [VCSFramework]VCSFramework.Registers::WSync()
  IL_001b:  ldc.i4.s   43
  IL_001d:  call       void [VCSFramework]VCSFramework.Registers::set_Tim64T(uint8)
  IL_0022:  ldc.i4.0
  IL_0023:  call       void [VCSFramework]VCSFramework.Registers::set_VSync(uint8)
  IL_0028:  ldsfld     uint8 Samples.NtscBackgroundColorsSample::BackgroundColor
  IL_002d:  ldsfld     uint8 Samples.NtscBackgroundColorsSample::Increment
  IL_0032:  add
  IL_0033:  conv.u1
  IL_0034:  stsfld     uint8 Samples.NtscBackgroundColorsSample::BackgroundColor
  IL_0039:  ldsfld     uint8 Samples.NtscBackgroundColorsSample::BackgroundColor
  IL_003e:  call       void [VCSFramework]VCSFramework.Registers::set_ColuBk(uint8)
  IL_0043:  call       uint8 [VCSFramework]VCSFramework.Registers::get_InTim()
  IL_0048:  brtrue.s   IL_0043
  IL_004a:  call       void [VCSFramework]VCSFramework.Registers::WSync()
  IL_004f:  ldc.i4.0
  IL_0050:  call       void [VCSFramework]VCSFramework.Registers::set_VBlank(uint8)
  IL_0055:  ldc.i4     0xbf
  IL_005a:  stloc.0
  IL_005b:  br.s       IL_0067
  IL_005d:  ldloc.0
  IL_005e:  ldc.i4.1
  IL_005f:  sub
  IL_0060:  conv.u1
  IL_0061:  stloc.0
  IL_0062:  call       void [VCSFramework]VCSFramework.Registers::WSync()
  IL_0067:  ldloc.0
  IL_0068:  brtrue.s   IL_005d
  IL_006a:  call       void [VCSFramework]VCSFramework.Registers::WSync()
  IL_006f:  ldc.i4.2
  IL_0070:  call       void [VCSFramework]VCSFramework.Registers::set_VBlank(uint8)
  IL_0075:  ldc.i4.s   30
  IL_0077:  stloc.0
  IL_0078:  br.s       IL_0084
  IL_007a:  ldloc.0
  IL_007b:  ldc.i4.1
  IL_007c:  sub
  IL_007d:  conv.u1
  IL_007e:  stloc.0
  IL_007f:  call       void [VCSFramework]VCSFramework.Registers::WSync()
  IL_0084:  ldloc.0
  IL_0085:  brtrue.s   IL_007a
  IL_0087:  br         IL_0006
} // end of method NtscBackgroundColorsSample::Main
*/

.cpu "6502"
.format "flat"
* = $F000

SIZE_System_Byte = 1

BackgroundColor = $82
Increment = $84

LOCAL_Main_00 = $85

VSYNC = $00
VBLANK = $01
WSYNC = $02
COLUBK = $09
TIM64T = $296
INTIM = $284

.include "../../../VCSCompiler/vil.h"

Start 
	.initialize
	.clearMemory

	.assignConstantToGlobal $1, Increment, INT_Byte_SIZE
	.pushConstant 1, SIZE_System_Byte
	.popToGlobal GLOBAL_Main_Increment, SIZE_System_Byte

Main__IL_0006
	.pushConstant 2, SIZE_System_Byte
	.popToGlobal VSYNC, SIZE_System_Byte
	.storeTo WSYNC
	.storeTo WSYNC
	.storeTo WSYNC
	.pushConstant 43, SIZE_System_Byte
	.popToGlobal TIM64T, SIZE_System_Byte
	.pushConstant 0, SIZE_System_Byte
	.popToGlobal VSYNC
	.pushGlobal GLOBAL_Main_BackgroundColor, SIZE_System_Byte
	.pushGlobal GLOBAL_Main_Increment, SIZE_System_Byte
	.addFromStack // @PARAMS
	.convertToByte // @PARAMS
	.popToGlobal GLOBAL_Main_BackgroundColor, SIZE_System_Byte
	.pushGlobal GLOBAL_Main_BackgroundColor, SIZE_System_Byte
	.popToGlobal COLUBK, SIZE_System_Byte

Main__IL_0043
	.pushGlobal INTIM, SIZE_System_Byte
	.branchTrueFromStack Main__IL_0043

	.storeTo WSYNC
	.pushConstant 0, SIZE_System_Byte
	.popToGlobal VBLANK, SIZE_System_Byte
	.pushConstant $BF, SIZE_System_Byte
	.popToLocal LOCAL_Main_V_0, SIZE_System_Byte
	.branchTo Main__IL_0067

Main__IL_005d
	.pushLocal LOCAL_Main_V_0, SIZE_System_Byte
	.pushConstant 1, SIZE_System_Byte
	.subFromStack // @PARAMS
	.convertToByte // @PARAMS
	.popToGlobal LOCAL_Main_V_0, SIZE_System_Byte
	.storeTo WSYNC

Main__IL_0067
	.pushLocal LOCAL_Main_V_0, SIZE_System_Byte
	.branchTrueFromStack Main__IL_005d

	.storeTo WSYNC
	.pushConstant 2, SIZE_System_Byte
	.popToGlobal VBLANK, SIZE_System_Byte
	.pushConstant 30, SIZE_System_Byte
	.popToLocal LOCAL_Main_V_0, SIZE_System_Byte
	.branchTo Main__IL_0084

Main__IL_007a
	.pushLocal LOCAL_Main_V_0, SIZE_System_Byte
	.pushConstant 1, SIZE_System_Byte
	.subFromStack // @PARAMS
	.convertToByte // @PARAMS
	.popToLocal LOCAL_Main_V_0, SIZE_System_Byte
	.storeTo WSYNC
	.pushLocal LOCAL_Main_V_0, SIZE_System_Byte
	.branchTrueFromStack Main__IL_007a

	.branch Main__IL_0006

// Special memory locations. Tells the 6502 where to go.
* = $FFFC
.word Start
.word Start