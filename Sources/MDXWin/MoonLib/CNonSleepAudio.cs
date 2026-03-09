using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace MoonLib {
    internal class CNonSleepAudio {
        private NAudio.Wave.WaveFormatExtensible GetWaveFormat(int SampleRate, int BitRate, int Channels) {
            return (new NAudio.Wave.WaveFormatExtensible(SampleRate, BitRate, Channels));
        }

        private NAudio.Wave.BufferedWaveProvider WaveBuf =null;
        private NAudio.Wave.WasapiOut WaveOut=null;

        public CNonSleepAudio() {
            WaveBuf = new NAudio.Wave.BufferedWaveProvider(GetWaveFormat(8000,16,1));
            WaveBuf.BufferDuration = System.TimeSpan.FromSeconds(1);

            WaveOut = new NAudio.Wave.WasapiOut();
            WaveOut.Init(WaveBuf);
            WaveOut.Play();
        }

        public void Update() {
            var EmptySamplesCount = (WaveBuf.BufferLength - WaveBuf.BufferedBytes) / WaveBuf.WaveFormat.BlockAlign;
            if (EmptySamplesCount ==0) { return; }

            var buf = new byte[EmptySamplesCount*2];
            WaveBuf.AddSamples(buf,0,buf.Length);
        }

        public void Free() {
            if (WaveOut != null) {
                WaveOut.Stop();
                WaveOut.Dispose();
                WaveOut = null;
            }
            if (WaveBuf != null) {
                WaveBuf = null;
            }
        }
    }
}
