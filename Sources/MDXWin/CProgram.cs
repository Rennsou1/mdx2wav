using Lang;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Printing;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace MDXWin {
    public class CProgram {
        private static MainWindow MainWindow = null;
        private static WFileSelector FileSelector = null;
        private static WVisual Visual = null;

        public static bool GetVisualVisible() { return CProgram.Visual.Visibility == Visibility.Visible; }
        public static bool GetFileSelectorVisible() { return CProgram.FileSelector.Visibility == Visibility.Visible; }


        private static MoonLib.CNonSleepAudio NonSleepAudio = null;

        private static CCmdLine CmdLine = new();

        private static CPlayList PlayList = new(null, "");

        public class CCmdLineMacro {
            private List<string> Lines = new();
            public void Add(string cmd, bool ParsePipe = true) {
                if (!ParsePipe) {
                    Lines.Add(cmd);
                } else {
                    var res = cmd.Split('|');
                    for (var idx = 0; idx < res.Length; idx++) {
                        Lines.Add(res[idx].Trim());
                    }
                }
            }

            public void LoadAutoExec() {
                if (!System.IO.File.Exists(CCommon.AutoexecIniFilename)) { return; }

                using (var rfs = new System.IO.StreamReader(CCommon.AutoexecIniFilename)) {
                    while (!rfs.EndOfStream) {
                        Add(rfs.ReadLine());
                    }
                }
            }
            public string GetOne() {
                if (Lines.Count == 0) { return ""; }
                var res = Lines[0];
                Lines.RemoveAt(0);
                return res;
            }
        }
        public static CCmdLineMacro CmdLineMacro = new CCmdLineMacro();

        public class CBootLog {
            private Stopwatch sw = new Stopwatch();
            private System.TimeSpan LastElp;
            private List<string> Logs = new List<string>();
            public CBootLog() {
                sw.Restart();
                LastElp = sw.Elapsed;
            }
            public void Add(string Title) {
                Logs.Add(sw.Elapsed.ToString() + " (" + (sw.Elapsed - LastElp).ToString() + ") " + Title);
                LastElp = sw.Elapsed;
            }
            public void WriteFile() {
                using (var wfs = new System.IO.StreamWriter("MDXWin.Boot.txt")) {
                    foreach (var Log in Logs) {
                        wfs.WriteLine(Log);
                    }
                }
            }
        }
        public static CBootLog BootLog = new CBootLog();

        private static void ShowFatalError(string Line1, string Line2, string Line3) {
            BootLog.WriteFile();
            new WFatalError(Line1, Line2, Line3);
            Environment.Exit(0);
        }
        private static void ShowFatalError(Lang.CLang.EBoot Line1, string Line2, string Line3) { ShowFatalError(Lang.CLang.GetBoot(Line1), Line2, Line3); }
        private static void ShowFatalError(Lang.CLang.EBoot Line1, Lang.CLang.EBoot Line2, string Line3) { ShowFatalError(Lang.CLang.GetBoot(Line1), Lang.CLang.GetBoot(Line2), Line3); }
        private static void ShowFatalError(Lang.CLang.EBoot Line1, Lang.CLang.EBoot Line2, Lang.CLang.EBoot Line3) { ShowFatalError(Lang.CLang.GetBoot(Line1), Lang.CLang.GetBoot(Line2), Lang.CLang.GetBoot(Line3)); }

        public static void ExecBoot_FromMainWindow() {
            CCommon.INI = new();
            BootLog.Add("INI loaded");

            try {
                if (!MDXOnline004.CClient.LoadSettings()) {
                    ShowFatalError(Lang.CLang.EBoot.Network_IlligalFormat, Lang.CLang.EBoot.Network_ServerStop1, Lang.CLang.EBoot.Network_ServerStop2);
                    return;
                }
            } catch {
                ShowFatalError(Lang.CLang.EBoot.Network_InfoCantGet, Lang.CLang.EBoot.Network_ServerStop1, Lang.CLang.EBoot.Network_ServerStop2);
                return;
            }
            BootLog.Add("Load settings");

            if (CCommon.INI.UserName.Equals("")) {
                var window = new WInternet();
                window.ShowDialog();
                if (!window.許可) {
                    BootLog.WriteFile();
                    Environment.Exit(0);
                    return;
                }
                CCommon.INI.UserName = window.UserNameText.Text;
            }
            BootLog.Add("Permit internet");

            CCommon.CGROM = new MoonLib.CCGROM();
            CCommon.CGROM.Load();
            BootLog.Add("CGROM loaded");
        }

        private static void ExecBoot_MainWindow_Window_Loaded_ins_SetWasapiSampleRate() {
            var WaveOut = new NAudio.Wave.WasapiOut();

            var rate = WaveOut.OutputWaveFormat.SampleRate;
            if (rate < 62500) { rate = 62500; }
            if (384000 < rate) { rate = 384000; }
            CCommon.AudioThread.Settings.SampleRate = rate;
        }

        public static void ExecBoot_MainWindow_Window_Loaded(MainWindow _MainWindow) {
            MainWindow = _MainWindow;

            MDXOnline004.CClient.HttpGetLang = CCommon.INI.LangMode.ToString();

            try {
                var FMPVersion = "";
                var PMDVersion = "";

                if (System.IO.File.Exists(FMPPMD.CFMP.DLLName) && System.IO.File.Exists(FMPPMD.CPMD.DLLName) && System.IO.File.Exists("ym2608_adpcm_rom.bin")) {
                    using (var FMP = new FMPPMD.CFMP("", 44100, ".")) {
                        if (!FMP.isLoaded()) { ShowFatalError(Lang.CLang.EBoot.FMPPMD_CannotLoadDLL, "", FMPPMD.CFMP.DLLName); return; }
                        FMPVersion = FMP.GetVersionStr();
                    }

                    using (var PMD = new FMPPMD.CPMD("", 44100, ".")) {
                        if (!PMD.isLoaded()) { ShowFatalError(Lang.CLang.EBoot.FMPPMD_CannotLoadDLL, "", FMPPMD.CPMD.DLLName); return; }
                        PMDVersion = PMD.GetVersionStr();
                    }
                    MDXOnline004.CClient.IncludeFMPPMD = true;
                    BootLog.Add("Load FMPPMD");
                }

                MoonLib.CTextEncode.Setup();
                BootLog.Add("Regist S-JIS encode");

                FileSelector = new WFileSelector();
                BootLog.Add("FileSelectorWindow");
                Visual = new WVisual();
                BootLog.Add("VisualWindow");

                CCommon.Console.WriteLine("");
                CCommon.Console.WriteLine(CCommon.AppConsole);
                CCommon.Console.WriteLine("");

                CCommon.Console.WriteLine((CCommon.CGROM.isLoaded() ? Lang.CLang.GetBoot(Lang.CLang.EBoot.CGROM_Loaded) : Lang.CLang.GetBoot(Lang.CLang.EBoot.CGROM_NotFound)));

                CCommon.Console.WriteLine(X68SoundMCh.CAPI.Version);
                BootLog.Add("Check CAPI version");

                if (!FMPVersion.Equals("")) { CCommon.Console.WriteLine(FMPVersion); }
                if (!PMDVersion.Equals("")) { CCommon.Console.WriteLine(PMDVersion); }

                MoonLib.CFFT.Init();
                BootLog.Add("FFT precalc");

                X68SoundMCh.CGlobal.InitTable();
                BootLog.Add("X68SoundMCh.CGlobal.InitTable");

                CCommon.AudioThread = new CAudioThread();
                foreach (var Line in CCommon.AudioThread.GetIntLog()) {
                    CCommon.Console.WriteLine(Line);
                }
                BootLog.Add("CCommon.AudioThread");

                ExecBoot_MainWindow_Window_Loaded_ins_SetWasapiSampleRate();
                BootLog.Add("Check output format");

                NonSleepAudio = new MoonLib.CNonSleepAudio();
                BootLog.Add("new CNonSleepAudio");

                if (MXDRV.CPDXHQBos.Voices_Load(true)) { CCommon.Console.WriteLine(MXDRV.CPDXHQBos.BosPdxHQFilename + " " + Lang.CLang.GetBoot(Lang.CLang.EBoot.PDXHQBos_Loaded)); }
                BootLog.Add("Load " + MXDRV.CPDXHQBos.BosPdxHQFilename);

                CCommon.MDXOnlineClient = new MDXOnline004.CClient(CCommon.Console);
                CCommon.Console.WriteLine(MDXOnline004.CClient.Version);
                BootLog.Add("new MDXOnline.CClient");

                try {
                    var res = CCommon.MDXOnlineClient.HttpGet_Login(CCommon.INI.UserName);
                    if (!res.Equals("")) { throw new Exception("HttpGet_Login httpres:" + res); }
                } catch {
                    ShowFatalError(Lang.CLang.EBoot.Network_CantLogin1, Lang.CLang.EBoot.Network_CantLogin2, Lang.CLang.EBoot.Network_CantLogin3);
                    return;
                }
                CCommon.Console.WriteLine(Lang.CLang.GetBoot(Lang.CLang.EBoot.Network_Logined1) + CCommon.INI.UserName + Lang.CLang.GetBoot(Lang.CLang.EBoot.Network_Logined2));
                if (!MDXOnline004.CClient.isLatestClient(CCommon.AppVersion)) { CCommon.Console.WriteLine(CLang.GetWInternet(CLang.EWInternet.ExistsLatestClient)); }
                BootLog.Add("Login");

                if (CCommon.INI.InitialBoot) {
                    int fid;
                    if (!int.TryParse(MDXOnline004.CClient.Settings.DefaultFolderID, out fid)) { fid = 0; }
                    var res = CCommon.MDXOnlineClient.HttpGetFolder(fid);
                    if (!res.Equals("")) {
                        var resroot = CCommon.MDXOnlineClient.HttpGetFolder(0);
                        if (!resroot.Equals("")) { throw new Exception("ContentsClient.Current.LoadInfos: " + res); }
                    }
                } else {
                    var res = CCommon.MDXOnlineClient.HttpGetFolder(CCommon.INI.CurrentFolderID);
                    if (res.Equals("")) {
                        CCommon.Console.WriteLine(Lang.CLang.GetBoot(Lang.CLang.EBoot.CurrentPath_Restore) + " [" + CCommon.MDXOnlineClient.CurrentFolder.BaseName + "]");
                    } else {
                        var resroot = CCommon.MDXOnlineClient.HttpGetFolder(0);
                        if (!resroot.Equals("")) { throw new Exception("ContentsClient.Current.LoadInfos: " + res); }
                        CCommon.Console.WriteLine(Lang.CLang.GetBoot(Lang.CLang.EBoot.CurrentPath_Error) + " " + res);
                    }
                }
                CCommon.INI.CurrentFolderID = CCommon.MDXOnlineClient.CurrentFolder.ID;
                FileSelector.UpdateFileList();
                BootLog.Add("Set init path");

                CCommon.Console.WriteLine();
                CCommon.Console.WriteLine(CCmdLine.Version);
                CCommon.Console.WriteLine();
                CCommon.Console.WriteLine("A>");
                BootLog.Add("Log write");

                MoonLib.CDPI.InitAfterWindowLoaded(MainWindow);
                var HDPISettings = new CCommon.CHDPISettings();
                CCommon.FontSize_Console = HDPISettings.FontSize_Console;
                CCommon.FontSize_FileSel = HDPISettings.FontSize_FileSel;
                CCommon.VisualMul = HDPISettings.VisualMul;
                BootLog.Add("Init High-DPI");

                CProgram.CmdLineMacro.LoadAutoExec();
                BootLog.Add("Load autoexec.bat");
            } catch (Exception ex) {
                ShowFatalError(Lang.CLang.EBoot.FatalErrorOnBoot, ex.Message, ex.StackTrace.Replace(@"C:\Users\morit\OneDrive\_VisualStudio\MDXWin\", "")); // 多少見やすく？
                return;
            }

            BootLog.WriteFile();

            CCommon.Console.Refresh();

            IntervalTimer.Tick += new EventHandler(IntervalTimerTick);
            IntervalTimer.Interval = TimeSpan.FromTicks(1);
            IntervalTimer.Start();
        }

        public static void Window_Closed() {
            IntervalTimer.Stop();

            Visual.Stop();

            if (CCommon.AudioThread != null) {
                CCommon.AudioThread.Music_Free();
                CCommon.AudioThread = null;
            }

            if (NonSleepAudio != null) {
                NonSleepAudio.Free();
                NonSleepAudio = null;
            }

            FileSelector.ReqClose = true;
            FileSelector.Close();
            Visual.ReqClose = true;
            Visual.Close();

            if (CCommon.SwitchXWindow != null) {
                CCommon.SwitchXWindow.Close();
                CCommon.SwitchXWindow = null;
            }
        }

        private static System.Windows.Threading.DispatcherTimer IntervalTimer = new System.Windows.Threading.DispatcherTimer();

        private static void IntervalTimerTick(object sender, EventArgs e) {
            IntervalTimer.Stop();

            NonSleepAudio.Update();

            var cmdline = CProgram.CmdLineMacro.GetOne();
            if (!cmdline.Equals("")) {
                ExecCommand(true, cmdline);
                IntervalTimer.Interval = TimeSpan.FromTicks(1);
                IntervalTimer.Start();
                return;
            }

            if (CCommon.AudioThread != null) {
                if (CCommon.AudioThread.Music_isLoaded()) {
                    var warnlog = CCommon.AudioThread.Music_GetWarnLog();
                    CCommon.Console.WriteLine最後から一行前に追加(warnlog);
                    MXDRV.CCommon.Log.WriteLine(warnlog);

                    var ExceptionLog = CCommon.AudioThread.Music_GetExceptionLog();
                    if (!ExceptionLog.Equals("")) {
                        CCommon.Console.WriteLine最後から一行前に追加(Lang.CLang.GetConsole(Lang.CLang.EConsole.MDX_ExceptionOnPlay));
                        CCommon.Console.WriteLine最後から一行前に追加(ExceptionLog);
                        MXDRV.CCommon.Log.WriteLine(ExceptionLog);
                        CCommon.AudioThread.Music_Free();
                        PlayList = new CPlayList(null, "");
                    }
                }

                var Status = CCommon.AudioThread.Music_GetStatusText();
                MainWindow.StatusBarContent.Content = Status;
                if (!Status.Equals("")) {
                    MoonLib.CNoSleep.Exec();
                }

                CCommon.Console.WriteLine最後から一行前に追加(CCommon.AudioThread.GetIntLog());

                var mdxex = CCommon.AudioThread.Music_GetCanIgnoreExceptionStack();
                if (mdxex != null) {
                    Debug.WriteLine("CanIgnoreException.Caption:" + mdxex.Caption);
                    Debug.WriteLine("CanIgnoreException.Detail:" + mdxex.Detail);
                    CCommon.AudioThread.Music_Free();
                    PlayList.Clear();
                }

                if (CCommon.AudioThread.Music_isEOF()) {
                    var MD5 = PlayList.Next();
                    if (MD5.Equals("")) {
                        CCommon.AudioThread.Music_Free();
                    } else {
                        foreach (var Line in MDXPlay(MD5)) {
                            CCommon.Console.WriteLine(Line);
                        }
                        CCommon.Console.WriteLine("A>");
                    }
                }
            }

            IntervalTimer.Interval = TimeSpan.FromSeconds(0.1);
            IntervalTimer.Start();
        }

        public static List<string> MDXPlay_CallFromFlacOut(string MD5, bool WASAPIOutput = false) { return MDXPlay(MD5, WASAPIOutput); } // FlacOut以外からは呼ばない
        private static List<string> MDXPlay(string MD5, bool WASAPIOutput = true) {
            var res = new List<string>();

            CCommon.AudioThread.Music_Free();

            var Zip = CCommon.MDXOnlineClient.HttpGetCompositeZip(MD5);
            if (Zip == null) { return new List<string> { "Download failed." }; }

            if (MXDRV.CPDXHQBos.isPDX_bos_pdx(Zip.Settings.PCM0MD5)) {
                if (!MXDRV.CPDXHQBos.Voices_Load()) {
                    if (!System.IO.File.Exists(MXDRV.CPDXHQBos.BosPdxHQFilename)) {
                        var buf = CCommon.MDXOnlineClient.HttpGet_GetBosPdxHQ();
                        using (var wfs = new System.IO.StreamWriter(MXDRV.CPDXHQBos.BosPdxHQFilename)) {
                            wfs.BaseStream.Write(buf);
                        }
                    }
                    if (MXDRV.CPDXHQBos.Voices_Load()) {
                        res.Add(MXDRV.CPDXHQBos.BosPdxHQFilename + " " + Lang.CLang.GetBoot(Lang.CLang.EBoot.PDXHQBos_Loaded));
                    }
                }
            }

            CCommon.AudioThread.Music_Load(Zip, WASAPIOutput);

            foreach (var Line in CCommon.AudioThread.Music_GetInfo()) {
                res.Add(Line);
            }

            var Exception = CCommon.AudioThread.Music_GetException();
            if (!Exception.Equals("")) {
                res.Add(Lang.CLang.GetConsole(Lang.CLang.EConsole.MDX_Exception1));
                res.Add(Lang.CLang.GetConsole(Lang.CLang.EConsole.MDX_Exception2));
                res.Add(Exception);
            }

            FileSelector.FileList_SetCursorToFileMD5(MD5);

            return (res);
        }

        private static void ExecCommand(bool Echo, string Line) {
            if (Echo) { CCommon.Console.Echo(Line); }

            var res = CProgram.CmdLine.Execute(CCommon.MDXOnlineClient, Line);
            CCommon.Console.WriteLine(res.Lines);
            if (res.ParamStr.ToLower().Equals("toggle")) {
                switch (res.ParentCommand) {
                    case CCmdLine.CExecuteRes.EParentCommand.Visual: res.ParamStr = (Visual.Visibility == Visibility.Visible) ? "OFF" : "ON"; break;
                    case CCmdLine.CExecuteRes.EParentCommand.FileSel: res.ParamStr = (FileSelector.Visibility == Visibility.Visible) ? "OFF" : "ON"; break;
                }
            }
            switch (res.ParentCommand) {
                case CCmdLine.CExecuteRes.EParentCommand.NOP: break;
                case CCmdLine.CExecuteRes.EParentCommand.Switch:
                    if (CCommon.SwitchXWindow != null) { CCommon.SwitchXWindow.Close(); }
                    CCommon.SwitchXWindow = new WSwitchX(MainWindow);
                    CCommon.SwitchXWindow.Show();
                    break;
                case CCmdLine.CExecuteRes.EParentCommand.ChangeDir:
                    var FolderID = int.Parse(res.ParamStr);
                    var cdres = CCommon.MDXOnlineClient.HttpGetFolder(FolderID);
                    if (!cdres.Equals("")) {
                        CCommon.Console.WriteLine(cdres);
                    } else {
                        CCommon.INI.CurrentFolderID = FolderID;
                        FileSelector.UpdateFileList();
                    }
                    break;
                case CCmdLine.CExecuteRes.EParentCommand.CloseApp: MainWindow.Close(); break;
                case CCmdLine.CExecuteRes.EParentCommand.Visual:
                    switch (res.ParamStr.ToLower()) {
                        case "off": Visual.Visibility = Visibility.Collapsed; break;
                        case "on": Visual.Visibility = Visibility.Visible; break;
                        default: CCommon.Console.WriteLine(Lang.CLang.GetCommand(Lang.CLang.ECommand.ParamError)); break;
                    }
                    break;
                case CCmdLine.CExecuteRes.EParentCommand.FileSel:
                    switch (res.ParamStr.ToLower()) {
                        case "off": FileSelector.Visibility = Visibility.Collapsed; break;
                        case "on": FileSelector.Visibility = Visibility.Visible; break;
                        default: CCommon.Console.WriteLine(Lang.CLang.GetCommand(Lang.CLang.ECommand.ParamError)); break;
                    }
                    break;
                case CCmdLine.CExecuteRes.EParentCommand.OpenFolder: {
                    var url = CCommon.MDXOnlineClient.GetBrowserURL_GetFolder(CCommon.MDXOnlineClient.CurrentFolder.ID, CCommon.INI.LangMode.ToString());
                    if (WOpenBrowser.許可を得る必要がある()) {
                        var OpenFolderWindow = new WOpenBrowser(url);
                        OpenFolderWindow.Owner = MainWindow;
                        OpenFolderWindow.ShowDialog();
                        if (OpenFolderWindow.許可) { WOpenBrowser.Exec(url); }
                    } else {
                        WOpenBrowser.Exec(url);
                    }
                }
                break;
                case CCmdLine.CExecuteRes.EParentCommand.OpenDocs: {
                    var ArchiveID = CCommon.AudioThread.Music_GetArchiveID();
                    if (ArchiveID.Equals("")) {
                        CCommon.Console.WriteLine(Lang.CLang.GetCommand(CLang.ECommand.OpenDocs_NotOpened));
                    } else {
                        var url = CCommon.MDXOnlineClient.GetBrowserURL_GetDocsHTML(ArchiveID, CCommon.INI.LangMode.ToString());
                        if (WOpenBrowser.許可を得る必要がある()) {
                            var OpenDocsWindow = new WOpenBrowser(url);
                            OpenDocsWindow.Owner = MainWindow;
                            OpenDocsWindow.ShowDialog();
                            if (OpenDocsWindow.許可) { WOpenBrowser.Exec(url); }
                        } else {
                            WOpenBrowser.Exec(url);
                        }
                    }
                }
                break;
                case CCmdLine.CExecuteRes.EParentCommand.ChangeFont: {
                    FileSelector.UpdateFont();
                }
                break;
                case CCmdLine.CExecuteRes.EParentCommand.SetFunc: {
                    // コマンドにカンマを含められるように自前で分解する
                    MainWindow.SetMenuFunc(res.ParamStr);
                }
                break;
                case CCmdLine.CExecuteRes.EParentCommand.MDXPlay: {
                    PlayList = new CPlayList(null, "");

                    var MD5 = res.ParamStr;
                    if (MD5.Equals("")) {
                        CCommon.Console.WriteLine(Lang.CLang.GetCommand(Lang.CLang.ECommand.CantLoadMDX));
                    } else {
                        foreach (var _Line in MDXPlay(MD5)) {
                            CCommon.Console.WriteLine(_Line);
                        }
                        PlayList = new CPlayList(CCommon.MDXOnlineClient.CurrentFolder.GetAllMD5s(), MD5);
                    }
                }
                break;
                case CCmdLine.CExecuteRes.EParentCommand.MDXStop: {
                    PlayList = new CPlayList(null, "");
                    CCommon.AudioThread.Music_Free();
                }
                break;
                case CCmdLine.CExecuteRes.EParentCommand.PlayMode:
                    if (!res.ParamStr.Equals("")) {
                        var f = false;
                        foreach (var mode in new CPlayList.EPlayMode[] { CPlayList.EPlayMode.Single, CPlayList.EPlayMode.Repeat, CPlayList.EPlayMode.Normal, CPlayList.EPlayMode.Random }) {
                            if (mode.ToString().Equals(res.ParamStr, StringComparison.OrdinalIgnoreCase)) {
                                f = true;
                                CPlayList.PlayMode = mode;
                            }
                        }
                        if (!f) { CCommon.Console.WriteLine(Lang.CLang.GetCommand(Lang.CLang.ECommand.PlayMode_Undef) + " " + res.ParamStr); }
                    }
                    CCommon.Console.WriteLine(CPlayList.PlayMode.ToString());
                    break;
                case CCmdLine.CExecuteRes.EParentCommand.PlayNext: {
                    var MD5 = PlayList.Next();
                    if (MD5.Equals("")) {
                        CCommon.Console.WriteLine(Lang.CLang.GetCommand(Lang.CLang.ECommand.PlayMode_Empty));
                        CCommon.AudioThread.Music_Free();
                    } else {
                        foreach (var _Line in MDXPlay(MD5)) {
                            CCommon.Console.WriteLine(_Line);
                        }
                    }
                }
                break;
                case CCmdLine.CExecuteRes.EParentCommand.FlacOut: {
                    MainWindow.Activate();
                    PlayList = new CPlayList(null, "");
                    CCommon.AudioThread.Music_Free();
                    if (CCommon.AudioThread.Settings.LoopCount == 0) {
                        CCommon.Console.WriteLine(CLang.GetFlacOut(CLang.EFlacOut.InfinityLoop));
                    } else {
                        if (!System.IO.File.Exists(CCommon.FlacExeFilename)) {
                            CCommon.Console.WriteLine(CLang.GetFlacOut(CLang.EFlacOut.NotFoundFlacExe));
                        } else {
                            var FlacOutWindow = new WFlacOut(res.ParamStr);
                            FlacOutWindow.Owner = MainWindow;
                            FlacOutWindow.ShowDialog();
                        }
                    }
                }
                break;
            }

            CCommon.Console.WriteLine("A>");
        }

    }
}
