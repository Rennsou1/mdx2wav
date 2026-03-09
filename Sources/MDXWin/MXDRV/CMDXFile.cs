using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Xps.Serialization;

namespace MXDRV {
    public class CMDXFile {
        private CBuffer Buffer;

        private string PDXFilename;

        private int VoiceDataOffset;

        private bool isPCM8;

        private CPDXFile PDXFile = null;
        private bool isPDX_bos_pdx;
        private CPDXHQBos.CRenderBase PDXHQBosRender = null;

        public Dictionary<int, string> MDXVoiceNames, PDXVoiceNames;

        private abstract class CChannelBase {
            private CBuffer Buffer;
            public int TopOffset;
            private int Position;

            protected int ChannelNum;

            private int VoiceDataOffset;

            internal string PDXFilename;

            public CChannelBase(CBuffer _Buffer, int _TopOffset, int _ChannelNum, int _VoiceDataOffset, string _PDXFilename) {
                Buffer = _Buffer;
                TopOffset = _TopOffset;
                Position = 0;

                ChannelNum = _ChannelNum;

                VoiceDataOffset = _VoiceDataOffset;

                PDXFilename = _PDXFilename;

                if (CCommon.古いMDXWinの音量テーブルを使う) {
                    if (Volume < 16) { Volume = 0xff - (Volume * 8); }
                }
            }

            public bool isEOF = false;

            public int LoopCount = 0;

            private int NextClockCount = 0;

            protected int Volume = 0x08; // v8

            private int KeyOffTimeout = 0;
            private bool NextNoteDiabledKeyOff = false;

            private int VoiceNum = 0;
            public int Panpot = 3;
            protected bool KeyOnFlag = false;
            protected int KeyCode = 0;
            protected int Detune = 0;
            private int KeyOnDelay = 0;
            private int KeyOnDelayTimeout = 0;
            private int KeyOnDelayKeyCode = -1;
            private int Quantize = 8;

            private class CRepeatBuffer {
                public int Count;
                public CRepeatBuffer(int _Count) {
                    Count = _Count;
                }
            }
            private List<CRepeatBuffer> RepeatBuffer = new List<CRepeatBuffer>();

            public abstract void NoteOff();
            public abstract void NoteOn();
            public abstract void ApplyVolume();
            public abstract void ApplyPanpot();
            public abstract void ApplyTone();
            public abstract void SetVoice(CBuffer Buffer, int VoiceDataOffset, int VoiceNum);
            public abstract void SetOPMNoiseFreqOrADPCMFreq(byte Data);
            public abstract void SetOPMLFO(byte SyncWave, byte LFRQ, byte PMD, byte AMD, byte PmsAms);
            public abstract void SetEnabledOPMLFO(bool Enabled);
            public abstract void GetPCM(int SampleRate, float[] buf, int SamplesCount, double TotalVolume);
            public abstract int GetVolume();
            public abstract int GetLastNoteNum();
            public abstract double GetLastNoteNumFine();

            public CPortamento Portamento = new CPortamento();
            public CSoftLFO SoftLFO = new CSoftLFO();

            public MusDriver.CCommon.CVisualPart GetVisualStatus(Dictionary<int, string> MDXVoiceNames, Dictionary<int, string> PDXVoiceNames) {
                var res = new MusDriver.CCommon.CVisualPart();

                res.Program = VoiceNum;
                res.KeyOnFlag = KeyOnFlag;
                res.KeyCode = KeyCode;
                res.Panpot = Panpot;

                res.LastNoteNum = GetLastNoteNum();
                res.LastNoteNumFine = GetLastNoteNumFine();

                {
                    var tmp = "";
                    {
                        string q;
                        if (Quantize < 0x80) {
                            q = "q:" + Quantize.ToString();
                        } else {
                            q = "q" + (0x100 - Quantize).ToString("X2");
                        }
                        tmp += q.PadRight(4);
                    }
                    {
                        string key;
                        if (res.KeyOnFlag) {
                            key = "K:" + res.KeyCode.ToString().PadLeft(2) + " " + MusDriver.CCommon.KeyCodeToString(res.KeyCode);
                        } else {
                            key = "K:--";
                        }
                        tmp += key.PadRight(10);
                    }
                    tmp += "D:" + MusDriver.CCommon.IntToStrPlusMinusPadLeft(Detune, 6) + " ";
                    tmp += "P:" + (SoftLFO.SoftLFO_GetEnabled() ? MusDriver.CCommon.IntToStrPlusMinusPadLeft((int)SoftLFO.PhaseLFO_GetOffset(), 6) : "   ---") + MusDriver.CCommon.IntToStrPlusMinusPadLeft((int)Portamento.GetOffset(), 6);
                    res.Text1 = tmp;
                }

                {
                    var tmp = "";
                    tmp += "V:$" + Volume.ToString("X2") + "@" + GetVolume().ToString().PadLeft(3, '0') + (SoftLFO.SoftLFO_GetEnabled() ? MusDriver.CCommon.IntToStrPlusMinusPadLeft(SoftLFO.AmpLFO_GetOffset(), 4) : " ---") + " ";
                    tmp += "DAT:";
                    {
                        var DataOffset = TopOffset + Position;
                        tmp += DataOffset.ToString("X4") + ":";
                        Buffer.SetPosition(DataOffset);
                        var len = Buffer.GetLength();
                        for (var idx = 0; idx < 4; idx++) {
                            if (Buffer.GetPosition() < len) {
                                tmp += Buffer.ReadU8().ToString("X2");
                            } else {
                                tmp += "--";
                            }
                        }
                        tmp += " ";
                    }
                    tmp += "Lop:" + (1 + LoopCount).ToString();
                    res.Text2 = tmp;
                }

                {
                    string VoiceName;
                    if (ChannelNum < 8) {
                        VoiceName = MDXVoiceNames.ContainsKey(VoiceNum) ? MDXVoiceNames[VoiceNum] : "";
                    } else {
                        VoiceName = PDXVoiceNames.ContainsKey((VoiceNum << 8) | KeyCode) ? PDXVoiceNames[(VoiceNum << 8) | KeyCode] : "";
                    }
                    if (VoiceName.Equals("")) {
                        res.Text3 = "";
                    } else {
                        res.Text3 = "@" + VoiceNum.ToString().PadLeft(3, '0') + " " + VoiceName;
                    }
                }

                return (res);
            }

            private void ExecKeyOn() {
                if (KeyOnFlag) {
                    Portamento.SetKeyOn(true);
                    SoftLFO.SetKeyOn(true);
                    ApplyTone();
                    ApplyVolume();
                    NoteOn();
                }

                if (NextNoteDiabledKeyOff) {
                    NextNoteDiabledKeyOff = false;
                    KeyOffTimeout = 0;
                } else {
                    if (Quantize == 0) {
                        KeyOffTimeout = NextClockCount;
                    } else {
                        if (Quantize < 0) {
                            KeyOffTimeout = NextClockCount + Quantize;
                        } else {
                            KeyOffTimeout = NextClockCount * Quantize / 8;
                        }
                    }
                    if (KeyOffTimeout < 1) { KeyOffTimeout = 1; }
                }
            }

            public bool NextClock(CMDXFile Parent) {
                if (isEOF) { return (true); }

                {
                    var PortaRes = Portamento.Calc();
                    var CalcRes = SoftLFO.Calc();
                    if (PortaRes || CalcRes.RequestToneUpdate) { ApplyTone(); }
                    if (CalcRes.RequestVolumeUpdate) { ApplyVolume(); }
                }

                if (1 <= KeyOffTimeout) {
                    KeyOffTimeout--;
                    if (KeyOffTimeout == 0) {
                        Portamento.SetKeyOn(false);
                        SoftLFO.SetKeyOn(false);
                        NoteOff();
                    }
                }

                if (1 <= KeyOnDelayTimeout) { // KeyOffTimeoutより後に処理する
                    KeyOnDelayTimeout--;
                    if (KeyOnDelayTimeout == 0) {
                        KeyOnFlag = true;
                        KeyCode = KeyOnDelayKeyCode;
                        KeyOnDelayKeyCode = -1;
                        ExecKeyOn();
                    }
                }

                if (Parent.GetSyncWait(ChannelNum)) { return (false); }

                while ((NextClockCount == 0) && !isEOF) {
                    Buffer.SetPosition(TopOffset + Position); // ループ命令でジャンプすることがあるので毎回シークする

                    var cmd = Buffer.ReadU8();
                    Position++;

                    if (cmd < 0xe0) {
                        if (cmd < 0x80) { // 休符データ
                            KeyOnFlag = false;
                            KeyOnDelayKeyCode = -1;
                            NextClockCount = 1 + cmd;
                            KeyOnDelayTimeout = 0;
                            KeyOffTimeout = NextClockCount + KeyOnDelay;
                        } else {  // 音符データ
                            if (KeyOnDelay == 0) {
                                KeyOnFlag = true;
                                KeyCode = cmd - 0x80;
                            } else {
                                KeyOnFlag = false;
                                KeyOnDelayKeyCode = cmd - 0x80;
                            }
                            NextClockCount = 1 + Buffer.ReadU8();
                            Position++;
                            if (KeyOnDelay == 0) {
                                KeyOnDelayTimeout = 0;
                                ExecKeyOn();
                            } else {
                                KeyOnDelayTimeout = KeyOnDelay;
                            }
                        }
                        continue;
                    }

                    switch (cmd) {
                        case 0xff: // テンポ設定
                            Parent.SetTimerB(Buffer.ReadU8());
                            Position++;
                            break;
                        case 0xfe: // OPMレジスタ設定
                            var Addr = Buffer.ReadU8();
                            var Data = Buffer.ReadU8();
                            Position += 2;
                            if (!CCommon.X68Sound.OpmPoke(Addr, Data, false)) {
                                CCanIgnoreException.Throw("OpmPoke RegAddr error.", "addr=0x" + Addr.ToString("x2") + ", Data=0x" + Data.ToString("x2"));
                            }
                            break;
                        case 0xfd: // 音色設定
                            VoiceNum = Buffer.ReadU8();
                            Position++;
                            NoteOff();
                            SetVoice(Buffer, VoiceDataOffset, VoiceNum);
                            break;
                        case 0xfc: // 出力位相設定
                            Panpot = Buffer.ReadU8();
                            if (3 < Panpot) { Panpot = 3; }
                            Position++;
                            ApplyPanpot();
                            break;
                        case 0xfb: // 音量設定
                            var _Volume = Buffer.ReadU8();
                            if (ChannelNum < 8) { // OPM
                                Volume = _Volume;
                            } else { // ADPCM PCM8を使わないときは音量設定を無視する
                                if (Parent.isPCM8) { Volume = _Volume; }
                            }
                            Position++;
                            if (CCommon.古いMDXWinの音量テーブルを使う) {
                                if (Volume < 16) { Volume = 0xff - (Volume * 8); }
                            }
                            ApplyVolume();
                            break;
                        case 0xfa: // 音量減小
                            var last = Volume;
                            if ((Volume & 0x80) != 0) {
                                if (Volume < 0xff) { Volume++; }
                            } else {
                                if (0 < Volume) { Volume--; }
                            }
                            ApplyVolume();
                            break;
                        case 0xf9: // 音量増大
                            last = Volume;
                            if ((Volume & 0x80) != 0) {
                                if (0x80 < Volume) { Volume--; }
                            } else {
                                if (Volume < 15) { Volume++; }
                            }
                            ApplyVolume();
                            break;
                        case 0xf8: // 発音長指定
                            Quantize = Buffer.ReadS8();
                            Position++;
                            break;
                        case 0xf7: // キーオフ無効
                            NextNoteDiabledKeyOff = true;
                            break;
                        case 0xf6: // リピート開始
                            var RepeatCount = Buffer.ReadU8();
                            var Repeat0x00 = Buffer.ReadU8();
                            Position += 2;
                            if (Repeat0x00 != 0x00) { CCanIgnoreException.Throw(Lang.CLang.GetMXDRV(Lang.CLang.EMXDRV.RepeatParamError), "Repeat0x00 0x00!=0x" + cmd.ToString("x2")); }
                            RepeatBuffer.Insert(0, new CRepeatBuffer(RepeatCount - 1));
                            break;
                        case 0xf5: // リピート終端
                            var ToRepeatStartOffset = Buffer.ReadS16() - 2;
                            Position += 2;
                            if (RepeatBuffer.Count == 0) {
                                CCanIgnoreException.Throw(Lang.CLang.GetMXDRV(Lang.CLang.EMXDRV.RepeatEndNotStarted), "");
                                Position += ToRepeatStartOffset + 2;
                            } else {
                                if (1 <= RepeatBuffer[0].Count) {
                                    if (RepeatBuffer[0].Count == 255) { // 255回ループは無限ループとして扱う（データエンドと同じでループ回数+1する）
                                        LoopCount++;
                                    } else {
                                        RepeatBuffer[0].Count--;
                                    }
                                    Position += ToRepeatStartOffset + 2;
                                } else {
                                    RepeatBuffer.RemoveAt(0);
                                }
                            }
                            break;
                        case 0xf4: // リピート脱出
                            var ToRepeatEndOffset = Buffer.ReadS16() - 1;
                            Position += 2;
                            var isRepeatExit = false;
                            if (RepeatBuffer.Count == 0) {
                                CCanIgnoreException.Throw("0xf4 RepeatExit", Lang.CLang.GetMXDRV(Lang.CLang.EMXDRV.RepeatExitNotStarted));
                                isRepeatExit = true;
                            }
                            if (RepeatBuffer[0].Count == 0) {
                                RepeatBuffer.RemoveAt(0);
                                isRepeatExit = true;
                            }
                            if (isRepeatExit) {
                                Position += ToRepeatEndOffset + 3;
                            }
                            break;
                        case 0xf3: // デチューン
                            Detune = Buffer.ReadS16();
                            Position += 2;
                            ApplyTone();
                            break;
                        case 0xf2: // ポルタメント
                            Portamento.SetDelta(Buffer.ReadS16());
                            Position += 2;
                            break;
                        case 0xf1: // データエンド
                            var LoopPointer = (int)Buffer.ReadU8();
                            Position++;
                            if (LoopPointer == 0x00) {
                                isEOF = true;
                            } else { // ポインタ位置から再演奏
                                Position--;
                                Buffer.SetPosition(TopOffset + Position);
                                LoopPointer = Buffer.ReadS16();
                                Position += 2;
                                Position += LoopPointer;
                            }
                            LoopCount++;
                            break;
                        case 0xf0: // キーオンディレイ
                            KeyOnDelay = Buffer.ReadU8();
                            Position++;
                            break;
                        case 0xef: // 同期信号送出
                            var SendSyncCh = Buffer.ReadU8();
                            Position++;
                            Parent.SetSyncWait(SendSyncCh, false);
                            break;
                        case 0xee: // 同期信号待機
                            Parent.SetSyncWait(ChannelNum, true);
                            return (false); // 強制的にループから抜ける
                        case 0xed: // ADPCM/ノイズ周波数設定
                            SetOPMNoiseFreqOrADPCMFreq(Buffer.ReadU8());
                            Position++;
                            break;
                        case 0xec: // 音程LFO制御
                            var PhaseLFO_Wave = Buffer.ReadU8();
                            Position++;
                            switch (PhaseLFO_Wave) {
                                case 0x80: // MPOF
                                    SoftLFO.PhaseLFO_SetEnabled(false);
                                    break;
                                case 0x81: // MPON
                                    SoftLFO.PhaseLFO_SetEnabled(true);
                                    break;
                                default: // Set ToneLFO
                                    var PhaseLFO_Freq = Buffer.ReadU16();
                                    var PhaseLFO_Delta = Buffer.ReadS16();
                                    Position += 4;
                                    SoftLFO.PhaseLFO_SetParams(PhaseLFO_Wave, PhaseLFO_Freq, PhaseLFO_Delta);
                                    break;
                            }
                            ApplyTone();
                            break;
                        case 0xeb: // 音量LFO制御
                            var AmpLFO_Wave = Buffer.ReadU8();
                            Position++;
                            switch (AmpLFO_Wave) {
                                case 0x80: // MAOF
                                    SoftLFO.AmpLFO_SetEnabled(false);
                                    break;
                                case 0x81: // MAON
                                    SoftLFO.AmpLFO_SetEnabled(true);
                                    break;
                                default: // Set AmpLFO
                                    var AmpLFO_Freq = Buffer.ReadU16();
                                    var AmpLFO_Delta = Buffer.ReadS16();
                                    Position += 4;
                                    SoftLFO.AmpLFO_SetParams(AmpLFO_Wave, AmpLFO_Freq, AmpLFO_Delta);
                                    break;
                            }
                            ApplyVolume();
                            break;
                        case 0xea: // OPMLFO制御
                            var SyncWave = Buffer.ReadU8();
                            Position++;
                            switch (SyncWave) {
                                case 0x80: // MHOF
                                    SetEnabledOPMLFO(false);
                                    break;
                                case 0x81: // MHON
                                    SetEnabledOPMLFO(true);
                                    break;
                                default: // Set OPMLFO
                                    var LFRQ = Buffer.ReadU8();
                                    var PMD = Buffer.ReadU8();
                                    var AMD = Buffer.ReadU8();
                                    var PmsAms = Buffer.ReadU8();
                                    Position += 4;
                                    SetOPMLFO(SyncWave, LFRQ, PMD, AMD, PmsAms);
                                    break;
                            }
                            break;
                        case 0xe9: // LFOディレイ設定(SoftLFOのみ)
                            SoftLFO.KeyOnDelay = Buffer.ReadU8();
                            Position++;
                            break;
                        case 0xe8: // PCM8拡張モード移行
                            break;
                        case 0xe7: // フェードアウト
                            var Fadeout0x01 = Buffer.ReadU8();
                            var FadeoutSpeed = Buffer.ReadU8();
                            Position += 2;
                            if (Fadeout0x01 != 0x01) { CCanIgnoreException.Throw(Lang.CLang.GetMXDRV(Lang.CLang.EMXDRV.FadeoutParamError), "Fadeout0x01 0x01!=0x" + cmd.ToString("x2")); }
                            Parent.SetFadeout(FadeoutSpeed);
                            break;
                        case 0xe6: // undef
                        case 0xe5:
                        case 0xe4:
                        case 0xe3:
                        case 0xe2:
                        case 0xe1:
                        case 0xe0:
                            CCanIgnoreException.Throw(Lang.CLang.GetMXDRV(Lang.CLang.EMXDRV.UndefCommand), "cmd=0x" + cmd.ToString("x2"));
                            break;
                        default:
                            ExceptionLog = Lang.CLang.GetMXDRV(Lang.CLang.EMXDRV.CommandSwitchError) + " cmd=0x" + cmd.ToString("x2");
                            return (true);
                    }
                }

                NextClockCount--;

                return (false);
            }
        }

        private class CChannelOPM : CChannelBase {
            private CMDXVoice MDXVoice = null;

            public CChannelOPM(CBuffer Buffer, int TopOffset, int ChannelNum, int VoiceDataOffset, string PDXFilename) : base(Buffer, TopOffset, ChannelNum, VoiceDataOffset, PDXFilename) {
                SetVoice(Buffer, VoiceDataOffset, -1);
            }

            public override void NoteOff() {
                MDXVoice.NoteOff();
            }
            public override void NoteOn() {
                if (MusDriver.CCommon.GetMuteCh(ChannelNum)) { return; }
                MDXVoice.NoteOn();
            }
            public override void ApplyVolume() {
                MDXVoice.ApplyVolume(Volume, SoftLFO.AmpLFO_GetOffset());
            }
            public override int GetVolume() {
                return (MDXVoice.GetVolume());
            }
            public override void ApplyPanpot() {
                MDXVoice.ApplyPanpot(Panpot);
            }
            public override void ApplyTone() {
                double d = Detune;
                d += Portamento.GetOffset();
                d += SoftLFO.PhaseLFO_GetOffset();
                MDXVoice.SetTone(KeyCode, d);
            }
            public override void SetVoice(CBuffer Buffer, int VoiceDataOffset, int VoiceNum) {
                MDXVoice = new CMDXVoice(ChannelNum, Buffer, VoiceDataOffset, VoiceNum);
                if (!MDXVoice.WarnLog.Equals("")) { WarnLog.Add(MDXVoice.WarnLog); }
                NoteOff();
                MDXVoice.ApplyBaseVoice();
                ApplyVolume();
                ApplyPanpot();
            }
            public override void SetOPMNoiseFreqOrADPCMFreq(byte Data) {
                if (ChannelNum != 7) { WarnLog.Add(Lang.CLang.GetMXDRV(Lang.CLang.EMXDRV.SetOPMNoiseChError) + " ChannelNum=" + ChannelNum.ToString("x")); return; }
                var NoiseFreq = Data;
                CCommon.X68Sound.OpmPoke(0x0f, NoiseFreq);
            }

            private byte OPMLFO_PmsAms = 0x00;
            public override void SetOPMLFO(byte SyncWave, byte LFRQ, byte PMD, byte AMD, byte PmsAms) {
                CCommon.X68Sound.OpmPoke(0x1b, SyncWave);
                CCommon.X68Sound.OpmPoke(0x18, LFRQ);
                CCommon.X68Sound.OpmPoke(0x19, PMD);
                CCommon.X68Sound.OpmPoke(0x19, AMD);
                OPMLFO_PmsAms = PmsAms;
                CCommon.X68Sound.OpmPoke(0x38 + ChannelNum, OPMLFO_PmsAms);
            }
            public override void SetEnabledOPMLFO(bool Enabled) {
                CCommon.X68Sound.OpmPoke(0x38 + ChannelNum, Enabled ? OPMLFO_PmsAms : 0x00);
            }
            public override void GetPCM(int SampleRate, float[] buf, int SamplesCount, double TotalVolume) { }
            public override int GetLastNoteNum() { return MDXVoice.LastNoteNum; }
            public override double GetLastNoteNumFine() { return MDXVoice.LastNoteNumFine; }
        }

        private class CChannelADPCM : CChannelBase {
            private CPDXFile PDXFile;

            private int SampleRateData = 0x04; // ADPCM 15625Hz
            private int VoiceNum = 0;

            private CPDXFile.CPCM PCM = null;

            public CChannelADPCM(CBuffer Buffer, int TopOffset, int ChannelNum, int VoiceDataOffset, string PDXFilename, CPDXFile _PDXFile) : base(Buffer, TopOffset, ChannelNum, VoiceDataOffset, PDXFilename) {
                PDXFile = _PDXFile;
            }

            public override void NoteOff() {
                PCM = null;
            }
            public override void NoteOn() {
                if (MusDriver.CCommon.GetMuteCh(ChannelNum)) { return; }
                if (PCM != null) { return; }
                if (PDXFile == null) { return; } // PDXファイルが読み込まれていないときは、警告も例外も出さない
                PCM = new CPDXFile.CPCM(PDXFile, VoiceNum, KeyCode, SampleRateData, Volume, SoftLFO.AmpLFO_GetOffset());
                if (PCM.Voice == null) {
                    if (PDXFilename.Equals("vbtr_stp.pdx") || PDXFilename.Equals("sonic04.pdx")) { return; }
                    CCanIgnoreException.Throw(Lang.CLang.GetMXDRV(Lang.CLang.EMXDRV.UndefPCMVoiceNote), "ChannelNum=" + ChannelNum.ToString("x") + ", Voice=" + VoiceNum + ", KeyCode=" + KeyCode.ToString() + "(" + MusDriver.CCommon.KeyCodeToString(KeyCode) + ")");
                }
            }
            public override void ApplyVolume() {
                if (PCM != null) { PCM.SetVolume(Volume, SoftLFO.AmpLFO_GetOffset()); }
            }
            public override int GetVolume() {
                if (PCM != null) { return (PCM.GetVolume()); }
                return (0);
            }
            public override void ApplyPanpot() { }
            public override void ApplyTone() { }
            public override void SetVoice(CBuffer Buffer, int VoiceDataOffset, int _VoiceNum) {
                VoiceNum = _VoiceNum;
            }
            public override void SetOPMNoiseFreqOrADPCMFreq(byte Data) {
                SampleRateData = Data;
                if (PCM != null) { PCM.SetSampleRateData(SampleRateData); }
            }
            public override void SetOPMLFO(byte SyncWave, byte LFRQ, byte PMD, byte AMD, byte PmsAms) { }
            public override void SetEnabledOPMLFO(bool Enabled) { }
            public override void GetPCM(int SampleRate, float[] buf, int SamplesCount, double TotalVolume) {
                if (PCM != null) { PCM.GetPCM(ChannelNum, SampleRate, buf, SamplesCount, (float)TotalVolume); }
            }
            public override int GetLastNoteNum() { return 1 + KeyCode; }
            public override double GetLastNoteNumFine() { return 0; }
        }
        private class CChannelADPCM_HQBos : CChannelBase {
            private CPDXHQBos.CRenderBase Render;

            private int SampleRateData = 0x04; // ADPCM 15625Hz
            private int VoiceNum = 0;

            private CPDXHQBos.CPCM PCM = null;

            public CChannelADPCM_HQBos(CBuffer Buffer, int TopOffset, int ChannelNum, int VoiceDataOffset, string PDXFilename, CPDXHQBos.CRenderBase _Render) : base(Buffer, TopOffset, ChannelNum, VoiceDataOffset, PDXFilename) {
                Render = _Render;
            }

            public override void NoteOff() {
                PCM = null;
            }
            public override void NoteOn() {
                if (MusDriver.CCommon.GetMuteCh(ChannelNum)) { return; }
                if (PCM != null) { return; }
                if (VoiceNum != 0) { return; }
                PCM = new CPDXHQBos.CPCM(Render, ChannelNum, KeyCode, SampleRateData, Volume, SoftLFO.AmpLFO_GetOffset(), Panpot);
            }
            public override void ApplyVolume() {
                if (PCM != null) { PCM.SetVolume(Volume, SoftLFO.AmpLFO_GetOffset()); }
            }
            public override int GetVolume() {
                if (PCM != null) { return (PCM.GetVolume()); }
                return (0);
            }
            public override void ApplyPanpot() {
                if (PCM != null) { PCM.SetPanpot(Panpot); }
            }
            public override void ApplyTone() { }
            public override void SetVoice(CBuffer Buffer, int VoiceDataOffset, int _VoiceNum) {
                VoiceNum = _VoiceNum;
            }
            public override void SetOPMNoiseFreqOrADPCMFreq(byte Data) {
                SampleRateData = Data;
                if (PCM != null) { PCM.SetSampleRateData(Data); }
            }
            public override void SetOPMLFO(byte SyncWave, byte LFRQ, byte PMD, byte AMD, byte PmsAms) { }
            public override void SetEnabledOPMLFO(bool Enabled) { }
            public override void GetPCM(int SampleRate, float[] buf, int SamplesCount, double TotalVolume) { }
            public override int GetLastNoteNum() { return 1 + KeyCode; }
            public override double GetLastNoteNumFine() { return 0; }
        }

        private CChannelBase[] Channels;

        private int TimerB;
        private System.TimeSpan TempoClock;

        private void SetTimerB(byte Data) {
            TimerB = Data;
            var micros = (1024 * (256 - TimerB)) / 4;
            TempoClock = System.TimeSpan.FromSeconds((double)micros / 1000 / 1000);
        }

        private int TotalClock;
        private System.TimeSpan PlayTS;

        public bool isEOF = false;

        private bool FadeoutEnabled = false;
        private double FadeoutAlpha = 1;
        public void SetFadeout(byte Speed) {
            if (!FadeoutEnabled) {
                FadeoutEnabled = true;
                FadeoutAlpha = 1;
                WarnLog.Add(Lang.CLang.GetMXDRV(Lang.CLang.EMXDRV.FadeoutStarted));
            }
        }
        public bool GetFadeout() { return (FadeoutEnabled); }
        public double GetFadeoutAlpha() { return (FadeoutAlpha); }

        private bool[] SyncWait = new bool[16];

        internal void SetSyncWait(int Ch, bool f) {
            if (Channels.Length <= Ch) { CCanIgnoreException.Throw(f ? Lang.CLang.GetMXDRV(Lang.CLang.EMXDRV.SyncSignalOverChOutOfRange_Wait) : Lang.CLang.GetMXDRV(Lang.CLang.EMXDRV.SyncSignalOverChOutOfRange_Send), "Ch=0x" + Ch.ToString("x")); }
            if (SyncWait[Ch] == f) {
                if (f) {
                    WarnLog.Add("Ch:" + Ch.ToString("x") + " " + Lang.CLang.GetMXDRV(Lang.CLang.EMXDRV.SyncSignalWaitOnWaiting));
                } else {
                    WarnLog.Add("Ch:" + Ch.ToString("x") + " " + Lang.CLang.GetMXDRV(Lang.CLang.EMXDRV.SyncSignalNoWaitOnNoWaiting));
                }
            }
            SyncWait[Ch] = f;
        }
        internal bool GetSyncWait(int Ch) { return (SyncWait[Ch]); }

        public CMDXFile(string MDXFilename, string BasePath, System.TimeSpan SkipTS) {
            try {
                CCanIgnoreException.GetStack();

                Channels = new CChannelBase[0]; // 例外が発生したときのために長さ0で作成しておく

                CCommon.X68Sound = new X68SoundMCh.CAPI();

                Buffer = new CBuffer(MDXFilename, 16, 0xff);

                if (Buffer.ReadU8() == 0x00) { throw new Exception(Lang.CLang.GetMXDRV(Lang.CLang.EMXDRV.NoMDXFormatFile_StartWith0x00)); }
                Buffer.SetPosition(0);

                {
                    var reader = new CMDXTitlePDXFilenameReader(Buffer);
                    var MDXTitle = reader.MDXTitle;
                    PDXFilename = reader.PDXFilename;
                    if (!PDXFilename.Equals("")) { PDXFilename = BasePath + @"\" + PDXFilename; }

                    var oldf = MDXTitle.IndexOf("by Moonlight", StringComparison.OrdinalIgnoreCase) != -1;
                    CCommon.古いMDXWinの音量テーブルを使う = oldf;
                    CCommon.PCM8Volume = oldf ? 0.9f : 0.65f;
                }

                var ChannelOffset = Buffer.GetPosition();

                VoiceDataOffset = ChannelOffset + Buffer.ReadU16();

                {  // Aチャネルの先頭データを読んで、PCM8対応MDXか判断する
                    var lastbufpos = Buffer.GetPosition();
                    Buffer.SetPosition(ChannelOffset + Buffer.ReadU16());
                    isPCM8 = Buffer.ReadU8() == 0xe8;
                    Buffer.SetPosition(lastbufpos);
                }

                MDXVoiceNames = CVoiceName.GetVoiceNamesFromMML(MDXFilename);

                var UsePDXHQBos = false;

                PDXVoiceNames = new();

                if (PDXFilename.Equals("") || !System.IO.File.Exists(PDXFilename)) {
                    isPDX_bos_pdx = false;
                } else {
                    var PDXRawData = System.IO.File.ReadAllBytes(PDXFilename);

                    isPDX_bos_pdx = CPDXHQBos.isPDX_bos_pdx(PDXRawData);

                    if (isPDX_bos_pdx && CCommon.UseBosPdxHQ) {
                        if (CPDXHQBos.Voices_Load()) {
                            UsePDXHQBos = true;
                        }
                    }
                    if (UsePDXHQBos) {
                        PDXHQBosRender = isPCM8 ? new CPDXHQBos.CRenderPCM8() : new CPDXHQBos.CRenderPCM1();
                    } else {
                        PDXFile = new CPDXFile(PDXRawData);
                    }

                    if (isPDX_bos_pdx) {
                        PDXVoiceNames = CVoiceName.GetVoiceNamesFromBosPDX();
                    } else {
                        try {
                            PDXVoiceNames = CVoiceName.GetVoiceNamesFromPDL(PDXFilename);
                        } catch (Exception ex) {
                            WarnLog.Add(Lang.CLang.GetMXDRV(Lang.CLang.EMXDRV.PDLLoadError) + " " + ex.Message);
                            PDXVoiceNames.Clear();
                        }
                    }
                }

                Channels = new CChannelBase[isPCM8 ? 16 : 9];
                for (var ch = 0; ch < Channels.Length; ch++) {
                    var ofs = ChannelOffset + Buffer.ReadU16();
                    if (ch < 8) {
                        Channels[ch] = new CChannelOPM(Buffer, ofs, ch, VoiceDataOffset, PDXFilename);
                    } else {
                        if (UsePDXHQBos) {
                            Channels[ch] = new CChannelADPCM_HQBos(Buffer, ofs, ch, VoiceDataOffset, PDXFilename, PDXHQBosRender);
                        } else {
                            Channels[ch] = new CChannelADPCM(Buffer, ofs, ch, VoiceDataOffset, PDXFilename, PDXFile);
                        }
                    }
                    SyncWait[ch] = false;
                }

                SetTimerB(200);

                TotalClock = 0;
                PlayTS = System.TimeSpan.FromSeconds(0);

                if (!PDXFilename.Equals("")) {
                    if ((PDXHQBosRender == null) && (PDXFile == null)) {
                        WarnLog.Add(Lang.CLang.GetMXDRV(Lang.CLang.EMXDRV.NotFoundPDX));
                    }
                }

                if (SkipTS != System.TimeSpan.FromSeconds(0)) {
                    while (GetPlayTS() < SkipTS) {
                        if (NextClock()) { break; }
                    }
                }
            } catch (Exception ex) {
                isEOF = true;
                ExceptionLog = "Constructor:" + ex.Message;
            }
        }

        public void PDXFile_OutputADPCMs(string OutputFileBase) {
            if (PDXFile != null) {
                PDXFile.OutputADPCMs(OutputFileBase);
            }
        }

        public List<string> GetInfo() {
            var res = new List<string>();

            if (isEOF) {
                res.Add(Lang.CLang.GetMXDRV(Lang.CLang.EMXDRV.PerformEnded));
                return (res);
            }

            {
                var Line = "OPM ch offsets. ";
                for (var ch = 0; ch < ((Channels.Length < 8) ? Channels.Length : 8); ch++) {
                    Line += ch.ToString("x") + ":0x" + Channels[ch].TopOffset + ", "; ;
                }
                res.Add(Line);
            }
            {
                var Line = "PCM ch offsets. ";
                for (var ch = 8; ch < Channels.Length; ch++) {
                    Line += ch.ToString("x") + ":0x" + Channels[ch].TopOffset + ", "; ;
                }
                res.Add(Line);
            }

            res.Add("VoiceDataOffset: 0x" + VoiceDataOffset.ToString("x4"));

            if (!PDXFilename.Equals("")) {
                if (PDXHQBosRender != null) {
                    res.Add(Lang.CLang.GetMXDRV(Lang.CLang.EMXDRV.UsePDXHQBos));
                } else {
                    if (PDXFile == null) {
                        res.Add(Lang.CLang.GetMXDRV(Lang.CLang.EMXDRV.NotFoundPDX));
                    } else {
                        foreach (var Line in PDXFile.GetInfo()) {
                            res.Add(Line);
                        }
                    }
                }
            }

            if (CCommon.古いMDXWinの音量テーブルを使う) { res.Add(Lang.CLang.GetMXDRV(Lang.CLang.EMXDRV.EnableOldVolumeTable)); }

            return (res);
        }

        public string GetPDXFilename() { return (PDXFilename); }

        private static string ExceptionLog = "";
        public string GetExceptionLog() {
            var res = ExceptionLog;
            ExceptionLog = "";
            return (res);
        }

        private static List<string> WarnLog = new List<string>();
        public List<string> GetWarnLog() {
            var log = WarnLog;
            WarnLog = new List<string>();
            return (log);
        }

        public int GetLoopCount() {
            var mincnt = -1;
            for (var ch = 0; ch < Channels.Length; ch++) {
                if (Channels[ch].isEOF) { continue; }
                if (GetSyncWait(ch)) { continue; }
                var LoopCount = Channels[ch].LoopCount;
                if (mincnt == -1) { mincnt = LoopCount; }
                if (LoopCount < mincnt) { mincnt = LoopCount; }
            }
            if (mincnt == -1) { return (0); } // 全チャネル停止
            return (mincnt);
        }

        public bool NextClock() {
            try {
                if (isEOF) { return (true); }

                TotalClock++;
                var ClockTS = GetClocksTime(1);
                PlayTS += ClockTS;

                if (GetFadeout()) {
                    FadeoutAlpha -= ClockTS / MusDriver.CCommon.FadeoutTS;
                    if (FadeoutAlpha < 0) { FadeoutAlpha = 0; }
                } else {
                    var MaxLoopCount = MDXWin.CCommon.AudioThread.Settings.LoopCount;
                    if (MaxLoopCount != 0) {
                        if (MaxLoopCount <= GetLoopCount()) { SetFadeout(0x00); }
                    }
                }
                if (FadeoutAlpha == 0) {
                    isEOF = true;
                    return (true);
                }

                isEOF = true;
                for (var ch = Channels.Length - 1; 0 <= ch; ch--) { // 同期信号を正しく処理するために後ろのチャネルから処理しなければいけなかったような気がする
                    try {
                        if (!Channels[ch].NextClock(this)) {
                            if (!GetSyncWait(ch)) {
                                isEOF = false;
                            }
                        }
                    } catch (Exception ex) {
                        var items = ex.Message.Split('|');
                        if ((items.Length == 3) && (items[0].Equals("CanIgnoreException"))) {
                            CCanIgnoreException.Stack = new CCanIgnoreException.CStack(items[1], items[2]);
                        } else {
                            ExceptionLog = "NextClock(" + ch.ToString("x") + "):" + ex.Message;
                        }
                        isEOF = true;
                        break;
                    }
                }

                foreach (var Line in CCanIgnoreException.GetLog()) {
                    WarnLog.Add(Line);
                }
            } catch (Exception ex) {
                ExceptionLog = "NextClock:" + ex.Message;
                isEOF = true;
            }

            return (isEOF);
        }

        public MusDriver.CCommon.EPCMSurroundMode GetPCM(int SampleRate, float[] buf, int SamplesCount) {
            if (MDXWin.CCommon.AudioThread.Settings.OPMEnabled) { CCommon.X68Sound.GetPcm(buf, SamplesCount, SampleRate); }
            if (MDXWin.CCommon.AudioThread.Settings.PCMEnabled) {
                for (var ch = 0; ch < Channels.Length; ch++) {
                    try {
                        Channels[ch].GetPCM(SampleRate, buf, SamplesCount, CCommon.PCM8Volume);
                    } catch (Exception ex) {
                        ExceptionLog = "GetPCM(" + ch.ToString("x") + "):" + ex.Message;
                        isEOF = true;
                    }
                }
                if (PDXHQBosRender != null) {
                    try {
                        PDXHQBosRender.GetPCM(SampleRate, buf, SamplesCount, CCommon.PCM8Volume);
                    } catch (Exception ex) {
                        ExceptionLog = "PDXHQBosRender:" + ex.Message;
                        isEOF = true;
                    }
                }
            }

            if (PDXHQBosRender != null) { return MusDriver.CCommon.EPCMSurroundMode.Surround; }

            return isPCM8 ? MusDriver.CCommon.EPCMSurroundMode.PCM8 : MusDriver.CCommon.EPCMSurroundMode.PCM1;
        }

        public bool GetPDXHQBosEnabled() {
            return (PDXHQBosRender != null);
        }
        public string GetPDXCaption() {
            if (PDXFilename.Equals("")) { return ("No PDX used."); }
            if (GetPDXHQBosEnabled()) {
                return ("HQ bos.pdx enabled.");
            } else {
                if (PDXFile == null) {
                    return ("Not found " + System.IO.Path.GetFileName(PDXFilename));
                } else {
                    return (System.IO.Path.GetFileName(PDXFilename));
                }
            }
        }

        public int GetPanpot(int ch) {
            if (Channels.Length <= ch) { return (0); }
            return (Channels[ch].Panpot);
        }

        public System.TimeSpan GetClocksTime(double Clocks) { return (TempoClock * Clocks); }
        public System.TimeSpan GetPlayTS() { return (PlayTS); }

        public MusDriver.CCommon.CVisualGlobal GetVisualGlobal() {
            var res = new MusDriver.CCommon.CVisualGlobal();
            res.FileFormat = MusDriver.CDriver.EFileFormat.MXDRV;
            res.CurrentTS = PlayTS;

            res.ExtInfo1_Tag1 = "Tempo"; res.ExtInfo1_Tag2 = "beat/min";
            res.ExtInfo1_Value = (int)(60 / (TempoClock * 48).TotalSeconds);

            res.ExtInfo2_Tag1 = "Timer-B"; res.ExtInfo2_Tag2 = "Count";
            res.ExtInfo2_Value = TimerB;

            res.TotalClock = TotalClock;
            res.LoopCount = GetLoopCount();
            res.Fadeout = GetFadeoutAlpha();
            res.isPDX_bos_pdx = isPDX_bos_pdx;
            return (res);
        }

        public MusDriver.CCommon.CVisualPart GetVisualStatus(int ch) {
            if (Channels.Length <= ch) { return (null); }
            if (Channels[ch].isEOF) { return (null); }
            return (Channels[ch].GetVisualStatus(MDXVoiceNames, PDXVoiceNames));
        }

    }
}
