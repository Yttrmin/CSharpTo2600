using VCSFramework;
using static VCSFramework.Registers;

namespace Samples
{
    public static class TableTopTennis
    {
		private const byte BGColor = 0x48;
		private const byte PFColor = 0x34;
		private const byte P0Color = 0xC6;
		private const byte P1Color = 0x94;
		private const byte BallColor = 0x0E;
		private const byte PaddleOnSprite = 0b00011000;
		private const byte PaddleOffSprite = 0b00000000;
		private const byte PaddleHeight = 16;
		private const byte BallHeight = 2;
		private const byte MaxPaddleY = 186 - PaddleHeight;
		private const byte MinPaddleY = 14;
		private const byte P0Goal = 0xC2;
		private const byte P1Goal = 0x33;
		private const byte BallStartX = 0x7A;
		private const byte BallStartY = 96;
		private const byte BallBaseTone = 0b00000001;
		private const byte BallXSpeedCap = 2;
		private const byte BallYSpeedCap = 3;
		private const byte BallYExVelMax = BallYSpeedCap + 1;
		private const byte BallYExVelMin = 255 - BallYSpeedCap;
		private const byte BallVolleyIncrement = 2;
		private const byte AITickRate = 2;
		private const byte ScoreLimit = 11;
		private const byte StartingWaitTime = 255;
		private const byte EndWaitTime = 80;

		private static byte YPosP0;
		private static byte YPosP1;
		private static byte YPosBall;
		private static byte ScoreP0;
		private static byte ScoreP1;
		private static byte SpriteP0;
		private static byte SpriteP1;
		private static byte BallEnabled;
		private static byte YVelBall;
		private static byte XVelBall;
		private static byte XPosBall;
		private static byte DeltaP0;
		private static byte DeltaP1;
		private static byte VolleyCount;
		private static byte ScoreP0MemLoc;
		private static byte ScoreP1MemLoc;
		private static byte AITicks;
		private static byte VictoryTime;
		private static byte WaitTime;
		private static byte NewXVelBall;

		public static void Main()
		{
			Initialize();
		}
		
		[AlwaysInline]
		public static void Initialize()
		{
			ColuBk = BGColor;
			ColuPf = PFColor;
			ColuP0 = P0Color;
			ColuP1 = P1Color;
			AudV0 = 0b00001111; // Crank the volume up.
			AudV1 = 0b00001111;
			AudF1 = 0b0000_0110;
			// Position paddles:
			WSync();
			// ~22 Machine cycles of horizontal blank.
			// First we do P0's paddle.
			//Timing.ConsumeCycles(21);
			ResP0();
			// Now for P1's paddle...
			//Timing.ConsumeCycles(42);
			ResP1();
			// Now for more fine-tuned adjustments...
			HMP1 = 0b0111_0000;
			WSync();
			HMove(); // Shift it 7 to the left...
			HMP1 = 0b0001_0000;
			WSync();
			HMove(); // And 1 more to the left and perfect!
			HmClr();
			// This could probably be cleaned up without HMOVE, but never touch again. // TODO - Touch again.
			YPosP0 = BallStartY;
			YPosP1 = BallStartY;
			YPosBall = BallStartY;

			ResetBall();
		}
		
		public static void ResetBall()
		{
			WSync();
			//Timing.ConsumeCycles(45);
			ResBl();
			XPosBall = BallStartX;
			YPosBall = BallStartY;
			// TODO - Implement starting Y-velocity table.
		}
    }
}
