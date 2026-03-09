using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace MXDRV {
    internal class CPDXFile {
        // 内部int32で処理するので、2GB以上のPDXファイルは処理できない

        public class CVoice {
            public int Offset = 0;
            public byte[] Data = null;

            public float[] DecodedADPCM = null;
            public CPCMConvert.CRateConverter RateConverterADPCM = null;
            public void SetupADPCM() {
                if (DecodedADPCM != null) { return; }
                DecodedADPCM = CPCMConvert.DecodeADPCM(Data);
                RateConverterADPCM = new CPCMConvert.CRateConverter(DecodedADPCM);
            }

            public float[] DecodedPCMS16 = null;
            public CPCMConvert.CRateConverter RateConverterPCMS16 = null;
            public void SetupPCMS16() {
                if (DecodedPCMS16 != null) { return; }
                DecodedPCMS16 = new float[Data.Length / 2];
                for (var idx = 0; idx < DecodedPCMS16.Length; idx++) {
                    var raw = new byte[] { Data[idx * 2 + 1], Data[idx * 2 + 0] };
                    DecodedPCMS16[idx] = BitConverter.ToInt16(raw);
                    DecodedPCMS16[idx] /= 0x100 * 8; // なんとなく聴いた感じこれくらいの音量かなぁ。
                }
                RateConverterPCMS16 = new CPCMConvert.CRateConverter(DecodedPCMS16);
            }

        }
        public CVoice[] Voices = new CVoice[128 * 0x60];

        public CPDXFile(byte[] RawData, bool 解析のみ = false) {
            for (var VoiceNum = 0; VoiceNum < 128; VoiceNum++) {
                for (var KeyCode = 0; KeyCode < 0x60; KeyCode++) {
                    Voices[VoiceNum * 0x60 + KeyCode] = null;
                }
            }

            {
                var ErrorPDXFilename = "";
                switch (RawData.Length) {
                    case 102836: ErrorPDXFilename = "69_99.PDX"; break;
                    case 27857: ErrorPDXFilename = "ACT.PDX"; break;
                    case 818445: ErrorPDXFilename = "gr3enogu.pdx"; break;
                    case 481583: ErrorPDXFilename = "TRAIN2.pdx"; break;
                    case 510647: ErrorPDXFilename = "DETA_01_.PDX"; break;
                    case 510192: ErrorPDXFilename = "DETA_01__.PDX"; break;
                    case 72871: ErrorPDXFilename = "POOO!_EX.PDX"; break;
                    case 77568: ErrorPDXFilename = "tenshin.PDX"; break;
                }
                if (!ErrorPDXFilename.Equals("")) {
                    CCanIgnoreException.Throw("Broken PDX.", ErrorPDXFilename);
                    return;
                }
            }

            CBuffer Buffer = new CBuffer(RawData, 16, 0x80);

            var minofs = -1;

            for (var idx = 0; idx < 128 * 0x60; idx++) {
                var HeaderOffset = idx * 8;
                if (minofs != -1) {
                    if (minofs <= HeaderOffset) { break; } // ヘッダが最初のデータに到達したら中断する
                }

                if ((idx % 0x60) == 0) {
                    Buffer.SetPosition(HeaderOffset);
                    if (Buffer.ReadU8() != 0x00) { break; } // 各ボイスマップの先頭1byteが0x00ではないときは中断する（16MBytes以上のPDXファイルは想定しない）
                }

                Buffer.SetPosition(HeaderOffset);
                var Offset = Buffer.ReadS32();
                var Length = Buffer.ReadS32();
                if ((Offset == 0) || (Length == 0)) { continue; }

                if ((Offset == 0x0c080808) || (Offset == 0x08080808)) { break; }
                if ((Length == 0x0c080808) || (Length == 0x08080808)) { break; }

                if ((Offset == 0x00007131) && (Length == 0x00022470)) { break; } // HERE.MDX and HAM.PDX size overflow.
                if ((Offset == 0x00018C75) && (Length == 0x00001388)) { break; } // SPLASH01.MDX and SPLASH.pdx size overflow.
                if ((Offset == 0x000125fc) && (Length == 0x00000b60)) { break; } // KAEND.MDX and KAEND.PDX size overflow.
                if ((Offset == 0x00002095) && (Length == 0x000009a8)) { break; } // GH0DA.MDX and GH0DA.PDX size overflow.
                if ((Offset == 0x0006f47a) && (Length == 0x0000d52e)) { break; } // q2_*.mdx and qrt2mu.pdx size overflow.
                if ((Offset == 0x0000c7ef) && (Length == 0x000011f6)) { break; } // KISSME.pdx size overflow.
                if ((Offset == 0x0003032f) && (Length == 0x00002454)) { break; } // RSII_I.PDX size overflow.
                if ((Offset == 0x00001045) && (Length == 0x000009a3)) { break; } // MJYU.PDX size overflow.
                if ((Offset == 0x0000cb9b) && (Length == 0x00001615)) { break; } // DISTINATION.pdx size overflow.

                if ((Offset < 0) || (Length < 0)) { break; } // 2GB以上のファイルは処理しない

                if (minofs == -1) { minofs = Offset; }
                if (Offset < minofs) { minofs = Offset; }

                if (Buffer.GetLength() <= Offset) { CCanIgnoreException.Throw("PDX file offset overflow.", "Voice=" + (idx / 0x60) + ", KeyCode=0x" + (idx % 0x60).ToString("x2") + " Offset=0x" + Offset.ToString("x8") + ", Length=0x" + Length.ToString("x8")); return; }
                if (Buffer.GetLength() < (Offset + Length)) {
                    if ((idx % 0x60) == 0) { break; } // ヘッダとデータの間にコメントを入れているPDXファイルがあったので、ProgramBank先頭のサイズエラーはヘッダ終端として扱う。
                    CCanIgnoreException.Throw("PDX file size overflow.", "Voice=" + (idx / 0x60) + ", KeyCode=0x" + (idx % 0x60).ToString("x2") + " Offset=0x" + Offset.ToString("x8") + ", Length=0x" + Length.ToString("x8")); return;
                }

                var Voice = new CVoice();

                Voice.Offset = Offset;
                Voice.Data = new byte[Length];

                if (!解析のみ) {
                    Buffer.SetPosition(Voice.Offset);
                    Buffer.ReadBytes(Voice.Data);
                    Voice.SetupADPCM();
                }

                Voices[idx] = Voice;
            }
        }

        public List<string> GetInfo() {
            var res = new List<string>();

            res.Add("PDX voice list.");

            for (var VoiceNum = 0; VoiceNum < 128; VoiceNum++) {
                var Line = "";
                for (var KeyCode = 0; KeyCode < 0x60; KeyCode++) {
                    var Voice = Voices[VoiceNum * 0x60 + KeyCode];
                    if ((Voice == null) || (Voice.Offset == 0)) { continue; }
                    Line += KeyCode.ToString() + "(" + MusDriver.CCommon.KeyCodeToString(KeyCode) + "):" + Voice.Offset.ToString("x") + ":" + Voice.Data.Length.ToString("x") + ", ";
                }
                if (!Line.Equals("")) { res.Add("@" + VoiceNum + " " + Line); }
            }

            return (res);
        }

        public void OutputADPCMs(string OutputPDXFileBase) {
            for (var idx = 0; idx < 128 * 0x60; idx++) {
                if (Voices[idx] == null) { continue; }
                if (Voices[idx].DecodedADPCM == null) { continue; }
                var name = idx.ToString().PadLeft(3, '0') + ".@" + (idx / 0x60).ToString().PadLeft(3, '0') + "n" + (idx % 0x60).ToString().PadLeft(3, '0') + "." + MusDriver.CCommon.KeyCodeToString(idx % 0x60);
                using (var WaveWriter = new NAudio.Wave.WaveFileWriter(OutputPDXFileBase + name + ".wav", new NAudio.Wave.WaveFormatExtensible(15625, 16, 1))) {
                    WaveWriter.WriteSamples(Voices[idx].DecodedADPCM, 0, Voices[idx].DecodedADPCM.Length);
                }
            }
        }

        public class CPCM {
            public CVoice Voice = null;

            private CPDXCommon.CPCMFormat PCMFormat;

            private int SamplePos = 0;
            private float LastSampleData = 0;
            private float SampleData = 0;
            private float Clock = 1;
            private float Volume = 0;

            public CPCM(CPDXFile PDXFile, int VoiceNum, int KeyCode, int SampleRateData, int VolumeData, int VolumeOffset) {
                if (PDXFile == null) { return; }
                if ((VoiceNum < 0) || (128 <= VoiceNum) || (KeyCode < 0) || (0x60 <= KeyCode)) { CCanIgnoreException.Throw(Lang.CLang.GetMXDRV(Lang.CLang.EMXDRV.PDX_IlligalPCMNum), "Voice=" + VoiceNum + ", KeyCode=" + KeyCode.ToString() + "(" + MusDriver.CCommon.KeyCodeToString(KeyCode) + ")"); return; }
                Voice = PDXFile.Voices[VoiceNum * 0x60 + KeyCode];
                PCMFormat = new CPDXCommon.CPCMFormat(SampleRateData);
                Volume = CPDXCommon.VolumeData2Volume(VolumeData, VolumeOffset);
            }

            public void SetSampleRateData(int SampleRateData) {
                PCMFormat = new CPDXCommon.CPCMFormat(SampleRateData);
                Debug.WriteLine(PCMFormat.Format.ToString() + "\t" + PCMFormat.SrcSampleRate.ToString());
            }
            public void SetVolume(int VolumeData, int VolumeOffset) { Volume = CPDXCommon.VolumeData2Volume(VolumeData, VolumeOffset); }
            public int GetVolume() { return ((int)(Volume * 128)); }

            public void GetPCM(int ChannelNum, int SampleRate, float[] buf, int SamplesCount, float TotalVolume) {
                if (Voice == null) { return; }

                var SrcSampleRate = PCMFormat.SrcSampleRate;
                if (SrcSampleRate == 0) { return; }

                switch (MDXWin.CCommon.AudioThread.Settings.ADPCMMode) {
                    case MDXWin.CAudioThread.CSettings.EADPCMMode.最近傍補間: {
                            switch (PCMFormat.Format) {
                                case CPDXCommon.CPCMFormat.EFormat.ADPCM:
                                    if (Voice.DecodedADPCM.Length <= SamplePos) { return; }
                                    for (var idx = 0; idx < SamplesCount; idx++) {
                                        Clock += SrcSampleRate / SampleRate;
                                        while (1 <= Clock) {
                                            Clock--;
                                            if (SamplePos < Voice.DecodedADPCM.Length) {
                                                SampleData = Voice.DecodedADPCM[SamplePos++];
                                            } else {
                                                SampleData = 0;
                                            }
                                        }

                                        var smp = SampleData;
                                        buf[idx * MusDriver.CCommon.OutputElements.Count + ChannelNum] = smp * Volume * TotalVolume;
                                    }
                                    break;
                                case CPDXCommon.CPCMFormat.EFormat.PCMS16:
                                    Voice.SetupPCMS16();
                                    if (Voice.DecodedPCMS16.Length <= SamplePos) { return; }
                                    for (var idx = 0; idx < SamplesCount; idx++) {
                                        Clock += SrcSampleRate / SampleRate;
                                        while (1 <= Clock) {
                                            Clock--;
                                            if (SamplePos < Voice.DecodedPCMS16.Length) {
                                                SampleData = Voice.DecodedPCMS16[SamplePos++];
                                            } else {
                                                SampleData = 0;
                                            }
                                        }

                                        var smp = SampleData;
                                        smp *= PCMFormat.PCMS16_Volume;
                                        buf[idx * MusDriver.CCommon.OutputElements.Count + ChannelNum] = smp * Volume * TotalVolume;
                                    }
                                    break;
                                case CPDXCommon.CPCMFormat.EFormat.PCMS8: break;
                            }
                        }
                        break;
                    case MDXWin.CAudioThread.CSettings.EADPCMMode.線形補間: {
                            switch (PCMFormat.Format) {
                                case CPDXCommon.CPCMFormat.EFormat.ADPCM:
                                    if (Voice.DecodedADPCM.Length <= SamplePos) { return; }
                                    for (var idx = 0; idx < SamplesCount; idx++) {
                                        Clock += SrcSampleRate / SampleRate;
                                        while (1 <= Clock) {
                                            Clock--;
                                            LastSampleData = SampleData;
                                            if (SamplePos < Voice.DecodedADPCM.Length) {
                                                SampleData = Voice.DecodedADPCM[SamplePos++];
                                            } else {
                                                SampleData = 0;
                                            }
                                        }

                                        var smp = (LastSampleData * (1 - Clock)) + (SampleData * Clock);
                                        buf[idx * MusDriver.CCommon.OutputElements.Count + ChannelNum] = smp * Volume * TotalVolume;
                                    }
                                    break;
                                case CPDXCommon.CPCMFormat.EFormat.PCMS16:
                                    Voice.SetupPCMS16();
                                    if (Voice.DecodedPCMS16.Length <= SamplePos) { return; }
                                    for (var idx = 0; idx < SamplesCount; idx++) {
                                        Clock += SrcSampleRate / SampleRate;
                                        while (1 <= Clock) {
                                            Clock--;
                                            LastSampleData = SampleData;
                                            if (SamplePos < Voice.DecodedPCMS16.Length) {
                                                SampleData = Voice.DecodedPCMS16[SamplePos++];
                                            } else {
                                                SampleData = 0;
                                            }
                                        }

                                        var smp = (LastSampleData * (1 - Clock)) + (SampleData * Clock);
                                        smp *= PCMFormat.PCMS16_Volume;
                                        buf[idx * MusDriver.CCommon.OutputElements.Count + ChannelNum] = smp * Volume * TotalVolume;
                                    }
                                    break;
                                case CPDXCommon.CPCMFormat.EFormat.PCMS8: break;
                            }
                        }
                        break;
                    case MDXWin.CAudioThread.CSettings.EADPCMMode.三次スプライン補間: {
                            switch (PCMFormat.Format) {
                                case CPDXCommon.CPCMFormat.EFormat.ADPCM:
                                    for (var idx = 0; idx < SamplesCount; idx++) {
                                        var pos = SamplePos * SrcSampleRate / SampleRate;
                                        if (Voice.DecodedADPCM.Length <= pos) { return; }
                                        SamplePos++;
                                        var smp = Voice.RateConverterADPCM.GetY(pos);
                                        buf[idx * MusDriver.CCommon.OutputElements.Count + ChannelNum] = smp * Volume * TotalVolume;
                                    }
                                    break;
                                case CPDXCommon.CPCMFormat.EFormat.PCMS16:
                                    Voice.SetupPCMS16();
                                    for (var idx = 0; idx < SamplesCount; idx++) {
                                        var pos = SamplePos * SrcSampleRate / SampleRate;
                                        if (Voice.DecodedPCMS16.Length <= pos) { return; }
                                        SamplePos++;
                                        var smp = Voice.RateConverterPCMS16.GetY(pos);
                                        smp *= PCMFormat.PCMS16_Volume;
                                        buf[idx * MusDriver.CCommon.OutputElements.Count + ChannelNum] = smp * Volume * TotalVolume;
                                    }
                                    break;
                                case CPDXCommon.CPCMFormat.EFormat.PCMS8: break;
                            }
                        }
                        break;
                }
            }
        }
    }
}