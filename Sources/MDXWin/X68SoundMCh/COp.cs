using System.Windows.Navigation;

namespace X68SoundMCh {
    internal class COp {
        internal const int KEYON = -1;
        internal const int ATACK = 0;
        internal const int DECAY = 1;
        internal const int SUSTAIN = 2;
        internal const int SUSTAIN_MAX = 3;
        internal const int RELEASE = 4;
        internal const int RELEASE_MAX = 5;

        internal const int CULC_ALPHA = 0x7FFFFFFF;

        public class CChain {
            public double Level;
        }

        internal readonly int[] NEXTSTAT = new int[RELEASE_MAX + 1] { DECAY, SUSTAIN, SUSTAIN_MAX, SUSTAIN_MAX, RELEASE_MAX, RELEASE_MAX, };
        internal readonly int[] MAXSTAT = new int[RELEASE_MAX + 1] { ATACK, SUSTAIN_MAX, SUSTAIN_MAX, SUSTAIN_MAX, RELEASE_MAX, RELEASE_MAX, };

        public CChain inp = new CChain();   // FM変調の入力
        private double T;  // 現在時間 (0 <= T < SIZESINTBL*PRECISION)
        private bool Ame;  // false(トレモロをかけない), true(トレモロをかける)

        public CChain out1;   // オペレータの出力先
        public CChain out2;   // オペレータの出力先(alg=5時のM1用)
        public CChain out3;   // オペレータの出力先(alg=5時のM1用)

        private double Pitch; // 0<=pitch<10*12*64
        private int Dt1Pitch; // Step に対する補正量
        private int Mul; // 0.5*2 1*2 2*2 3*2 ... 15*2
        private int Tl;  // (128-TL)*8

        private double Out2Fb; // フィードバックへの出力値
        private double Inp_last; // 最後の入力値
        private double Fl;  // フィードバックレベルの掛け数

        private int NoiseCounter; // Noise用カウンタ
        private int NoiseStep; // Noise用カウントダウン値
        private int NoiseCycle; // Noise周期 32*2^25(0) ～ 1*2^25(31) NoiseCycle==0の時はノイズオフ
        private int NoiseValue; // ノイズ値  1 or -1

        // エンベロープ関係
        private int Xr_stat;
        private int Xr_el;
        private int Xr_step;
        private int Xr_and;
        private int Xr_cmp;
        private int Xr_add;
        private int Xr_limit;

        private int Note; // 音階 (0 <= Note < 10*12)
        private int Kc;  // 音階 (1 <= Kc <= 128)
        private double Kf;  // 微調整 (0 <= Kf < 64)
        private int Ar;  // 0 <= Ar < 31
        private int D1r; // 0 <= D1r < 31
        private int D2r; // 0 <= D2r < 31
        private int Rr;  // 0 <= Rr < 15
        private int Ks;  // 0 <= Ks <= 3
        private int Dt2; // Pitch に対する補正量(0, 384, 500, 608)
        private int Dt1; // DT1の値(0～7)
        private int Nfrq; // Noiseflag,NFRQの値

        struct TStatTbl {
            public int iand, cmp, add, limit;
        }
        private TStatTbl[] StatTbl = new TStatTbl[RELEASE_MAX + 1]; // 状態推移テーブル
                                                                    //           ATACK     DECAY   SUSTAIN     SUSTAIN_MAX RELEASE     RELEASE_MAX
                                                                    // and     :                               4097                    4097
                                                                    // cmp     :                               2048                    2048
                                                                    // add     :                               0                       0
                                                                    // limit   : 0         D1l     63          63          63          63
                                                                    // nextstat: DECAY     SUSTAIN SUSTAIN_MAX SUSTAIN_MAX RELEASE_MAX RELEASE_MAX


        private void CulcArStep() {
            if (Ar != 0) {
                int ks = (Ar << 1) + (Kc >> (5 - Ks));
                StatTbl[ATACK].iand = CGlobal.Datas.XRTBL[ks].iand;
                StatTbl[ATACK].cmp = CGlobal.Datas.XRTBL[ks].iand >> 1;
                if (ks < 62) {
                    StatTbl[ATACK].add = CGlobal.Datas.XRTBL[ks].add;
                } else {
                    StatTbl[ATACK].add = 128;
                }
            } else {
                StatTbl[ATACK].iand = 4097;
                StatTbl[ATACK].cmp = 2048;
                StatTbl[ATACK].add = 0;
            }
            if (Xr_stat == ATACK) {
                Xr_and = StatTbl[Xr_stat].iand;
                Xr_cmp = StatTbl[Xr_stat].cmp;
                Xr_add = StatTbl[Xr_stat].add;
            }
        }
        private void CulcD1rStep() {
            if (D1r != 0) {
                int ks = (D1r << 1) + (Kc >> (5 - Ks));
                StatTbl[DECAY].iand = CGlobal.Datas.XRTBL[ks].iand;
                StatTbl[DECAY].cmp = CGlobal.Datas.XRTBL[ks].iand >> 1;
                StatTbl[DECAY].add = CGlobal.Datas.XRTBL[ks].add;
            } else {
                StatTbl[DECAY].iand = 4097;
                StatTbl[DECAY].cmp = 2048;
                StatTbl[DECAY].add = 0;
            }
            if (Xr_stat == DECAY) {
                Xr_and = StatTbl[Xr_stat].iand;
                Xr_cmp = StatTbl[Xr_stat].cmp;
                Xr_add = StatTbl[Xr_stat].add;
            }
        }
        private void CulcD2rStep() {
            if (D2r != 0) {
                int ks = (D2r << 1) + (Kc >> (5 - Ks));
                StatTbl[SUSTAIN].iand = CGlobal.Datas.XRTBL[ks].iand;
                StatTbl[SUSTAIN].cmp = CGlobal.Datas.XRTBL[ks].iand >> 1;
                StatTbl[SUSTAIN].add = CGlobal.Datas.XRTBL[ks].add;
            } else {
                StatTbl[SUSTAIN].iand = 4097;
                StatTbl[SUSTAIN].cmp = 2048;
                StatTbl[SUSTAIN].add = 0;
            }
            if (Xr_stat == SUSTAIN) {
                Xr_and = StatTbl[Xr_stat].iand;
                Xr_cmp = StatTbl[Xr_stat].cmp;
                Xr_add = StatTbl[Xr_stat].add;
            }
        }
        private void CulcRrStep() {
            int ks = (Rr << 2) + 2 + (Kc >> (5 - Ks));
            StatTbl[RELEASE].iand = CGlobal.Datas.XRTBL[ks].iand;
            StatTbl[RELEASE].cmp = CGlobal.Datas.XRTBL[ks].iand >> 1;
            StatTbl[RELEASE].add = CGlobal.Datas.XRTBL[ks].add;
            if (Xr_stat == RELEASE) {
                Xr_and = StatTbl[Xr_stat].iand;
                Xr_cmp = StatTbl[Xr_stat].cmp;
                Xr_add = StatTbl[Xr_stat].add;
            }
        }
        private void CulcPitch() {
            Pitch = (Note << 6) + Kf + Dt2;
        }
        private void CulcDt1Pitch() {
            Dt1Pitch = CGlobal.Datas.DT1TBL[(Kc & 0xFC) + (Dt1 & 3)];
            if ((Dt1 & 0x04) != 0) {
                Dt1Pitch = -Dt1Pitch;
            }
        }
        private void CulcNoiseCycle() {
            if ((Nfrq & 0x80) != 0) {
                NoiseCycle = (32 - (Nfrq & 31)) << 25;
                if (NoiseCycle < NoiseStep) {
                    NoiseCycle = NoiseStep;
                }
                NoiseCounter = NoiseCycle;
            } else {
                NoiseCycle = 0;
            }
        }

        public void Init() {
            Note = 5 * 12 + 8;
            Kc = 5 * 16 + 8 + 1;
            Kf = 5;
            Ar = 10;
            D1r = 10;
            D2r = 5;
            Rr = 12;
            Ks = 1;
            Dt2 = 0;
            Dt1 = 0;

            Fl = 0;
            Out2Fb = 0;
            inp.Level = 0;
            Inp_last = 0;
            T = 0;
            Tl = (128 - 127) << 3;
            Xr_el = 1024;
            Xr_step = 0;
            Mul = 2;
            Ame = false;

            NoiseStep = (1 << 26) * CGlobal.OpmRateSamprateRatio;
            SetNFRQ(0);
            NoiseValue = 1;

            // 状態推移テーブルを作成
            // StatTbl[ATACK].nextstat = DECAY;
            // StatTbl[DECAY].nextstat = SUSTAIN;
            // StatTbl[SUSTAIN].nextstat = SUSTAIN_MAX;
            // StatTbl[SUSTAIN_MAX].nextstat = SUSTAIN_MAX;
            // StatTbl[RELEASE].nextstat = RELEASE_MAX;
            // StatTbl[RELEASE_MAX].nextstat = RELEASE_MAX;

            StatTbl[ATACK].limit = 0;
            StatTbl[DECAY].limit = CGlobal.Datas.D1LTBL[0];
            StatTbl[SUSTAIN].limit = 63;
            StatTbl[SUSTAIN_MAX].limit = 63;
            StatTbl[RELEASE].limit = 63;
            StatTbl[RELEASE_MAX].limit = 63;

            StatTbl[SUSTAIN_MAX].iand = 4097;
            StatTbl[SUSTAIN_MAX].cmp = 2048;
            StatTbl[SUSTAIN_MAX].add = 0;
            StatTbl[RELEASE_MAX].iand = 4097;
            StatTbl[RELEASE_MAX].cmp = 2048;
            StatTbl[RELEASE_MAX].add = 0;

            Xr_stat = RELEASE_MAX;
            Xr_and = StatTbl[Xr_stat].iand;
            Xr_cmp = StatTbl[Xr_stat].cmp;
            Xr_add = StatTbl[Xr_stat].add;
            Xr_limit = StatTbl[Xr_stat].limit;

            CulcArStep();
            CulcD1rStep();
            CulcD2rStep();
            CulcRrStep();
            CulcPitch();
            CulcDt1Pitch();
        }
        public void InitSamprate() {
            NoiseStep = (1 << 26) * CGlobal.OpmRateSamprateRatio;
            CulcNoiseCycle();

            CulcArStep();
            CulcD1rStep();
            CulcD2rStep();
            CulcRrStep();
            CulcPitch();
            CulcDt1Pitch();
        }
        public void SetFL(int n) {
            n = (n >> 3) & 7;
            if (n == 0) {
                Fl = 0;
            } else {
                var _Fl = (7 - n + 1 + 1);
                Fl = 1d / (1 << _Fl);
            }
        }
        public void SetKC(int n) {
            Kc = n & 127;
            int note = Kc & 15;
            Note = ((Kc >> 4) + 1) * 12 + note - (note >> 2);
            ++Kc;
            CulcPitch();
            CulcDt1Pitch();
            CulcArStep();
            CulcD1rStep();
            CulcD2rStep();
            CulcRrStep();
        }
        public void SetKF(int n) {
            Kf = (n & 255) >> 2;
            CulcPitch();
        }
        public void SetKF(double n) {
            Kf = n;
            CulcPitch();
        }
        public void SetDT1MUL(int n) {
            Dt1 = (n >> 4) & 7;
            CulcDt1Pitch();
            Mul = (n & 15) << 1;
            if (Mul == 0) {
                Mul = 1;
            }
        }
        public void SetTL(int n) {
            Tl = (128 - (n & 127)) << 3;
        }

        public void SetKSAR(int n) {
            Ks = (n & 255) >> 6;
            Ar = n & 31;
            CulcArStep();
            CulcD1rStep();
            CulcD2rStep();
            CulcRrStep();
        }
        public void SetAMED1R(int n) {
            D1r = n & 31;
            CulcD1rStep();
            Ame = (n & 0x80) != 0;
        }
        public void SetDT2D2R(int n) {
            Dt2 = CGlobal.Datas.DT2TBL[(n & 255) >> 6];
            CulcPitch();
            D2r = n & 31;
            CulcD2rStep();
        }
        public void SetD1LRR(int n) {
            StatTbl[DECAY].limit = CGlobal.Datas.D1LTBL[(n & 255) >> 4];
            if (Xr_stat == DECAY) {
                Xr_limit = StatTbl[DECAY].limit;
            }

            Rr = n & 15;
            CulcRrStep();
        }

        public void KeyON() {
            if (Xr_stat >= RELEASE) {
                // KEYON
                T = 0;

                if (Xr_el == 0) {
                    Xr_stat = DECAY;
                    Xr_and = StatTbl[Xr_stat].iand;
                    Xr_cmp = StatTbl[Xr_stat].cmp;
                    Xr_add = StatTbl[Xr_stat].add;
                    Xr_limit = StatTbl[Xr_stat].limit;
                    if ((Xr_el >> 4) == Xr_limit) {
                        Xr_stat = NEXTSTAT[Xr_stat];
                        Xr_and = StatTbl[Xr_stat].iand;
                        Xr_cmp = StatTbl[Xr_stat].cmp;
                        Xr_add = StatTbl[Xr_stat].add;
                        Xr_limit = StatTbl[Xr_stat].limit;
                    }
                } else {
                    Xr_stat = ATACK;
                    Xr_and = StatTbl[Xr_stat].iand;
                    Xr_cmp = StatTbl[Xr_stat].cmp;
                    Xr_add = StatTbl[Xr_stat].add;
                    Xr_limit = StatTbl[Xr_stat].limit;
                }
            }
        }
        public void KeyOFF() {
            Xr_stat = RELEASE;
            Xr_and = StatTbl[Xr_stat].iand;
            Xr_cmp = StatTbl[Xr_stat].cmp;
            Xr_add = StatTbl[Xr_stat].add;
            Xr_limit = StatTbl[Xr_stat].limit;
            if ((Xr_el >> 4) >= 63) {
                Xr_el = 1024;
                Xr_stat = MAXSTAT[Xr_stat];
                Xr_and = StatTbl[Xr_stat].iand;
                Xr_cmp = StatTbl[Xr_stat].cmp;
                Xr_add = StatTbl[Xr_stat].add;
                Xr_limit = StatTbl[Xr_stat].limit;
            }
        }

        public void Envelope(int env_counter) {
            if ((env_counter & Xr_and) == Xr_cmp) {

                if (Xr_stat == ATACK) {
                    // ATACK
                    Xr_step += Xr_add;
                    Xr_el += ((~Xr_el) * (Xr_step >> 3)) >> 4;
                    Xr_step &= 7;

                    if (Xr_el <= 0) {
                        Xr_el = 0;
                        Xr_stat = DECAY;
                        Xr_and = StatTbl[Xr_stat].iand;
                        Xr_cmp = StatTbl[Xr_stat].cmp;
                        Xr_add = StatTbl[Xr_stat].add;
                        Xr_limit = StatTbl[Xr_stat].limit;
                        if ((Xr_el >> 4) == Xr_limit) {
                            Xr_stat = NEXTSTAT[Xr_stat];
                            Xr_and = StatTbl[Xr_stat].iand;
                            Xr_cmp = StatTbl[Xr_stat].cmp;
                            Xr_add = StatTbl[Xr_stat].add;
                            Xr_limit = StatTbl[Xr_stat].limit;
                        }
                    }
                } else {
                    // DECAY, SUSTAIN, RELEASE
                    Xr_step += Xr_add;
                    Xr_el += Xr_step >> 3;
                    Xr_step &= 7;

                    int e = Xr_el >> 4;
                    if (e == 63) {
                        Xr_el = 1024;
                        Xr_stat = MAXSTAT[Xr_stat];
                        Xr_and = StatTbl[Xr_stat].iand;
                        Xr_cmp = StatTbl[Xr_stat].cmp;
                        Xr_add = StatTbl[Xr_stat].add;
                        Xr_limit = StatTbl[Xr_stat].limit;
                    } else if (e == Xr_limit) {
                        Xr_stat = NEXTSTAT[Xr_stat];
                        Xr_and = StatTbl[Xr_stat].iand;
                        Xr_cmp = StatTbl[Xr_stat].cmp;
                        Xr_add = StatTbl[Xr_stat].add;
                        Xr_limit = StatTbl[Xr_stat].limit;
                    }
                }
            }
        }
        public void SetNFRQ(int nfrq) {
            if (((Nfrq ^ nfrq) & 0x80) != 0) {
            }
            Nfrq = nfrq;
            CulcNoiseCycle();
        }

        public struct TOutputParams {
            public readonly bool UpdateClock;
            public readonly double DeltaRatio = 1;
            public readonly double lfopitch;
            public readonly int lfolevel;
            public TOutputParams(bool _UpdateClock, double _DeltaRatio, double _lfopitch, int _lfolevel) {
                UpdateClock = _UpdateClock;
                DeltaRatio = _DeltaRatio;
                lfopitch = _lfopitch;
                lfolevel = _lfolevel;
            }
        }

        private double TempDeltaT = 0;
        private double TempAlpha = 0;
        private double NoiseLevel = 0;

        public void Output0(TOutputParams Params) {   // オペレータ0用
            if (Params.UpdateClock) {
                TempDeltaT = ((CGlobal.ConvDeltaTFromPitch(Pitch + Params.lfopitch) + Dt1Pitch) * Mul) / 128;
            }
            T += TempDeltaT * Params.DeltaRatio;

            if (Params.UpdateClock) {
                var lfolevel = Ame ? Params.lfolevel : 0;
                TempAlpha = CGlobal.ConvAlphaFromEnv(Tl - Xr_el - lfolevel); // 最終的なエンベロープ出力値
            }
            var o = TempAlpha * CGlobal.CalcSin((T + Out2Fb) / CGlobal.PRECISION);

            if (Params.UpdateClock) {
                Out2Fb = (o + Inp_last) * Fl;
                Inp_last = o;
            }

            out1.Level = o;
            out2.Level = o; // alg=5用
            out3.Level = o; // alg=5用
        }
        public void Output(TOutputParams Params) {   // 一般オペレータ用
            if (Params.UpdateClock) {
                TempDeltaT = ((CGlobal.ConvDeltaTFromPitch(Pitch + Params.lfopitch) + Dt1Pitch) * Mul) / 128;
            }
            T += TempDeltaT * Params.DeltaRatio;

            if (Params.UpdateClock) {
                var lfolevel = Ame ? Params.lfolevel : 0;
                TempAlpha = CGlobal.ConvAlphaFromEnv(Tl - Xr_el - lfolevel);
            }
            var o = TempAlpha * CGlobal.CalcSin((T + inp.Level) / CGlobal.PRECISION);

            out1.Level += o;
        }
        public void Output32(TOutputParams Params) {   // スロット32用
            if (Params.UpdateClock) {
                TempDeltaT = ((CGlobal.ConvDeltaTFromPitch(Pitch + Params.lfopitch) + Dt1Pitch) * Mul) / 128;
            }
            T += TempDeltaT * Params.DeltaRatio;

            var lfolevel = Ame ? Params.lfolevel : 0;
            if (NoiseCycle == 0) {
                if (Params.UpdateClock) {
                    TempAlpha = CGlobal.ConvAlphaFromEnv(Tl - Xr_el - lfolevel);
                }
                var o = TempAlpha * CGlobal.CalcSin((T + inp.Level) / CGlobal.PRECISION);
                out1.Level += o;
            } else {
                if (Params.UpdateClock) {
                    NoiseCounter -= NoiseStep;
                    if (NoiseCounter <= 0) {
                        NoiseValue = (int)((CGlobal.Datas.irnd() >> 30) & 2) - 1;
                        NoiseCounter += NoiseCycle;
                    }
                    var Alpha = CGlobal.ConvNoiseAlphaFromEnv(Tl - Xr_el - lfolevel);
                    NoiseLevel = Alpha * NoiseValue * CGlobal.MAXSINVAL;
                }
                out1.Level += NoiseLevel;
            }
        }
    }
}
