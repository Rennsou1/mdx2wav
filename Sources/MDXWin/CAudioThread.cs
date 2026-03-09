using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using static MDXWin.CAudioThread;

namespace MDXWin {
    public class CAudioThread {
        private static object LObj = new object();

        private static NAudio.Wave.WaveFormatExtensible GetWaveFormat(int SampleRate, int BitRate, int Channels) {
            return (new NAudio.Wave.WaveFormatExtensible(SampleRate, BitRate, Channels));
        }

        public const bool EnableSurroundOutput = true;

        public CAudioThread() {
        }

        private List<string> IntLog = new List<string>();
        public List<string> GetIntLog() {
            lock (LObj) {
                var res = IntLog;
                IntLog = new List<string>();
                return (res);
            }
        }

        public class CStackPCM {
            private NAudio.Wave.BufferedWaveProvider WaveBuf;

            private System.IO.MemoryStream ms;
            private System.IO.BinaryWriter bw;

            public CStackPCM(NAudio.Wave.BufferedWaveProvider _WaveBuf) {
                WaveBuf = _WaveBuf;
                ms = new System.IO.MemoryStream();
                bw = new System.IO.BinaryWriter(ms);
            }
            public void WaveWrite(bool EnableSurroundOutput, float FL, float FR, float C, float LFE, float SL, float SR) {
                if (EnableSurroundOutput) {
                    switch (WaveBuf.WaveFormat.BitsPerSample) {
                        case 16: {
                            var mul = 1 << 15;
                            bw.Write((short)(FL * mul));
                            bw.Write((short)(FR * mul));
                            bw.Write((short)(C * mul));
                            bw.Write((short)(LFE * mul));
                            bw.Write((short)(SL * mul));
                            bw.Write((short)(SR * mul));
                        }
                        break;
                        case 24: {
                            var mul = 1 << 23;
                            bw.Write((int)(FL * mul));
                            bw.Write((int)(FR * mul));
                            bw.Write((int)(C * mul));
                            bw.Write((int)(LFE * mul));
                            bw.Write((int)(SL * mul));
                            bw.Write((int)(SR * mul));
                        }
                        break;
                        case 32:
                            bw.Write(FL);
                            bw.Write(FR);
                            bw.Write(C);
                            bw.Write(LFE);
                            bw.Write(SL);
                            bw.Write(SR);
                            break;
                        default: throw new Exception(Lang.CLang.GetConsole(Lang.CLang.EConsole.AudioThread_UndefBitDepth));
                    }
                } else {
                    switch (WaveBuf.WaveFormat.BitsPerSample) {
                        case 16: {
                            var mul = 1 << 15;
                            bw.Write((short)(FL * mul));
                            bw.Write((short)(FR * mul));
                        }
                        break;
                        case 24: {
                            var mul =1 << 23;
                            bw.Write((int)(FL * mul));
                            bw.Write((int)(FR * mul));
                        }
                        break;
                        case 32:
                            bw.Write(FL);
                            bw.Write(FR);
                            break;
                        default: throw new Exception(Lang.CLang.GetConsole(Lang.CLang.EConsole.AudioThread_UndefBitDepth));
                    }
                }
            }
            public void Flush() {
                if (ms.Position != 0) {
                    WaveBuf.AddSamples(ms.GetBuffer(), 0, (int)ms.Position);
                    ms.Position = 0;
                }
            }
            public int GetEmptySamplesCount() {
                if (ms.Position != 0) { throw new Exception(Lang.CLang.GetConsole(Lang.CLang.EConsole.AudioThread_StillHaveSamplesInBuffer)); }
                var res = (WaveBuf.BufferLength - WaveBuf.BufferedBytes) / WaveBuf.WaveFormat.BlockAlign;
                return (res);
            }
            public void ClearBuffer() {
                WaveBuf.ClearBuffer();
                ms.Position = 0;
            }
        }

        private volatile bool RequestTerminate = false;
        public void SetRequestTerminate() { lock (LObj) { RequestTerminate = true; } }

        private volatile bool RequestClearBuffer = false;
        public void SetRequestClearBuffer() { lock (LObj) { RequestClearBuffer = true; } }

        private void ThreadLoop() {
            try {
                NAudio.Wave.BufferedWaveProvider WaveBuf = null;
                NAudio.Wave.WasapiOut WaveOut = null;

                CStackPCM StackPCM = null;

                var OverflowVolume = 1f;

                while (true) {
                    lock (LObj) {
                        if (RequestTerminate) {
                            RequestTerminate = false;
                            break;
                        }
                        if (RequestClearBuffer) {
                            RequestClearBuffer = false;
                            StackPCM.ClearBuffer();
                            CPlayStatus.Clear();
                        }
                    }

                    int SampleRate;
                    System.TimeSpan SamplesTS;
                    int SamplesCount;
                    float[] buf;
                    MusDriver.CCommon.EPCMSurroundMode PCMSurroundMode;
                    float FadeoutAlpha;
                    int[] Panpots = new int[16];
                    var Status = new CPlayStatus.CItem();

                    var StartDT = System.DateTime.Now;

                    float VolumeMul;

                    lock (LObj) {
                        if (!Music_isLoaded()) { throw new Exception(Lang.CLang.GetConsole(Lang.CLang.EConsole.AudioThread_MDXNotLoad)); }

                        SampleRate = Settings.SampleRate;

                        if ((WaveBuf == null) || (WaveBuf.WaveFormat.SampleRate != SampleRate)) {
                            if (WaveOut != null) {
                                WaveOut.Stop();
                                WaveOut.Dispose();
                                WaveOut = null;
                            }

                            WaveBuf = new NAudio.Wave.BufferedWaveProvider(GetWaveFormat(SampleRate, CCommon.BitRate_WasapiOut, EnableSurroundOutput ? CCommon.SurroundChannels : 2));
                            WaveBuf.BufferDuration = CCommon.WaveBufferDuration;

                            StackPCM = new CStackPCM(WaveBuf);

                            WaveOut = new NAudio.Wave.WasapiOut();
                            WaveOut.Init(WaveBuf);
                        }

                        Status.PresentDT = System.DateTime.Now + System.TimeSpan.FromSeconds((double)WaveBuf.BufferedBytes / WaveBuf.WaveFormat.BlockAlign / SampleRate) + System.TimeSpan.FromSeconds(0.1); // サウンド遅延を計測したい。環境によって違う？

                        SamplesTS = Driver.GetSamplesTS();
                        SamplesCount = (int)(SampleRate * SamplesTS.TotalSeconds);
                        var EmptySamplesCount = StackPCM.GetEmptySamplesCount();
                        if (EmptySamplesCount < SamplesCount) { WaveOut.Play(); System.Threading.Thread.Sleep(1); continue; }

                        if (Driver.ExecNextClock_演奏が終了していたらTrueを返す()) {
                            if ((WaveOut != null) && (WaveBuf != null)) {
                                WaveOut.Play();
                                while (WaveBuf.BufferedBytes != 0) {
                                    System.Threading.Thread.Sleep(1);
                                }
                            }
                            continue;
                        }


                        buf = new float[SamplesCount * MusDriver.CCommon.OutputElements.Count];
                        PCMSurroundMode = Driver.GetPCM(SampleRate, buf, SamplesCount);

                        FadeoutAlpha = Driver.GetFadeoutAlpha();

                        for (var ch = 0; ch < 16; ch++) {
                            Panpots[ch] = Driver.GetPanpot(ch);
                        }

                        Status.VisualGlobal = Driver.GetVisualGlobal();
                        if (Status.VisualGlobal != null) {
                            for (var ch = 0; ch < 16; ch++) {
                                Status.VisualParts[ch] = Driver.GetVisualStatus(ch);

                                var Rate = SampleRate / 22050d; // オシロは22050Hzに縮小して表示する
                                switch (Status.VisualGlobal.FileFormat) {
                                    case MusDriver.CDriver.EFileFormat.MXDRV:
                                        var storecnt = (int)(SamplesCount / Rate);
                                        Status.Oscilloscope[ch].Samples = new float[storecnt];
                                        for (var idx = 0; idx < storecnt; idx++) {
                                            Status.Oscilloscope[ch].Samples[idx] = buf[(int)(idx * Rate) * MusDriver.CCommon.OutputElements.Count + ch];
                                        }
                                        break;
                                    case MusDriver.CDriver.EFileFormat.FMP:
                                    case MusDriver.CDriver.EFileFormat.PMD:
                                    case MusDriver.CDriver.EFileFormat.Unknown:
                                        Status.Oscilloscope[ch].Samples = null;
                                        break;
                                }
                            }
                        }

                        var ReplayGain = Driver.Settings.ReplayGain;
                        if (ReplayGain <= 0) { ReplayGain = 100; } // 例外が発生したファイルは、小さめの音量で再生する
                        if (ReplayGain <= 70) { ReplayGain = 70; } // 極端に音が小さかった曲でも、そんなには大きな音で再生しない
                        VolumeMul = (float)Math.Pow(10, (Settings.VolumeDB - ReplayGain) / 20) * OverflowVolume;
                    }

                    { // 音量検知
                        var VolumeMulForVisual = (Driver.Settings.ReplayGain == 0) ? 0 : Math.Pow(10, (90 - Driver.Settings.ReplayGain) / 20);
                        switch (Status.VisualGlobal.FileFormat) {
                            case MusDriver.CDriver.EFileFormat.MXDRV: Status.SoundLevel.UseLog = true; break;
                            case MusDriver.CDriver.EFileFormat.FMP: Status.SoundLevel.UseLog = false; break;
                            case MusDriver.CDriver.EFileFormat.PMD: Status.SoundLevel.UseLog = false; break;
                            case MusDriver.CDriver.EFileFormat.Unknown: Status.SoundLevel.UseLog = false; break;
                        }
                        for (var ch = 0; ch < 16; ch++) {
                            if (Status.VisualParts[ch] == null) {
                                Status.SoundLevel.Values[ch] = 0;
                                continue;
                            }
                            switch (Status.VisualGlobal.FileFormat) {
                                case MusDriver.CDriver.EFileFormat.MXDRV: {
                                    var sum = 0d;
                                    var last = buf[(0 * MusDriver.CCommon.OutputElements.Count) + ch];
                                    for (var idx = 1; idx < SamplesCount; idx++) {
                                        var sample = buf[(idx * MusDriver.CCommon.OutputElements.Count) + ch];
                                        sum += System.Math.Abs(last - sample);
                                        last = sample;
                                    }
                                    Status.SoundLevel.Values[ch] = (sum / (SamplesCount - 1)) * VolumeMulForVisual;
                                }
                                break;
                                case MusDriver.CDriver.EFileFormat.FMP:
                                case MusDriver.CDriver.EFileFormat.PMD:
                                    Status.SoundLevel.Values[ch] = Status.VisualParts[ch].FMPPMD_LastNoteNumVolume; break;
                                case MusDriver.CDriver.EFileFormat.Unknown: default: Status.SoundLevel.Values[ch] = 0; break;
                            }
                        }
                    }

                    var Overflow = 0f;

                    Status.SpeanaLeft = new float[SamplesCount];
                    Status.SpeanaCenter = new float[SamplesCount];
                    Status.SpeanaRight = new float[SamplesCount];

                    for (var idx = 0; idx < SamplesCount; idx++) {
                        var FL = 0f;
                        var FR = 0f;
                        var C = 0f;
                        var SL = 0f;
                        var SR = 0f;

                        var bufofs = idx * MusDriver.CCommon.OutputElements.Count;

                        if (PCMSurroundMode == MusDriver.CCommon.EPCMSurroundMode.SimpleLR) {
                            FL = buf[bufofs + MusDriver.CCommon.OutputElements.FL];
                            Status.SpeanaLeft[idx] += FL;
                            FR = buf[bufofs + MusDriver.CCommon.OutputElements.FR];
                            Status.SpeanaRight[idx] += FR;
                        } else {
                            // Panpot3（中央出力）は、左右から同じ音量の音が出るので、トータル音量が倍になる。

                            for (var ch = MusDriver.CCommon.OutputElements.OPM; ch < MusDriver.CCommon.OutputElements.OPM + MusDriver.CCommon.OutputElements.OPM_Count; ch++) {
                                var smp = buf[bufofs + ch];
                                var FrontBack = 0.1f + ((ch % 4) * 0.05f);
                                var LeftRight = 0f;
                                switch (Panpots[ch]) {
                                    case 0: smp = 0; break;
                                    case 1: LeftRight = 0f + ((ch % 4) * 0.05f); Status.SpeanaLeft[idx] += smp; break;
                                    case 2: LeftRight = 1f - ((ch % 4) * 0.05f); Status.SpeanaRight[idx] += smp; break;
                                    case 3: C += smp; LeftRight = 0.425f + (ch % 4) * 0.05f; Status.SpeanaCenter[idx] += smp * 2; break;
                                }
                                if (smp != 0) {
                                    FL += smp * (1 - LeftRight) * (1 - FrontBack);
                                    FR += smp * LeftRight * (1 - FrontBack);
                                    SL += smp * (1 - LeftRight) * FrontBack;
                                    SR += smp * LeftRight * FrontBack;
                                }
                            }

                            switch (PCMSurroundMode) {
                                case MusDriver.CCommon.EPCMSurroundMode.SimpleLR: { throw new Exception(); }
                                case MusDriver.CCommon.EPCMSurroundMode.PCM1: {
                                    var ch = MusDriver.CCommon.OutputElements.PCM;
                                    var smp = buf[bufofs + ch];
                                    var FrontBack = 0.1f;
                                    var LeftRight = 0f;
                                    switch (Panpots[ch]) {
                                        case 0: smp = 0; break;
                                        case 1: LeftRight = 0.25f; Status.SpeanaLeft[idx] += smp; break;
                                        case 2: LeftRight = 0.75f; Status.SpeanaRight[idx] += smp; break;
                                        case 3: LeftRight = 0.5f; smp *= 2; Status.SpeanaCenter[idx] += smp; break;
                                    }
                                    if (smp != 0) {
                                        FL += smp * (1 - LeftRight) * (1 - FrontBack);
                                        FR += smp * LeftRight * (1 - FrontBack);
                                        SL += smp * (1 - LeftRight) * FrontBack;
                                        SR += smp * LeftRight * FrontBack;
                                    }
                                }
                                break;
                                case MusDriver.CCommon.EPCMSurroundMode.PCM8: {
                                    for (var ch = MusDriver.CCommon.OutputElements.PCM; ch < MusDriver.CCommon.OutputElements.PCM + MusDriver.CCommon.OutputElements.PCM_Count; ch++) {
                                        var smp = buf[bufofs + ch];
                                        var FrontBack = 0.1f + ((ch % 4) * 0.1f);
                                        var LeftRight = 0f;
                                        switch (Panpots[ch]) {
                                            case 0: smp = 0; break;
                                            case 1: LeftRight = 0f + ((ch % 4) * 0.05f); Status.SpeanaLeft[idx] += smp; break;
                                            case 2: LeftRight = 1f - ((ch % 4) * 0.05f); Status.SpeanaRight[idx] += smp; break;
                                            case 3: LeftRight = 0.35f + (ch % 4) * 0.1f; smp *= 2; Status.SpeanaCenter[idx] += smp; break;
                                        }
                                        if (smp != 0) {
                                            FL += smp * (1 - LeftRight) * (1 - FrontBack);
                                            FR += smp * LeftRight * (1 - FrontBack);
                                            SL += smp * (1 - LeftRight) * FrontBack;
                                            SR += smp * LeftRight * FrontBack;
                                        }
                                    }
                                }
                                break;
                                case MusDriver.CCommon.EPCMSurroundMode.Surround: {
                                    FL += buf[bufofs + MusDriver.CCommon.OutputElements.FL];
                                    FR += buf[bufofs + MusDriver.CCommon.OutputElements.FR];
                                    C += buf[bufofs + MusDriver.CCommon.OutputElements.C];
                                    SL += buf[bufofs + MusDriver.CCommon.OutputElements.SL];
                                    SR += buf[bufofs + MusDriver.CCommon.OutputElements.SR];
                                    Status.SpeanaLeft[idx] += FL + SL;
                                    Status.SpeanaCenter[idx] += C;
                                    Status.SpeanaRight[idx] += SL + SR;
                                }
                                break;
                            }
                        }

                        {
                            var v = VolumeMul * FadeoutAlpha;
                            FL *= v;
                            FR *= v;
                            C *= v;
                            SL *= v;
                            SR *= v;
                        }

                        if (FL < -1) { if (Overflow < -FL) { Overflow = -FL; } FL = -1; }
                        if (1 < FL) { if (Overflow < FL) { Overflow = FL; } FL = 1; }
                        if (FR < -1) { if (Overflow < -FR) { Overflow = -FR; } FR = -1; }
                        if (1 < FR) { if (Overflow < FR) { Overflow = FR; } FR = 1; }
                        if (C < -1) { if (Overflow < -C) { Overflow = -C; } C = -1; }
                        if (1 < C) { if (Overflow < C) { Overflow = C; } C = 1; }
                        if (SL < -1) { if (Overflow < -SL) { Overflow = -SL; } SL = -1; }
                        if (1 < SL) { if (Overflow < SL) { Overflow = SL; } SL = 1; }
                        if (SR < -1) { if (Overflow < -SR) { Overflow = -SR; } SR = -1; }
                        if (1 < SR) { if (Overflow < SR) { Overflow = SR; } SR = 1; }

                        if (!EnableSurroundOutput) {
                            FL += (C / 2) + SL;
                            FR += (C / 2) + SR;
                        }

                        StackPCM.WaveWrite(EnableSurroundOutput, FL, FR, C, 0, SL, SR);
                    }

                    {
                        var v = VolumeMul * FadeoutAlpha;
                        for (var idx = 0; idx < SamplesCount; idx++) {
                            Status.SpeanaLeft[idx] *= v;
                            Status.SpeanaCenter[idx] *= v;
                            Status.SpeanaRight[idx] *= v;
                        }
                    }

                    StackPCM.Flush();

                    var EndDT = System.DateTime.Now;
                    Status.AudioCPULoad = (EndDT - StartDT) / SamplesTS;

                    CPlayStatus.Append(Status);

                    if (Overflow != 0) {
                        lock (LObj) {
                            OverflowVolume *= 1 / Overflow;
                            if (Music_isLoaded()) {
                                IntLog.Add(Driver.GetPlayTS() + " " + Lang.CLang.GetConsole(Lang.CLang.EConsole.AudioThread_SamplePeakClipped) + " OverflowVolume=" + (OverflowVolume * 100).ToString("F0") + "%");
                            }
                        }
                    }
                }

                if (WaveOut != null) {
                    WaveOut.Dispose();
                    WaveOut = null;
                }
            } catch (Exception ex) {
                Debug.WriteLine("CAudioThread Exception: " + ex.Message + "\n" + ex.StackTrace);
            }
        }

        public class CThreadLoop_FileWriter_WriteSettings : IDisposable {
            public bool EnableSurroundOutput;
            public string WaveFilename;
            public NAudio.Wave.WaveFileWriter WaveWriter;
            public float OverflowVolume = 1f;
            public CThreadLoop_FileWriter_WriteSettings(bool _EnableSurroundOutput, string _WaveFilename) {
                EnableSurroundOutput = _EnableSurroundOutput;
                WaveFilename = _WaveFilename;
                var wfex = GetWaveFormat(CCommon.AudioThread.Settings.SampleRate, CCommon.BitRate_WaveFile, EnableSurroundOutput ? CCommon.SurroundChannels : 2);
                WaveWriter = new NAudio.Wave.WaveFileWriter(WaveFilename, wfex);
            }

            public void Dispose() {
                WaveWriter.Dispose();
            }
        }
        public System.TimeSpan ThreadLoop_FileWriter(CThreadLoop_FileWriter_WriteSettings WriteSettings) { // コピペ
            try {
                int SampleRate;
                System.TimeSpan SamplesTS;
                int SamplesCount;
                float[] buf;
                MusDriver.CCommon.EPCMSurroundMode PCMSurroundMode;
                float FadeoutAlpha;
                int[] Panpots = new int[16];

                float VolumeMul;

                lock (LObj) {
                    if (!Music_isLoaded()) { throw new Exception(Lang.CLang.GetConsole(Lang.CLang.EConsole.AudioThread_MDXNotLoad)); }

                    SampleRate = Settings.SampleRate;

                    SamplesTS = Driver.GetSamplesTS();
                    SamplesCount = (int)(SampleRate * SamplesTS.TotalSeconds);

                    if (Driver.ExecNextClock_演奏が終了していたらTrueを返す()) { return System.TimeSpan.FromTicks(0); }

                    buf = new float[SamplesCount * MusDriver.CCommon.OutputElements.Count];
                    PCMSurroundMode = Driver.GetPCM(SampleRate, buf, SamplesCount);

                    FadeoutAlpha = Driver.GetFadeoutAlpha();

                    for (var ch = 0; ch < 16; ch++) {
                        Panpots[ch] = Driver.GetPanpot(ch);
                    }

                    var ReplayGain = Driver.Settings.ReplayGain;
                    if (ReplayGain <= 0) { ReplayGain = 100; } // 例外が発生したファイルは、小さめの音量で再生する
                    if (ReplayGain <= 70) { ReplayGain = 70; } // 極端に音が小さかった曲でも、そんなには大きな音で再生しない
                    VolumeMul = (float)Math.Pow(10, (Settings.VolumeDB - ReplayGain) / 20) * WriteSettings.OverflowVolume;
                }

                var Overflow = 0f;

                var outbuf = new float[SamplesCount * (WriteSettings. EnableSurroundOutput ? CCommon.SurroundChannels : 2)];

                for (var idx = 0; idx < SamplesCount; idx++) {
                    var FL = 0f;
                    var FR = 0f;
                    var C = 0f;
                    var SL = 0f;
                    var SR = 0f;

                    var bufofs = idx * MusDriver.CCommon.OutputElements.Count;

                    if (PCMSurroundMode == MusDriver.CCommon.EPCMSurroundMode.SimpleLR) {
                        FL = buf[bufofs + MusDriver.CCommon.OutputElements.FL];
                        FR = buf[bufofs + MusDriver.CCommon.OutputElements.FR];
                    } else {
                        // Panpot3（中央出力）は、左右から同じ音量の音が出るので、トータル音量が倍になる。

                        for (var ch = MusDriver.CCommon.OutputElements.OPM; ch < MusDriver.CCommon.OutputElements.OPM + MusDriver.CCommon.OutputElements.OPM_Count; ch++) {
                            var smp = buf[bufofs + ch];
                            var FrontBack = 0.1f + ((ch % 4) * 0.05f);
                            var LeftRight = 0f;
                            switch (Panpots[ch]) {
                                case 0: smp = 0; break;
                                case 1: LeftRight = 0f + ((ch % 4) * 0.05f); break;
                                case 2: LeftRight = 1f - ((ch % 4) * 0.05f); break;
                                case 3: C += smp; LeftRight = 0.425f + (ch % 4) * 0.05f; break;
                            }
                            if (smp != 0) {
                                FL += smp * (1 - LeftRight) * (1 - FrontBack);
                                FR += smp * LeftRight * (1 - FrontBack);
                                SL += smp * (1 - LeftRight) * FrontBack;
                                SR += smp * LeftRight * FrontBack;
                            }
                        }

                        switch (PCMSurroundMode) {
                            case MusDriver.CCommon.EPCMSurroundMode.SimpleLR: { throw new Exception(); }
                            case MusDriver.CCommon.EPCMSurroundMode.PCM1: {
                                var ch = MusDriver.CCommon.OutputElements.PCM;
                                var smp = buf[bufofs + ch];
                                var FrontBack = 0.1f;
                                var LeftRight = 0f;
                                switch (Panpots[ch]) {
                                    case 0: smp = 0; break;
                                    case 1: LeftRight = 0.25f; break;
                                    case 2: LeftRight = 0.75f; break;
                                    case 3: LeftRight = 0.5f; smp *= 2; break;
                                }
                                if (smp != 0) {
                                    FL += smp * (1 - LeftRight) * (1 - FrontBack);
                                    FR += smp * LeftRight * (1 - FrontBack);
                                    SL += smp * (1 - LeftRight) * FrontBack;
                                    SR += smp * LeftRight * FrontBack;
                                }
                            }
                            break;
                            case MusDriver.CCommon.EPCMSurroundMode.PCM8: {
                                for (var ch = MusDriver.CCommon.OutputElements.PCM; ch < MusDriver.CCommon.OutputElements.PCM + MusDriver.CCommon.OutputElements.PCM_Count; ch++) {
                                    var smp = buf[bufofs + ch];
                                    var FrontBack = 0.1f + ((ch % 4) * 0.1f);
                                    var LeftRight = 0f;
                                    switch (Panpots[ch]) {
                                        case 0: smp = 0; break;
                                        case 1: LeftRight = 0f + ((ch % 4) * 0.05f); break;
                                        case 2: LeftRight = 1f - ((ch % 4) * 0.05f); break;
                                        case 3: LeftRight = 0.35f + (ch % 4) * 0.1f; smp *= 2; break;
                                    }
                                    if (smp != 0) {
                                        FL += smp * (1 - LeftRight) * (1 - FrontBack);
                                        FR += smp * LeftRight * (1 - FrontBack);
                                        SL += smp * (1 - LeftRight) * FrontBack;
                                        SR += smp * LeftRight * FrontBack;
                                    }
                                }
                            }
                            break;
                            case MusDriver.CCommon.EPCMSurroundMode.Surround: {
                                FL += buf[bufofs + MusDriver.CCommon.OutputElements.FL];
                                FR += buf[bufofs + MusDriver.CCommon.OutputElements.FR];
                                C += buf[bufofs + MusDriver.CCommon.OutputElements.C];
                                SL += buf[bufofs + MusDriver.CCommon.OutputElements.SL];
                                SR += buf[bufofs + MusDriver.CCommon.OutputElements.SR];
                            }
                            break;
                        }
                    }

                    {
                        var v = VolumeMul * FadeoutAlpha;
                        FL *= v;
                        FR *= v;
                        C *= v;
                        SL *= v;
                        SR *= v;
                    }

                    if (FL < -1) { if (Overflow < -FL) { Overflow = -FL; } FL = -1; }
                    if (1 < FL) { if (Overflow < FL) { Overflow = FL; } FL = 1; }
                    if (FR < -1) { if (Overflow < -FR) { Overflow = -FR; } FR = -1; }
                    if (1 < FR) { if (Overflow < FR) { Overflow = FR; } FR = 1; }
                    if (C < -1) { if (Overflow < -C) { Overflow = -C; } C = -1; }
                    if (1 < C) { if (Overflow < C) { Overflow = C; } C = 1; }
                    if (SL < -1) { if (Overflow < -SL) { Overflow = -SL; } SL = -1; }
                    if (1 < SL) { if (Overflow < SL) { Overflow = SL; } SL = 1; }
                    if (SR < -1) { if (Overflow < -SR) { Overflow = -SR; } SR = -1; }
                    if (1 < SR) { if (Overflow < SR) { Overflow = SR; } SR = 1; }

                    if (!WriteSettings.EnableSurroundOutput) {
                        FL += (C / 2) + SL;
                        FR += (C / 2) + SR;
                    }

                    var outbufidx = idx * (WriteSettings.EnableSurroundOutput ? CCommon.SurroundChannels : 2);
                    if (WriteSettings.EnableSurroundOutput) {
                        outbuf[outbufidx + 0] = FL;
                        outbuf[outbufidx + 1] = FR;
                        outbuf[outbufidx + 2] = C;
                        outbuf[outbufidx + 3] = 0;
                        outbuf[outbufidx + 4] = SL;
                        outbuf[outbufidx + 5] = SR;
                    } else {
                        outbuf[outbufidx + 0] = FL;
                        outbuf[outbufidx + 1] = FR;
                    }
                }

                WriteSettings.WaveWriter.WriteSamples(outbuf, 0, outbuf.Length);

                if (Overflow != 0) {
                    lock (LObj) {
                        WriteSettings.OverflowVolume *= 1 / Overflow;
                        if (Music_isLoaded()) {
                            IntLog.Add(Driver.GetPlayTS() + " " + Lang.CLang.GetConsole(Lang.CLang.EConsole.AudioThread_SamplePeakClipped) + " OverflowVolume=" + (WriteSettings.OverflowVolume * 100).ToString("F0") + "%");
                        }
                    }
                }

                return SamplesTS;
            } catch (Exception ex) {
                Debug.WriteLine("CAudioThread Exception: " + ex.Message + "\n" + ex.StackTrace);
                return System.TimeSpan.FromTicks(0);
            }
        }

        public class CSettings {
            private static object LObj = new object();

            private float _VolumeDB = 85;
            public float VolumeDB { get { lock (LObj) { return (_VolumeDB); } } set { lock (LObj) { _VolumeDB = value; } } }

            private bool _OPMEnabled = true;
            public bool OPMEnabled { get { lock (LObj) { return (_OPMEnabled); } } set { lock (LObj) { _OPMEnabled = value; } } }

            private bool _PCMEnabled = true;
            public bool PCMEnabled { get { lock (LObj) { return (_PCMEnabled); } } set { lock (LObj) { _PCMEnabled = value; } } }

            private int _LoopCount = 2;
            public int LoopCount { get { lock (LObj) { return (_LoopCount); } } set { lock (LObj) { _LoopCount = value; } } }

            public enum EADPCMMode { 最近傍補間 = 0, 線形補間 = 1, 三次スプライン補間 = 2 };
            private EADPCMMode _ADPCMMode = EADPCMMode.三次スプライン補間;
            public EADPCMMode ADPCMMode { get { lock (LObj) { return (_ADPCMMode); } } set { lock (LObj) { _ADPCMMode = value; } } }
            public string GetADPCMModeStr() {
                var name = "undef";
                switch (ADPCMMode) {
                    case EADPCMMode.最近傍補間: name = Lang.CLang.GetCommand(Lang.CLang.ECommand.ADPCMMode_NearestNeighborInterpolation); break;
                    case EADPCMMode.線形補間: name = Lang.CLang.GetCommand(Lang.CLang.ECommand.ADPCMMode_LinearInterpolation); break;
                    case EADPCMMode.三次スプライン補間: name = Lang.CLang.GetCommand(Lang.CLang.ECommand.ADPCMMode_CubicSplineInterpolation); break;
                }
                return (((int)ADPCMMode).ToString() + ":" + name);
            }
            public int GetADPCMModeInt() {
                return ((int)ADPCMMode);
            }
            public bool SetADPCMModeInt(int v) {
                switch (v) {
                    case (int)EADPCMMode.最近傍補間: ADPCMMode = EADPCMMode.最近傍補間; return true;
                    case (int)EADPCMMode.線形補間: ADPCMMode = EADPCMMode.線形補間; return true;
                    case (int)EADPCMMode.三次スプライン補間: ADPCMMode = EADPCMMode.三次スプライン補間; return true;
                }
                return false;
            }

            private int _SampleRate = 192000;
            public int SampleRate { get { lock (LObj) { return (_SampleRate); } } set { lock (LObj) { _SampleRate = value; } } }
            public string GetRenderFormat(MusDriver.CDriver.EFileFormat FileFormat) {
                var res = SampleRate.ToString().PadLeft(6) + "Hz";
                switch (FileFormat) {
                    case MusDriver.CDriver.EFileFormat.MXDRV:
                        res += "," + CCommon.BitRate_WasapiOut.ToString() + "bit";
                        if (CCommon.SurroundChannels != 6) {
                            res += ", " + CCommon.SurroundChannels.ToString() + "ch";
                        } else {
                            res += ",5.1ch";
                        }
                        return res;
                    case MusDriver.CDriver.EFileFormat.FMP:
                    case MusDriver.CDriver.EFileFormat.PMD:
                        res += ",16bit,2ch";
                        return res;
                    case MusDriver.CDriver.EFileFormat.Unknown:
                    default:
                        return "";
                }
            }
        }
        public CSettings Settings = new CSettings();

        public MusDriver.CDriver Driver = null;

        public static System.Threading.Tasks.Task ThreadTask = null;

        public bool Music_isLoaded() {
            lock (LObj) {
                return (Driver != null);
            }
        }
        public void Music_Load(MDXOnline004.CZip Zip, bool WASAPIOutput) {
            CPlayStatus.Clear();

            Music_GetFlacTag_CurrentSettings = Zip.Settings;
            ArchiveID = Zip.Settings.MD5;

            var BasePath = System.IO.Path.GetTempPath() + @"MDXOnlineTemp_" + Zip.Settings.MD5;
            System.IO.Directory.CreateDirectory(BasePath);
            foreach (var pair in Zip.Files) {
                using (var wfs = new System.IO.StreamWriter(BasePath + @"\" + pair.Key)) {
                    wfs.BaseStream.Write(pair.Value);
                }
            }

            lock (LObj) {
                if (Driver != null) {
                    Driver.Free();
                    Driver = null;
                }
                try {
                    Driver = new MusDriver.CDriver(Zip.Settings, BasePath, Settings.SampleRate);
                    if (Music_GetReplayGain() == 0) {
                        IntLog.Add(Lang.CLang.GetConsole(Lang.CLang.EConsole.AudioThread_MDXFileHaveErrorSoSmallVolume));
                    }
                    if (WASAPIOutput) {
                        ThreadTask = System.Threading.Tasks.Task.Run(CCommon.AudioThread.ThreadLoop);
                    }
                } catch {
                    if (Driver != null) {
                        var ex = Driver.GetExceptionLog();
                        if (!ex.Equals("")) {
                            IntLog.Add(ex);
                        }
                        Driver.Free();
                        Driver = null;
                    }
                }
            }
        }
        public List<string> Music_GetWarnLog() {
            lock (LObj) {
                if (Driver == null) { return (new List<string>()); }
                return (Driver.GetWarnLog());
            }
        }
        public string Music_GetExceptionLog() {
            lock (LObj) {
                if (Driver == null) { return (""); }
                return (Driver.GetExceptionLog());
            }
        }
        public List<string> Music_GetInfo() {
            lock (LObj) {
                if (Driver == null) { return (new List<string>()); }
                return (Driver.GetInfo());
            }
        }
        public void Music_SeekMDX(System.TimeSpan SkipTS) {
            lock (LObj) {
                if (Driver == null) { return; }
                Driver.Seek(SkipTS);
                SetRequestClearBuffer();
            }
        }
        public void Music_Free() {
            if (ThreadTask != null) {
                SetRequestTerminate();
                ThreadTask.Wait(System.TimeSpan.FromSeconds(5)); // 5秒待って停止しなかったら無視して終了する
                ThreadTask = null;
            }
            lock (LObj) {
                if (Driver == null) { return; }
                Driver.Free();
                Driver = null;
            }
        }
        public void Music_SetFadeout(byte Speed) {
            lock (LObj) {
                if (Driver == null) { return; }
                Driver.SetFadeout(Speed);
            }
        }
        public string Music_GetException() {
            lock (LObj) {
                if (!Music_isLoaded()) { return (""); }
                return (Driver.Settings.Exception);
            }
        }

        public string Music_GetStatusText() {
            lock (LObj) {
                if (!Music_isLoaded() || Driver.isEOF()) { return (""); }

                var Line = "";

                if (Driver.GetFadeout()) { Line += "Fadeout. "; }

                Line += (1 + Driver.GetLoopCount()).ToString() + "/" + MDXWin.CCommon.AudioThread.Settings.LoopCount.ToString() + "Loops";

                var curts = Driver.GetPlayTS();
                var lents = Driver.Settings.PlayTS;
                Line += ", " + ((curts.TotalSeconds / lents.TotalSeconds) * 100).ToString("F0") + "% " + curts + " / " + lents;

                return (Line);
            }
        }

        public string Music_GetMD5() {
            lock (LObj) {
                if (!Music_isLoaded()) { return (""); }
                return Driver.Settings.MD5;
            }
        }

        public class CMusic_GetMDXPDXFilenameTitleRes {
            public MusDriver.CDriver.EFileFormat FileFormat = MusDriver.CDriver.EFileFormat.Unknown;
            public string MD5 = "";
            public string Path = "";
            public string PCMCaption = "";
            public string Title = "";
            public TimeSpan PlayTS = System.TimeSpan.FromTicks(0);
            public double ReplayGain = 0;
        }
        public CMusic_GetMDXPDXFilenameTitleRes Music_GetMDXPDXFilenameTitle() {
            lock (LObj) {
                var res = new CMusic_GetMDXPDXFilenameTitleRes();
                if (!Music_isLoaded()) { return (res); }
                res.FileFormat = Driver.FileFormat;
                res.MD5 = Driver.Settings.MD5;
                res.Path = Driver.Settings.Path;
                res.PCMCaption = Driver.GetPDXCaption();
                res.Title = Driver.Settings.Title;
                res.PlayTS = Driver.Settings.PlayTS;
                res.ReplayGain = Driver.Settings.ReplayGain;
                return (res);
            }
        }
        public double Music_GetReplayGain() {
            lock (LObj) {
                if (!Music_isLoaded()) { return (0); }
                return (Driver.Settings.ReplayGain);
            }
        }
        public System.TimeSpan Music_GetPlayTS() {
            lock (LObj) {
                if (!Music_isLoaded()) { return (System.TimeSpan.FromTicks(0)); }
                return (Driver.Settings.PlayTS);
            }
        }

        public bool Music_isEOF() {
            lock (LObj) {
                if (!Music_isLoaded()) { return (true); }
                return (Driver.isEOF());
            }
        }
        public MXDRV.CCanIgnoreException.CStack Music_GetCanIgnoreExceptionStack() {
            lock (LObj) {
                return (MXDRV.CCanIgnoreException.GetStack());
            }
        }

        public class CMusic_GetFlacTag_res {
            public string Title, Artist, Album, Lyricist;
        }
        public MDXOnline004.CZip.CSettings Music_GetFlacTag_CurrentSettings;
        public CMusic_GetFlacTag_res Music_GetFlacTag() {
            var res = new CMusic_GetFlacTag_res();

            res.Title = Music_GetFlacTag_CurrentSettings.Title;
            res.Artist = "MDXWin " + Driver.GetDriverVersion();
            res.Album = Music_GetFlacTag_CurrentSettings.Path;

            var PCMFilenames = new List<string>();
            foreach (var PCMFilename in new string[] { Music_GetFlacTag_CurrentSettings.PCM0Filename, Music_GetFlacTag_CurrentSettings.PCM1Filename, Music_GetFlacTag_CurrentSettings.PCM2Filename, Music_GetFlacTag_CurrentSettings.PCM3Filename }) {
                if (!PCMFilename.Equals("")) { PCMFilenames.Add(PCMFilename); }
            }
            res.Lyricist = string.Join(", ", PCMFilenames);

            return res;
        }

        public bool Music_isMXDRV() {
            return Driver.isMXDRV();
        }

        private string ArchiveID=""; // 演奏を終了した後でもURLを生成できるように
        public string Music_GetArchiveID() {
            return ArchiveID;
        }
    }
}
