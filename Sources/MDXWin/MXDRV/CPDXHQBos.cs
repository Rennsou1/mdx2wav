using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Compression;
using System.Linq;
using System.Text;

#pragma warning disable SYSLIB0021 // System.Security.Cryptography.MD5CryptoServiceProvider 型またはメンバーが旧型式です

namespace MXDRV {
    internal class CPDXHQBos {
        private const float InternalVolume = 1.4f;

        private static string MD5_ComputeBuf(byte[] buf) {
            if (buf.Length == 0) { throw new Exception("長さ0のバッファは処理できません。"); }
            using (var md5 = System.Security.Cryptography.MD5.Create()) {
                var res = new System.Text.StringBuilder();
                foreach (var b in md5.ComputeHash(buf)) {
                    res.Append(b.ToString("x2"));
                }
                return res.ToString();
            }
        }

        public static bool isPDX_bos_pdx(string MD5) {
            if (MD5.Equals("d3ac3a815884e6b0c02504435879b3cd")) { return true; }
            if (MD5.Equals("7d850ab13a5f1d5badcf8c31df19ed86")) { return true; }
            return false;
        }
        public static bool isPDX_bos_pdx(byte[] buf) {
            return isPDX_bos_pdx(MD5_ComputeBuf(buf));
        }

        public class CVoice {
            public int KeyCode;
            public string TimbreName;
            public bool EnableNoteOff;
            public int SrcSampleRate = 0;
            public float[] Samples = null;
            public CPCMConvert.CRateConverter RateConverter = null;
            public float LeftRight, FrontBack;
            public CVoice(int _KeyCode, string _TimbreName, bool _EnableNoteOff, float _LeftRight, float _FrontBack, byte[] wavbuf) {
                KeyCode = _KeyCode;
                TimbreName = _TimbreName;
                EnableNoteOff = _EnableNoteOff;
                LeftRight = _LeftRight;
                FrontBack = _FrontBack;

                byte[] srcbuf;
                int bits;

                using (var mr = new System.IO.MemoryStream(wavbuf)) {
                    using (var wr = new NAudio.Wave.WaveFileReader(mr)) {
                        if (wr.WaveFormat.Channels != 1) { throw new Exception(Lang.CLang.GetMXDRV(Lang.CLang.EMXDRV.PDXHQBos_OnlySupportMonoCh) + " Channels=" + wr.WaveFormat.Channels); }
                        SrcSampleRate = wr.WaveFormat.SampleRate;
                        bits = wr.WaveFormat.BitsPerSample;
                        srcbuf = new byte[wr.Length];
                        wr.Read(srcbuf);
                    }
                }

                switch (bits) {
                    case 16:
                        Samples = new float[srcbuf.Length / 2];
                        for (var idx = 0; idx < Samples.Length; idx++) {
                            Samples[idx] = BitConverter.ToInt16(srcbuf, idx * 2) / (float)(1 << 15);
                        }
                        break;
                    case 24:
                        Samples = new float[srcbuf.Length / 3];
                        for (var idx = 0; idx < Samples.Length; idx++) {
                            var tmp = new byte[4] { 0x00, srcbuf[idx * 3 + 0], srcbuf[idx * 3 + 1], srcbuf[idx * 3 + 2] };
                            Samples[idx] = BitConverter.ToInt32(tmp) / (float)(1 << 31);
                        }
                        break;
                    default: throw new Exception(Lang.CLang.GetMXDRV(Lang.CLang.EMXDRV.PDXHQBos_OnlySupport1624bits) + " BitsPerSample=" + bits);
                }

                RateConverter = new CPCMConvert.CRateConverter(Samples);
            }
            public bool isLoaded() { return ((SrcSampleRate != 0) && (Samples != null) && (RateConverter != null)); }
        }
        public static CVoice[] Voices = null;

        public const string BosPdxHQFilename = "bos.pdx.hq.zip";

        public static bool Voices_Load(bool ExistsCheckOnly = false) {
            if (Voices != null) { return (true); }

            if (!System.IO.File.Exists(BosPdxHQFilename)) { return (false); }
            if (ExistsCheckOnly) { return true; }

            var files = new Dictionary<string, byte[]>();
            using (var zip = ZipFile.Open(BosPdxHQFilename, ZipArchiveMode.Read)) {
                foreach (var entry in zip.Entries) {
                    var buf = new byte[entry.Length];
                    var bufpos = 0;
                    using (var decompstream = entry.Open()) {
                        while (true) {
                            var size = decompstream.Read(buf, bufpos, buf.Length - bufpos);
                            if (size == 0) { break; }
                            bufpos += size;
                        }
                    }
                    if (buf.Length != bufpos) { throw new Exception(Lang.CLang.GetMXDRV(Lang.CLang.EMXDRV.PDXHQBos_UnpackZipError) + " " + entry.FullName); }
                    files[entry.FullName.ToLower()] = buf;
                }
            }

            Voices = new CVoice[0x60];
            using (var mr = new System.IO.MemoryStream(files["Mapping.txt".ToLower()])) {
                using (var rs = new System.IO.StreamReader(mr)) {
                    while (!rs.EndOfStream) {
                        var items = rs.ReadLine().Split(',');
                        var KeyCode = Convert.ToInt32(items[0], 16);
                        var TimbreName = items[1].Trim();
                        var EnableNoteOff = bool.Parse(items[2]);
                        var LeftRight = int.Parse(items[3]) / 128f;
                        var FrontBack = int.Parse(items[4]) / 128f;
                        var wavfn = "pcm" + KeyCode.ToString("x2") + ".wav";
                        var wavbuf = files[wavfn.ToLower()];
                        Voices[KeyCode] = new CVoice(KeyCode, TimbreName, EnableNoteOff, LeftRight, FrontBack, wavbuf);
                    }
                }
            }

            return (true);
        }

        public abstract class CRenderBase {
            public abstract void NoteOn(int ChannelNum, int KeyCode, float SrcSampleRate, float Volume, int Panpot);
            public abstract void GetPCM(int SampleRate, float[] buf, int SamplesCount, float TotalVolume);
        }

        public class CRenderPCM1 : CRenderBase {
            private class CItem {
                public int KeyCode;
                public CVoice Voice;
                public float SrcSampleRate;
                public int SamplePos = 0;
                public float Volume;
                public int Panpot;
                public CItem(int _KeyCode, CVoice _Voice, float _SrcSampleRate, float _Volume, int _Panpot) {
                    KeyCode = _KeyCode;
                    Voice = _Voice;
                    SrcSampleRate = _SrcSampleRate;
                    if (Voice == null) { SamplePos = -1; }
                    Volume = _Volume;
                    Panpot = _Panpot;
                }
            }
            private List<CItem> Items = new List<CItem>();

            public override void NoteOn(int ChannelNum, int KeyCode, float SrcSampleRate, float Volume, int Panpot) {
                // NoteOffが有効な音色は停止する
                if (1 <= Items.Count) {
                    var item = Items[Items.Count - 1];
                    if (item.Voice.EnableNoteOff) { item.SamplePos = -1; }
                }

                // 同じ音色を連続して発音したときは停止する
                foreach (var Item in Items) {
                    if (Item.KeyCode == KeyCode) { Item.SamplePos = -1; }
                }

                var Voice = Voices[KeyCode];
                if (Voice != null) {
                    Items.Add(new CItem(KeyCode, Voice, SrcSampleRate, Volume, Panpot));
                }
            }

            public override void GetPCM(int SampleRate, float[] buf, int SamplesCount, float TotalVolume) {
                var EOFItems = new List<CItem>();

                foreach (var Item in Items) {
                    if (Item.SamplePos != -1) {
                        float LeftRight, FrontBack;
                        switch (Item.Panpot) {
                            case 0: continue;
                            case 1: LeftRight = 0.25f; FrontBack = 0.2f; break;
                            case 2: LeftRight = 0.75f; FrontBack = 0.2f; break;
                            case 3: LeftRight = Item.Voice.LeftRight; FrontBack = Item.Voice.FrontBack; break;
                            default: continue;
                        }
                        for (var idx = 0; idx < SamplesCount; idx++) {
                            var pos = Item.SamplePos * (Item.SrcSampleRate / 15625) * Item.Voice.SrcSampleRate / SampleRate; // int32だとオーバーフローするのでfloatかdoubleで計算する
                            if (Item.Voice.Samples.Length <= pos) {
                                Item.SamplePos = -1;
                                break;
                            }
                            Item.SamplePos++;
                            var smp = Item.Voice.RateConverter.GetY(pos) * Item.Volume * InternalVolume * TotalVolume;
                            buf[idx * MusDriver.CCommon.OutputElements.Count + MusDriver.CCommon.OutputElements.PCM] += smp;
                            buf[idx * MusDriver.CCommon.OutputElements.Count + MusDriver.CCommon.OutputElements.FL] += smp * (1 - LeftRight) * (1 - FrontBack);
                            buf[idx * MusDriver.CCommon.OutputElements.Count + MusDriver.CCommon.OutputElements.FR] += smp * LeftRight * (1 - FrontBack);
                            buf[idx * MusDriver.CCommon.OutputElements.Count + MusDriver.CCommon.OutputElements.SL] += smp * (1 - LeftRight) * FrontBack;
                            buf[idx * MusDriver.CCommon.OutputElements.Count + MusDriver.CCommon.OutputElements.SR] += smp * LeftRight * FrontBack;
                        }
                    }
                    if (Item.SamplePos == -1) { EOFItems.Add(Item); }
                }

                foreach (var Item in EOFItems) {
                    Items.Remove(Item);
                }
            }
        }

        public class CRenderPCM8 : CRenderBase {
            private class CChannel {
                public int KeyCode;
                public CVoice Voice;
                public float SrcSampleRate;
                public int SamplePos = 0;
                public float Volume;
                public int Panpot;
                public CChannel(int _KeyCode, CVoice _Voice, float _SrcSampleRate, float _Volume, int _Panpot) {
                    KeyCode = _KeyCode;
                    Voice = _Voice;
                    SrcSampleRate = _SrcSampleRate;
                    if (Voice == null) { SamplePos = -1; }
                    Volume = _Volume;
                    Panpot = _Panpot;
                }
            }
            private CChannel[] Channels = new CChannel[8];

            public override void NoteOn(int ChannelNum, int KeyCode, float SrcSampleRate, float Volume, int Panpot) {
                var Voice = Voices[KeyCode];
                if (Voice != null) {
                    Channels[ChannelNum - 8] = new CChannel(KeyCode, Voice, SrcSampleRate, Volume, Panpot);
                }
            }

            public override void GetPCM(int SampleRate, float[] buf, int SamplesCount, float TotalVolume) {
                for (var ch = 0; ch < Channels.Length; ch++) {
                    var Item = Channels[ch];
                    if (Item == null) { continue; }
                    float LeftRight, FrontBack;
                    switch (Item.Panpot) {
                        case 0: continue;
                        case 1: LeftRight = 0.25f; FrontBack = 0.2f; break;
                        case 2: LeftRight = 0.75f; FrontBack = 0.2f; break;
                        case 3: LeftRight = Item.Voice.LeftRight; FrontBack = Item.Voice.FrontBack; break;
                        default: continue;
                    }
                    if (Item.SamplePos != -1) {
                        for (var idx = 0; idx < SamplesCount; idx++) {
                            var pos = Item.SamplePos * (Item.SrcSampleRate / 15625) * Item.Voice.SrcSampleRate / SampleRate; // int32だとオーバーフローするのでfloatかdoubleで計算する
                            if (Item.Voice.Samples.Length <= pos) {
                                Item.SamplePos = -1;
                                break;
                            }
                            Item.SamplePos++;
                            var smp = Item.Voice.RateConverter.GetY(pos) * Item.Volume * InternalVolume * TotalVolume;
                            buf[idx * MusDriver.CCommon.OutputElements.Count + MusDriver.CCommon.OutputElements.PCM + ch] += smp;
                            buf[idx * MusDriver.CCommon.OutputElements.Count + MusDriver.CCommon.OutputElements.FL] += smp * (1 - LeftRight) * (1 - FrontBack);
                            buf[idx * MusDriver.CCommon.OutputElements.Count + MusDriver.CCommon.OutputElements.FR] += smp * LeftRight * (1 - FrontBack);
                            buf[idx * MusDriver.CCommon.OutputElements.Count + MusDriver.CCommon.OutputElements.SL] += smp * (1 - LeftRight) * FrontBack;
                            buf[idx * MusDriver.CCommon.OutputElements.Count + MusDriver.CCommon.OutputElements.SR] += smp * LeftRight * FrontBack;
                        }
                    }
                }
            }
        }

        public class CPCM {
            private CRenderBase Render;

            private CPDXCommon.CPCMFormat PCMFormat;
            private float Volume;
            private int Panpot;

            public CPCM(CRenderBase _Render, int ChannelNum, int KeyCode, int SampleRateData, int VolumeData, int VolumeOffset, int Panpot) {
                Render = _Render;
                if (Render == null) { CCanIgnoreException.Throw(Lang.CLang.GetMXDRV(Lang.CLang.EMXDRV.PDXHQBos_InternalError_CPCMWithNoRenderPCM), ""); return; }
                if ((KeyCode < 0) || (0x60 <= KeyCode)) { CCanIgnoreException.Throw(Lang.CLang.GetMXDRV(Lang.CLang.EMXDRV.PDX_IlligalPCMNum), "KeyCode=0x" + KeyCode.ToString("x2") + "(" + MusDriver.CCommon.KeyCodeToString(KeyCode) + ")"); }
                PCMFormat = new CPDXCommon.CPCMFormat(SampleRateData);
                if (PCMFormat.Format != CPDXCommon.CPCMFormat.EFormat.ADPCM) { throw new Exception(Lang.CLang.GetMXDRV(Lang.CLang.EMXDRV.PDXHQBos_InternalError_BosPdxWithAnotherPCMFormat) + " SampleRateData=0x" + SampleRateData.ToString("x2")); }
                Volume = CPDXCommon.VolumeData2Volume(VolumeData, VolumeOffset);
                Render.NoteOn(ChannelNum, KeyCode, PCMFormat.SrcSampleRate, Volume, Panpot);
            }

            public void SetSampleRateData(int SampleRateData) { PCMFormat = new CPDXCommon.CPCMFormat(SampleRateData); } // 再生中の周波数変更には対応していない
            public void SetVolume(int VolumeData, int VolumeOffset) { Volume = CPDXCommon.VolumeData2Volume(VolumeData, VolumeOffset); } // 再生中のボリューム変更には対応していない
            public void SetPanpot(int _Panpot) { Panpot = _Panpot; } // 再生中のパンポット変更には対応していない
            public int GetVolume() { return ((int)(Volume * 128)); }
        }

    }
}
