using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Text;
using System.Xml.Linq;

namespace FMPPMD {
    public class CFMP : IDisposable {
        public const string DLLName = "WinFMP.dll";
        private CFMP_DLL DLL;

        private const int BufLenMax = CFMP_Work.FMP_COMMENTDATASIZE;

        private string MusicFilename = "";
        private CFMP_DLL.EErrorCode MusicLoadErrorCode = CFMP_DLL.EErrorCode.WINFMP_OK;
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

            GetStartAddress_ins_Apply(work.OpenWork.Parts_FM_0.CPatS.PartSpoint);
            GetStartAddress_ins_Apply(work.OpenWork.Parts_FM_1.CPatS.PartSpoint);
            GetStartAddress_ins_Apply(work.OpenWork.Parts_FM_2.CPatS.PartSpoint);
            GetStartAddress_ins_Apply(work.OpenWork.Parts_FM_3.CPatS.PartSpoint);
            GetStartAddress_ins_Apply(work.OpenWork.Parts_FM_4.CPatS.PartSpoint);
            GetStartAddress_ins_Apply(work.OpenWork.Parts_FM_5.CPatS.PartSpoint);
            GetStartAddress_ins_Apply(work.OpenWork.Parts_ADPCM.CPatS.PartSpoint);
            GetStartAddress_ins_Apply(work.OpenWork.Parts_FMx_0.CPatS.PartSpoint);
            GetStartAddress_ins_Apply(work.OpenWork.Parts_FMx_1.CPatS.PartSpoint);
            GetStartAddress_ins_Apply(work.OpenWork.Parts_FMx_2.CPatS.PartSpoint);
            GetStartAddress_ins_Apply(work.OpenWork.Parts_PPZ_0.CPatS.PartSpoint);
            GetStartAddress_ins_Apply(work.OpenWork.Parts_PPZ_1.CPatS.PartSpoint);
            GetStartAddress_ins_Apply(work.OpenWork.Parts_PPZ_2.CPatS.PartSpoint);
            GetStartAddress_ins_Apply(work.OpenWork.Parts_PPZ_3.CPatS.PartSpoint);
            GetStartAddress_ins_Apply(work.OpenWork.Parts_PPZ_4.CPatS.PartSpoint);
            GetStartAddress_ins_Apply(work.OpenWork.Parts_PPZ_5.CPatS.PartSpoint);
            GetStartAddress_ins_Apply(work.OpenWork.Parts_PPZ_6.CPatS.PartSpoint);
            GetStartAddress_ins_Apply(work.OpenWork.Parts_PPZ_7.CPatS.PartSpoint);
            GetStartAddress_ins_Apply(work.OpenWork.Parts_SSG_0.CPatS.PartSpoint);
            GetStartAddress_ins_Apply(work.OpenWork.Parts_SSG_1.CPatS.PartSpoint);
            GetStartAddress_ins_Apply(work.OpenWork.Parts_SSG_2.CPatS.PartSpoint);
            GetStartAddress_ins_Apply(work.OpenWork._R.CPatS.PartSpoint);

            MusicStop();
        }

        private MusDriver.CCommon.CLastKeyOn[] LastKeyOns = null;

        public CFMP(string _MusicFilename, int SampleRate, string _PCMFindPath, string RhythmPath = ".") {
            MusicFilename = _MusicFilename;
            PCMFindPath = _PCMFindPath;

            try {
                DLL = new CFMP_DLL(DLLName);

                if (!DLL.init("")) { Console.WriteLine("Init error."); this.Dispose(); return; }

                if (!DLL.loadrhythmsample(RhythmPath)) { Console.WriteLine("Rhythm sample load error."); }

                DLL.setpcmdir(new string[] { PCMFindPath, null });
                DLL.setpcmrate(SampleRate);

                if (!MusicFilename.Equals("")) {
                    Comments = GetComments();
                    using (var rfs = new System.IO.StreamReader(MusicFilename)) {
                        MusicBuf = new byte[rfs.BaseStream.Length];
                        rfs.BaseStream.Read(MusicBuf, 0, MusicBuf.Length);
                    }
                    MusicLoadErrorCode = DLL.load2(MusicBuf, MusicBuf.Length);
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

        public void MusicStart() { DLL.start(); }
        public void MusicStop() { DLL.stop(); }
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
                var maskch = MapChannels[ch].MaskCh;
                if (maskch != -1) {
                    if (MuteChs[ch]) {
                        DLL.maskon(false, maskch);
                    } else {
                        DLL.maskoff(false, maskch);
                    }
                }
            }
        }

        private string GetStringFromIntPtr(IntPtr p) {
            if (!isLoaded()) { throw new Exception("Not loaded DLL. " + DLLName); }

            var buf = new byte[BufLenMax];
            Marshal.Copy(p, buf, 0, BufLenMax);
            for (var idx = 0; idx < buf.Length; idx++) {
                if (buf[idx] == 0x00) {
                    return MoonLib.CTextEncode.SJIS.GetString(buf, 0, idx);
                }
            }
            return MoonLib.CTextEncode.SJIS.GetString(buf);
        }

        private class CComments {
            public string[] Comments = new string[3];
        }
        private CComments GetComments() { // 多分、２バイト半角文字を通常の半角文字に変換して、エスケープシーケンスの除去する。
            if (!isLoaded()) { throw new Exception("Not loaded DLL. " + DLLName); }

            var res = new CComments();

            var Comments = new CFMP_DLL.TComments();
            Comments.Comment1 = Marshal.AllocCoTaskMem(BufLenMax);
            Comments.Comment2 = Marshal.AllocCoTaskMem(BufLenMax);
            Comments.Comment3 = Marshal.AllocCoTaskMem(BufLenMax);

            if (DLL.fgetcomment3(ref Comments, MusicFilename) != CFMP_DLL.EErrorCode.WINFMP_OK) { throw new Exception(""); }

            res.Comments[0] = GetStringFromIntPtr(Comments.Comment1);
            res.Comments[1] = GetStringFromIntPtr(Comments.Comment2);
            res.Comments[2] = GetStringFromIntPtr(Comments.Comment3);

            Marshal.FreeCoTaskMem(Comments.Comment1);
            Marshal.FreeCoTaskMem(Comments.Comment2);
            Marshal.FreeCoTaskMem(Comments.Comment3);

            return res;
        }
        private CComments Comments = null;

        public class CGetPVIFilenames_res {
            public string DefinedPCMFilename, DefinedPPZFilename, PCMFilename, PPZFilename;
            public string[] FilenamesArr;
            public string FilenamesStr;
        }
        public CGetPVIFilenames_res GetPVIFilenames(bool PaddingSpace = false) {
            if (!isLoaded()) { throw new Exception("Not loaded DLL. " + DLLName); }

            var res = new CGetPVIFilenames_res();

            var buf = Marshal.AllocCoTaskMem(BufLenMax); // MAXは260らしいけど念のため

            DLL.fgetdefinedpcmfilename(buf, MusicFilename); res.DefinedPCMFilename = GetStringFromIntPtr(buf);
            DLL.fgetdefinedppzfilename(buf, MusicFilename, 0); res.DefinedPPZFilename = GetStringFromIntPtr(buf);
            DLL.getpcmfilename(buf); res.PCMFilename = System.IO.Path.GetFileName(GetStringFromIntPtr(buf));
            DLL.getppzfilename(buf, 0); res.PPZFilename = System.IO.Path.GetFileName(GetStringFromIntPtr(buf));

            Marshal.FreeCoTaskMem(buf);

            var fns = new HashSet<string>();
            if (!res.PCMFilename.Equals("")) { fns.Add(res.PCMFilename); }
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
            if (!isLoaded()) { throw new Exception("Not loaded DLL. " + DLLName); }

            var res = new CGetLength_res();

            int length, loop;
            DLL.getlength(MusicFilename, out length, out loop);
            res.FirstTS = System.TimeSpan.FromMilliseconds(length);
            res.SecondTS = System.TimeSpan.FromMilliseconds(loop);

            return res;
        }

        public class CMapChannel {
            public int SrcCh;
            public enum EModType { FM, SSG, ADPCM, Rhythm, FM3ex, PPZ8 };
            public EModType Type;
            public string Name;
            public int MaskCh;
            public CMapChannel(int _SrcCh, EModType _Type, int _MaskCh) {
                SrcCh = _SrcCh;
                Type = _Type;
                switch (Type) {
                    case CMapChannel.EModType.FM: Name = "FM"; break;
                    case CMapChannel.EModType.SSG: Name = "SSG"; break;
                    case CMapChannel.EModType.ADPCM: Name = "ADP"; break;
                    case CMapChannel.EModType.Rhythm: Name = "RHY"; break;
                    case CMapChannel.EModType.FM3ex: Name = "FMx"; break;
                    case CMapChannel.EModType.PPZ8: Name = "PPZ"; break;
                    default: Name = ""; break;
                }
                MaskCh = _MaskCh;
            }
        }

        private static CMapChannel[] MapChannels = new CMapChannel[MusDriver.CCommon.ChannelsCount] {
            new CMapChannel( 0, CMapChannel.EModType.FM, 0),
            new CMapChannel( 1, CMapChannel.EModType.FM, 1),
            new CMapChannel( 2, CMapChannel.EModType.FM, 2),
            new CMapChannel( 3, CMapChannel.EModType.FM, 3),
            new CMapChannel( 4, CMapChannel.EModType.FM, 4),
            new CMapChannel( 5, CMapChannel.EModType.FM, 5),
            new CMapChannel( 0, CMapChannel.EModType.SSG, 18),
            new CMapChannel( 1, CMapChannel.EModType.SSG, 19),
            new CMapChannel( 2, CMapChannel.EModType.SSG, 20),
            new CMapChannel(-1, CMapChannel.EModType.ADPCM, 21),
            new CMapChannel( 0, CMapChannel.EModType.FM3ex, 6),
            new CMapChannel( 1, CMapChannel.EModType.FM3ex, 7),
            new CMapChannel( 2, CMapChannel.EModType.FM3ex, 8),
            new CMapChannel( 0, CMapChannel.EModType.PPZ8, -1), // PPZ8の実チャネル番号がわからない…。
            new CMapChannel( 1, CMapChannel.EModType.PPZ8, -1),
            new CMapChannel( 2, CMapChannel.EModType.PPZ8, -1),
        };

        public static string GetChannelName(int ch) {
            if (ch < MapChannels.Length) { return MapChannels[ch].Name; }
            return "";
        }

        private class CGetWork_res {
            public CFMP_Work.TWORKS2 OpenWork;
            public class CCh {
                public CMapChannel.EModType Type;
                public CFMP_Work.TCPATS General;
                public CFMP_Work.TFPATS FM;
                public CFMP_Work.TSPATS SSG;
                public CFMP_Work.TAPATS ADPCM;
                public CCh(CFMP_Work.TPARTS_FM Part) {
                    Type = CMapChannel.EModType.FM;
                    General = Part.CPatS;
                    FM = Part.FPatS;
                }
                public CCh(CFMP_Work.TPARTS_SSG Part) {
                    Type = CMapChannel.EModType.SSG;
                    General = Part.CPatS;
                    SSG = Part.SPatS;
                }
                public CCh(CFMP_Work.TPARTS_ADPCM Part) {
                    Type = CMapChannel.EModType.ADPCM;
                    General = Part.CPatS;
                    ADPCM = Part.APatS;
                }
            }
            public CCh[] Chs;
            public byte[,] PartsData;
        }
        private CGetWork_res GetWork() {
            if (!isLoaded()) { throw new Exception("Not loaded DLL. " + DLLName); }

            var res = new CGetWork_res();
            res.OpenWork = Marshal.PtrToStructure<CFMP_Work.TWORKS2>(DLL.getworks());

            res.Chs = new CGetWork_res.CCh[MusDriver.CCommon.ChannelsCount];
            res.Chs[0] = new CGetWork_res.CCh(res.OpenWork.Parts_FM_0);
            res.Chs[1] = new CGetWork_res.CCh(res.OpenWork.Parts_FM_1);
            res.Chs[2] = new CGetWork_res.CCh(res.OpenWork.Parts_FM_2);
            res.Chs[3] = new CGetWork_res.CCh(res.OpenWork.Parts_FM_3);
            res.Chs[4] = new CGetWork_res.CCh(res.OpenWork.Parts_FM_4);
            res.Chs[5] = new CGetWork_res.CCh(res.OpenWork.Parts_FM_5);
            res.Chs[6] = new CGetWork_res.CCh(res.OpenWork.Parts_SSG_0);
            res.Chs[7] = new CGetWork_res.CCh(res.OpenWork.Parts_SSG_1);
            res.Chs[8] = new CGetWork_res.CCh(res.OpenWork.Parts_SSG_2);
            res.Chs[9] = new CGetWork_res.CCh(res.OpenWork.Parts_ADPCM);
            res.Chs[10] = new CGetWork_res.CCh(res.OpenWork.Parts_FMx_0);
            res.Chs[11] = new CGetWork_res.CCh(res.OpenWork.Parts_FMx_1);
            res.Chs[12] = new CGetWork_res.CCh(res.OpenWork.Parts_FMx_2);
            res.Chs[13] = new CGetWork_res.CCh(res.OpenWork.Parts_PPZ_0);
            res.Chs[14] = new CGetWork_res.CCh(res.OpenWork.Parts_PPZ_1);
            res.Chs[15] = new CGetWork_res.CCh(res.OpenWork.Parts_PPZ_2);

            res.PartsData = new byte[MusDriver.CCommon.ChannelsCount, 4];
            for (var ch = 0; ch < MusDriver.CCommon.ChannelsCount; ch++) {
                if (res.Chs[ch].General.PartSpoint != 0) {
                    var buf = Marshal.ReadInt32(new IntPtr(res.Chs[ch].General.PartSpoint));
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
            var res = new MusDriver.CCommon.CVisualGlobal();

            res.FileFormat = MusDriver.CDriver.EFileFormat.FMP;
            res.CurrentTS = GetPos();

            CurWork = GetWork();

            res.ExtInfo1_Tag1 = "Measure"; res.ExtInfo1_Tag2 = "Count";
            res.ExtInfo1_Value = 1 + CurWork.OpenWork.ExtBuff.FmpSsho;

            res.ExtInfo2_Tag1 = "Timer-B"; res.ExtInfo2_Tag2 = "Speed";
            res.ExtInfo2_Value = CurWork.OpenWork.ExtBuff.FmpStempo;

            res.TotalClock = CurWork.OpenWork.MusicClockCnt * CurWork.OpenWork.ExtBuff.FmpSsho;
            res.TotalClock += CurWork.OpenWork.MusicClockCnt - CurWork.OpenWork.ClockCnt;

            res.LoopCount = DLL.getloopcount();
            if (res.LoopCount < 0) { res.LoopCount = 0; }

            return res;
        }

        public MusDriver.CCommon.CVisualPart GetVisualStatus(int ch) {
            var res = new MusDriver.CCommon.CVisualPart();

            var info = CurWork.Chs[ch];

            if ((StartAddress == 0) || (info.General.PartSpoint < StartAddress)) { return null; }

            if ((info.General.PartSstatus & (int)CFMP_Work.EState_flg.This_channel_off) != 0) { return null; }

            res.Program = info.General.PartStone;
            res.KeyOnFlag = (info.General.PartSkeyon != 0) && (info.General.PartSbefore != 0x61) && (0 <= info.General.PartSbefore);

            string TextExt1;

            int VolumeOut;
            double VolumeVisual;
            switch (info.Type) {
                case CMapChannel.EModType.FM:
                    VolumeOut = 127 - info.General.PartSvol;
                    VolumeVisual = VolumeOut / 127d;
                    TextExt1 = "Alg:$" + info.General.PartSalg.ToString("X2");
                    break;
                case CMapChannel.EModType.SSG:
                    VolumeOut = info.General.PartSvol - 1;
                    VolumeVisual = VolumeOut / 15d;
                    TextExt1 = "Env:";
                    if ((1 <= info.General.PartStmpvol)) {
                        TextExt1 += (info.General.PartStmpvol / 0x10).ToString().PadLeft(3);
                    }
                    break;
                case CMapChannel.EModType.ADPCM:
                    VolumeOut = info.General.PartSvol;
                    VolumeVisual = VolumeOut / 127d; // 最大値255のときもある？
                    TextExt1 = "";
                    break;
                default: throw new Exception();
            }

            if (LastKeyOns == null) {
                LastKeyOns = new MusDriver.CCommon.CLastKeyOn[MusDriver.CCommon.ChannelsCount];
                for (var idx = 0; idx < LastKeyOns.Length; idx++) {
                    LastKeyOns[idx] = new();
                }
            }

            string keystr;
            if (res.KeyOnFlag) {
                res.KeyCode = info.General.PartSbefore;
                res.LastNoteNum = res.KeyCode;
                res.LastNoteNumFine = -1;
                LastKeyOns[ch].KeyOn((res.Program << 8) | res.KeyCode, VolumeVisual);
                keystr = res.KeyCode.ToString().PadLeft(2) + " " + MusDriver.CCommon.KeyCodeToString(res.KeyCode, 12).PadRight(4);
            } else {
                res.KeyCode = -1;
                res.LastNoteNum = -1;
                res.LastNoteNumFine = 0;
                LastKeyOns[ch].KeyOff();
                keystr = new string(' ', 2 + 1 + 4);
            }

            res.FMPPMD_LastNoteNumVolume = LastKeyOns[ch].GetVolume();
            LastKeyOns[ch].Update();

            int Panpot;
            switch (info.Type) {
                case CMapChannel.EModType.FM:
                    switch (info.General.PartSpan & 0xc0) {
                        case 0x80: Panpot = 1; break;
                        case 0x40: Panpot = 2; break;
                        case 0xc0: Panpot = 3; break;
                        default: Panpot = 0; break;
                    }
                    break;
                case CMapChannel.EModType.SSG: Panpot = 3; break;
                case CMapChannel.EModType.ADPCM:
                    switch (info.General.PartSpan & 0xc0) {
                        case 0x80: Panpot = 1; break;
                        case 0x40: Panpot = 2; break;
                        case 0xc0: Panpot = 3; break;
                        default: Panpot = 0; break;
                    }
                    break;
                default: throw new Exception();
            }
            res.Panpot = Panpot;

            res.Text1 = "V:" + VolumeOut.ToString().PadLeft(3) + " " + TextExt1.PadRight(7) + " K:" + keystr + " D:" + MusDriver.CCommon.IntToStrPlusMinusPadLeft(info.General.PartSdetune, 4) + " P:" + MusDriver.CCommon.IntToStrPlusMinusPadLeft(info.General.PartSpitch.PitSdat, 5) + " q" + info.General.PartSorg_q.ToString("X");

            res.Text2 = "LFO:$" + info.General.PartSlfo_f.ToString("X2") + " S:$" + info.General.PartSstatus.ToString("X2");

            res.Text2 += new string(' ', 10);

            res.Text2 += " DAT:" + (info.General.PartSpoint - StartAddress).ToString("X4") + ":" + CurWork.PartsData[ch, 0].ToString("X2") + CurWork.PartsData[ch, 1].ToString("X2") + CurWork.PartsData[ch, 2].ToString("X2") + CurWork.PartsData[ch, 3].ToString("X2");

            res.Text3 = "";

            return res;
        }

        public List<string> GetInfo() {
            var res = new List<string>();
            if (Comments == null) { return res; }

            res.Add(Comments.Comments[0]);
            res.Add(Comments.Comments[1]);
            res.Add(Comments.Comments[2]);

            return res;
        }
    }
}