using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MoonLib {
    internal class CSpline {
        private int N;
        private float[] srcx, srcy;
        private float[] z;
        public CSpline(float[] _srcx, float[] _srcy, int _N) {
            srcx = _srcx;
            srcy = _srcy;
            N = _N;

            if ((srcx.Length != N) || (srcy.Length != N)) { throw new Exception("入力配列のサイズが違います。"); }

            // ３項方程式の係数の表を作る
            float[] a = new float[N];
            float[] b = new float[N];
            float[] c = new float[N];
            float[] d = new float[N];
            for (int i = 1; i < N - 1; i++) {
                a[i] = srcx[i] - srcx[i - 1];
                b[i] = 2.0f * (srcx[i + 1] - srcx[i - 1]);
                c[i] = srcx[i + 1] - srcx[i];
                d[i] = 6.0f * ((srcy[i + 1] - srcy[i]) / (srcx[i + 1] - srcx[i]) - (srcy[i] - srcy[i - 1]) / (srcx[i] - srcx[i - 1]));
            }

            // ３項方程式を解く (ト－マス法)
            float[] g = new float[N];
            float[] s = new float[N];
            g[1] = b[1];
            s[1] = d[1];
            for (int i = 2; i < N - 1; i++) {
                g[i] = b[i] - a[i] * c[i - 1] / g[i - 1];
                s[i] = d[i] - a[i] * s[i - 1] / g[i - 1];
            }

            z = new float[N];
            z[0] = 0;
            z[N - 1] = 0;
            z[N - 2] = s[N - 2] / g[N - 2];
            for (int i = N - 3; i >= 1; i--) {
                z[i] = (s[i] - c[i] * z[i + 1]) / g[i];
            }
        }

        public float GetY(float d) {
            int k; // 補間関数値がどの区間にあるか

            if (false) {
                k = -1;
                for (int i = 1; i < N; i++) {
                    if (d <= srcx[i]) {
                        k = i - 1;
                        break;
                    }
                }
                if (k == -1) { throw new Exception("対象の区間が見つかりませんでした。"); }
            } else {
                k = (int)System.Math.Truncate(d); // srcx[idx]==idx の場合はこれが使える
            }

            float d1 = srcx[k + 1] - d;
            float d2 = d - srcx[k];
            float d3 = srcx[k + 1] - srcx[k];

            var res = 0f;
            res += (float)(z[k] * Math.Pow(d1, 3) + z[k + 1] * Math.Pow(d2, 3)) / (6.0f * d3);
            res += (srcy[k] / d3 - z[k] * d3 / 6.0f) * d1;
            res += (srcy[k + 1] / d3 - z[k + 1] * d3 / 6.0f) * d2;
            return (res);
        }
    }
}
