using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace X68SoundMCh {
    internal class CGlobal {
        public const int OpmRate = 62500;    // 入力クロック÷64
        private const int Samprate = OpmRate; // アップサンプリングもダウンサンプリングもしない
        public const int OpmRateSamprateRatio = OpmRate / Samprate; // クロック比（1以外は異常動作）

        public const int N_CH = 8;

        public const int PRECISION_BITS = 10;
        public const int PRECISION = 1 << PRECISION_BITS;

        private const int SIZESINTBL_BITS = 10;
        public const int SIZESINTBL = 1 << SIZESINTBL_BITS;
        public const int MAXSINVAL = 1 << (SIZESINTBL_BITS + 2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double CalcSin(double i) {
            var res = System.Math.Sin(2.0 * System.Math.PI * i / SIZESINTBL);
            return (res * MAXSINVAL);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double ConvDeltaTFromPitch(double Pitch) { // 半音分解能は64 この精度は他の精度と関連している
            var res = 162.64; // 恐らくPRECISIONと関連している
            res *= System.Math.Pow(2, Pitch / (12 * 64));
            return (res * 64);
        }

        private const int SIZEALPHATBL_BITS = 10; // この精度は他の精度と関連している
        public const int SIZEALPHATBL = 1 << SIZEALPHATBL_BITS;

        private const bool ConvAlphaFromEnv_ConvNoiseAlphaFromEnv_UseTable = true;
        private static double[] ConvAlphaFromEnv_Table = new double[SIZEALPHATBL];
        private static double[] ConvNoiseAlphaFromEnv_Table = new double[SIZEALPHATBL];

        public static void InitTable() {
            if (ConvAlphaFromEnv_ConvNoiseAlphaFromEnv_UseTable) {
                for (int Tl = 0; Tl < ConvAlphaFromEnv_Table.Length; Tl++) {
                    ConvAlphaFromEnv_Table[Tl] = System.Math.Pow(2.0, -(SIZEALPHATBL - Tl) * (128.0 / 8.0) / SIZEALPHATBL) * 1.0 * 1.0 * PRECISION + 0.0;
                }
                for (int Tl = 0; Tl < ConvNoiseAlphaFromEnv_Table.Length; Tl++) {
                    ConvNoiseAlphaFromEnv_Table[Tl] = Tl * 1.0 / SIZEALPHATBL * 1.0 * 0.25 * PRECISION + 0.0; // Noise音量はOpの1/4
                }
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double ConvAlphaFromEnv(int Tl) { // 最終的なエンベロープ出力値
            if (Tl < 0) { return (0); }
            if (SIZEALPHATBL <= Tl) { return SIZEALPHATBL; }

            if (ConvAlphaFromEnv_ConvNoiseAlphaFromEnv_UseTable) {
                return ConvAlphaFromEnv_Table[Tl];
            } else {
                return System.Math.Pow(2.0, -(SIZEALPHATBL - Tl) * (128.0 / 8.0) / SIZEALPHATBL) * 1.0 * 1.0 * PRECISION + 0.0;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double ConvNoiseAlphaFromEnv(int Tl) { // 最終的なエンベロープ出力値
            if (Tl < 0) { return (0); }
            if (SIZEALPHATBL <= Tl) { return SIZEALPHATBL/4; }

            if (ConvAlphaFromEnv_ConvNoiseAlphaFromEnv_UseTable) {
                return ConvNoiseAlphaFromEnv_Table[Tl];
            } else {
                return Tl * 1.0 / SIZEALPHATBL * 1.0 * 0.25 * PRECISION + 0.0; // Noise音量はOpの1/4
            }
        }

        public class CDatas {
            public readonly int[] D1LTBL = new int[16];

            public readonly int[] DT1TBL = new int[128 + 4];
            public readonly int[] DT1TBL_org = new int[128 + 4] {
                0, 0, 1, 2,
                0, 0, 1, 2,
                0, 0, 1, 2,
                0, 0, 1, 2,
                0, 1, 2, 2,
                0, 1, 2, 3,
                0, 1, 2, 3,
                0, 1, 2, 3,
                0, 1, 2, 4,
                0, 1, 3, 4,
                0, 1, 3, 4,
                0, 1, 3, 5,
                0, 2, 4, 5,
                0, 2, 4, 6,
                0, 2, 4, 6,
                0, 2, 5, 7,
                0, 2, 5, 8,
                0, 3, 6, 8,
                0, 3, 6, 9,
                0, 3, 7, 10,
                0, 4, 8, 11,
                0, 4, 8, 12,
                0, 4, 9, 13,
                0, 5, 10, 14,
                0, 5, 11, 16,
                0, 6, 12, 17,
                0, 6, 13, 19,
                0, 7, 14, 20,
                0, 8, 16, 22,
                0, 8, 16, 22,
                0, 8, 16, 22,
                0, 8, 16, 22,

                0,0,0,0,
            };

            public struct XR_ELE {
                public int iand;
                public int add;
                public XR_ELE(int _iand, int _add) {
                    iand = _iand;
                    add = _add;
                }
            };

            public readonly XR_ELE[] XRTBL = new XR_ELE[64 + 32] {
                new XR_ELE(4095,8),
                new XR_ELE( 2047,5),new XR_ELE(2047,6),new XR_ELE(2047,7),new XR_ELE(2047,8),
                new XR_ELE(1023,5),new XR_ELE(1023,6),new XR_ELE(1023,7),new XR_ELE(1023,8),
                new XR_ELE(511,5),new XR_ELE(511,6),new XR_ELE(511,7),new XR_ELE(511,8),
                new XR_ELE(255,5),new XR_ELE(255,6),new XR_ELE(255,7),new XR_ELE(255,8),
                new XR_ELE(127,5),new XR_ELE(127,6),new XR_ELE(127,7),new XR_ELE(127,8),
                new XR_ELE(63,5),new XR_ELE(63,6),new XR_ELE(63,7),new XR_ELE(63,8),
                new XR_ELE(31,5),new XR_ELE(31,6),new XR_ELE(31,7),new XR_ELE(31,8),
                new XR_ELE(15,5),new XR_ELE(15,6),new XR_ELE(15,7),new XR_ELE(15,8),
                new XR_ELE(7,5),new XR_ELE(7,6),new XR_ELE(7,7),new XR_ELE(7,8),
                new XR_ELE(3,5),new XR_ELE(3,6),new XR_ELE(3,7),new XR_ELE(3,8),
                new XR_ELE(1,5),new XR_ELE(1,6),new XR_ELE(1,7),new XR_ELE(1,8),
                new XR_ELE(0,5),new XR_ELE(0,6),new XR_ELE(0,7),new XR_ELE(0,8),
                new XR_ELE(0,10),new XR_ELE(0,12),new XR_ELE(0,14),new XR_ELE(0,16),
                new XR_ELE(0,20),new XR_ELE(0,24),new XR_ELE(0,28),new XR_ELE(0,32),
                new XR_ELE(0,40),new XR_ELE(0,48),new XR_ELE(0,56),new XR_ELE(0,64),
                new XR_ELE(0,64),new XR_ELE(0,64),new XR_ELE(0,64),
                new XR_ELE(0,64),new XR_ELE(0,64),new XR_ELE(0,64),new XR_ELE(0,64),new XR_ELE(0,64),new XR_ELE(0,64),new XR_ELE(0,64),new XR_ELE(0,64),
                new XR_ELE(0,64),new XR_ELE(0,64),new XR_ELE(0,64),new XR_ELE(0,64),new XR_ELE(0,64),new XR_ELE(0,64),new XR_ELE(0,64),new XR_ELE(0,64),
                new XR_ELE(0,64),new XR_ELE(0,64),new XR_ELE(0,64),new XR_ELE(0,64),new XR_ELE(0,64),new XR_ELE(0,64),new XR_ELE(0,64),new XR_ELE(0,64),
                new XR_ELE(0,64),new XR_ELE(0,64),new XR_ELE(0,64),new XR_ELE(0,64),new XR_ELE(0,64),new XR_ELE(0,64),new XR_ELE(0,64),new XR_ELE(0,64),
            };

            public readonly int[] DT2TBL = new int[4] { 0, 384, 500, 608 };

            public CDatas() {
                // D1L → D1l 変換テーブルを作成
                for (var i = 0; i < 15; ++i) {
                    D1LTBL[i] = i * 2;
                }
                D1LTBL[15] = (15 + 16) * 2;

                for (var i = 0; i <= 128 + 4 - 1; ++i) {
                    DT1TBL[i] = DT1TBL_org[i] * 64 * OpmRateSamprateRatio;
                }
            }

            private uint irnd_seed = 1;
            public uint irnd() {
                irnd_seed = (uint)(irnd_seed * 1566083941UL + 1);
                return irnd_seed;
            }
        }

        public static CDatas Datas = new CDatas(); // X68SoundMCh以外からはアクセスされないことを確認済み 2022/07/31
    }
}
