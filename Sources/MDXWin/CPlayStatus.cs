using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MDXWin {
    public class CPlayStatus {
        private static Object LockObj = new Object();

        public class CItem {
            public class CSoundLevel {
                public bool UseLog;
                public double[] Values = new double[MusDriver.CCommon.ChannelsCount];
            }
            public System.DateTime PresentDT;
            public bool SoundLevelUseLog;
            public CSoundLevel SoundLevel = new();
            public double AudioCPULoad;
            public MusDriver.CCommon.CVisualGlobal VisualGlobal;
            public MusDriver.CCommon.CVisualPart[] VisualParts = new MusDriver.CCommon.CVisualPart[16];
            public float[] SpeanaLeft, SpeanaCenter, SpeanaRight;

            public class COscilloscope {
                public float[] Samples;
            }
            public COscilloscope[] Oscilloscope = new COscilloscope[16];

            public CItem() {
                for (var ch = 0; ch < Oscilloscope.Length; ch++) {
                    Oscilloscope[ch] = new();
                }
            }
        }

        private static List<CItem> Items = new List<CItem>();

        public static void Clear() {
            lock (LockObj) {
                Items.Clear();
            }
        }

        public static void Append(CItem item) {
            lock (LockObj) {
                Items.Add(item);
            }
        }

        private class CBackup {
            public double SoundLevel = 0;

            public int LastNoteNum = -1;
            public double LastNoteNumFine = 0;
            public double FMPPMD_LastNoteNumVolume = 0;
        }

        public static CItem GetBefore(System.DateTime TargetDT) {
            var Targets = new List<CItem>();
            lock (LockObj) {
                foreach (var Item in Items) {
                    if (Item.PresentDT < TargetDT) { Targets.Add(Item); }
                }

                foreach (var Item in Targets) {
                    Items.Remove(Item);
                }
            }

            const int chs = 16;

            var AudioCPULoad = 0d;
            var AudioCPULoadCount = 0;

            var Backups = new CBackup[chs];
            for (var ch = 0; ch < chs; ch++) {
                Backups[ch] = new();
            }

            var Osillos = new List<CPlayStatus.CItem.COscilloscope[]>();

            CItem Status = null;

            foreach (var Item in Targets) {
                Status = Item;
                AudioCPULoad += Item.AudioCPULoad;
                AudioCPULoadCount++;
                for (var ch = 0; ch < chs; ch++) {
                    if (Backups[ch].SoundLevel < Item.SoundLevel.Values[ch]) { Backups[ch].SoundLevel = Item.SoundLevel.Values[ch]; }
                    if (Status.VisualParts[ch] != null) {
                        if (Status.VisualParts[ch].LastNoteNum != -1) {
                            Backups[ch].LastNoteNum = Status.VisualParts[ch].LastNoteNum;
                            Backups[ch].LastNoteNumFine = Status.VisualParts[ch].LastNoteNumFine;
                            if (Backups[ch].FMPPMD_LastNoteNumVolume < Status.VisualParts[ch].FMPPMD_LastNoteNumVolume) {
                                Backups[ch].FMPPMD_LastNoteNumVolume = Status.VisualParts[ch].FMPPMD_LastNoteNumVolume;
                            }
                        }
                    }
                }
                Osillos.Add(Item.Oscilloscope);
            }

            if (Status != null) {
                Status.AudioCPULoad = AudioCPULoad / AudioCPULoadCount;

                for (var ch = 0; ch < chs; ch++) {
                    Status.SoundLevel.Values[ch] = Backups[ch].SoundLevel;
                    if (Status.VisualParts[ch] != null) {
                        Status.VisualParts[ch].LastNoteNum = Backups[ch].LastNoteNum;
                        Status.VisualParts[ch].LastNoteNumFine = Backups[ch].LastNoteNumFine;
                        Status.VisualParts[ch].FMPPMD_LastNoteNumVolume = Backups[ch].FMPPMD_LastNoteNumVolume;
                    }
                }

                if (!CCommon.VisualOscillo) {
                    Status.Oscilloscope = new CPlayStatus.CItem.COscilloscope[chs];
                    for (var ch = 0; ch < chs; ch++) {
                        Status.Oscilloscope[ch] = null;
                    }
                } else {
                    var TotalSamplesCount = 0;
                    foreach (var Osillo in Osillos) {
                        if (Osillo[0].Samples != null) {
                            TotalSamplesCount += Osillo[0].Samples.Length;
                        }
                    }

                    Status.Oscilloscope = new CPlayStatus.CItem.COscilloscope[chs];
                    for (var ch = 0; ch < chs; ch++) {
                        Status.Oscilloscope[ch] = new();
                        Status.Oscilloscope[ch].Samples = new float[TotalSamplesCount];
                        var wofs = 0;
                        foreach (var Osillo in Osillos) {
                            if (Osillo[0].Samples != null) {
                                Array.Copy(Osillo[ch].Samples, 0, Status.Oscilloscope[ch].Samples, wofs, Osillo[ch].Samples.Length);
                                wofs += Osillo[ch].Samples.Length;
                            }
                        }
                    }
                }
            }

            return Status;
        }

        public class COscilloscope_DetectZeroCross_res {
            public enum EMode { サンプル数が足りない, ゼロポイントが見つからない, 音量が小さすぎる, 検出 }
            public EMode Mode;
            public float MinLevel, MaxLevel;
            public int ZeroPointIndex;
            public COscilloscope_DetectZeroCross_res(EMode _Mode) {
                Mode = _Mode;
            }
            public COscilloscope_DetectZeroCross_res(float _MinLevel, float _MaxLevel, int _ZeroPointIndex) {
                Mode = COscilloscope_DetectZeroCross_res.EMode.検出;
                MinLevel = _MinLevel;
                MaxLevel = _MaxLevel;
                ZeroPointIndex = _ZeroPointIndex;
            }
        }
        public static COscilloscope_DetectZeroCross_res Oscilloscope_DetectZeroCross(float[] Samples, int 最小残りサンプル数) {
            var cnt = Samples.Length - 最小残りサンプル数;
            if (cnt <= 0) { return new COscilloscope_DetectZeroCross_res(COscilloscope_DetectZeroCross_res.EMode.サンプル数が足りない); }

            var MinLevel = 1f;
            var MaxLevel = 0f;
            foreach (var Sample in Samples) {
                var Level = System.Math.Abs(Sample);
                if (Level < MinLevel) { MinLevel = Level; }
                if (MaxLevel < Level) { MaxLevel = Level; }
            }

            if (MaxLevel < 0.0005) { return new COscilloscope_DetectZeroCross_res(COscilloscope_DetectZeroCross_res.EMode.音量が小さすぎる); }

            var maxdiff = 0f;
            var ZeroPointIndex = -1;
            var LastSample = Samples[0];

            for (var idx = 1; idx < cnt; idx++) {
                var Sample = Samples[idx];
                if ((LastSample < 0) && (0 <= Sample)) {
                    var diff = Sample - LastSample;
                    if (maxdiff < diff) {
                        maxdiff = diff;
                        ZeroPointIndex = idx;
                    }
                }
                LastSample = Sample;
            }

            if (ZeroPointIndex != -1) { return new COscilloscope_DetectZeroCross_res(MinLevel, MaxLevel, ZeroPointIndex); }

            return new COscilloscope_DetectZeroCross_res(COscilloscope_DetectZeroCross_res.EMode.ゼロポイントが見つからない);
        }
    }
}
