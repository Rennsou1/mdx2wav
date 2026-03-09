using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MXDRV {
    internal class CPDXCommon {
        public class CPCMFormat {
            public enum EFormat { ADPCM, PCMS16, PCMS8 };
            public EFormat Format;
            public float SrcSampleRate;
            public float PCMS16_Volume;
            public CPCMFormat(int SampleRateData) {
                var f3906 = 46875 / 12f;
                var f5208 = 62500 / 12f;
                var f7812 = 93750 / 12f;
                var f10416 = 125000 / 12f;
                var f15625 = 15625f;
                var f20833 = 125000 / 6f;
                var f31250 = 15625 * 2f;

                if (false) {
                    // 0x06以降のフォーマットを使用しているMDXデータは所有していなかった。
                    // まーきゅりーゆにっと用MDXデータは、0x05しか使っていない。
                    // 0x83と0x84はよくわからないので無視する。
                    switch (SampleRateData) {
                        case 0x00: case 0x30: Format = EFormat.ADPCM; SrcSampleRate = f3906; break;
                        case 0x01: case 0x31: Format = EFormat.ADPCM; SrcSampleRate = f5208; break;
                        case 0x02: case 0x32: Format = EFormat.ADPCM; SrcSampleRate = f7812; break;
                        case 0x03: case 0x33: Format = EFormat.ADPCM; SrcSampleRate = f10416; break;
                        case 0x04: case 0x34: Format = EFormat.ADPCM; SrcSampleRate = f15625; break;
                        case 0x07: case 0x35: Format = EFormat.ADPCM; SrcSampleRate = f20833; break;
                        case 0x0a: case 0x36: Format = EFormat.ADPCM; SrcSampleRate = f31250; break;
                        case 0x10: Format = EFormat.PCMS16; SrcSampleRate = f3906; break;
                        case 0x11: Format = EFormat.PCMS16; SrcSampleRate = f5208; break;
                        case 0x12: Format = EFormat.PCMS16; SrcSampleRate = f7812; break;
                        case 0x13: Format = EFormat.PCMS16; SrcSampleRate = f10416; break;
                        case 0x14: case 0x05: Format = EFormat.PCMS16; SrcSampleRate = f15625; break;
                        case 0x15: case 0x08: Format = EFormat.PCMS16; SrcSampleRate = f20833; break;
                        case 0x16: case 0x0b: Format = EFormat.PCMS16; SrcSampleRate = f31250; break;
                        case 0x20: Format = EFormat.PCMS8; SrcSampleRate = f3906; break;
                        case 0x21: Format = EFormat.PCMS8; SrcSampleRate = f5208; break;
                        case 0x22: Format = EFormat.PCMS8; SrcSampleRate = f7812; break;
                        case 0x23: Format = EFormat.PCMS8; SrcSampleRate = f10416; break;
                        case 0x24: case 0x06: Format = EFormat.PCMS8; SrcSampleRate = f15625; break;
                        case 0x25: case 0x09: Format = EFormat.PCMS8; SrcSampleRate = f20833; break;
                        case 0x26: case 0x0c: Format = EFormat.PCMS8; SrcSampleRate = f31250; break;
                        default: throw new Exception(Lang.CLang.GetMXDRV(Lang.CLang.EMXDRV.PDX_UndefPCMFormat) + " SampleRateData=0x" + SampleRateData.ToString("x2"));
                    }
                } else {
                    // PCM8++ マーキュリー用PCMドライバー ver0.83d
                    bool Stereo;
                    switch (SampleRateData) {
                        case 0x00: Format = EFormat.ADPCM; SrcSampleRate = f3906; Stereo = false; break;
                        case 0x01: Format = EFormat.ADPCM; SrcSampleRate = f5208; Stereo = false; break;
                        case 0x02: Format = EFormat.ADPCM; SrcSampleRate = f7812; Stereo = false; break;
                        case 0x03: Format = EFormat.ADPCM; SrcSampleRate = f10416; Stereo = false; break;
                        case 0x04: Format = EFormat.ADPCM; SrcSampleRate = f15625; Stereo = false; break;
                        case 0x05: Format = EFormat.PCMS16; SrcSampleRate = f15625; Stereo = false; break;
                        case 0x06: Format = EFormat.PCMS8; SrcSampleRate = f15625; Stereo = false; break;
                        case 0x07: Format = EFormat.PCMS16; SrcSampleRate = 0; Stereo = false; break;
                        case 0x08: Format = EFormat.PCMS16; SrcSampleRate = f15625; Stereo = false; break;
                        case 0x09: Format = EFormat.PCMS16; SrcSampleRate = 16000; Stereo = false; break;
                        case 0x0A: Format = EFormat.PCMS16; SrcSampleRate = 22050; Stereo = false; break;
                        case 0x0B: Format = EFormat.PCMS16; SrcSampleRate = 24000; Stereo = false; break;
                        case 0x0C: Format = EFormat.PCMS16; SrcSampleRate = 32000; Stereo = false; break;
                        case 0x0D: Format = EFormat.PCMS16; SrcSampleRate = 44100; Stereo = false; break;
                        case 0x0E: Format = EFormat.PCMS16; SrcSampleRate = 48000; Stereo = false; break;
                        case 0x0F: Format = EFormat.PCMS16; SrcSampleRate = 0; Stereo = false; break;
                        case 0x10: Format = EFormat.PCMS8; SrcSampleRate = 15625; Stereo = false; break;
                        case 0x11: Format = EFormat.PCMS8; SrcSampleRate = 16000; Stereo = false; break;
                        case 0x12: Format = EFormat.PCMS8; SrcSampleRate = 22050; Stereo = false; break;
                        case 0x13: Format = EFormat.PCMS8; SrcSampleRate = 24000; Stereo = false; break;
                        case 0x14: Format = EFormat.PCMS8; SrcSampleRate = 32000; Stereo = false; break;
                        case 0x15: Format = EFormat.PCMS8; SrcSampleRate = 44100; Stereo = false; break;
                        case 0x16: Format = EFormat.PCMS8; SrcSampleRate = 48000; Stereo = false; break;
                        case 0x17: Format = EFormat.PCMS8; SrcSampleRate = 0; Stereo = false; break;
                        case 0x18: Format = EFormat.PCMS16; SrcSampleRate = 15625; Stereo = true; break;
                        case 0x19: Format = EFormat.PCMS16; SrcSampleRate = 16000; Stereo = true; break;
                        case 0x1A: Format = EFormat.PCMS16; SrcSampleRate = 22050; Stereo = true; break;
                        case 0x1B: Format = EFormat.PCMS16; SrcSampleRate = 24000; Stereo = true; break;
                        case 0x1C: Format = EFormat.PCMS16; SrcSampleRate = 32000; Stereo = true; break;
                        case 0x1D: Format = EFormat.PCMS16; SrcSampleRate = 44100; Stereo = true; break;
                        case 0x1E: Format = EFormat.PCMS16; SrcSampleRate = 48000; Stereo = true; break;
                        case 0x1F: Format = EFormat.PCMS16; SrcSampleRate = 0; Stereo = true; break;
                        case 0x20: Format = EFormat.PCMS8; SrcSampleRate = 15625; Stereo = true; break;
                        case 0x21: Format = EFormat.PCMS8; SrcSampleRate = 16000; Stereo = true; break;
                        case 0x22: Format = EFormat.PCMS8; SrcSampleRate = 22050; Stereo = true; break;
                        case 0x23: Format = EFormat.PCMS8; SrcSampleRate = 24000; Stereo = true; break;
                        case 0x24: Format = EFormat.PCMS8; SrcSampleRate = 32000; Stereo = true; break;
                        case 0x25: Format = EFormat.PCMS8; SrcSampleRate = 44100; Stereo = true; break;
                        case 0x26: Format = EFormat.PCMS8; SrcSampleRate = 48000; Stereo = true; break;
                        case 0x27: Format = EFormat.PCMS8; SrcSampleRate = 0; Stereo = true; break;
                        case 0x28: Format = EFormat.ADPCM; SrcSampleRate = 0; Stereo = false; break;
                        case 0x29: Format = EFormat.PCMS16; SrcSampleRate = 0; Stereo = false; break;
                        default: throw new Exception(Lang.CLang.GetMXDRV(Lang.CLang.EMXDRV.PDX_UndefPCMFormat) + " SampleRateData=0x" + SampleRateData.ToString("x2"));
                    }
                    if ((SampleRateData <= 0x06) || (SampleRateData == 0x28)) {
                        PCMS16_Volume = 1f;
                    } else {
                        PCMS16_Volume = 1f / 16;
                    }
                }
            }
        }

        public static float VolumeData2Volume(int VolumeData, int VolumeOffset) {
            if (CCommon.古いMDXWinの音量テーブルを使う) {
                float tl;
                if ((VolumeData & 0x80) != 0) {
                    tl = (float)(0x7f - (VolumeData & 0x7f)) / 0x7f;
                } else {
                    throw new Exception(Lang.CLang.GetMXDRV(Lang.CLang.EMXDRV.PDX_IlligalVolumeForOldMDXWinVolumeTable));
                }
                return (tl);
            } else {
                int tl;
                if ((VolumeData & 0x80) != 0) {
                    tl = VolumeData & 0x7f;
                } else {
                    if (0x10 <= VolumeData) { CCanIgnoreException.Throw(Lang.CLang.GetMXDRV(Lang.CLang.EMXDRV.PDX_VolumeError), "Volume=0x" + VolumeData.ToString("x")); return (0); }
                    var volume_tbl = new byte[0x10] {
                        0x2a,0x28,0x25,0x22,0x20,0x1d,0x1a,0x18,
                        0x15,0x12,0x10,0x0d,0x0a,0x08,0x05,0x02,
                    };
                    tl = volume_tbl[VolumeData];
                }
                tl += VolumeOffset;

                var PCMVolume = new byte[0x2b]  {
                    0x0f,0x0f,0x0f,0x0e,0x0e,0x0e,0x0d,0x0d,
                    0x0d,0x0c,0x0c,0x0b,0x0b,0x0b,0x0a,0x0a,
                    0x0a,0x09,0x09,0x08,0x08,0x08,0x07,0x07,
                    0x07,0x06,0x06,0x05,0x05,0x05,0x04,0x04,
                    0x04,0x03,0x03,0x02,0x02,0x02,0x01,0x01,
                    0x01,0x00,0x00, // 0xff, 使っていない
                };

                if (tl < 0) {
                    tl = 0;
                } else if (tl < 0x2b) {
                    tl = PCMVolume[tl];
                } else {
                    tl = 0;
                }

                var PCM8VOLTBL = new byte[16] { 2, 3, 4, 5, 6, 8, 10, 12, 16, 20, 24, 32, 40, 48, 64, 80, };

                return (PCM8VOLTBL[tl] / 16f); // int Volume;	// x/16 ボリュームを1以上にできる
            }
        }

    }
}
