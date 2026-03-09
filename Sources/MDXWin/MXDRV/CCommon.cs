using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MXDRV {
    internal class CCommon {
        public static CLog Log = new CLog();

        public static X68SoundMCh.CAPI X68Sound;

        private static object _lock = new object();

        public static bool _古いMDXWinの音量テーブルを使う = false;
        public static bool 古いMDXWinの音量テーブルを使う {
            get { lock (_lock) { return (_古いMDXWinの音量テーブルを使う); } }
            set { lock (_lock) { _古いMDXWinの音量テーブルを使う = value; } }
        }

        public static float _PCM8Volume = 0;
        public static float PCM8Volume { get { lock (_lock) { return (_PCM8Volume); } } set { lock (_lock) { _PCM8Volume = value; } } }

        public static bool _UseBosPdxHQ = true;
        public static bool UseBosPdxHQ {
            get { lock (_lock) { return (_UseBosPdxHQ); } }
            set { lock (_lock) { _UseBosPdxHQ = value; } }
        }

        public static string GetChannelName(int ch) {
                switch (ch) {
                case 0:
                case 1:
                case 2:
                case 3:
                case 4:
                case 5:
                case 6:
                case 7: return "OPM";
                case 8:
                case 9:
                case 10:
                case 11:
                case 12:
                case 13:
                case 14:
                case 15: return "PCM";
                default: return "";
            }
        }

    }
}
