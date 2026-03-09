using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MXDRV {
    internal class CPCMConvert {
        public static float[] DecodeADPCM(byte[] Data) {
            var PCM = new float[Data.Length * 2];

            // ADPCM decoder X68Sound.dll PCM8
            var dltLTBL = new int[48 + 1] {
                16,17,19,21,23,25,28,31,34,37,41,45,50,55,60,66,
                73,80,88,97,107,118,130,143,157,173,190,209,230,253,279,307,
                337,371,408,449,494,544,598,658,724,796,876,963,1060,1166,1282,1411,1552,
            };

            var DCT = new int[16] {
                -1,-1,-1,-1,2,4,6,8,
                -1,-1,-1,-1,2,4,6,8,
            };

            var Scale = 0;
            float Sample = 0;
            for (var ofs = 0; ofs < PCM.Length; ofs++) {
                byte adpcm = Data[ofs / 2];
                if ((ofs & 1) == 0) {
                    adpcm &= 0x0f;
                } else {
                    adpcm >>= 4;
                }

                var dltL = dltLTBL[Scale];

                dltL = (dltL & (((adpcm & 4) != 0) ? -1 : 0)) + ((dltL >> 1) & (((adpcm & 2) != 0) ? -1 : 0)) + ((dltL >> 2) & (((adpcm & 1) != 0) ? -1 : 0)) + (dltL >> 3);
                int sign = ((adpcm & 8) != 0) ? -1 : 0;
                dltL = (dltL ^ sign) + (sign & 1);

                Sample += dltL; // 本当は最下位2ビットを捨てるみたいだけど、捨てない。
                PCM[ofs] = Sample / 2048f;

                Scale += DCT[adpcm];
                if (Scale < 0) { Scale = 0; }
                if (48 < Scale) { Scale = 48; }
            }

            return (PCM);
        }

        public static float[] RateConvert(float[] src, float srcfreq, float dstfreq) {
            var srclen = src.Length;

            var tmpx = new float[srclen + 2];
            var tmpy = new float[srclen + 2];
            for (var idx = 0; idx < srclen; idx++) {
                tmpx[idx] = idx;
                tmpy[idx] = src[idx];
            }
            tmpx[srclen + 0] = srclen + 0;
            tmpy[srclen + 0] = 0;
            tmpx[srclen + 1] = srclen + 1;
            tmpy[srclen + 1] = 0;
            var Spline = new MoonLib.CSpline(tmpx, tmpy, srclen + 2);

            var res = new float[(int)(srclen * dstfreq / srcfreq)];
            for (var idx = 0; idx < res.Length; idx++) {
                var pos = idx * srcfreq / dstfreq;
                res[idx] = Spline.GetY(1 + pos);
            }

            return (res);
        }

        public class CRateConverter {
            MoonLib.CSpline Spline;
            public CRateConverter(float[] src) {
                var srclen = src.Length;

                var cx = new float[srclen + 2];
                var cy = new float[srclen + 2];

                for (var idx = 0; idx < srclen; idx++) {
                    cx[idx] = idx;
                    cy[idx] = src[idx];
                }

                cx[srclen + 0] = srclen + 0;
                cy[srclen + 0] = 0;
                cx[srclen + 1] = srclen + 1;
                cy[srclen + 1] = 0;

                Spline = new MoonLib.CSpline(cx, cy, srclen + 2);
            }

            public float GetY(float x) {
                return (Spline.GetY(1 + x));
            }
        }
    }
}
