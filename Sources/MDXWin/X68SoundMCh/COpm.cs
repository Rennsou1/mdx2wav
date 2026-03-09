using System.Diagnostics;
using System.Windows.Documents;

namespace X68SoundMCh {
    internal class COpm {
        private const string Author = "m_puusan";

        private COp[,] op = new COp[CGlobal.N_CH, 4]; // オペレータ0～31
        private COp.CChain[] OpOut = new COp.CChain[CGlobal.N_CH];
        private readonly COp[] SLOTTBL = new COp[CGlobal.N_CH * 4];

        private COp.CChain OpOutDummy = new COp.CChain();

        private int EnvCounter1; // エンベロープ用カウンタ1 (0,1,2,3,4,5,6,...)
        private int EnvCounter2; // エンベロープ用カウンタ2 (3,2,1,3,2,1,3,2,...)
                                 // int con[N_CH]; // アルゴリズム 0～7
        private byte[] pan = new byte[CGlobal.N_CH]; // 0:無音, 1:左, 2:右, 3:中央
        private CLfo lfo = new CLfo();

        private void SetConnection(int ch, int alg) {
            switch (alg) {
                case 0:
                    op[ch, 0].out1 = op[ch, 1].inp;
                    op[ch, 0].out2 = OpOutDummy;
                    op[ch, 0].out3 = OpOutDummy;
                    op[ch, 1].out1 = op[ch, 2].inp;
                    op[ch, 2].out1 = op[ch, 3].inp;
                    op[ch, 3].out1 = OpOut[ch];
                    break;
                case 1:
                    op[ch, 0].out1 = op[ch, 2].inp;
                    op[ch, 0].out2 = OpOutDummy;
                    op[ch, 0].out3 = OpOutDummy;
                    op[ch, 1].out1 = op[ch, 2].inp;
                    op[ch, 2].out1 = op[ch, 3].inp;
                    op[ch, 3].out1 = OpOut[ch];
                    break;
                case 2:
                    op[ch, 0].out1 = op[ch, 3].inp;
                    op[ch, 0].out2 = OpOutDummy;
                    op[ch, 0].out3 = OpOutDummy;
                    op[ch, 1].out1 = op[ch, 2].inp;
                    op[ch, 2].out1 = op[ch, 3].inp;
                    op[ch, 3].out1 = OpOut[ch];
                    break;
                case 3:
                    op[ch, 0].out1 = op[ch, 1].inp;
                    op[ch, 0].out2 = OpOutDummy;
                    op[ch, 0].out3 = OpOutDummy;
                    op[ch, 1].out1 = op[ch, 3].inp;
                    op[ch, 2].out1 = op[ch, 3].inp;
                    op[ch, 3].out1 = OpOut[ch];
                    break;
                case 4:
                    op[ch, 0].out1 = op[ch, 1].inp;
                    op[ch, 0].out2 = OpOutDummy;
                    op[ch, 0].out3 = OpOutDummy;
                    op[ch, 1].out1 = OpOut[ch];
                    op[ch, 2].out1 = op[ch, 3].inp;
                    op[ch, 3].out1 = OpOut[ch];
                    break;
                case 5:
                    op[ch, 0].out1 = op[ch, 1].inp;
                    op[ch, 0].out2 = op[ch, 2].inp;
                    op[ch, 0].out3 = op[ch, 3].inp;
                    op[ch, 1].out1 = OpOut[ch];
                    op[ch, 2].out1 = OpOut[ch];
                    op[ch, 3].out1 = OpOut[ch];
                    break;
                case 6:
                    op[ch, 0].out1 = op[ch, 1].inp;
                    op[ch, 0].out2 = OpOutDummy;
                    op[ch, 0].out3 = OpOutDummy;
                    op[ch, 1].out1 = OpOut[ch];
                    op[ch, 2].out1 = OpOut[ch];
                    op[ch, 3].out1 = OpOut[ch];
                    break;
                case 7:
                    op[ch, 0].out1 = OpOut[ch];
                    op[ch, 0].out2 = OpOutDummy;
                    op[ch, 0].out3 = OpOutDummy;
                    op[ch, 1].out1 = OpOut[ch];
                    op[ch, 2].out1 = OpOut[ch];
                    op[ch, 3].out1 = OpOut[ch];
                    break;
            }
        }

        private int TimerAreg10; // OPMreg$10の値
        private int TimerAreg11; // OPMreg$11の値
        private int TimerA;         // タイマーAのオーバーフロー設定値
        private int TimerB;         // タイマーBのオーバーフロー設定値
        private int TimerReg;  // タイマー制御レジスタ (OPMreg$14の下位4ビット)

        private const bool UseOpmFlag = true;  // OPMを利用するかどうかのフラグ

        public COpm() {
            for (var ch = 0; ch < CGlobal.N_CH; ch++) {
                for (var opidx = 0; opidx < 4; opidx++) {
                    op[ch, opidx] = new COp();
                }
                OpOut[ch] = new COp.CChain();
            }

            // C1 <-> M2 入れ替えテーブルを作成
            for (var slot = 0; slot < 8; ++slot) {
                SLOTTBL[slot] = op[slot, 0];
                SLOTTBL[slot + 8] = op[slot, 2];
                SLOTTBL[slot + 16] = op[slot, 1];
                SLOTTBL[slot + 24] = op[slot, 3];
            }
        }

        private int OPMUpdateTime = 0;

        public void GetPcm(float[] pPcmBuf, int ndata, int SampleRate) {
            var DeltaRatio = (double)CGlobal.OpmRate / SampleRate;

            double[] LfoPitch = new double[CGlobal.N_CH];
            int[] LfoLevel = new int[CGlobal.N_CH];

            for (var ch = 0; ch < CGlobal.N_CH; ++ch) {
                LfoPitch[ch] = lfo.GetPmValue(ch);
                LfoLevel[ch] = lfo.GetAmValue(ch);
            }

            for (var i = 0; i < ndata; ++i) {
                if (UseOpmFlag) {
                    var UpdateClock = false;

                    OPMUpdateTime -= CGlobal.OpmRate;
                    if (OPMUpdateTime < 0) {
                        OPMUpdateTime += SampleRate;
                        UpdateClock = true;
                    }

                    if (UpdateClock) {
                        if (CGlobal.OpmRateSamprateRatio != 1) { throw new System.Exception(Lang.CLang.GetMXDRV(Lang.CLang.EMXDRV.X68Sound_UnsupportFreqConversion)); }
                        if ((--EnvCounter2) == 0) {
                            EnvCounter2 = 3;
                            ++EnvCounter1;
                            for (var ch = 0; ch < CGlobal.N_CH; ch++) {
                                for (var slot = 0; slot < 4; ++slot) {
                                    op[ch, slot].Envelope(EnvCounter1);
                                }
                            }
                        }
                        lfo.Update();
                        for (var ch = 0; ch < CGlobal.N_CH; ++ch) {
                            LfoPitch[ch] = lfo.GetPmValue(ch);
                            LfoLevel[ch] = lfo.GetAmValue(ch);
                        }
                    }

                    {
                        for (var ch = 0; ch < CGlobal.N_CH; ++ch) {
                            op[ch, 1].inp.Level = op[ch, 2].inp.Level = op[ch, 3].inp.Level = OpOut[ch].Level = 0;
                        }
                        for (var ch = 0; ch < CGlobal.N_CH; ++ch) {
                            var Params = new COp.TOutputParams(UpdateClock, DeltaRatio, LfoPitch[ch], LfoLevel[ch]);
                            // オペレータを処理する順番が影響するかも？
                            op[ch, 0].Output0(Params);
                            op[ch, 1].Output(Params);
                            op[ch, 2].Output(Params);
                            if (ch < 7) {
                                op[ch, 3].Output(Params);
                            } else {
                                op[7, 3].Output32(Params);
                            }
                        }
                    }

                    // OutInpOpm[] に OPM の出力PCMをモノラル出力 (26BitsPCM)
                    for (var ch = 0; ch < CGlobal.N_CH; ch++) {
                        var Sample = (float)(-OpOut[ch].Level / (1 << (26 - 1))); // 正負反転？
                        pPcmBuf[i * MusDriver.CCommon.OutputElements.Count + ch] = Sample;
                    }
                } // UseOpmFlag
            }
        }

        public void MakeTable() {
        }
        public void StartPcm() {
            MakeTable();
            Reset();
        }
        public void Reset() {
            // 全オペレータを初期化
            {
                int ch;
                for (ch = 0; ch < CGlobal.N_CH; ++ch) {
                    op[ch, 0].Init();
                    op[ch, 1].Init();
                    op[ch, 2].Init();
                    op[ch, 3].Init();
                    //   con[ch] = 0;
                    SetConnection(ch, 0);
                    pan[ch] = pan[ch] = 0;
                }
            }

            // エンベロープ用カウンタを初期化
            {
                EnvCounter1 = 0;
                EnvCounter2 = 3;
            }


            // LFO初期化
            lfo.Init();

            // タイマー関係の初期化
            TimerAreg10 = 0;
            TimerAreg11 = 0;
            TimerA = 1024 - 0;
            TimerB = (256 - 0) << (10 - 6);
            TimerReg = 0;
        }

        public bool OpmPoke(byte OpmRegNo, byte data) {
            if (OpmRegNo < 0x20) { // Controls
                switch (OpmRegNo) {
                    case 0x01: // LFO RESET
                        if ((data & 0x02) != 0) {
                            lfo.LfoReset();
                        } else {
                            lfo.LfoStart();
                        }
                        break;
                    case 0x06: case 0x07: break; // 未定義レジスタ
                    case 0x08: { // KON
                            var ch = data & 7;
                            int s, bit;
                            for (s = 0, bit = 8; s < 4; ++s, bit += bit) {
                                if ((data & bit) != 0) {
                                    op[ch, s].KeyON();
                                } else {
                                    op[ch, s].KeyOFF();
                                }
                            }
                        }
                        break;
                    case 0x0F: op[7, 3].SetNFRQ(data & 0xFF); break; // NE,NFRQ
                    case 0x10:
                    case 0x11: // TimerA
                        if (OpmRegNo == 0x10) {
                            TimerAreg10 = data;
                        } else {
                            TimerAreg11 = data & 3;
                        }
                        TimerA = 1024 - ((TimerAreg10 << 2) + TimerAreg11);
                        break;
                    case 0x12: TimerB = (256 - (int)data) << (10 - 6); break; // TimerB
                    case 0x14: TimerReg = data & 0x0F; break; // タイマー制御レジスタ
                    case 0x17: break; // 未定義レジスタ
                    case 0x18: lfo.SetLFRQ(data & 0xFF); break; // LFRQ
                    case 0x19: lfo.SetPMDAMD(data & 0xFF); break; // PMD/AMD
                    case 0x1B: lfo.SetWaveForm(data & 0xFF); break; // WaveForm
                    case 0x1C: break; // 未定義レジスタ
                    default: break; // 未定義レジスタ
                }
            } else if (OpmRegNo < 0x28) { // PAN/FL/CON
                var ch = OpmRegNo - 0x20;
                SetConnection(ch, data & 7);
                pan[ch] = (byte)(data >> 6);
                op[ch, 0].SetFL(data);
            } else if (OpmRegNo < 0x30) { // KC
                var ch = OpmRegNo - 0x28;
                op[ch, 0].SetKC(data);
                op[ch, 1].SetKC(data);
                op[ch, 2].SetKC(data);
                op[ch, 3].SetKC(data);
            } else if (OpmRegNo < 0x38) { // KF
                var ch = OpmRegNo - 0x30;
                op[ch, 0].SetKF(data);
                op[ch, 1].SetKF(data);
                op[ch, 2].SetKF(data);
                op[ch, 3].SetKF(data);
            } else if (OpmRegNo < 0x40) { // PMS/AMS
                var ch = OpmRegNo - 0x38;
                lfo.SetPMSAMS(ch, data & 0xFF);
            } else if (OpmRegNo < 0x60) { // DT1/MUL
                SLOTTBL[OpmRegNo - 0x40].SetDT1MUL(data);
            } else if (OpmRegNo < 0x80) { // TL
                SLOTTBL[OpmRegNo - 0x60].SetTL(data);
            } else if (OpmRegNo < 0xA0) { // KS/AR
                SLOTTBL[OpmRegNo - 0x80].SetKSAR(data);
            } else if (OpmRegNo < 0xC0) { // AME/D1R
                SLOTTBL[OpmRegNo - 0xA0].SetAMED1R(data);
            } else if (OpmRegNo < 0xE0) { // DT2/D2R
                SLOTTBL[OpmRegNo - 0xC0].SetDT2D2R(data);
            } else { // D1L/RR
                SLOTTBL[OpmRegNo - 0xE0].SetD1LRR(data);
            }

            return (true);
        }

        public void OpmPoke_SetKF(int ch, double data) {
            op[ch, 0].SetKF(data);
            op[ch, 1].SetKF(data);
            op[ch, 2].SetKF(data);
            op[ch, 3].SetKF(data);
        }
    }
}