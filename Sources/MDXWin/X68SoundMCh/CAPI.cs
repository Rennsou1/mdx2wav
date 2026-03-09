using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace X68SoundMCh {
    internal class CAPI {
        // 出力周波数は62500Hz以上
        // Timer-AとTimer-B、割り込み、ADPCM、PCM8は処理しない

        public const string Version = "X68Sound.dll .NET8.0/C# 個別チャネル出力対応版 2023/12/10 (改変元:X68Sound_020615.zip)";

        private COpm Opm;

        public CAPI() {
            Opm = new COpm();
            Opm.StartPcm();
        }

        public void GetPcm(float[] buf, int SamplesCount, int SampleRate) {
            Opm.GetPcm(buf, SamplesCount, SampleRate);
        }

        public bool OpmPoke(int addr, int data, bool TrapException = true) {
            if ((addr < 0x00) || (0x100 <= addr) || (data < 0x00) || (0x100 <= data)) { throw new Exception("X68SoundMCh.Write Poke2 addr or data overflow. addr=0x" + addr.ToString("x2") + ", data=0x" + data.ToString("x2")); }
            if (!Opm.OpmPoke((byte)addr, (byte)data)) {
                if (TrapException) {
                    throw new System.Exception("OpmPoke RegAddr error. addr=0x" + addr.ToString("x2") + ", data=0x" + data.ToString("x2"));
                }
                return (false);
            }
            return (true);
        }

        public void OpmPoke_SetKF(int ch, double data) {
            Opm.OpmPoke_SetKF(ch, data);
        }
    }
}
