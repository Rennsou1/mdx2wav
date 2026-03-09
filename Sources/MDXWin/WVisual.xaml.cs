using MusDriver;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Tab;

namespace MDXWin {
    public partial class WVisual : Window {
        private bool WindowFullScreen = false;
        private double WindowMul = 1;

        private MoonLib.CDirectDraw DirectDraw = null;
        private System.Threading.Tasks.Task ThreadTask = null;

        private object Thread_DirectDrawLObj = new object();

        private object ThreadTerminateLObj = new object();
        private volatile bool _ThreadTerminate = false;
        public bool ThreadTerminate { get { lock (ThreadTerminateLObj) { return (_ThreadTerminate); } } set { lock (ThreadTerminateLObj) { _ThreadTerminate = value; } } }

        private object EnabledLObj = new object();
        private volatile bool _Enabled = false;
        private bool Enabled { get { lock (EnabledLObj) { return (_Enabled); } } set { lock (EnabledLObj) { _Enabled = value; } } }

        private const int ImgWidth = 960;
        private const int ImgHeight = 540;

        private const int ChHeight = 32;
        private const int ElementHeight = 28;

        private const int LeftPosX = 8; // 左側のエレメントの左端
        private const int LeftPosY = 8; // 左側のエレメントの上端

        private const int RightPosX = ImgWidth / 2; // 右側のエレメントの左端

        private const int SeekBarHeight = 16;

        private const int 右上曲情報X = ImgWidth - 6 - 140;

        private const int OscilloWidth = 100;

        private const int SpeanaY = 246 + 8;
        private const int SpeanaHeight = 96;

        private const int LeftBottomChsBarY = SpeanaY + SpeanaHeight + 8;
        private const int LeftBottomChsBarHeight = 108;

        private class CColors {
            public UInt32 Bright = 0xa0a0ff;
            public UInt32 BrightShadow = 0x505080;
            public UInt32 Dark = 0x6969a8;
            public UInt32 DarkShadow = 0x373758;
            public UInt32 Gray = 0xd0d0d0;
            public UInt32 GrayShadow = 0x707070;
            public UInt32 DataGray = 0x606060;
            public UInt32 DataGrayShadow = 0x303030;
            public UInt32 BarBright = 0x6464a0;
            public UInt32 BarFloat = 0x3c3c60;
            public UInt32 BarDark = 0x191928;
            public UInt32 Speana = 0x3c3c60;
            public UInt32 SeekBarBright = 0x5f5f98;
            public UInt32 SeekBarFloat = 0xa0a0ff;
            public UInt32 SeekBarDark = 0x2d2d48;
            public UInt32 OscilloscopeBright = 0xa0a0a0;
            public UInt32 OscilloscopeDark = 0x808080;

            public CColors() {
                var fn = @"MDXWin_Visual.ini";
                try {
                    if (System.IO.File.Exists(fn)) {
                        using (var rfs = new System.IO.StreamReader(fn)) {
                            while (!rfs.EndOfStream) {
                                var Items = rfs.ReadLine().Split('=');
                                if (Items.Length != 2) { continue; }
                                UInt32 Value;
                                try {
                                    Value = Convert.ToUInt32(Items[1], 16);
                                } catch (Exception ex) {
                                    Debug.WriteLine(Items[1] + " は16進数の文字列ではありません。 " + ex.ToString());
                                    continue;
                                }
                                switch (Items[0]) {
                                    case "Bright": Bright = Value; break;
                                    case "BrightShadow": BrightShadow = Value; break;
                                    case "Dark": Dark = Value; break;
                                    case "DarkShadow": DarkShadow = Value; break;
                                    case "Gray": Gray = Value; break;
                                    case "GrayShadow": GrayShadow = Value; break;
                                    case "DataGray": DataGray = Value; break;
                                    case "DataGrayShadow": DataGrayShadow = Value; break;
                                    case "BarBright": BarBright = Value; break;
                                    case "BarFloat": BarFloat = Value; break;
                                    case "BarDark": BarDark = Value; break;
                                    case "Speana": Speana = Value; break;
                                    case "SeekBarBright": SeekBarBright = Value; break;
                                    case "SeekBarFloat": SeekBarFloat = Value; break;
                                    case "SeekBarDark": SeekBarDark = Value; break;
                                    case "OscilloscopeBright": OscilloscopeBright = Value; break;
                                    case "OscilloscopeDark": OscilloscopeDark = Value; break;
                                    default: Debug.WriteLine("未知の項目です。 " + Items[0]); break;
                                }
                            }
                        }
                    }
                } catch (Exception ex) {
                    Debug.WriteLine("定義ファイル " + fn + " の読み込みに失敗しました。 " + ex.ToString());
                }
            }
        }
        private static CColors Colors = new CColors();

        public WVisual() {
            InitializeComponent();
            Title = "Visual                                     .";
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            VisNotes_Init();
            Speana_Init();
            OscilloLast_Init();

            IntervalTimerTick(null, null);

            IntervalTimer.Tick += new EventHandler(IntervalTimerTick);
            IntervalTimer.Interval = TimeSpan.FromSeconds(0.1);
            IntervalTimer.Start();
        }

        public void Stop() {
            IntervalTimer.Stop();

            if (ThreadTask != null) {
                ThreadTerminate = true;
                ThreadTask.Wait();
                ThreadTask = null;
            }

            if (DirectDraw != null) {
                DirectDraw.Free();
                DirectDraw = null;
            }
        }

        private object ReqClose_LObj = new object();
        private bool _ReqClose = false;
        public bool ReqClose { get { lock (ReqClose_LObj) { return (_ReqClose); } } set { lock (ReqClose_LObj) { _ReqClose = value; } } }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            if (!ReqClose) {
                e.Cancel = true;
                this.Visibility = Visibility.Collapsed;
            }
        }

        private void WinFormsPanel_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e) {
            var pos = new System.Windows.Point(e.X / WindowMul, e.Y / WindowMul);

            {
                const int ChWidth = 26;
                for (var ch = 0; ch < MusDriver.CCommon.ChannelsCount; ch++) {
                    var x = RightPosX + ((1 + ch) * ChWidth);
                    var y = LeftBottomChsBarY;

                    var w = ChWidth;
                    var h = LeftBottomChsBarHeight + 3 + 16; // パンポットの辺りまで

                    if ((x <= pos.X) && (pos.X < (x + w)) && (y <= pos.Y) && (pos.Y < (y + h))) {
                        switch (e.Button) {
                            case System.Windows.Forms.MouseButtons.Left:
                                CProgram.CmdLineMacro.Add("mxmute " + MusDriver.CCommon.ChannelMap.Substring(ch, 1));
                                break;
                            case System.Windows.Forms.MouseButtons.Middle: break;
                            case System.Windows.Forms.MouseButtons.Right:
                                CProgram.CmdLineMacro.Add("mxmute all");
                                break;
                        }
                    }
                }
            }

            if ((ImgHeight - (SeekBarHeight + 16)) <= pos.Y) { // シークバーより広い高さでマウスキャプチャする
                var per = pos.X / ImgWidth;
                var totalsecs = CCommon.AudioThread.Music_GetPlayTS().TotalSeconds;
                if (totalsecs != 0) {
                    CProgram.CmdLineMacro.Add("mxseek " + (totalsecs * per).ToString());
                }
            }
        }

        private UInt32 RGB2Color(UInt32 rgb) {
            return (rgb);
        }

        private int DrawElement(MoonLib.CRawBitmap bm, int x, int y, string Head1, string Head2, int HeadWidth, string Body, int BodyWidth, int BodyFontSize = 12, bool DrawBothLine = false) {
            if (DrawBothLine || (Body == null)) {
                var ds = new MoonLib.CCGROM.CDrawSettings(bm, 8, Colors.Dark, Colors.DarkShadow);
                ds.rawbm.FillRect(x, y, 2, 15, Colors.Bright);
                CCommon.CGROM.DrawString(ds, x + 6, y + 0, Head1);
                CCommon.CGROM.DrawString(ds, x + 6, y + 8, Head2);
            }
            var w = 6 + HeadWidth + 4;
            if (DrawBothLine || (Body != null)) {
                var ds = new MoonLib.CCGROM.CDrawSettings(bm, BodyFontSize, Colors.Bright, Colors.BrightShadow);
                CCommon.CGROM.DrawString(ds, x + w, y + 16 - BodyFontSize, Body);
            }
            w += BodyWidth;
            return (w);
        }

        private class CVisNote {
            const int Width = RightPosX - 16;
            const int NoteNumCount = 96;
            const int DrawWidth = 3;
            byte[] NoteBM = new byte[Width];
            byte[] FineBM = new byte[Width];
            public CVisNote() {
            }
            public void Clear() {
                for (var x = 0; x < Width; x++) {
                    NoteBM[x] = 0x00;
                    FineBM[x] = 0x00;
                }
            }
            public void DrawNote(double NoteNum, double NoteNumFine, byte Level) {
                var NoteX = (int)(NoteNum / NoteNumCount * Width);
                var FineX = (int)(NoteNumFine / NoteNumCount * Width);

                if ((0 <= NoteX) && ((NoteX + DrawWidth) < Width)) {
                    for (var x = 0; x < DrawWidth; x++) {
                        if (NoteBM[NoteX + x] < Level) {
                            NoteBM[NoteX + x] = Level;
                        }
                    }
                }
                if ((0 <= FineX) && ((FineX + DrawWidth) < Width)) {
                    for (var x = 0; x < DrawWidth; x++) {
                        if (FineBM[FineX + x] < Level) {
                            FineBM[FineX + x] = Level;
                        }
                    }
                }
            }
            public void Fade() {
                for (var x = 0; x < Width; x++) {
                    {
                        var v = NoteBM[x];
                        if (v != 0) { NoteBM[x] = (byte)((v >> 1) + (v >> 2)); } // *= 0.75
                    }
                    {
                        var v = FineBM[x];
                        if (v != 0) { FineBM[x] = (byte)((v >> 1) + (v >> 2)); } // *= 0.75
                    }
                }
            }
            public void DrawBM(MoonLib.CRawBitmap bm, int y) {
                bm.WVisual_CVisNote_DrawBM_Add(y, Width, NoteBM, FineBM);
            }
        }
        private CVisNote[] VisNotes = null;
        private void VisNotes_Init() {
            if (VisNotes == null) {
                VisNotes = new CVisNote[MusDriver.CCommon.ChannelsCount];
                for (var ch = 0; ch < VisNotes.Length; ch++) {
                    VisNotes[ch] = new CVisNote();
                }
            }

            for (var ch = 0; ch < VisNotes.Length; ch++) {
                VisNotes[ch].Clear();
            }
        }

        private class CSpeana {
            public class CBody {
                public const int BitmapWidth = MoonLib.CFFT.BandsCount;
                float[] Samples = new float[MoonLib.CFFT.SamplesCount];
                float[] Bands = new float[MoonLib.CFFT.BandsCount];
                public void Store(int SampleRate, float[] SrcSamples) {
                    var StoreSamplesCount = SrcSamples.Length * MoonLib.CFFT.CalcSampleRate / SampleRate;
                    if (Samples.Length < StoreSamplesCount) { StoreSamplesCount = Samples.Length; }
                    for (var idx = 0; idx < (Samples.Length - StoreSamplesCount); idx++) {
                        Samples[idx] = Samples[StoreSamplesCount + idx];
                    }
                    var wofs = Samples.Length - StoreSamplesCount;
                    for (var idx = 0; idx < StoreSamplesCount; idx++) {
                        Samples[wofs + idx] = SrcSamples[idx * SampleRate / MoonLib.CFFT.CalcSampleRate];
                    }
                }
                public void ExecFFT() {
                    MoonLib.CFFT.Exec(Samples, Bands);
                }
                private float GetBand(int idx) {
                    var res = Bands[idx];
                    res *= 1 + ((float)idx / Bands.Length);
                    return res;
                }
                public void Draw(MoonLib.CRawBitmap bm, int x, int y) {
                    var muls = new float[5] { 0.5f, 0.75f, 1f, 0.75f, 0.5f };

                    for (var idx = 0; idx < Bands.Length - 1; idx++) {
                        var Level = 0f;
                        var Count = 0f;
                        for (var avg = 0; avg < 5; avg++) {
                            var idx2 = idx + (avg - 1);
                            if ((0 <= idx2) && (idx2 < (Bands.Length - 1))) {
                                var mul = muls[avg];
                                Level += GetBand(idx2) * mul;
                                Count += mul;
                            }
                        }
                        Level /= Count;
                        Level *= 1.25f;
                        Level *= SpeanaHeight;
                        if (Level < 0) { Level = 0; }
                        if (SpeanaHeight < Level) { Level = SpeanaHeight; }
                        bm.Draw縦棒(x + idx, y + (int)(SpeanaHeight - Level), (int)Level, Colors.Speana);
                    }
                }
            }
            private CBody BodyLeft = new CBody();
            private CBody BodyCenter = new CBody();
            private CBody BodyRight = new CBody();
            public void Store(int SampleRate, float[] LeftBuf, float[] CenterBuf, float[] RightBuf) {
                BodyLeft.Store(SampleRate, LeftBuf);
                BodyCenter.Store(SampleRate, CenterBuf);
                BodyRight.Store(SampleRate, RightBuf);
            }
            public void ExecFFT() {
                BodyLeft.ExecFFT();
                BodyCenter.ExecFFT();
                BodyRight.ExecFFT();
            }
            public void Draw(MoonLib.CRawBitmap bm, int y) {
                var ds8 = new MoonLib.CCGROM.CDrawSettings(bm, 8, Colors.Dark, Colors.DarkShadow);

                int x;

                x = RightPosX;
                CCommon.CGROM.DrawString(ds8, x + 128 - CCommon.CGROM.MeasureString(8, "L").Width, y + 8, "L");
                BodyLeft.Draw(bm, x, y);
                x = RightPosX + 153;
                CCommon.CGROM.DrawString(ds8, x + 128 - CCommon.CGROM.MeasureString(8, "Speana.C").Width, y + 8, "Speana.C");
                BodyCenter.Draw(bm, x, y);
                x = 右上曲情報X;
                CCommon.CGROM.DrawString(ds8, x + 128 - CCommon.CGROM.MeasureString(8, "R").Width, y + 8, "R");
                BodyRight.Draw(bm, x, y);
            }
        }
        private CSpeana Speana = new CSpeana();
        private void Speana_Init() {
            Speana = new CSpeana();
        }

        private int[,] OscilloLast = new int[MusDriver.CCommon.ChannelsCount, OscilloWidth];
        private void OscilloLast_Init() {
            for (var ch = 0; ch < MusDriver.CCommon.ChannelsCount; ch++) {
                for (var idx = 0; idx < OscilloWidth; idx++) {
                    OscilloLast[ch, idx] = -1;
                }
            }
        }

        private void DrawStatus(MoonLib.CRawBitmap bm, CResources Resources, CPlayStatus.CItem Status, CAudioThread.CMusic_GetMDXPDXFilenameTitleRes FilenameTitle = null) {
            Resources.BaseBM.CopyTo(bm);

            var FileFormat = MusDriver.CDriver.EFileFormat.Unknown;
            if (Status != null) { FileFormat = Status.VisualGlobal.FileFormat; }
            if (FilenameTitle != null) { FileFormat = FilenameTitle.FileFormat; }

            { // 左側チャネル詳細
                for (var ch = 0; ch < MusDriver.CCommon.ChannelsCount; ch++) {
                    var x = LeftPosX;
                    var y = LeftPosY + (ch * ChHeight);
                    var VisNoteY = y;
                    if (Status == null) { // チャネル名
                        var ds8 = new MoonLib.CCGROM.CDrawSettings(bm, 8, Colors.Bright, Colors.BrightShadow);
                        var ds12 = new MoonLib.CCGROM.CDrawSettings(bm, 12, Colors.Bright, Colors.BrightShadow);
                        var chstr = MusDriver.CCommon.ChannelMap.Substring(ch, 1);
                        CCommon.CGROM.DrawString(ds8, x, y + 0, MusDriver.CCommon.GetChannelName(FileFormat, ch));
                        CCommon.CGROM.DrawString(ds8, x, y + 8, "Trk.");
                        CCommon.CGROM.DrawString(ds12, x + 32, y + 16 - 12 - 1, chstr);
                    }
                    x += 32 + 12 + 4;
                    switch (FileFormat) {
                        case CDriver.EFileFormat.MXDRV: {
                            if (Status != null) {// オシロスコープ
                                if ((Status.VisualParts[ch] != null) && (Status.Oscilloscope[ch] != null)) {
                                    var Samples = Status.Oscilloscope[ch].Samples;
                                    if (Samples != null) {
                                        for (var idx = 0; idx < OscilloWidth; idx++) {
                                            var px = x + idx;
                                            var py = OscilloLast[ch, idx];
                                            if (py != -1) {
                                                bm.SetPixel(px, py, Colors.OscilloscopeDark);
                                            }
                                        }
                                        var ZeroCross = CPlayStatus.Oscilloscope_DetectZeroCross(Samples, OscilloWidth);
                                        switch (ZeroCross.Mode) {
                                            case CPlayStatus.COscilloscope_DetectZeroCross_res.EMode.サンプル数が足りない: break;
                                            case CPlayStatus.COscilloscope_DetectZeroCross_res.EMode.ゼロポイントが見つからない: break;
                                            case CPlayStatus.COscilloscope_DetectZeroCross_res.EMode.音量が小さすぎる:
                                                for (var idx = 0; idx < OscilloWidth; idx++) {
                                                    var px = x + idx;
                                                    var py = y + (ChHeight / 2);
                                                    OscilloLast[ch, idx] = py;
                                                    bm.SetPixel(px, py, Colors.OscilloscopeBright);
                                                }
                                                break;
                                            case CPlayStatus.COscilloscope_DetectZeroCross_res.EMode.検出:
                                                var mul = 1 / ZeroCross.MaxLevel;
                                                if (20 < mul) { mul = 20; }
                                                for (var idx = 0; idx < OscilloWidth; idx++) {
                                                    var px = x + idx;
                                                    var py = y + (ChHeight / 2);
                                                    var Level = ChHeight / 2 * (-Samples[ZeroCross.ZeroPointIndex + idx] * mul);
                                                    if (Level < -(ChHeight / 2)) { Level = -(ChHeight / 2); }
                                                    if ((ChHeight / 2) < Level) { Level = ChHeight / 2; }
                                                    py += (int)Level;
                                                    OscilloLast[ch, idx] = py;
                                                    bm.SetPixel(px, py, Colors.OscilloscopeBright);
                                                }
                                                break;
                                        }
                                    }
                                }
                            }
                            x += OscilloWidth + 4;
                        }
                        break;
                        case CDriver.EFileFormat.FMP: x += 16; break;
                        case CDriver.EFileFormat.PMD: x += 16; break;
                        case CDriver.EFileFormat.Unknown: default: break;
                    }
                    if (Status != null) { // 詳細情報
                        if (Status.VisualParts[ch] != null) {
                            var st = Status.VisualParts[ch];
                            var ds8 = new MoonLib.CCGROM.CDrawSettings(bm, 8, Colors.Gray, Colors.GrayShadow);
                            CCommon.CGROM.DrawString(ds8, x, y, st.Text1);
                            y += 8;

                            var _x = x;

                            var ds8d = new MoonLib.CCGROM.CDrawSettings(bm, 8, RGB2Color(Colors.DataGray), RGB2Color(Colors.DataGrayShadow));
                            switch (FileFormat) {
                                case MusDriver.CDriver.EFileFormat.Unknown: break;
                                case MusDriver.CDriver.EFileFormat.MXDRV:
                                    CCommon.CGROM.DrawString(ds8, x, y, st.Text2.Substring(0, 18));
                                    x += 8 * 18;
                                    CCommon.CGROM.DrawString(ds8d, x, y, st.Text2.Substring(18, 13));
                                    x += 8 * 13;
                                    CCommon.CGROM.DrawString(ds8, x, y, st.Text2.Substring(18 + 13));
                                    break;
                                case MusDriver.CDriver.EFileFormat.FMP:
                                    CCommon.CGROM.DrawString(ds8, x, y, st.Text2.Substring(0, 28));
                                    x += 8 * 28;
                                    CCommon.CGROM.DrawString(ds8d, x, y, st.Text2.Substring(28));
                                    break;
                                case MusDriver.CDriver.EFileFormat.PMD:
                                    CCommon.CGROM.DrawString(ds8, x, y, st.Text2.Substring(0, 23));
                                    x += 8 * 23;
                                    CCommon.CGROM.DrawString(ds8d, x, y, st.Text2.Substring(23));
                                    break;
                            }
                            y += 8;

                            x = _x;
                            CCommon.CGROM.DrawString(ds8, x, y, st.Text3);
                        }
                    }
                    if (Status != null) { // 音程音調音量グラフィック
                        if (Status.VisualParts[ch] != null) {
                            VisNotes[ch].Fade();
                            if (Status.VisualParts[ch].KeyOnFlag) {
                                int Level;
                                if (Status.SoundLevel.UseLog) {
                                    Level = (int)(System.Math.Log(1 + Status.SoundLevel.Values[ch] * 200000, 2000) * 0xff);
                                } else {
                                    Level = (int)((0.5 + Status.SoundLevel.Values[ch] / 2) * 0xff);
                                }
                                if (0xff < Level) { Level = 0xff; }
                                VisNotes[ch].DrawNote(Status.VisualParts[ch].LastNoteNum, Status.VisualParts[ch].LastNoteNumFine, (byte)Level);
                            }
                            VisNotes[ch].DrawBM(bm, VisNoteY + 16);
                        }
                    }
                }
            }

            var ElementY = 4 + 32;
            var ElementDivWidth = 153;

            { // 右上システム情報
                var x = RightPosX;
                var y = ElementY;
                if (Status == null) {
                    DrawElement(bm, x, y, "Use", "Driver", 8 * 6, MusDriver.CDriver.GetDriverName(FileFormat), 12 * 6, 12, true);
                }
                DrawElement(bm, x + ElementDivWidth, y, "Play", "Mode", 8 * 8, (Status == null) ? null : CPlayList.PlayMode.ToString(), 0);
                y += ElementHeight;
                {
                    DrawElement(bm, x, y, "Audio", "CPU Load", 8 * 8, (Status == null) ? null : ((Status.AudioCPULoad * 100).ToString("F0") + "%").PadLeft(3), 12 * 3);
                    DrawElement(bm, x + ElementDivWidth, y, "Visual", "FPS", 8 * 8, (Status == null) ? null : Resources.Fps.LastCount.ToString().PadLeft(2), 12 * 2);
                }
                y += ElementHeight;
                {
                    if (Status == null) {
                        var str = "";
                        if (FileFormat != CDriver.EFileFormat.Unknown) { str = FilenameTitle.ReplayGain.ToString("F2").PadLeft(5); }
                        DrawElement(bm, x, y, "Replay", "Gain(dB)", 8 * 8, str, 12 * 5, 12, true);
                    }
                    DrawElement(bm, x + ElementDivWidth, y, "Target", "Gain(dB)", 8 * 8, (Status == null) ? null : CCommon.AudioThread.Settings.VolumeDB.ToString("F2").PadLeft(5), 12 * 5);
                }
                y += ElementHeight;
                DrawElement(bm, x, y, "Render", "Format", 8 * 6, (Status == null) ? null : CCommon.AudioThread.Settings.GetRenderFormat(Status.VisualGlobal.FileFormat), 0);
                y += ElementHeight;
                if (Status == null) {
                    if (FileFormat != CDriver.EFileFormat.Unknown) {
                        DrawElement(bm, x, y, "", "", 0, null, 0, 16, false);
                        string str;
                        switch (FileFormat) {
                            case CDriver.EFileFormat.MXDRV:
                                var tmppath = System.IO.Path.GetDirectoryName(FilenameTitle.Path);
                                MDXOnline004.CFolderTag FolderTag = null;
                                while (true) {
                                    var tmptag = new MDXOnline004.CFolderTag(tmppath);
                                    if (!tmptag.TitleJpnEng.Equals("")) {
                                        FolderTag = tmptag;
                                        break;
                                    }
                                    tmppath = System.IO.Path.GetDirectoryName(tmppath);
                                    if ((tmppath == null) || tmppath.Equals(@"\")) { break; }
                                }
                                if (FolderTag == null) { FolderTag = new MDXOnline004.CFolderTag(System.IO.Path.GetDirectoryName(FilenameTitle.Path)); }
                                str = FolderTag.GetTextFromFolderTag(false);
                                break;
                            case CDriver.EFileFormat.FMP:
                            case CDriver.EFileFormat.PMD:
                            case CDriver.EFileFormat.Unknown:
                            default:
                                str = FilenameTitle.Path; break;
                        }
                        var BodyFontSize = 16;
                        var ds = new MoonLib.CCGROM.CDrawSettings(bm, BodyFontSize, Colors.Bright, Colors.BrightShadow);
                        var w = 6 + 0 + 4;
                        var lim = 40 * 8;
                        if (lim < CCommon.CGROM.MeasureString(16, str).Width) {
                            for (var loop = 0; loop < 1000; loop++) {
                                if (CCommon.CGROM.MeasureString(16, str).Width <= lim) { break; }
                                str = str.Substring(0, str.Length - 1);
                            }
                        }
                        CCommon.CGROM.DrawString(ds, x + w, y + 16 - BodyFontSize, str);
                    } else {
                        DrawElement(bm, x, y, "", "", 0, null, 0, 16, false);
                    }
                }
                y += ElementHeight;
                if (Status == null) {
                    var str = System.IO.Path.GetFileName(FilenameTitle.Path);
                    if (30 < str.Length) { str = str.Substring(0, 30); }
                    DrawElement(Resources.BaseBM, x, y, "Music", "Filename", 8 * 8, str, 0, 16, true);
                }
                y += ElementHeight;
                if (Status == null) {
                    string ffstr;
                    switch (FileFormat) {
                        case CDriver.EFileFormat.MXDRV: ffstr = "PDX"; break;
                        case CDriver.EFileFormat.FMP: ffstr = "PVI"; break;
                        case CDriver.EFileFormat.PMD: ffstr = "PCM"; break;
                        case CDriver.EFileFormat.Unknown: default: ffstr = ""; break;
                    }
                    var fnstr = FilenameTitle.PCMCaption;
                    if (30 < fnstr.Length) { fnstr = fnstr.Substring(0, 30); }
                    DrawElement(Resources.BaseBM, x, y, ffstr, "Filename", 8 * 8, fnstr, 0, 16, true);
                }
                y += ElementHeight;
                if (Status == null) {
                    DrawElement(Resources.BaseBM, x, y, "Mus", "Ttl", 8 * 3, FilenameTitle.Title, 0, 16, true);
                }
            }

            { // 右上曲情報
                var x = 右上曲情報X;
                var y = ElementY;
                var ElementW = 64;
                DrawElement(bm, x, y, "Digital", "Clock", ElementW, (Status == null) ? null : System.DateTime.Now.ToString("HH:mm"), 12 * 5);
                y += ElementHeight;
                DrawElement(bm, x, y, "Passed", "Time", ElementW, (Status == null) ? null : Status.VisualGlobal.CurrentTS.ToString(@"mm\:ss"), 12 * 5);
                y += ElementHeight;
                if (Status == null) {
                    var str = "";
                    if (FileFormat != CDriver.EFileFormat.Unknown) {
                        if (FilenameTitle.PlayTS == System.TimeSpan.FromTicks(0)) {
                            str = "Error";
                        } else {
                            str = FilenameTitle.PlayTS.ToString(@"mm\:ss");
                        }
                    }
                    DrawElement(bm, x, y, "Loop", "Time", ElementW, str, 12 * 5, 12, true);
                }
                y += ElementHeight;
                if (Status == null) {
                } else {
                    DrawElement(bm, x, y, Status.VisualGlobal.ExtInfo1_Tag1, Status.VisualGlobal.ExtInfo1_Tag2, ElementW, (Status == null) ? null : Status.VisualGlobal.ExtInfo1_Value.ToString().PadLeft(5), 12 * 5, 12, true);
                }
                y += ElementHeight;
                if (Status == null) {
                } else {
                    DrawElement(bm, x, y, Status.VisualGlobal.ExtInfo2_Tag1, Status.VisualGlobal.ExtInfo2_Tag2, ElementW, (Status == null) ? null : Status.VisualGlobal.ExtInfo2_Value.ToString().PadLeft(5), 12 * 5, 12, true);
                }
                y += ElementHeight;
                DrawElement(bm, x, y, "Total", "Clock", ElementW, (Status == null) ? null : Status.VisualGlobal.TotalClock.ToString().PadLeft(5), 12 * 5);
                y += ElementHeight;
                {
                    string str = null;
                    if (Status != null) {
                        if (Status.VisualGlobal.Fadeout == 1) {
                            str = ((1 + Status.VisualGlobal.LoopCount).ToString() + "/" + CCommon.AudioThread.Settings.LoopCount.ToString()).PadLeft(5);
                        } else {
                            str = ("F." + (Status.VisualGlobal.Fadeout * 100).ToString("F0") + "%").PadLeft(5);
                        }
                    }
                    DrawElement(bm, x, y, "Loop", "Count", ElementW, str, 12 * 5);
                }
                y += ElementHeight;
            }

            var SpeAna = CCommon.VisualSpeAna;

            if ((Status != null) && SpeAna) { // 右中スペアナ
                Speana.Store(CCommon.AudioThread.Settings.SampleRate, Status.SpeanaLeft, Status.SpeanaCenter, Status.SpeanaRight);
                Speana.ExecFFT();
                Speana.Draw(bm, SpeanaY);
            }

            { // 右下チャネル情報
                const int ChWidth = 26;
                var ds8 = new MoonLib.CCGROM.CDrawSettings(bm, 8, Colors.Gray, Colors.GrayShadow);
                for (var ch = -1; ch < MusDriver.CCommon.ChannelsCount; ch++) {
                    var x = RightPosX + ((1 + ch) * ChWidth);
                    int y;
                    int BarHeight, BarHeightM1;

                    if (SpeAna) { // スペアナ有無によって高さを変える（ミュート切り替えマウス範囲はそのまま）
                        y = LeftBottomChsBarY;
                        BarHeight = LeftBottomChsBarHeight / 2;
                        BarHeightM1 = BarHeight - 1;
                    } else {
                        y = SpeanaY;
                        BarHeight = (LeftBottomChsBarHeight + SpeanaHeight + 8) / 2;
                        BarHeightM1 = BarHeight - 1;
                    }

                    if (ch != -1) {
                        if (Status != null) {
                            int max;
                            if (Status.SoundLevel.UseLog) {
                                max = (int)(System.Math.Log(1 + Status.SoundLevel.Values[ch] * 8000, 80) * BarHeight);
                            } else {
                                max = (int)(Status.SoundLevel.Values[ch] * BarHeight);
                            }
                            var slf = Resources.SoundLevelFloat[ch];
                            if (slf.Level < max) {
                                slf.Level = max;
                                slf.Speed = -1;
                            } else {
                                slf.Speed += 0.05;
                                if (0 < slf.Speed) {
                                    slf.Level -= slf.Speed;
                                }
                            }
                            var slfl = (int)Resources.SoundLevelFloat[ch].Level;
                            if (BarHeightM1 < slfl) { slfl = BarHeightM1; }
                            for (var idx = 0; idx < BarHeight; idx++) {
                                var padx = 3;
                                var col = Colors.BarDark;
                                if ((BarHeightM1 - max) < idx) { col = Colors.BarBright; }
                                if (idx == (BarHeightM1 - slfl)) { col = Colors.BarFloat; }
                                bm.Draw横棒(x + padx, y + (idx * 2), ChWidth - (padx * 2), col);
                            }
                        }
                    }

                    y += (BarHeight * 2) + 3;
                    if (ch == -1) {
                        if (Status == null) {
                            var padx = (ChWidth - (8 * 3)) / 2;
                            CCommon.CGROM.DrawString(ds8, x + padx, y + 0, "PAN");
                            CCommon.CGROM.DrawString(ds8, x + padx, y + 8, "L/R");
                            CCommon.CGROM.DrawString(ds8, x + padx, y + 18, "PRG");
                            CCommon.CGROM.DrawString(ds8, x + padx, y + 27, "KEY");
                        }
                    } else {
                        var pan = 0;
                        var prg = -1;
                        var key = -1;
                        if (Status != null) {
                            if (Status.VisualParts[ch] != null) {
                                pan = Status.VisualParts[ch].Panpot;
                                prg = Status.VisualParts[ch].Program;
                                key = Status.VisualParts[ch].KeyCode;
                            }
                        }
                        if (MusDriver.CCommon.GetMuteCh(ch)) { pan = 0; }
                        {
                            var w = Resources.PanpotBM.Width / 4;
                            var h = Resources.PanpotBM.Height;
                            var padx = (ChWidth - w) / 2;
                            var pansrcx = w * pan;
                            var srcrect = new Int32Rect(pansrcx, 0, w, h);
                            bm.Blit(x + padx, y, Resources.PanpotBM, srcrect);
                        }
                        if (Status != null) {
                            var padx = (ChWidth - (8 * 3)) / 2;
                            CCommon.CGROM.DrawString(ds8, x + padx, y + 18, ((prg == -1) ? "---" : prg.ToString().PadLeft(3)));
                            CCommon.CGROM.DrawString(ds8, x + padx, y + 27, ((key == -1) ? "   " : key.ToString().PadLeft(3)));
                        }
                    }
                }
            }

            { // 下側シークバー
                var PlayPos = 0;
                if (Status != null) {
                    var PlayTS = CCommon.AudioThread.Music_GetPlayTS();
                    if (PlayTS != System.TimeSpan.FromTicks(0)) {
                        PlayPos = (int)(Status.VisualGlobal.CurrentTS / PlayTS * ImgWidth);
                    }
                }
                if (PlayPos < 0) { PlayPos = 0; }
                if ((ImgWidth - 1) < PlayPos) { PlayPos = ImgWidth - 1; }
                for (var x = 0; x < ImgWidth; x += 2) {
                    bm.Draw縦棒(x, ImgHeight - SeekBarHeight, SeekBarHeight, (x < PlayPos) ? Colors.SeekBarBright : Colors.SeekBarDark);
                }
                bm.Draw縦棒(PlayPos, ImgHeight - SeekBarHeight, SeekBarHeight, Colors.SeekBarFloat);
            }
        }

        private string LastMD5 = "";

        private static string _MDXTitle = "";
        private static object MDXTitle_LObj = new();
        private static string MDXTitle { get { lock (MDXTitle_LObj) { var res = _MDXTitle; _MDXTitle = ""; return (res); } } set { lock (MDXTitle_LObj) { _MDXTitle = value; } } }

        private void UpdateBaseBM(MoonLib.CRawBitmap bm, CResources Resources) {
            var MD5 = CCommon.AudioThread.Music_GetMD5();
            if (LastMD5.Equals(MD5)) { return; }
            LastMD5 = MD5;

            var info = CCommon.AudioThread.Music_GetMDXPDXFilenameTitle();

            Resources.VisualBGBM.CopyTo(Resources.BaseBM);
            DrawStatus(Resources.BaseBM, Resources, null, info);

            MDXTitle = System.IO.Path.GetFileName(info.Path) + " " + info.Title;

            VisNotes_Init();
            Speana_Init();
            OscilloLast_Init();
        }

        private class CResources {
            public MoonLib.CRawBitmap VisualBGBM, PanpotBM;

            public MoonLib.CRawBitmap BaseBM; // 更新しない部分

            public class CSoundLevelFloat {
                public double Level = 0;
                public double Speed = 0;
            }
            public CSoundLevelFloat[] SoundLevelFloat = new CSoundLevelFloat[MusDriver.CCommon.ChannelsCount];

            public class CFps {
                public int LastCount = 0;
                public System.DateTime TimeoutDT = System.DateTime.FromBinary(0);
                public int Count = 0;
                public double AudioCPULoad = 0;
                public double AudioCPULoadCurrent = 0;
                public void Update(double StatusAudioCPULoad) {
                    Count++;
                    if (AudioCPULoad < StatusAudioCPULoad) { AudioCPULoad = StatusAudioCPULoad; }
                    if (TimeoutDT < System.DateTime.Now) {
                        LastCount = Count;
                        Count = 0;
                        TimeoutDT = System.DateTime.Now + System.TimeSpan.FromSeconds(1);
                        AudioCPULoadCurrent = AudioCPULoad;
                        AudioCPULoad = 0;
                    }
                }
            }
            public CFps Fps = new CFps();

            public CResources() {
                for (var ch = 0; ch < SoundLevelFloat.Length; ch++) {
                    SoundLevelFloat[ch] = new CSoundLevelFloat();
                }
            }
        }

        private bool ReqRedraw = true;

        private void ThreadExec() {
            try {
                var Resources = new CResources();

                Resources.VisualBGBM = null;
                if (System.IO.File.Exists("MDXWin_VisualBG.jpg")) {
                    try {
                        Resources.VisualBGBM = new MoonLib.CRawBitmap(new Uri("MDXWin_VisualBG.jpg", UriKind.Relative));
                        if ((Resources.VisualBGBM != null) && (Resources.VisualBGBM.Width != ImgWidth)) { Resources.VisualBGBM = null; }
                        if ((Resources.VisualBGBM != null) && (Resources.VisualBGBM.Height != ImgHeight)) { Resources.VisualBGBM = null; }
                    } catch {
                        Resources.VisualBGBM = null;
                    }
                }
                if (Resources.VisualBGBM == null) { Resources.VisualBGBM = new MoonLib.CRawBitmap(ImgWidth, ImgHeight); }

                Resources.PanpotBM = new MoonLib.CRawBitmap(new Uri("pack://application:,,,/Resources/Panpot.png"));
                Resources.BaseBM = new MoonLib.CRawBitmap(ImgWidth, ImgHeight);

                {
                    var bgbm = Resources.VisualBGBM;
                    var ds8 = new MoonLib.CCGROM.CDrawSettings(bgbm, 8, Colors.Dark, Colors.DarkShadow);
                    var ds16 = new MoonLib.CCGROM.CDrawSettings(bgbm, 16, Colors.Dark, Colors.DarkShadow);
                    var ds24 = new MoonLib.CCGROM.CDrawSettings(bgbm, 24, Colors.Bright, Colors.BrightShadow);
                    CCommon.CGROM.DrawString(ds24, RightPosX, 4, "MDXWin");
                    CCommon.CGROM.DrawString(ds16, RightPosX + (12 * 7) + 4, 4 + 5, CCommon.AppTitle.Substring(7));
                    var meas = CCommon.CGROM.MeasureString(16, CCommon.AppTitle.Substring(7));
                    if (!CCommon.CGROM.isLoaded()) {
                        var ds10 = new MoonLib.CCGROM.CDrawSettings(bgbm, 13, Colors.Dark, Colors.DarkShadow);
                        CCommon.CGROM.DrawString(ds10, RightPosX + (12 * 7) + 4 + meas.Width + 5, 4 + 0, Lang.CLang.GetBoot(Lang.CLang.EBoot.CGROM_NotFoundForVisual1));
                        CCommon.CGROM.DrawString(ds10, RightPosX + (12 * 7) + 4 + meas.Width + 5, 4 + 13, Lang.CLang.GetBoot(Lang.CLang.EBoot.CGROM_NotFoundForVisual2));
                    } else {
                        CCommon.CGROM.DrawString(ds8, 右上曲情報X + 6, 4 + 3, CCommon.AppVersion);
                        CCommon.CGROM.DrawString(ds8, 右上曲情報X + 6, 4 + 12, CCommon.AppAuthor);
                    }
                }

                Resources.VisualBGBM.CopyTo(Resources.BaseBM);
                DrawStatus(Resources.BaseBM, Resources, null, new CAudioThread.CMusic_GetMDXPDXFilenameTitleRes());

                VisNotes_Init();
                Speana_Init();
                OscilloLast_Init();

                var bm = new MoonLib.CRawBitmap(ImgWidth, ImgHeight);

                while (true) {
                    System.Threading.Thread.Sleep(1);

                    if (ThreadTerminate) { break; }

                    if (!Enabled) { continue; }

                    var Status = CPlayStatus.GetBefore(System.DateTime.Now);
                    if (Status == null) {
                        lock (Thread_DirectDrawLObj) {
                            if (ReqRedraw) {
                                ReqRedraw = false;
                                DirectDraw.UpdateFrame(Resources.BaseBM.GetRawBuffer());
                            }
                        }
                        continue;
                    }

                    Resources.Fps.Update(Status.AudioCPULoad);
                    Status.AudioCPULoad = Resources.Fps.AudioCPULoadCurrent;

                    UpdateBaseBM(bm, Resources);

                    DrawStatus(bm, Resources, Status);

                    lock (Thread_DirectDrawLObj) {
                        DirectDraw.UpdateFrame(bm.GetRawBuffer());
                    }
                }
            } catch (Exception ex) {
                Debug.WriteLine("WVisual Exception: " + ex.Message + "\n" + ex.StackTrace);
            }
        }

        private System.Windows.Threading.DispatcherTimer IntervalTimer = new System.Windows.Threading.DispatcherTimer();
        private void IntervalTimerTick(object sender, EventArgs e) {
            Enabled = this.Visibility == Visibility.Visible;

            if (Enabled) {
                var DispWidth = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width;
                var DispHeight = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height;

                var SrcWidth = ImgWidth;
                var SrcHeight = ImgHeight;

                if (WindowFullScreen) {
                    this.WindowStyle = WindowStyle.None;
                    this.WindowState = WindowState.Maximized;
                    var mx = (double)DispWidth / SrcWidth;
                    var my = (double)DispHeight / SrcHeight;
                    WindowMul = (mx < my) ? mx : my;
                } else {
                    this.WindowStyle = WindowStyle.SingleBorderWindow;
                    this.WindowState = WindowState.Normal;
                    WindowMul = CCommon.VisualMul;
                }

                var WinWidth = SrcWidth * WindowMul;
                var WinHeight = SrcHeight * WindowMul;

                if (WindowFullScreen) {
                    MainGrid.Width = DispWidth / MoonLib.CDPI.GetRatioX();
                    MainGrid.Height = DispHeight / MoonLib.CDPI.GetRatioY();
                } else {
                    MainGrid.Width = WinWidth / MoonLib.CDPI.GetRatioX();
                    MainGrid.Height = WinHeight / MoonLib.CDPI.GetRatioY();
                }

                lock (Thread_DirectDrawLObj) {
                    if ((DirectDraw == null) || (DirectDraw.DstWidth != (int)WinWidth) || (DirectDraw.DstHeight != (int)WinHeight)) {
                        if (DirectDraw != null) { DirectDraw.Free(); }
                        WinFormsHost.Width = WinWidth / MoonLib.CDPI.GetRatioX();
                        WinFormsHost.Height = WinHeight / MoonLib.CDPI.GetRatioX();
                        DirectDraw = new MoonLib.CDirectDraw(WinFormsPanel, SrcWidth, SrcHeight, (int)WinWidth, (int)WinHeight);
                    }
                }
                if (ThreadTask == null) {
                    ThreadTask = System.Threading.Tasks.Task.Run(ThreadExec);
                }

                var tmp = MDXTitle;
                if (!tmp.Equals("")) { Title = tmp + "                                     ."; }
            }
        }

        private void WinFormsPanel_Paint(object sender, System.Windows.Forms.PaintEventArgs e) {
            lock (Thread_DirectDrawLObj) {
                ReqRedraw = true;
            }
        }

        private void Window_KeyUp(object sender, System.Windows.Input.KeyEventArgs e) {
            switch (e.Key) {
                case Key.Space:
                    CProgram.CmdLineMacro.Add("PlayNext");
                    e.Handled = true;
                    break;
                case Key.Escape:
                    CProgram.CmdLineMacro.Add("MxStop");
                    e.Handled = true;
                    break;
                case Key.F:
                    WindowFullScreen = !WindowFullScreen;
                    break;
            }
        }

        private void WinFormsPanel_DoubleClick(object sender, EventArgs e) {
            WindowFullScreen = !WindowFullScreen;
        }
    }
}
