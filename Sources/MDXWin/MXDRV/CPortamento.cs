using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MXDRV {
    internal class CPortamento {
        public int Offset = 0;
        private int Delta = 0;
        private int NextDelta = 0;

        public void SetDelta(int Delta) {
            Offset = 0;
            NextDelta = Delta;
        }

        public void SetKeyOn(bool f) {
            if (f) {
                Offset = 0;
                Delta = NextDelta;
                NextDelta = 0;
            }
        }

        public bool Calc() {
            var res = false;

            if (Delta != 0) {
                Offset += Delta;
                res = true;
            }

            return (res);
        }

        public double GetOffset() {
            return (Offset / 256d);
        }

    }
}
