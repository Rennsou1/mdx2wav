using FMPPMD;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MusDriver {
    public class CDriver {
        public enum EFileFormat { Unknown, MXDRV, FMP, PMD };
        public static EFileFormat GetFileFormat(string fn) {
            switch (System.IO.Path.GetExtension(fn).ToLower()) {
                case ".mdx":
                    return EFileFormat.MXDRV;
                case ".opi":
                case ".ovi":
                case ".ozi":
                    return EFileFormat.FMP;
                case ".m":
                case ".m2":
                case ".mp":
                case ".ms":
                case ".mz":
                    return EFileFormat.PMD;
            }
            return EFileFormat.Unknown;
        }
        public EFileFormat FileFormat = EFileFormat.Unknown;

        public static string GetDriverName(EFileFormat FileFormat) {
            switch (FileFormat) {
                case EFileFormat.MXDRV: return "MXDRVm";
                case EFileFormat.FMP: return System.IO.Path.GetFileNameWithoutExtension(FMPPMD.CFMP.DLLName);
                case EFileFormat.PMD: return System.IO.Path.GetFileNameWithoutExtension(FMPPMD.CPMD.DLLName);
                case EFileFormat.Unknown:
                default:
                    return "Unknown";
            }
        }

        public MDXOnline004.CZip.CSettings Settings;
        public string BasePath;

        private MXDRV.CMDXFile MDXFile = null;
        private FMPPMD.CFMP FMP = null;
        private FMPPMD.CPMD PMD = null;

        private bool FMPPMD_Fadeout = false;
        private TimeSpan FMPPMD_FadeoutLate = System.TimeSpan.FromTicks(0);

        public CDriver(MDXOnline004.CZip.CSettings _Settings, string _BasePath,int SampleRate) {
            CCommon.SetMuteChsAll(false);

            Settings = _Settings;
            BasePath = _BasePath;

            MXDRV.CCommon.Log.sw = null;

            FileFormat = GetFileFormat(Settings.Path);
            var fn = BasePath + @"\" + System.IO.Path.GetFileName(Settings.Path);

            switch (FileFormat) {
                case EFileFormat.MXDRV:
                    MDXFile = new MXDRV.CMDXFile(fn, BasePath, System.TimeSpan.FromSeconds(0));
                    break;
                case EFileFormat.FMP:
                    FMP = new FMPPMD.CFMP(fn, SampleRate, BasePath);
                    FMP.MusicStart();
                    break;
                case EFileFormat.PMD:
                    PMD = new FMPPMD.CPMD(fn, SampleRate, BasePath);
                    PMD.MusicStart();
                    break;
                case EFileFormat.Unknown:
                default:
                    throw new Exception("Not found music file.");
            }
        }

        public List<string> GetInfo() {
            var res = new List<string>();

            res.Add("Title: " + Settings.Title);
            res.Add("Filename: " + Settings.Path);
            if (!Settings.PCM0Filename.Equals("")) { res.Add("PCM0Filename: " + Settings.PCM0Filename); }
            if (!Settings.PCM1Filename.Equals("")) { res.Add("PCM1Filename: " + Settings.PCM1Filename); }
            if (!Settings.PCM2Filename.Equals("")) { res.Add("PCM2Filename: " + Settings.PCM2Filename); }
            if (!Settings.PCM3Filename.Equals("")) { res.Add("PCM3Filename: " + Settings.PCM3Filename); }

            List<string> Infos = null;

            if (MDXFile != null) { Infos = MDXFile.GetInfo(); }
            if (FMP != null) { Infos = FMP.GetInfo(); }
            if (PMD != null) { Infos = PMD.GetInfo(); }

            if (Infos != null) {
                foreach (var Line in Infos) {
                    res.Add(Line);
                }
            }

            for (var idx = 0; idx < res.Count; idx++) {
                res[idx] = MoonLib.CTextEncode.COptimizer.Opt改行除去(res[idx]);
            }

            return (res);
        }

        public void Seek(System.TimeSpan SkipTS) {
            FMPPMD_Fadeout = false;
            FMPPMD_FadeoutLate = System.TimeSpan.FromTicks(0);

            var mutechs = CCommon.GetMuteChs();
            CCommon.SetMuteChsAll(true);
            if (MDXFile != null) {
                var fn = BasePath + @"\" + System.IO.Path.GetFileName(Settings.Path);
                MDXFile = new MXDRV.CMDXFile(fn, BasePath, SkipTS); 
            }
            if (FMP != null) { FMP.Seek(SkipTS); }
            if (PMD != null) { PMD.Seek(SkipTS); }
            CCommon.SetMuteChs(mutechs);
        }

        public void Free() {
            if (MDXFile != null) { MDXFile = null; }
            if (FMP != null) { FMP.MusicStop(); FMP.Dispose(); FMP = null; }
            if (PMD != null) { PMD.MusicStop(); PMD.Dispose(); PMD = null; }

            if (MXDRV.CCommon.Log.sw != null) { MXDRV.CCommon.Log.sw.Dispose(); MXDRV.CCommon.Log.sw = null; }
        }

        public TimeSpan GetSamplesTS() {
            if (MDXFile != null) { return MDXFile.GetClocksTime(1); }
            if ((FMP != null) || (PMD != null)) { return TimeSpan.FromSeconds(1 / 120d); }
            throw new Exception();
        }

        public bool ExecNextClock_演奏が終了していたらTrueを返す() {
            if (MDXFile != null) { return MDXFile.NextClock(); }
            if ((FMP != null) || (PMD != null)) {
                if (isEOF()) { return true; }
                if (!FMPPMD_Fadeout) {
                    var MaxLoopCount = MDXWin.CCommon.AudioThread.Settings.LoopCount;
                    if (MaxLoopCount != 0) {
                        if (MaxLoopCount <= GetLoopCount()) { SetFadeout(0x00); }
                    }
                }
                return false;
            }
            throw new Exception();
        }

        public CCommon.EPCMSurroundMode GetPCM(int SampleRate, float[] buf, int SamplesCount) {
            if (MDXFile != null) { return MDXFile.GetPCM(SampleRate, buf, SamplesCount); }
            if ((FMP != null) || (PMD != null)) {
                var tmpbuf = new short[SamplesCount * 2];
                if (FMP != null) { FMP.ApplyMuteChs(CCommon.GetMuteChs()); FMP.GetPCM(tmpbuf, SamplesCount); }
                if (PMD != null) { PMD.ApplyMuteChs(CCommon.GetMuteChs()); PMD.GetPCM(tmpbuf, SamplesCount); }
                for (var idx = 0; idx < SamplesCount; idx++) {
                    buf[idx * CCommon.OutputElements.Count + CCommon.OutputElements.FL] = tmpbuf[idx * 2 + 0] / 32768f;
                    buf[idx * CCommon.OutputElements.Count + CCommon.OutputElements.FR] = tmpbuf[idx * 2 + 1] / 32768f;
                }
                FMPPMD_FadeoutLate -= System.TimeSpan.FromSeconds((double)SamplesCount / SampleRate);
                return CCommon.EPCMSurroundMode.SimpleLR;
            }
            throw new Exception();
        }

        public string GetPDXCaption() {
            if (MDXFile != null) { return MDXFile.GetPDXCaption(); }
            if (FMP != null) { return FMP.GetPVIFilenames().FilenamesStr; }
            if (PMD != null) { return PMD.GetPVIFilenames().FilenamesStr; }
            throw new Exception();
        }

        public bool isEOF() {
            if (MDXFile != null) { return MDXFile.isEOF; }
            if (FMP != null) { return FMP.GetLoopCount() == -1; }
            if (PMD != null) { return PMD.GetLoopCount() == -1; }
            throw new Exception();
        }

        public int GetLoopCount() {
            if (MDXFile != null) { return MDXFile.GetLoopCount(); }
            if (FMP != null) { return FMP.GetLoopCount(); }
            if (PMD != null) { return PMD.GetLoopCount(); }
            throw new Exception();
        }

        public bool GetFadeout() {
            if (MDXFile != null) { return MDXFile.GetFadeout(); }
            if (FMP != null) { return FMPPMD_Fadeout; }
            if (PMD != null) { return FMPPMD_Fadeout; }
            throw new Exception();
        }

        public float GetFadeoutAlpha() {
            if (MDXFile != null) { return (float)MDXFile.GetFadeoutAlpha(); }
            if (FMP != null) { return 1; }
            if (PMD != null) { return 1; }
            throw new Exception();
        }

        public CCommon.CVisualGlobal GetVisualGlobal() {
            if (MDXFile != null) { return MDXFile.GetVisualGlobal(); }
            if ((FMP != null) || (PMD != null)) {
                CCommon.CVisualGlobal res = null;
                if (FMP != null) { res = FMP.GetVisualGlobal(); }
                if (PMD != null) { res = PMD.GetVisualGlobal(); }
                if (res == null) { return res; }
                if (!FMPPMD_Fadeout) {
                    res.Fadeout = 1;
                } else {
                    var alpha = FMPPMD_FadeoutLate / CCommon.FadeoutTS;
                    if (alpha < 0) { alpha = 0; }
                    res.Fadeout = alpha;
                }
                res.isPDX_bos_pdx = false;
                return res;
            }
            throw new Exception();
        }

        public CCommon.CVisualPart GetVisualStatus(int ch) {
            if (MDXFile != null) { return MDXFile.GetVisualStatus(ch); }
            if (FMP != null) { return FMP.GetVisualStatus(ch); }
            if (PMD != null) { return PMD.GetVisualStatus(ch); }
            throw new Exception();
        }

        public int GetPanpot(int ch) {
            if (MDXFile != null) { return MDXFile.GetPanpot(ch); }
            if (FMP != null) { return 0; }
            if (PMD != null) { return 0; }
            throw new Exception();
        }

        public TimeSpan GetPlayTS() {
            if (MDXFile != null) { return MDXFile.GetPlayTS(); }
            if (FMP != null) { return FMP.GetPos(); }
            if (PMD != null) { return PMD.GetPos(); }
            throw new Exception();
        }

        public string GetExceptionLog() {
            if (MDXFile != null) { return MDXFile.GetExceptionLog(); }
            if (FMP != null) { return ""; }
            if (PMD != null) { return ""; }
            throw new Exception();
        }

        public List<string> GetWarnLog() {
            if (MDXFile != null) { return MDXFile.GetWarnLog(); }
            if (FMP != null) { return new List<string>(); }
            if (PMD != null) { return new List<string>(); }
            throw new Exception();
        }

        public string GetDriverVersion() {
            if (MDXFile != null) { return X68SoundMCh.CAPI.Version; }
            if (FMP != null) { return FMP.GetVersionStr(); }
            if (PMD != null) { return PMD.GetVersionStr(); }
            throw new Exception();
        }

        public void SetFadeout(byte Speed) {
            if (MDXFile != null) { MDXFile.SetFadeout(Speed); return; }
            if ((FMP != null) || (PMD != null)) {
                if (!FMPPMD_Fadeout) {
                    FMPPMD_Fadeout = true;
                    FMPPMD_FadeoutLate = CCommon.FadeoutTS;
                    if (FMP != null) { FMP.StartFadeout(TimeSpan.FromSeconds(5)); }
                    if (PMD != null) { PMD.StartFadeout(TimeSpan.FromSeconds(5)); }
                }
                return;
            }
            throw new Exception();
        }

        public bool isMXDRV() {
            if (MDXFile != null) { return true; }
            if (FMP != null) { return false; }
            if (PMD != null) { return false; }
            throw new Exception();
        }
    }
}
