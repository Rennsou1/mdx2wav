using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml.Linq;

namespace FMPPMD {
    public class CPMD : IDisposable {
        public const string DLLName = "PMDWin.dll";
        private CPMD_DLL DLL;

        private string MusicFilename = "";
        private CPMD_DLL.EErrorCode MusicLoadErrorCode = CPMD_DLL.EErrorCode.PMDWIN_OK;
        private byte[] MusicBuf = null;

        private string PCMFindPath;

        private long StartAddress = 0;
        private void GetStartAddress_ins_Apply(long ptr) {
            if (ptr == 0) { return; }
            if (StartAddress == 0) { StartAddress = ptr; }
            if (ptr < StartAddress) { StartAddress = ptr; }
        }
        private void GetStartAddress() {
            StartAddress = 0;

            MusicStart();
            var work = GetWork();

            var Part = new CPMD_Work.TQQ();
            for (var ch = 0; ch < CPMD_Work.NumOfAllPart; ch++) {
                unsafe {
                    Part = Marshal.PtrToStructure<CPMD_Work.TQQ>(new IntPtr(work.OpenWork.MusPart[ch]));
                }
                GetStartAddress_ins_Apply(Part.address);
            }

            MusicStop();
        }

        private MusDriver.CCommon.CLastKeyOn[] LastKeyOns = null;

        public CPMD(string _MusicFilename, int SampleRate, string _PCMFindPath, string RhythmPath = ".") {
            MusicFilename = _MusicFilename;
            PCMFindPath = _PCMFindPath;

            try {
                DLL = new CPMD_DLL(DLLName);

                if (!DLL.pmdwininit("")) { Console.WriteLine("Init error."); this.Dispose(); return; }
                if (!DLL.loadrhythmsample(RhythmPath)) { Console.WriteLine("Rhythm sample load error."); }

                DLL.setpcmdir(new string[] { PCMFindPath, null });
                DLL.setpcmrate(SampleRate);

                if (!MusicFilename.Equals("")) {
                    Comments = GetComments();
                    using (var rfs = new System.IO.StreamReader(MusicFilename)) {
                        MusicBuf = new byte[rfs.BaseStream.Length];
                        rfs.BaseStream.Read(MusicBuf, 0, MusicBuf.Length);
                    }
                    MusicLoadErrorCode = DLL.music_load2(MusicBuf, MusicBuf.Length);
                    GetStartAddress();
                }
            } catch (Exception ex) {
                this.Dispose();
                Console.WriteLine("Can not load DLL. " + ex.ToString());
            }
        }

        public void Dispose() {
            if (DLL != null) {
                DLL.Dispose();
                DLL = null;
            }
        }

        public bool isLoaded() { return DLL.isLoaded(); }

        public string GetVersionStr() {
            if (!DLL.isLoaded()) { throw new Exception("Not loaded DLL. " + DLLName); }

            return DLLName + " Ver." + (DLL.getversion() / 100) + "." + (DLL.getversion() % 100).ToString().PadLeft(2, '0') + " Interface." + (DLL.getinterfaceversion() / 100) + "." + (DLL.getinterfaceversion() % 100).ToString().PadLeft(2, '0');
        }

        public void MusicStart() { DLL.music_start(); }
        public void MusicStop() { DLL.music_stop(); }
        public TimeSpan GetPos() { return TimeSpan.FromMilliseconds(DLL.getpos()); }
        public void Seek(TimeSpan SkipTS) {
            DLL.setpos((int)SkipTS.TotalMilliseconds);
            LastKeyOns = null;
        }
        public void GetPCM(short[] buf, int SamplesCount) { DLL.getpcmdata(buf, SamplesCount); }
        public int GetLoopCount() { return DLL.getloopcount(); }
        public void StartFadeout(TimeSpan Duration) { DLL.fadeout2((int)Duration.TotalMilliseconds); }

        public void ApplyMuteChs(bool[] MuteChs) {
            for (var ch = 0; ch < MapChannels.Length; ch++) {
                var maskch = MapChannels[ch].SrcCh;
                if (maskch != -1) {
                    if (MuteChs[ch]) {
                        DLL.maskon(maskch);
                    } else {
                        DLL.maskoff(maskch);
                    }
                }
            }
        }

        private string GetStringFromIntPtr(IntPtr p) {
            if (!DLL.isLoaded()) { throw new Exception("Not loaded DLL. " + DLLName); }

            var buf = new byte[CPMD_DLL.BufLenMax];
            Marshal.Copy(p, buf, 0, CPMD_DLL.BufLenMax);
            for (var idx = 0; idx < buf.Length; idx++) {
                if (buf[idx] == 0x00) {
                    return MoonLib.CTextEncode.SJIS.GetString(buf, 0, idx);
                }
            }
            return MoonLib.CTextEncode.SJIS.GetString(buf);
        }

        private class CComments {
            public string Title, Composer, Arranger;
            public List<string> Memos;
        }
        private CComments GetComments() { // ２バイト半角文字を通常の半角文字に変換して、エスケープシーケンスの除去する。
            if (!DLL.isLoaded()) { throw new Exception("Not loaded DLL. " + DLLName); }

            var res = new CComments();

            var buf = Marshal.AllocCoTaskMem(CPMD_DLL.BufLenMax);

            if (DLL.fgetmemo3(buf, MusicFilename, 1) != CPMD_DLL.EErrorCode.PMDWIN_OK) { throw new Exception(""); }
            res.Title = GetStringFromIntPtr(buf);
            if (DLL.fgetmemo3(buf, MusicFilename, 2) != CPMD_DLL.EErrorCode.PMDWIN_OK) { throw new Exception(""); }
            res.Composer = GetStringFromIntPtr(buf);
            if (DLL.fgetmemo3(buf, MusicFilename, 3) != CPMD_DLL.EErrorCode.PMDWIN_OK) { throw new Exception(""); }
            res.Arranger = GetStringFromIntPtr(buf);

            res.Memos = new();
            for (var idx = 4; idx < 0x100; idx++) {
                if (DLL.fgetmemo3(buf, MusicFilename, idx) == CPMD_DLL.EErrorCode.PMDWIN_OK) {
                    var Memo = GetStringFromIntPtr(buf);
                    if (!Memo.Trim().Equals("")) { res.Memos.Add(Memo); }
                }
            }

            Marshal.FreeCoTaskMem(buf);

            return res;
        }
        private CComments Comments = null;

        public class CGetPVIFilenames_res {
            public string PCMFilename, PPCFilename, PPSFilename, P86Filename, PPZFilename;
            public string[] FilenamesArr;
            public string FilenamesStr;
        }
        public CGetPVIFilenames_res GetPVIFilenames(bool PaddingSpace = false) {
            if (!DLL.isLoaded()) { throw new Exception("Not loaded DLL. " + DLLName); }

            var res = new CGetPVIFilenames_res();

            var buf = Marshal.AllocCoTaskMem(CPMD_DLL.BufLenMax);

            DLL.getpcmfilename(buf); res.PCMFilename = GetStringFromIntPtr(buf);
            DLL.getppcfilename(buf); res.PPCFilename = GetStringFromIntPtr(buf);
            DLL.getppsfilename(buf); res.PPSFilename = GetStringFromIntPtr(buf);
            DLL.getp86filename(buf); res.P86Filename = GetStringFromIntPtr(buf);
            DLL.getppzfilename(buf, 0); res.PPZFilename = GetStringFromIntPtr(buf);

            Marshal.FreeCoTaskMem(buf);

            var fns = new HashSet<string>();
            if (!res.PCMFilename.Equals("")) { fns.Add(res.PCMFilename); }
            if (!res.PPCFilename.Equals("")) { fns.Add(res.PPCFilename); }
            if (!res.PPSFilename.Equals("")) { fns.Add(res.PPSFilename); }
            if (!res.P86Filename.Equals("")) { fns.Add(res.P86Filename); }
            if (!res.PPZFilename.Equals("")) { fns.Add(res.PPZFilename); }
            res.FilenamesArr = fns.ToArray();

            for (var idx = 0; idx < res.FilenamesArr.Length; idx++) {
                var fn = res.FilenamesArr[idx];
                if (System.IO.Path.GetDirectoryName(fn).Equals("")) { res.FilenamesArr[idx] = PCMFindPath + @"\" + fn; }
            }

            res.FilenamesStr = "";
            foreach (var fn in res.FilenamesArr) {
                if (!fn.Equals("")) {
                    if (!res.FilenamesStr.Equals("")) { res.FilenamesStr += "," + (PaddingSpace ? " " : ""); }
                    res.FilenamesStr += System.IO.Path.GetFileName(fn);
                }
            }

            return res;
        }

        public class CGetLength_res {
            public System.TimeSpan FirstTS, SecondTS;
        }
        public CGetLength_res GetLength() {
            if (!DLL.isLoaded()) { throw new Exception("Not loaded DLL. " + DLLName); }

            var res = new CGetLength_res();

            int length, loop;
            DLL.getlength(MusicFilename, out length, out loop);
            res.FirstTS = System.TimeSpan.FromMilliseconds(length);
            res.SecondTS = System.TimeSpan.FromMilliseconds(loop);

            return res;
        }

        public enum EChType { FM, SSG, ADPCM, Rhythm, FM3ex, PPZ8 };

        public class CMapChannel {
            public int SrcCh;
            public EChType Type;
            public string Name;
            public CMapChannel(int _SrcCh, EChType _Type) {
                SrcCh = _SrcCh;
                Type = _Type;
                switch (Type) {
                    case EChType.FM: Name = "FM"; break;
                    case EChType.SSG: Name = "SSG"; break;
                    case EChType.ADPCM: Name = "ADP"; break;
                    case EChType.Rhythm: Name = "RHY"; break;
                    case EChType.FM3ex: Name = "FMx"; break;
                    case EChType.PPZ8: Name = "PPZ"; break;
                    default: Name = ""; break;
                }
            }
        }

        private static CMapChannel[] MapChannels = new CMapChannel[MusDriver.CCommon.ChannelsCount] {
            new CMapChannel( 0, EChType.FM), // &FMPart[0];
            new CMapChannel( 1, EChType.FM), // &FMPart[1];
            new CMapChannel( 2, EChType.FM), // &FMPart[2];
            new CMapChannel( 3, EChType.FM), // &FMPart[3];
            new CMapChannel( 4, EChType.FM), // &FMPart[4];
            new CMapChannel( 5, EChType.FM), // &FMPart[5];
            new CMapChannel( 6, EChType.SSG), // &SSGPart[0];
            new CMapChannel( 7, EChType.SSG), // &SSGPart[1];
            new CMapChannel( 8, EChType.SSG), // &SSGPart[2];
            new CMapChannel( 9, EChType.ADPCM), // &ADPCMPart;
            new CMapChannel(10, EChType.Rhythm), // &RhythmPart;
            new CMapChannel(11, EChType.FM3ex), // &ExtPart[0];
            new CMapChannel(12, EChType.FM3ex), // &ExtPart[1];
            new CMapChannel(13, EChType.FM3ex), // &ExtPart[2];
            new CMapChannel(16, EChType.PPZ8), // &PPZ8Part[0];
            new CMapChannel(17, EChType.PPZ8), // &PPZ8Part[1];
        };

        public static string GetChannelName(int ch) {
            if (ch < MapChannels.Length) { return MapChannels[ch].Name; }
            return "";
        }

        private class CGetWork_res {
            public CPMD_Work.TOPEN_WORK2 OpenWork;
            public CPMD_Work.TQQ[] Parts;
            public byte[,] PartsData;
        }
        private CGetWork_res GetWork() {
            if (!DLL.isLoaded()) { throw new Exception("Not loaded DLL. " + DLLName); }

            var res = new CGetWork_res();

            res.OpenWork = Marshal.PtrToStructure<CPMD_Work.TOPEN_WORK2>(DLL.getopenwork());

            res.Parts = new CPMD_Work.TQQ[MusDriver.CCommon.ChannelsCount];
            res.PartsData = new byte[MusDriver.CCommon.ChannelsCount, 4];
            for (var ch = 0; ch < res.Parts.Length; ch++) {
                unsafe {
                    res.Parts[ch] = Marshal.PtrToStructure<CPMD_Work.TQQ>(new IntPtr(res.OpenWork.MusPart[MapChannels[ch].SrcCh]));
                }
                if (res.Parts[ch].address != 0) {
                    var buf = Marshal.ReadInt32(new IntPtr(res.Parts[ch].address));
                    res.PartsData[ch, 0] = (byte)((buf >> 8 * 0) & 0xff);
                    res.PartsData[ch, 1] = (byte)((buf >> 8 * 1) & 0xff);
                    res.PartsData[ch, 2] = (byte)((buf >> 8 * 2) & 0xff);
                    res.PartsData[ch, 3] = (byte)((buf >> 8 * 3) & 0xff);
                }
            }

            return res;
        }

        private CGetWork_res CurWork = null;

        public MusDriver.CCommon.CVisualGlobal GetVisualGlobal() {
            if (!DLL.isLoaded()) { throw new Exception("Not loaded DLL. " + DLLName); }

            var res = new MusDriver.CCommon.CVisualGlobal();

            res.FileFormat = MusDriver.CDriver.EFileFormat.PMD;
            res.CurrentTS = GetPos();

            CurWork = GetWork();

            res.ExtInfo1_Tag1 = "Measure"; res.ExtInfo1_Tag2 = "Count";
            res.ExtInfo1_Value = 1 + CurWork.OpenWork.syousetu;

            res.ExtInfo2_Tag1 = "Timer-B"; res.ExtInfo2_Tag2 = "Speed";
            res.ExtInfo2_Value = CurWork.OpenWork.TimerB_speed;

            res.TotalClock = CurWork.OpenWork.TimerAtime;

            res.LoopCount = DLL.getloopcount();
            if (res.LoopCount < 0) { res.LoopCount = 0; }

            return res;
        }

        public MusDriver.CCommon.CVisualPart GetVisualStatus(int ch) {
            if (!DLL.isLoaded()) { throw new Exception("Not loaded DLL. " + DLLName); }

            var CurPart = CurWork.Parts[ch];
            var ChType = MapChannels[ch].Type;

            if ((StartAddress == 0) || (CurPart.address < StartAddress)) { return null; }

            var res = new MusDriver.CCommon.CVisualPart();

            res.Program = CurPart.voicenum;
            res.KeyOnFlag = (CurPart.onkai & 0x0f) < 12;

            var VolumeMul = 0x01;
            if ((ChType == EChType.Rhythm) || (ChType == EChType.SSG)) { VolumeMul = 0x08; }

            if (LastKeyOns == null) {
                LastKeyOns = new MusDriver.CCommon.CLastKeyOn[MusDriver.CCommon.ChannelsCount];
                for (var idx = 0; idx < LastKeyOns.Length; idx++) {
                    LastKeyOns[idx] = new();
                }
            }

            if (res.KeyOnFlag) {
                res.KeyCode = ((CurPart.onkai >> 4) * 12) + (CurPart.onkai & 0x0f);
                res.LastNoteNum = res.KeyCode;
                res.LastNoteNumFine = res.LastNoteNum + (CurPart.detune + CurPart.porta_num + CurPart.lfodat) / 64d;
                LastKeyOns[ch].KeyOn(CurPart.keyon_flag, (CurPart.volume * VolumeMul) / 128d);
            } else {
                res.KeyCode = -1;
                res.LastNoteNum = -1;
                res.LastNoteNumFine = 0;
                LastKeyOns[ch].KeyOff();
            }

            res.FMPPMD_LastNoteNumVolume = LastKeyOns[ch].GetVolume();
            LastKeyOns[ch].Update();

            int Panpot;
            switch (ChType) {
                case EChType.FM:
                    switch (CurPart.fmpan & 0xc0) {
                        case 0x80: Panpot = 1; break;
                        case 0x40: Panpot = 2; break;
                        case 0xc0: Panpot = 3; break;
                        default: Panpot = 0; break;
                    }
                    break;
                case EChType.SSG: Panpot = 3; break;
                case EChType.ADPCM:
                    switch (CurPart.fmpan & 0xc0) {
                        case 0x80: Panpot = 1; break;
                        case 0x40: Panpot = 2; break;
                        case 0xc0: Panpot = 3; break;
                        default: Panpot = 0; break;
                    }
                    break;
                case EChType.Rhythm: Panpot = 3; break;
                case EChType.FM3ex: Panpot = 3; break;
                case EChType.PPZ8:
                    switch (CurPart.fmpan & 0xc0) {
                        case 1: case 2: case 3: case 4: Panpot = 1; break;
                        case 5: Panpot = 2; break;
                        case 6: case 7: case 8: case 9: Panpot = 3; break;
                        default: Panpot = 0; break;
                    }
                    break;
                default: Panpot = 3; break;
            }
            res.Panpot = Panpot;

            var keystr = new string(' ', 2 + 1 + 4);
            if ((res.KeyCode != -1) && (res.KeyCode < 100)) { keystr = res.KeyCode.ToString().PadLeft(2) + " " + MusDriver.CCommon.KeyCodeToString(res.KeyCode, 12).PadRight(4); }

            string portastr;
            if (CurPart.porta_num < -999) {
                portastr = "-inf";
            } else if (999 < CurPart.porta_num) {
                portastr = "+inf";
            } else {
                portastr = MusDriver.CCommon.IntToStrPlusMinusPadLeft(CurPart.porta_num, 4);
            }

            string lfostr;
            if (CurPart.lfodat < -999) {
                lfostr = "-inf";
            } else if (999 < CurPart.lfodat) {
                lfostr = "+inf";
            } else {
                lfostr = MusDriver.CCommon.IntToStrPlusMinusPadLeft(CurPart.lfodat, 4);
            }

            res.Text1 = "V" + CurPart.volume.ToString().PadLeft(3) + MusDriver.CCommon.IntToStrPlusMinusPadLeft(CurPart.eenv_volume, 4) + " K:" + keystr + " D" + MusDriver.CCommon.IntToStrPlusMinusPadLeft(CurPart.detune, 4) + " P" + portastr + " LFO" + lfostr + "/" + CurPart.delay.ToString().PadLeft(2, ' ');

            var ext = "";
            switch (ChType) {
                case EChType.FM: ext = "Alg:$" + CurPart.alg_fb.ToString("X2"); break;
                case EChType.SSG: ext = "Pat:$" + CurPart.psgpat.ToString("X2"); break;
                case EChType.ADPCM: break;
                case EChType.Rhythm: break;
                case EChType.FM3ex: break;
                case EChType.PPZ8: break;
                default: break;
            }
            if (ext.Equals("")) { ext = new string(' ', 7); }

            res.Text2 = "@" + CurPart.voicenum.ToString().PadLeft(3) + " q" + CurPart.qdat.ToString("X2") + " " + ext;

            res.Text2 += new string(' ', 2);

            res.Text2 += " DAT:" + (CurPart.address - StartAddress).ToString("X4") + ":" + CurWork.PartsData[ch, 0].ToString("X2") + CurWork.PartsData[ch, 1].ToString("X2") + CurWork.PartsData[ch, 2].ToString("X2") + CurWork.PartsData[ch, 3].ToString("X2");

            res.Text3 = "";

            return res;
        }

        public List<string> GetInfo() {
            var res = new List<string>();
            if (Comments == null) { return res; }

            res.Add("Title    :" + Comments.Title);
            res.Add("Composer :" + Comments.Composer);
            res.Add("Arranger :" + Comments.Arranger);

            foreach(var Memo in Comments.Memos) {
                res.Add("Memo: " + Memo);
            }

            return res;
        }
    }
}
