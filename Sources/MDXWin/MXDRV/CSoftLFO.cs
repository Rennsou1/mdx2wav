using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace MXDRV {
    internal class CSoftLFO {
        private bool KeyOn = false;
        public int KeyOnDelay = 0;

        public abstract class CWaveBase {
            public abstract void Reset();
            public abstract bool Update();
            public abstract int GetOffset();
        }

        private class CPhaseLFO {
            public bool Enabled = false;
            public int KeyOnDelayTimeout;

            public class CWave0_鋸歯状波 : CWaveBase {
                private int Index, Count;
                private int Delta, Offset;
                public CWave0_鋸歯状波(int _Freq, int _Delta) {
                    Count = _Freq;
                    Index = Count / 2;
                    Delta = _Delta;
                    Offset = 0;
                }
                public override void Reset() {
                    Index = Count / 2;
                    Offset = 0;
                }
                public override bool Update() {
                    Offset += Delta;
                    Index++;
                    if (Count <= Index) {
                        Index -= Count;
                        Offset = -Offset;
                    }
                    return (true);
                }
                public override int GetOffset() { return (Offset); }
            }
            public class CWave1_矩形波 : CWaveBase {
                private int Index, Count;
                private int Delta, Offset;
                public CWave1_矩形波(int _Freq, int _Delta) {
                    Count = _Freq;
                    Index = 0;
                    Delta = _Delta;
                    Offset = Delta;
                }
                public override void Reset() {
                    Index = 0;
                    Offset = Delta;
                }
                public override bool Update() {
                    Index++;
                    if (Count <= Index) {
                        Index -= Count;
                        Offset = -Offset;
                        return (true);
                    }
                    return (false);
                }
                public override int GetOffset() { return (Offset); }
            }
            public class CWave2_三角波 : CWaveBase {
                private int Index, Count;
                private int Delta, Offset, Sign;
                public CWave2_三角波(int _Freq, int _Delta) {
                    Count = _Freq;
                    Index = Count / 2;
                    Delta = _Delta;
                    Offset = 0;
                    Sign = 1;
                }
                public override void Reset() {
                    Index = Count / 2;
                    Offset = 0;
                    Sign = 1;
                }
                public override bool Update() {
                    Offset += Delta * Sign;
                    Index++;
                    if (Count <= Index) {
                        Index -= Count;
                        Sign = -Sign;
                    }
                    return (true);
                }
                public override int GetOffset() { return (Offset); }
            }
            public class CWave3_ランダム : CWaveBase {
                private int Index, Count;
                private int Delta, Offset;
                private System.Random rand = new System.Random();
                public CWave3_ランダム(int _Freq, int _Delta) {
                    Count = _Freq;
                    Index = Count - 1;
                    Delta = _Delta;
                    Offset = 0;
                }
                public override void Reset() {
                    Index = Count - 1;
                    Offset = 0;
                }
                public override bool Update() {
                    Index++;
                    if (Count <= Index) {
                        Index -= Count;
                        if (Delta < 0) {
                            Offset = rand.Next(Delta << 15, -Delta << 15);
                        } else {
                            Offset = rand.Next(-Delta << 15, Delta << 15);
                        }
                        return (true);
                    }
                    return (false);
                }
                public override int GetOffset() { return (Offset); }
            }

            private CWaveBase WaveBase = new CWave0_鋸歯状波(0, 0);

            public void Init(int Wave, int Freq, int Delta) {
                switch (Wave) {
                    case 0: WaveBase = new CWave0_鋸歯状波(Freq, Delta); break;
                    case 1: WaveBase = new CWave1_矩形波(Freq, Delta); break;
                    case 2: WaveBase = new CWave2_三角波(Freq, Delta); break;
                    case 3: WaveBase = new CWave3_ランダム(Freq, Delta); break;
                    default: CCanIgnoreException.Throw(Lang.CLang.GetMXDRV(Lang.CLang.EMXDRV.UnsupportPhaseLFOType), "Wave=" + Wave); return;
                }
            }

            public void Reset() {
                WaveBase.Reset();
            }

            public bool Update() {
                if (Enabled) {
                    if (1 <= KeyOnDelayTimeout) {
                        KeyOnDelayTimeout--;
                    } else {
                        return (WaveBase.Update());
                    }
                }
                return (false);
            }

            public double GetOffset() {
                if (Enabled && (KeyOnDelayTimeout == 0)) { return (WaveBase.GetOffset() / 256d); }
                return (0);
            }
        }
        private CPhaseLFO PhaseLFO = new CPhaseLFO();

        private class CAmpLFO {
            public bool Enabled = false;
            public int KeyOnDelayTimeout;

            public class CWave0_鋸歯状波 : CWaveBase {
                private int Index, Count;
                private int Delta, Offset;
                public CWave0_鋸歯状波(int _Freq, int _Delta) {
                    Count = _Freq;
                    Index = 0;
                    Delta = _Delta;
                    if (0 <= Delta) {
                        Offset = 0;
                    } else {
                        Offset = -Delta * Count;
                    }
                }
                public override void Reset() {
                    Index = 0;
                    if (0 <= Delta) {
                        Offset = 0;
                    } else {
                        Offset = -Delta * Count;
                    }
                }
                public override bool Update() {
                    Offset += Delta;
                    Index++;
                    if (Count <= Index) {
                        Index -= Count;
                        if (0 <= Delta) {
                            Offset = 0;
                        } else {
                            Offset = -Delta * Count;
                        }
                    }
                    return (true);
                }
                public override int GetOffset() { return (Offset); }
            }
            public class CWave1_矩形波 : CWaveBase {
                private int Index, Count;
                private int Delta, Offset;
                public CWave1_矩形波(int _Freq, int _Delta) {
                    Count = _Freq;
                    Index = 0;
                    Delta = _Delta;
                    Offset = 0;
                }
                public override void Reset() {
                    Index = 0;
                    Offset = 0;
                }
                public override bool Update() {
                    Index++;
                    if (Count <= Index) {
                        Index -= Count;
                        if (Offset != 0) {
                            Offset = 0;
                        } else {
                            Offset = Delta;
                        }
                        return (true);
                    }
                    return (false);
                }
                public override int GetOffset() { return (Offset); }
            }
            public class CWave2_三角波 : CWaveBase {
                private int Index, Count;
                private int Delta, Offset, Sign;
                public CWave2_三角波(int _Freq, int _Delta) {
                    Count = _Freq;
                    Index = 0;
                    Delta = _Delta;
                    if (0 <= Delta) {
                        Offset = 0;
                        Sign = 1;
                    } else {
                        Offset = -Delta * Count;
                        Sign = -1;
                    }
                }
                public override void Reset() {
                    Index = 0;
                    if (0 <= Delta) {
                        Offset = 0;
                        Sign = 1;
                    } else {
                        Offset = -Delta * Count;
                        Sign = -1;
                    }
                }
                public override bool Update() {
                    Offset += System.Math.Abs(Delta) * Sign;
                    Index++;
                    if (Count <= Index) {
                        Index -= Count;
                        Sign = -Sign;
                    }
                    return (true);
                }
                public override int GetOffset() { return (Offset); }
            }
            public class CWave3_ランダム : CWaveBase {
                private int Index, Count;
                private int Delta, Offset;
                public System.Random rand = new System.Random();
                public CWave3_ランダム(int _Freq, int _Delta) {
                    Count = _Freq;
                    Index = Count - 1;
                    Delta = _Delta;
                    Offset = 0;
                }
                public override void Reset() {
                    Index = Count - 1;
                    Offset = 0;
                }
                public override bool Update() {
                    Index++;
                    if (Count <= Index) {
                        Index -= Count;
                        if (Delta < 0) {
                            Offset = rand.Next(Delta << 16, 0);
                        } else {
                            Offset = rand.Next(0, Delta << 16);
                        }
                        return (true);
                    }
                    return (false);
                }
                public override int GetOffset() { return (Offset); }
            }

            private CWaveBase WaveBase = new CWave0_鋸歯状波(0, 0);

            public void Init(int Wave, int Freq, int Delta) {
                switch (Wave) {
                    case 0: WaveBase = new CWave0_鋸歯状波(Freq, Delta); break;
                    case 1: WaveBase = new CWave1_矩形波(Freq, Delta); break;
                    case 2: WaveBase = new CWave2_三角波(Freq, Delta); break;
                    case 3: WaveBase = new CWave3_ランダム(Freq, Delta); break;
                    default: CCanIgnoreException.Throw(Lang.CLang.GetMXDRV(Lang.CLang.EMXDRV.UnsupportAmpLFOType), "Wave=" + Wave); return;
                }
            }

            public void Reset() {
                WaveBase.Reset();
            }

            public bool Update() {
                if (Enabled) {
                    if (1 <= KeyOnDelayTimeout) {
                        KeyOnDelayTimeout--;
                    } else {
                        return (WaveBase.Update());
                    }
                }
                return (false);
            }

            public int GetOffset() {
                if (Enabled && (KeyOnDelayTimeout == 0)) { return (WaveBase.GetOffset() >> 8); }
                return (0);
            }
        }
        private CAmpLFO AmpLFO = new CAmpLFO();

        public void SetKeyOn(bool f) {
            if (KeyOn != f) {
                KeyOn = f;

                if (KeyOn) {
                    PhaseLFO.KeyOnDelayTimeout = KeyOnDelay;
                    PhaseLFO.Reset();
                    AmpLFO.KeyOnDelayTimeout = KeyOnDelay;
                    AmpLFO.Reset();
                }
            }

            if (KeyOn) {
            }
        }

        public bool SoftLFO_GetEnabled() {
            if (!KeyOn) { return (false); }
            if (PhaseLFO.Enabled && (PhaseLFO.KeyOnDelayTimeout == 0)) { return (true); }
            if (AmpLFO.Enabled && (AmpLFO.KeyOnDelayTimeout == 0)) { return (true); }
            return (false);
        }

        public struct TCalcRes {
            public bool RequestVolumeUpdate = false;
            public bool RequestToneUpdate = false;
            public TCalcRes() { }
        }

        public TCalcRes Calc() {
            TCalcRes res = new TCalcRes();

            res.RequestToneUpdate |= PhaseLFO.Update();
            res.RequestVolumeUpdate = AmpLFO.Update();

            return (res);
        }

        public void PhaseLFO_SetEnabled(bool Enabled) {
            PhaseLFO.Enabled = Enabled;

            if (PhaseLFO.Enabled) {
                PhaseLFO.Reset();
                PhaseLFO.KeyOnDelayTimeout = KeyOnDelay;
            }
        }

        public void PhaseLFO_SetParams(int Wave, int Freq, int Delta) {
            if ((Wave & 0x04) != 0) {
                Wave &= ~0x04;
                Delta *= 0x100;
            }

            PhaseLFO.Init(Wave, Freq, Delta);
            PhaseLFO_SetEnabled(true);
        }
        public double PhaseLFO_GetOffset() { return (PhaseLFO.GetOffset()); }

        public void AmpLFO_SetEnabled(bool Enabled) {
            AmpLFO.Enabled = Enabled;

            if (AmpLFO.Enabled) {
                AmpLFO.Reset();
                AmpLFO.KeyOnDelayTimeout = KeyOnDelay;
            }
        }

        public void AmpLFO_SetParams(int Wave, int Freq, int Delta) {
            if ((Wave & 0x04) != 0) {
                Wave &= ~0x04;
                Delta *= 0x100;
            }

            AmpLFO.Init(Wave, Freq, Delta);
            AmpLFO_SetEnabled(true);
        }

        public int AmpLFO_GetOffset() { return (AmpLFO.GetOffset()); }
    }
}
