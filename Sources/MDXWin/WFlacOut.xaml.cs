using Lang;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MDXWin {
    /// <summary>
    /// Interaction logic for WFlacOut.xaml
    /// </summary>
    public partial class WFlacOut : Window {
        private List<string> StackFiles = new List<string>();
        private int StackFilesIndex = 0;

        public WFlacOut(string MD5) {
            InitializeComponent();

            this.Title = "MDXWin FlacOut";

            CCommon.AudioThread.Music_Free();

            if (MD5.Equals("all", StringComparison.CurrentCultureIgnoreCase)) {
                foreach (var File in CCommon.MDXOnlineClient.CurrentFolder.Files) {
                    StackFiles.Add(File.MD5);
                }
            } else {
                StackFiles.Add(MD5);
            }

            IntervalTimer.Tick += new EventHandler(IntervalTimerTick);
            IntervalTimer.Interval = TimeSpan.FromTicks(1);
            IntervalTimer.Start();
        }

        private void Window_Closed(object sender, EventArgs e) {
            if (IntervalTimer != null) {
                IntervalTimer.Stop();
                IntervalTimer = null;
            }
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e) {
            CancelBtn.IsEnabled = false;
        }

        private class CTask : IDisposable {
            public string MD5;
            public string FlacFilename;
            public string WaveFilename;
            public CAudioThread.CThreadLoop_FileWriter_WriteSettings Settings;
            public TimeSpan PlayTS;
            public TimeSpan CurrentTS = System.TimeSpan.FromTicks(0);
            public void Dispose() {
                Settings.Dispose();
            }
        }
        private CTask Task = null;

        private System.Windows.Threading.DispatcherTimer IntervalTimer = new System.Windows.Threading.DispatcherTimer();
        private void IntervalTimerTick(object sender, EventArgs e) {
            if (Task == null) {
                if (StackFilesIndex == StackFiles.Count) {
                    CCommon.AudioThread.Music_Free();
                    CCommon.Console.WriteLine(CLang.GetFlacOut(CLang.EFlacOut.Completed));
                    this.Close();
                    return;
                }
                var MD5 = StackFiles[StackFilesIndex++];
                foreach (var Line in CProgram.MDXPlay_CallFromFlacOut(MD5)) {
                    CCommon.Console.WriteLine(Line);
                }
                CCommon.Console.WriteLine(CLang.GetFlacOut(CLang.EFlacOut.Render)+" " + StackFilesIndex + " / " + StackFiles.Count + " " + System.IO.Path.GetFileName(CCommon.AudioThread.Music_GetMDXPDXFilenameTitle().Path));

                Task = new CTask();
                Task.MD5 = MD5;
                Task.FlacFilename = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) + @"\" + System.IO.Path.GetFileName(CCommon.AudioThread.Music_GetMDXPDXFilenameTitle().Path) + ".flac";
                Task.WaveFilename = System.IO.Path.GetTempFileName() + ".wav";
                var EnableSurroundOutput = CCommon.AudioThread.Music_isMXDRV();
                Task.Settings = new CAudioThread.CThreadLoop_FileWriter_WriteSettings(EnableSurroundOutput,Task.WaveFilename);
                Task.PlayTS = (CCommon.AudioThread.Music_GetPlayTS() * CCommon.AudioThread.Settings.LoopCount) + MusDriver.CCommon.FadeoutTS;

                CCommon.Console.WriteLine(""); // 進行状況表示行
            }

            var isEnded = false;
            if (!CancelBtn.IsEnabled) {
                isEnded = true;
                IntervalTimer.Stop();
            } else {
                var timeout = System.DateTime.Now + System.TimeSpan.FromSeconds(0.1);
                while (!isEnded) {
                    var SamplesTS = CCommon.AudioThread.ThreadLoop_FileWriter(Task.Settings);
                    Task.CurrentTS += SamplesTS;
                    foreach(var Line in CCommon.AudioThread.GetIntLog()) {
                        CCommon.Console.WriteLine最後の行を置き換え(Line);
                        CCommon.Console.WriteLine(""); // 進行状況表示行
                    }
                    if (SamplesTS == System.TimeSpan.FromTicks(0)) { isEnded = true; }
                    if (timeout <= System.DateTime.Now) { break; }
                }
                CCommon.Console.WriteLine最後の行を置き換え(Task.CurrentTS.ToString(@"hh\:mm\:ss") + " / " + Task.PlayTS.ToString(@"hh\:mm\:ss") + " (" + (Task.CurrentTS / Task.PlayTS * 100).ToString("F0") + "%)");
            }

            if (isEnded) {
                var FlacTag = CCommon.AudioThread.Music_GetFlacTag();
                CCommon.AudioThread.Music_Free();

                CCommon.Console.WriteLine最後の行を置き換え(CLang.GetFlacOut(CLang.EFlacOut.Rendered));
                var FlacFilename = Task.FlacFilename;
                var WaveFilename = Task.WaveFilename;
                Task.Dispose();
                Task = null;

                if (!CancelBtn.IsEnabled) {
                    CCommon.Console.WriteLine(CLang.GetFlacOut(CLang.EFlacOut.Canceled));
                    System.IO.File.Delete(WaveFilename);
                    this.Close();
                    return;
                } else {
                    ConvertWaveToFlac(WaveFilename, FlacFilename, FlacTag);
                    System.IO.File.Delete(WaveFilename);
                }
            }
        }

        private void ConvertWaveToFlac(string WaveFilename, string FlacFilename, CAudioThread.CMusic_GetFlacTag_res FlacTag) {
            { // Flac圧縮
                var proc = new Process();
                proc.StartInfo.FileName = CCommon.FlacExeFilename;
                proc.StartInfo.ArgumentList.Add("--output-name=" + FlacFilename);
                proc.StartInfo.ArgumentList.Add(WaveFilename);
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.CreateNoWindow = true;
                proc.Start();
                proc.WaitForExit();
            }

            using (var flac = new FlacLibSharp.FlacFile(FlacFilename)) { // Flacタグ付与
                var comment = flac.VorbisComment;
                comment.Title.Value = FlacTag.Title;
                comment.Artist = new FlacLibSharp.VorbisCommentValues(FlacTag.Artist);
                comment.Album = new FlacLibSharp.VorbisCommentValues(FlacTag.Album);
                comment.Add("Lyricist", FlacTag.Lyricist);
                flac.Save();
            }
        }
    }
}
