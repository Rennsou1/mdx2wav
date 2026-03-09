using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml.Schema;

namespace MDXWin {
    public class CCmdLine {
        public const string Version = "Command verion 0.13";

        private class CParsed {
            public string cmd, param;
            public CParsed(string Line) {
                Line = Line.ToLower().Trim();

                cmd = Line;
                param = "";

                var pos = cmd.IndexOf(" ");
                if (pos != -1) {
                    param = cmd.Substring(1 + pos);
                    cmd = cmd.Substring(0, pos);
                }

                param = param.Replace('/', '\\');
            }
        }

        public class CExecuteRes {
            public List<CConsole.CLog.CItem> Lines = new List<CConsole.CLog.CItem>();
            public enum EParentCommand { NOP, Switch, ChangeDir, CloseApp, Visual, FileSel, ChangeFont, SetFunc, MDXPlay, MDXStop, PlayMode, PlayNext, FlacOut,OpenFolder,OpenDocs };
            public EParentCommand ParentCommand = EParentCommand.NOP;
            public string ParamStr = "";
        }
        public CExecuteRes Execute(MDXOnline004.CClient ContentsClient, string _Line) {
            var res = new CExecuteRes();

            var Parsed = new CParsed(_Line);
            if (Parsed.cmd.Equals("") || Parsed.cmd.StartsWith("#")) { return (res); }

            switch (Parsed.cmd) {
                case "help":
                    foreach (var Line in Lang.CLang.GetCommandHelp()) {
                        res.Lines.Add(Line);
                    }
                    break;
                case "switch":
                    res.ParentCommand = CExecuteRes.EParentCommand.Switch;
                    res.ParamStr = Parsed.param;
                    break;
                // ---------------------------------------------------------- システム関連
                case "cd":
                    if ((Parsed.param == null) || Parsed.param.Equals("")) {
                        res.Lines.Add(new CConsole.CLog.CItem(ContentsClient.CurrentFolder.ID + " " + ContentsClient.CurrentFolder.BaseName));
                    } else {
                        if (Parsed.param.Equals(@"\")) { Parsed.param = "0"; }
                        res.ParentCommand = CExecuteRes.EParentCommand.ChangeDir;
                        res.ParamStr = Parsed.param;
                    }
                    break;
                case "dir":
                case "ls": {
                    if (1 <= ContentsClient.CurrentFolder.ParentFolders.Count) {
                        foreach (var Folder in ContentsClient.CurrentFolder.ParentFolders) {
                            res.Lines.Add(new CConsole.CLog.CItem("Parent ID:" + Folder.ID.ToString().PadLeft(4) + " " + new MDXOnline004.CFolderTag(Folder.BaseName).GetTextFromFolderTag(), CConsole.CLog.CItem.EMode.Dir, Folder.ID.ToString()));
                        }
                        res.Lines.Add(new CConsole.CLog.CItem("CurrentID:" + ContentsClient.CurrentFolder.ID.ToString().PadLeft(4) + " " + new MDXOnline004.CFolderTag(ContentsClient.CurrentFolder.BaseName).GetTextFromFolderTag(), CConsole.CLog.CItem.EMode.Dir, ContentsClient.CurrentFolder.ID.ToString()));
                        res.Lines.Add(new CConsole.CLog.CItem(""));
                    }
                    if (1 <= ContentsClient.CurrentFolder.InFolders.Count) {
                        foreach (var Folder in ContentsClient.CurrentFolder.InFolders) {
                            res.Lines.Add(new CConsole.CLog.CItem("Folder ID:" + Folder.ID.ToString().PadLeft(4) + " " + new MDXOnline004.CFolderTag(Folder.DirName).GetTextFromFolderTag(), CConsole.CLog.CItem.EMode.Dir, Folder.ID.ToString()));
                        }
                        res.Lines.Add(new CConsole.CLog.CItem(""));
                    }
                    var Uses = 0L;
                    if (1 <= ContentsClient.CurrentFolder.Files.Count) {
                        var MaxLen = 0;
                        foreach (var File in ContentsClient.CurrentFolder.Files) {
                            var len = MoonLib.CTextEncode.GetLength_半角カナを1文字として数える(System.IO.Path.GetFileNameWithoutExtension(File.Filename));
                            if (MaxLen < len) { MaxLen = len; }
                        }
                        foreach (var File in ContentsClient.CurrentFolder.Files) {
                            Uses += File.Size;
                            var fn = System.IO.Path.GetFileNameWithoutExtension(File.Filename);
                            {
                                var len = MoonLib.CTextEncode.GetLength_半角カナを1文字として数える(fn);
                                if (len < MaxLen) { fn += new string(' ', MaxLen - len); }
                            }
                            fn += System.IO.Path.GetExtension(File.Filename).PadRight(4);
                            var PlayTSStr = "ERROR";
                            if (File.PlayTS != System.TimeSpan.FromTicks(0)) {
                                PlayTSStr = File.PlayTS.TotalMinutes.ToString("F0").PadLeft(2) + ":" + File.PlayTS.Seconds.ToString("F0").PadLeft(2, '0');
                            }
                            var Title = File.Title;
                            res.Lines.Add(new CConsole.CLog.CItem(fn + " " + File.Size.ToString().PadLeft(6) + " " + File.DateTime.ToString("yy-MM-dd") + " " + PlayTSStr + " " + File.TitleRaw, CConsole.CLog.CItem.EMode.File, File.MD5));
                        }
                        res.Lines.Add(new CConsole.CLog.CItem(""));
                    }
                    res.Lines.Add(new CConsole.CLog.CItem(ContentsClient.CurrentFolder.Files.Count.ToString().PadLeft(6) + " " + Lang.CLang.GetCommand(Lang.CLang.ECommand.Dir_File) + " " + (Uses / 1024).ToString() + "K Byte " + Lang.CLang.GetCommand(Lang.CLang.ECommand.Dir_Used)));
                }
                break;
                case "openfolder": res.ParentCommand = CExecuteRes.EParentCommand.OpenFolder; res.ParamStr = Parsed.param; break;
                case "opendocs": res.ParentCommand = CExecuteRes.EParentCommand.OpenDocs; res.ParamStr = Parsed.param; break;
                case "exit": res.ParentCommand = CExecuteRes.EParentCommand.CloseApp; break;
                case "filesel": res.ParentCommand = CExecuteRes.EParentCommand.FileSel; res.ParamStr = Parsed.param; break;
                case "igexcept": {
                    if (!Parsed.param.Equals("")) {
                        switch (Parsed.param) {
                            case "off": MXDRV.CCanIgnoreException.IgnoreException = false; break;
                            case "on": MXDRV.CCanIgnoreException.IgnoreException = true; break;
                            default: res.Lines.Add(new CConsole.CLog.CItem(Lang.CLang.GetCommand(Lang.CLang.ECommand.WrongParam))); break;
                        }
                    }
                    res.Lines.Add(new CConsole.CLog.CItem(MXDRV.CCanIgnoreException.IgnoreException ? "ON" : "OFF"));
                }
                break;
                case "playmode": res.ParentCommand = CExecuteRes.EParentCommand.PlayMode; res.ParamStr = Parsed.param; break;
                case "playnext": res.ParentCommand = CExecuteRes.EParentCommand.PlayNext; break;
                case "setfunc":
                    res.ParentCommand = CExecuteRes.EParentCommand.SetFunc;
                    res.ParamStr = Parsed.param;
                    break;
                case "visual": res.ParentCommand = CExecuteRes.EParentCommand.Visual; res.ParamStr = Parsed.param; break;
                case "visualmul":
                    if (!Parsed.param.Equals("")) {
                        double v;
                        if (double.TryParse(Parsed.param, out v)) {
                            if ((v < 0.5) || (4 < v)) {
                                res.Lines.Add(new CConsole.CLog.CItem(Lang.CLang.GetCommand(Lang.CLang.ECommand.OutOfRange)));
                            } else {
                                CCommon.VisualMul = v;
                            }
                        } else {
                            res.Lines.Add(new CConsole.CLog.CItem(Lang.CLang.GetCommand(Lang.CLang.ECommand.VisualMul_Help)));
                        }
                    }
                    res.Lines.Add(new CConsole.CLog.CItem(CCommon.VisualMul.ToString()));
                    break;
                case "speana": {
                    if (!Parsed.param.Equals("")) {
                        switch (Parsed.param) {
                            case "off": CCommon.VisualSpeAna = false; break;
                            case "on": CCommon.VisualSpeAna = true; break;
                            default: res.Lines.Add(new CConsole.CLog.CItem(Lang.CLang.GetCommand(Lang.CLang.ECommand.WrongParam))); break;
                        }
                    }
                    res.Lines.Add(new CConsole.CLog.CItem(CCommon.VisualSpeAna ? "ON" : "OFF"));
                }
                break;
                case "oscillo": {
                    if (!Parsed.param.Equals("")) {
                        switch (Parsed.param) {
                            case "off": CCommon.VisualOscillo = false; break;
                            case "on": CCommon.VisualOscillo = true; break;
                            default: res.Lines.Add(new CConsole.CLog.CItem(Lang.CLang.GetCommand(Lang.CLang.ECommand.WrongParam))); break;
                        }
                    }
                    res.Lines.Add(new CConsole.CLog.CItem(CCommon.VisualOscillo ? "ON" : "OFF"));
                }
                break;
                // ---------------------------------------------------------- フォント関連
                case "consolefs":
                    if (!Parsed.param.Equals("")) {
                        int FontSize;
                        if (int.TryParse(Parsed.param, out FontSize)) {
                            if (!CCommon.CGROM.isHaveFontSize(FontSize)) {
                                res.Lines.Add(new CConsole.CLog.CItem(Lang.CLang.GetCommand(Lang.CLang.ECommand.FS_Error)));
                            } else {
                                CCommon.FontSize_Console = FontSize;
                                res.ParentCommand = CExecuteRes.EParentCommand.ChangeFont;
                            }
                        } else {
                            res.Lines.Add(new CConsole.CLog.CItem(Lang.CLang.GetCommand(Lang.CLang.ECommand.FS_Help)));
                        }
                    }
                    res.Lines.Add(new CConsole.CLog.CItem(CCommon.CGROM.GetFontName(CCommon.FontSize_Console)));
                    break;
                case "fileselfs":
                    if (!Parsed.param.Equals("")) {
                        int FontSize;
                        if (int.TryParse(Parsed.param, out FontSize)) {
                            if (!CCommon.CGROM.isHaveFontSize(FontSize)) {
                                res.Lines.Add(new CConsole.CLog.CItem(Lang.CLang.GetCommand(Lang.CLang.ECommand.FS_Error)));
                            } else {
                                CCommon.FontSize_FileSel = FontSize;
                                res.ParentCommand = CExecuteRes.EParentCommand.ChangeFont;
                            }
                        } else {
                            res.Lines.Add(new CConsole.CLog.CItem(Lang.CLang.GetCommand(Lang.CLang.ECommand.FS_Help)));
                        }
                    }
                    res.Lines.Add(new CConsole.CLog.CItem(CCommon.CGROM.GetFontName(CCommon.FontSize_FileSel)));
                    break;
                // ---------------------------------------------------------- MXDRV関連
                case "adpcm":
                    if (!Parsed.param.Equals("")) {
                        int ADPCMMode;
                        if (int.TryParse(Parsed.param, out ADPCMMode)) {
                            if (!CCommon.AudioThread.Settings.SetADPCMModeInt(ADPCMMode)) {
                                res.Lines.Add(new CConsole.CLog.CItem(Lang.CLang.GetCommand(Lang.CLang.ECommand.OutOfRange)));
                            }
                        } else {
                            res.Lines.Add(new CConsole.CLog.CItem(Lang.CLang.GetCommand(Lang.CLang.ECommand.ADPCM_Help)));
                        }
                    }
                    res.Lines.Add(new CConsole.CLog.CItem(CCommon.AudioThread.Settings.GetADPCMModeStr()));
                    break;
                case "bospdxhq":
                    if (!Parsed.param.Equals("")) {
                        switch (Parsed.param) {
                            case "off": MXDRV.CCommon.UseBosPdxHQ = false; break;
                            case "on": MXDRV.CCommon.UseBosPdxHQ = true; break;
                            default: res.Lines.Add(new CConsole.CLog.CItem(Lang.CLang.GetCommand(Lang.CLang.ECommand.WrongParam))); break;
                        }
                    }
                    res.Lines.Add(new CConsole.CLog.CItem(MXDRV.CCommon.UseBosPdxHQ ? "ON" : "OFF"));
                    break;
                case "mxfade":
                    if (!CCommon.AudioThread.Music_isLoaded()) {
                        res.Lines.Add(new CConsole.CLog.CItem(Lang.CLang.GetCommand(Lang.CLang.ECommand.NoLoadedMDX)));
                    } else {
                        CCommon.AudioThread.Music_SetFadeout(0x00);
                    }
                    break;
                case "mxloop":
                    if (!Parsed.param.Equals("")) {
                        int LoopCount;
                        if (int.TryParse(Parsed.param, out LoopCount)) {
                            if ((LoopCount < 0) || (255 < LoopCount)) {
                                res.Lines.Add(new CConsole.CLog.CItem(Lang.CLang.GetCommand(Lang.CLang.ECommand.OutOfRange)));
                            } else {
                                CCommon.AudioThread.Settings.LoopCount = LoopCount;
                            }
                        } else {
                            res.Lines.Add(new CConsole.CLog.CItem(Lang.CLang.GetCommand(Lang.CLang.ECommand.MXLoop_Help)));
                        }
                    }
                    res.Lines.Add(new CConsole.CLog.CItem(CCommon.AudioThread.Settings.LoopCount.ToString()));
                    break;
                case "mxmute":
                    if (Parsed.param.Equals("all")) {
                        var curmute = false;
                        for (var ch = 0; ch < 16; ch++) {
                            if (MusDriver.CCommon.GetMuteCh(ch)) { curmute = true; }
                        }
                        curmute = !curmute;
                        for (var ch = 0; ch < 16; ch++) {
                            MusDriver.CCommon.SetMuteCh(ch, curmute);
                        }
                    } else {
                        int ch;
                        if (int.TryParse(Parsed.param, out ch)) {
                            ch--;
                        } else {
                            ch = MusDriver.CCommon.ChannelMap.ToLower().IndexOf(Parsed.param);
                        }
                        if ((ch < 0) || (16 <= ch)) {
                            res.Lines.Add(new CConsole.CLog.CItem(Lang.CLang.GetCommand(Lang.CLang.ECommand.MXMute_WrongCh)));
                        } else {
                            MusDriver.CCommon.SetMuteCh(ch, !MusDriver.CCommon.GetMuteCh(ch));
                        }
                    }
                    var mutestr = "";
                    for (var ch = 0; ch < 16; ch++) {
                        mutestr += MusDriver.CCommon.ChannelMap.Substring(ch, 1) + ":" + (MusDriver.CCommon.GetMuteCh(ch) ? "Mute" : "On") + ", ";
                    }
                    res.Lines.Add(new CConsole.CLog.CItem(mutestr.Trim()));
                    break;
                case "mxopm":
                    if (!Parsed.param.Equals("")) {
                        switch (Parsed.param) {
                            case "off": CCommon.AudioThread.Settings.OPMEnabled = false; break;
                            case "on": CCommon.AudioThread.Settings.OPMEnabled = true; break;
                            default: res.Lines.Add(new CConsole.CLog.CItem(Lang.CLang.GetCommand(Lang.CLang.ECommand.WrongParam))); break;
                        }
                    }
                    res.Lines.Add(new CConsole.CLog.CItem(CCommon.AudioThread.Settings.OPMEnabled ? "ON" : "OFF"));
                    break;
                case "mxpcm":
                    if (!Parsed.param.Equals("")) {
                        switch (Parsed.param) {
                            case "off": CCommon.AudioThread.Settings.PCMEnabled = false; break;
                            case "on": CCommon.AudioThread.Settings.PCMEnabled = true; break;
                            default: res.Lines.Add(new CConsole.CLog.CItem(Lang.CLang.GetCommand(Lang.CLang.ECommand.WrongParam))); break;
                        }
                    }
                    res.Lines.Add(new CConsole.CLog.CItem(CCommon.AudioThread.Settings.PCMEnabled ? "ON" : "OFF"));
                    break;
                case "mxp": // for MXDRV
                case "play": // for FMP
                case "pmp": // for PMD
                    if (Parsed.param.Equals("")) {
                        res.Lines.Add(new CConsole.CLog.CItem("music player version 0.02 (c)2023 Moonlight."));
                    } else {
                        res.ParentCommand = CExecuteRes.EParentCommand.MDXPlay;
                        res.ParamStr = Parsed.param;
                    }
                    break;
                case "mxseek":
                    if (Parsed.param.Equals("")) {
                        res.Lines.Add(new CConsole.CLog.CItem(Lang.CLang.GetCommand(Lang.CLang.ECommand.MXSeek_Help)));
                    } else {
                        System.TimeSpan ts = System.TimeSpan.FromSeconds(-1);

                        if (Parsed.param.EndsWith("%")) {
                            double par;
                            if (double.TryParse(Parsed.param.Substring(0, Parsed.param.Length - 1), out par)) {
                                ts = CCommon.AudioThread.Music_GetPlayTS() * (par / 100);
                            }
                        } else {
                            double secs;
                            if (double.TryParse(Parsed.param, out secs)) {
                                ts = System.TimeSpan.FromSeconds(secs);
                            } else {
                                if (System.TimeSpan.TryParse(Parsed.param, out ts)) {
                                }
                            }
                        }
                        if (ts < System.TimeSpan.FromSeconds(0)) {
                            res.Lines.Add(new CConsole.CLog.CItem(Lang.CLang.GetCommand(Lang.CLang.ECommand.MXSeek_NoNum)));
                        } else {
                            if (!CCommon.AudioThread.Music_isLoaded()) {
                                res.Lines.Add(new CConsole.CLog.CItem(Lang.CLang.GetCommand(Lang.CLang.ECommand.NoLoadedMDX)));
                            } else {
                                res.Lines.Add(new CConsole.CLog.CItem("Seek to " + ts.ToString()));
                                CCommon.AudioThread.Music_SeekMDX(ts);
                            }
                        }
                    }
                    break;
                case "mxstat":
                    if (!CCommon.AudioThread.Music_isLoaded()) {
                        res.Lines.Add(new CConsole.CLog.CItem(Lang.CLang.GetCommand(Lang.CLang.ECommand.NoLoadedMDX)));
                    } else {
                        foreach (var Line in CCommon.AudioThread.Music_GetInfo()) {
                            res.Lines.Add(new CConsole.CLog.CItem(Line));
                        }
                    }
                    break;
                case "mxstop":
                    if (!CCommon.AudioThread.Music_isLoaded()) {
                        res.Lines.Add(new CConsole.CLog.CItem(Lang.CLang.GetCommand(Lang.CLang.ECommand.NoLoadedMDX)));
                    } else {
                        res.ParentCommand = CExecuteRes.EParentCommand.MDXStop;
                    }
                    break;
                case "samplerate":
                    if (!Parsed.param.Equals("")) {
                        int SampleRate;
                        if (int.TryParse(Parsed.param, out SampleRate)) {
                            if ((SampleRate < 62500) || (384000 < SampleRate)) {
                                res.Lines.Add(new CConsole.CLog.CItem(Lang.CLang.GetCommand(Lang.CLang.ECommand.OutOfRange)));
                            } else {
                                CCommon.AudioThread.Settings.SampleRate = SampleRate;
                            }
                        } else {
                            res.Lines.Add(new CConsole.CLog.CItem(Lang.CLang.GetCommand(Lang.CLang.ECommand.SampleRate_Help)));
                        }
                    }
                    res.Lines.Add(new CConsole.CLog.CItem(CCommon.AudioThread.Settings.SampleRate.ToString()));
                    break;
                // ---------------------------------------------------------- 音声出力関連
                case "volume":
                    if (!Parsed.param.Equals("")) {
                        float Volume;
                        if (float.TryParse(Parsed.param, out Volume)) {
                            if ((Volume < 70) || (100 < Volume)) {
                                res.Lines.Add(new CConsole.CLog.CItem(Lang.CLang.GetCommand(Lang.CLang.ECommand.OutOfRange)));
                            } else {
                                CCommon.AudioThread.Settings.VolumeDB = Volume;
                            }
                        } else {
                            res.Lines.Add(new CConsole.CLog.CItem(Lang.CLang.GetCommand(Lang.CLang.ECommand.Volume_Help)));
                        }
                    }
                    res.Lines.Add(new CConsole.CLog.CItem(CCommon.AudioThread.Settings.VolumeDB.ToString()));
                    break;
                case "flacout": res.ParentCommand = CExecuteRes.EParentCommand.FlacOut; res.ParamStr = Parsed.param; break;
                default: res.Lines.Add(new CConsole.CLog.CItem(Lang.CLang.GetCommand(Lang.CLang.ECommand.NoCommandOrNoFilename))); break;
            }

            return (res);
        }

        public static string GetMusicPlayCommand(string MusicFilename) {
            return "mxp " + MusicFilename;
        }
    }
}
