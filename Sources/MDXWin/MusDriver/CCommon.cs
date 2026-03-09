using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusDriver {
    public class CCommon {
        public const int ChannelsCount = 16;

        public enum EPCMSurroundMode { SimpleLR, PCM1, PCM8, Surround };

        public class OutputElements {
            public const int OPM = 0;
            public const int OPM_Count = 8;

            public const int PCM = OPM + OPM_Count;
            public const int PCM_Count = 8;

            public const int Surround = PCM + PCM_Count;
            public const int Surround_Count = 5;
            public const int FL = Surround + 0;
            public const int FR = Surround + 1;
            public const int C = Surround + 2;
            public const int SL = Surround + 3;
            public const int SR = Surround + 4;

            public const int Count = OPM_Count + PCM_Count + Surround_Count; // レンダリングチャンネル数
        }

        public static TimeSpan FadeoutTS = TimeSpan.FromSeconds(5);

        public const string ChannelMap = "ABCDEFGHPQRSTUVW";

        private static bool[] MuteChs = new bool[ChannelsCount];
        public static bool[] GetMuteChs() {
            lock (MuteChs) {
                var res = new bool[ChannelsCount];
                Array.Copy(MuteChs, res, MuteChs.Length);
                return (res);
            }
        }
        public static void SetMuteChs(bool[] fs) {
            lock (MuteChs) {
                Array.Copy(fs, MuteChs, MuteChs.Length);
            }
        }
        public static void SetMuteChsAll(bool f) {
            for (var ch = 0; ch < MuteChs.Length; ch++) {
                MuteChs[ch] = f;
            }
        }
        public static bool GetMuteCh(int ch) { lock (MuteChs) { return (MuteChs[ch]); } }
        public static void SetMuteCh(int ch, bool f) { lock (MuteChs) { MuteChs[ch] = f; } }


        public class CVisualGlobal {
            public CDriver.EFileFormat FileFormat;
            public System.TimeSpan CurrentTS;
            public string ExtInfo1_Tag1, ExtInfo1_Tag2;
            public int ExtInfo1_Value;
            public string ExtInfo2_Tag1, ExtInfo2_Tag2;
            public int ExtInfo2_Value;
            public int TotalClock;
            public int LoopCount;
            public double Fadeout;
            public bool isPDX_bos_pdx;
        }

        public class CVisualPart {
            public int Program;
            public bool KeyOnFlag;
            public int KeyCode;
            public int Panpot;
            public int LastNoteNum;
            public double LastNoteNumFine;
            public string Text1, Text2, Text3;
            public double FMPPMD_LastNoteNumVolume = -1;
        }

        public static string KeyCodeToString(int KeyCode, int Shift = 3) { // MDX=3, FMP=12, PMD=12.
            KeyCode += Shift;
            var oct = KeyCode / 12;
            var notes = new string[] { "c", "c+", "d", "d+", "e", "f", "f+", "g", "g+", "a", "a+", "b", };
            var note = notes[KeyCode % 12];
            return ("o" + oct + note);
        }

        public static int StringToKeyCode(string KeyCode, int Shift = 3) {
            for (var idx = 0; idx < 96; idx++) {
                if (KeyCode.Equals(CCommon.KeyCodeToString(idx, Shift), StringComparison.OrdinalIgnoreCase)) { return idx; }
            }
            return -1;
        }

        public static string IntToStrPlusMinusPadLeft(int v, int pad) {
            var res = System.Math.Abs(v).ToString().PadLeft(pad - 1);
            res = ((v < 0) ? "-" : "+") + res;
            return (res);
        }

        public static string ParseTitleString(string Title) {
            Title = Title.Replace("　", " ");

            for (var idx = 0; idx < 100; idx++) {
                if (Title.IndexOf("  ") == -1) { break; }
                Title = Title.Replace("  ", " ");
            }

            return Title.Trim();
        }

        public static string GetChannelName(CDriver.EFileFormat FileFormat, int ch) {
            switch (FileFormat) {
                case CDriver.EFileFormat.MXDRV: return MXDRV.CCommon.GetChannelName(ch);
                case CDriver.EFileFormat.FMP: return FMPPMD.CFMP.GetChannelName(ch);
                case CDriver.EFileFormat.PMD: return FMPPMD.CPMD.GetChannelName(ch);
                case CDriver.EFileFormat.Unknown:
                default: return "";
            }
        }

        public class CLastKeyOn { // FMPPMD用
            public int Count = -1;
            public double Volume = 0;
            public void KeyOn(int _Count, double _Volume) {
                if (Count == _Count) { return; }
                Count = _Count;
                Volume = _Volume;
            }
            public void KeyOff() { Count = -1; Volume = 0; }
            public double GetVolume() { return Volume; }
            public void Update() { Volume = Volume * 0.99; } // 減衰速度は呼び出し間隔依存
        }
    }
}
