using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MDXWin {
    internal class CCommon {
        private CCommon() { }

        public const string AppTitle = "MDXWin 雑なMXDRVエミュレータ";
        public const string AppAuthor = "by Moonlight";
        public const string AppVersion = "2023/12/14";
        public const string AppConsole = "MDXWin version " + AppVersion;

        public const string AutoexecIniFilename = @"autoexec.txt";

        public const string FlacExeFilename = "flac.exe";

        public static CINI INI = null;

        public const int BitRate_WaveFile = 24;
        public const int BitRate_WasapiOut = 32;
        public const int SurroundChannels = 6;

        public static CConsole Console;

        public static MoonLib.CCGROM CGROM = null; // ウィンドウのLoadedイベントが来るまでは使えない

        public static WSwitchX SwitchXWindow = null;

        private static int _FontSize_Console = 24;
        private static object FontSize_Console_LObj = new();
        public static int FontSize_Console { get { lock (FontSize_Console_LObj) { return (_FontSize_Console); } } set { lock (FontSize_Console_LObj) { _FontSize_Console = value; } } }

        private static int _FontSize_FileSel = 24;
        private static object FontSize_FileSel_LObj = new();
        public static int FontSize_FileSel { get { lock (FontSize_FileSel_LObj) { return (_FontSize_FileSel); } } set { lock (FontSize_FileSel_LObj) { _FontSize_FileSel = value; } } }

        private static double _VisualMul = 1;
        private static object VisualMul_LObj = new();
        public static double VisualMul { get { lock (VisualMul_LObj) { return (_VisualMul); } } set { lock (VisualMul_LObj) { _VisualMul = value; } } }

        private static bool _VisualSpeAna = true;
        private static object VisualSpeAna_LObj = new();
        public static bool VisualSpeAna { get { lock (VisualSpeAna_LObj) { return (_VisualSpeAna); } } set { lock (VisualSpeAna_LObj) { _VisualSpeAna = value; } } }

        private static bool _VisualOscillo = true;
        private static object VisualOscillo_LObj = new();
        public static bool VisualOscillo { get { lock (VisualOscillo_LObj) { return (_VisualOscillo); } } set { lock (VisualOscillo_LObj) { _VisualOscillo = value; } } }

        public static CAudioThread AudioThread;

        public static TimeSpan WaveBufferDuration = TimeSpan.FromSeconds(0.5);

        public static MDXOnline004.CClient MDXOnlineClient = null;

        public class CHDPISettings {
            public int FontSize_Console=24;
            public int FontSize_FileSel=24;
            public double VisualMul=1;
            public CHDPISettings() {
                if ((96 < MoonLib.CDPI.GetDpiX()) || (96 < MoonLib.CDPI.GetDpiY())) { // 高DPI時はデフォルトで大きめにする
                    FontSize_Console = 32;
                    FontSize_FileSel = 32;
                    VisualMul = 2;
                }
            }
        }
    }
}
