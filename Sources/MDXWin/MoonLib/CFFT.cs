using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MoonLib {
    //============================================================================
    // FFT ユニット Ver.0.10
    // original by 奥村晴彦「C言語によるアルゴリズム入門」
    // Pascal Converted by C60
    //============================================================================

    //============================================================================
    // Speana ユニット Ver.0.20
    // Copyright & Programmed by C60
    // C# converted by Moonlight. 2023/09/05
    //============================================================================

    internal class CFFT {
        public const int SamplesCount = 4096;

        private static float[] sintbl = new float[SamplesCount + (SamplesCount / 4)];
        private static int[] bitrev = new int[SamplesCount];

        public const int CalcSampleRate = 62500;

        private static float[] HummngWindow = new float[SamplesCount];

        public const int BandsCount = 128;

        private static int[] BandScale = new int[BandsCount] {
              12,   13,   14,   15,   16,   17,   18,   19,
              20,   21,   22,   23,   24,   25,   26,   27,
              28,   29,   30,   31,   33,   34,   35,   37,
              38,   40,   41,   43,   45,   46,   48,   50,
              52,   54,   56,   59,   61,   63,   66,   68,
              71,   74,   77,   80,   83,   86,   90,   93,
              97,  101,  105,  109,  113,  118,  123,  127,
             132,  138,  143,  149,  155,  161,  167,  174,
             181,  188,  195,  203,  211,  219,  228,  237,
             246,  256,  266,  277,  288,  299,  311,  323,
             336,  349,  363,  378,  393,  408,  424,  441,
             459,  477,  496,  515,  536,  557,  579,  602,
             626,  650,  676,  703,  731,  760,  790,  821,
             854,  887,  923,  959,  997, 1036, 1078, 1120,
            1165, 1211, 1259, 1308, 1360, 1414, 1470, 1528,
            1589, 1652, 1717, 1785, 1856, 1929, 2005, 2085
        };

        private static void make_sintbl() {
            for (var idx = 0; idx < sintbl.Length; idx++) {
                sintbl[idx] =(float) System.Math.Sin(System.Math.PI / SamplesCount * 2 * idx);
            }
        }
        private static void make_bitrev() {
            int i = 0;
            int j = 0;

            while (true) {
                bitrev[i] = j;
                i++;
                if (i >= SamplesCount) { break; }
                int k = SamplesCount / 2;
                while (k <= j) {
                    j -= k;
                    k = k / 2;
                }
                j += k;
            }
        }

        public static void Init() {
            make_sintbl();
            make_bitrev();

            for (var idx = 0; idx < SamplesCount; idx++) {
                HummngWindow[idx] = (float)(0.54 - 0.46 * System.Math.Cos(2 * System.Math.PI * idx / SamplesCount));
            }
        }

        private static float[] FFT(float[] x) {
            var y = new float[SamplesCount];
            for (var i = 0; i < SamplesCount; i++) {
                y[i] = 0;
            }

            for (var i = 0; i < SamplesCount; i++) {
                int j = bitrev[i];
                if (i < j) {
                    float t = x[i]; x[i] = x[j]; x[j] = t;
                    // t = y[i]; y[i] = y[j]; y[j] = t;
                }
            }

            int k = 1;

            while (k < SamplesCount) {
                int h = 0; int k2 = k + k; int d = SamplesCount / k2;
                for (var j = 0; j < k; j++) {
                    float c = sintbl[h + (SamplesCount / 4)];
                    float s = sintbl[h];

                    int i = j;
                    while (i < SamplesCount) {
                        int ik = i + k;
                        float dx = s * y[ik] + c * x[ik];
                        float dy = c * y[ik] - s * x[ik];
                        x[ik] = x[i] - dx; x[i] = x[i] + dx;
                        y[ik] = y[i] - dy; y[i] = y[i] + dy;
                        i += k2;
                    }
                    h += d;
                }
                k += k;
            }

            for (var i = 0; i < SamplesCount; i++) {
                x[i] /= SamplesCount;
                y[i] /= SamplesCount;
            }

            return (y);
        }

        public static void Exec(float[] Samples, float[] Bands) {
            if (Samples.Length < SamplesCount) { throw new Exception("サンプルバッファが足りません。"); }
            if (Bands.Length != BandsCount) { throw new Exception("バンドバッファサイズとバンド数が違います。"); }

            var x = new float[SamplesCount];

            for (var idx = 0; idx < SamplesCount; idx++) {
                x[idx] = Samples[idx]*512 * HummngWindow[idx];
            }

            var y = FFT(x);

            // それぞれの band の高さを計算
            int j = 0;
            var v2 = new float[BandsCount];
            for (var i = 0; i < BandsCount; i++) {
                v2[i] = 0;
            }

            for (var i = 1; i < SamplesCount / 2; i++) {
                if (i == BandScale[j]) {
                    if (j == 0) {
                        v2[j] = v2[j] / BandScale[j];
                    } else {
                        v2[j] = v2[j] / (BandScale[j] - BandScale[j - 1]);
                    }
                    j++;
                    if (j == BandsCount) { throw new Exception("ここには来ないはず？"); }
                }

                v2[j] += x[i] * x[i] + y[i] * y[i];
            }

            // 対数スケールに変換
            for (var i = 0; i < BandsCount; i++) {
                Bands[i] =(float) System.Math.Log(1 + (v2[i] * BandsCount / 4))/12;
            }
        }
    }
}
