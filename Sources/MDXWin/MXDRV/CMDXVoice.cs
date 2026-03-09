using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace MXDRV {
    internal class CMDXVoice {
        public int ChannelNum;

        public byte FLCON = 0;
        public byte CarrierSlot = 0;
        public byte SLOTMASK = 0;
        public byte[] dt1_mul = new byte[4] { 0, 0, 0, 0 };
        public byte[] TL = new byte[4] { 0, 0, 0, 0 };
        public byte[] ks_ar = new byte[4] { 0, 0, 0, 0 };
        public byte[] ame_d1r = new byte[4] { 0, 0, 0, 0 };
        public byte[] dt2_d2r = new byte[4] { 0, 0, 0, 0 };
        public byte[] d1l_rr = new byte[4] { 0, 0, 0, 0 };

        public bool FindVoice(CBuffer Buffer, int TopOffset, int VoiceNum) {
            var LastPos = Buffer.GetPosition();

            for (var findidx = 0; findidx < 256; findidx++) {
                var ofs = TopOffset + (findidx * 27);
                if (!Buffer.SetPosition(ofs)) { return (false); } // 音色が見つからなかった
                var FindVoiceNum = Buffer.ReadU8();
                if ((VoiceNum == -1) || (FindVoiceNum == VoiceNum)) {
                    FLCON = Buffer.ReadU8();

                    var carrier_slot_tbl = new byte[8] { 0x08, 0x08, 0x08, 0x08, 0x0c, 0x0e, 0x0e, 0x0f };
                    CarrierSlot = carrier_slot_tbl[FLCON & 0x7];

                    SLOTMASK = Buffer.ReadU8();

                    for (var idx = 0; idx < 4; idx++) { dt1_mul[idx] = Buffer.ReadU8(); }
                    for (var idx = 0; idx < 4; idx++) { TL[idx] = Buffer.ReadU8(); }
                    for (var idx = 0; idx < 4; idx++) { ks_ar[idx] = Buffer.ReadU8(); }
                    for (var idx = 0; idx < 4; idx++) { ame_d1r[idx] = Buffer.ReadU8(); }
                    for (var idx = 0; idx < 4; idx++) { dt2_d2r[idx] = Buffer.ReadU8(); }
                    for (var idx = 0; idx < 4; idx++) { d1l_rr[idx] = Buffer.ReadU8(); }

                    Buffer.SetPosition(LastPos);
                    return (true);
                }
            }

            return (false);
        }

        public string WarnLog = "";

        public CMDXVoice(int _ChannelNum, CBuffer Buffer, int TopOffset, int VoiceNum) { // VoiceNumが-1の時、または、指定された音色が見つからなかった時は、最初に見つかった音色を読み込む
            ChannelNum = _ChannelNum;

            if (VoiceNum == -1) { return; }

            if (!FindVoice(Buffer, TopOffset, VoiceNum)) {
                WarnLog = Lang.CLang.GetMXDRV(Lang.CLang.EMXDRV.Voice_NotFoundVoiceData) + " TopOffset=0x" + TopOffset.ToString("x4") + ", VoiceNum=" + VoiceNum.ToString();
            }
        }

        public void ApplyBaseVoice() { // PanpotとVolumeは設定しない
            for (var idx = 0; idx < 4; idx++) {
                CCommon.X68Sound.OpmPoke(0x40 + ChannelNum + (idx * 8), dt1_mul[idx]);
                CCommon.X68Sound.OpmPoke(0x80 + ChannelNum + (idx * 8), ks_ar[idx]);
                CCommon.X68Sound.OpmPoke(0xa0 + ChannelNum + (idx * 8), ame_d1r[idx]);
                CCommon.X68Sound.OpmPoke(0xc0 + ChannelNum + (idx * 8), dt2_d2r[idx]);
                CCommon.X68Sound.OpmPoke(0xe0 + ChannelNum + (idx * 8), d1l_rr[idx]);
            }
        }

        public void ApplyPanpot(int Panpot) {
            CCommon.X68Sound.OpmPoke(0x20 + ChannelNum, (Panpot << 6) | FLCON);
        }

        private int StoreTL = 127;

        public void ApplyVolume(int Volume, int Offset) {
            var volume_tbl = new byte[16] {
                0x2a,0x28,0x25,0x22,0x20,0x1d,0x1a,0x18,
                0x15,0x12,0x10,0x0d,0x0a,0x08,0x05,0x02,
            };

            if ((Volume & 0x80) != 0) {
                StoreTL = Volume & 0x7f;
            } else {
                if (0x10 <= Volume) { CCanIgnoreException.Throw(Lang.CLang.GetMXDRV(Lang.CLang.EMXDRV.Voice_VolumeError), "Volume=0x" + Volume.ToString("x")); return; }
                StoreTL = volume_tbl[Volume];
            }
            StoreTL += Offset;

            for (var idx = 0; idx < 4; idx++) {
                int att;
                if ((CarrierSlot & (1 << idx)) != 0) {
                    att = TL[idx] + StoreTL;
                } else {
                    att = TL[idx];
                }
                if ((att < 0x00) || (0x7f < att)) { att = 0x7f; }
                var Addr = 0x60 + ChannelNum + (idx * 8);
                CCommon.X68Sound.OpmPoke(Addr, att);
            }
        }
        public int GetVolume() {
            return (127 - StoreTL);
        }

        public int LastNoteNum = 0;
        public double LastNoteNumFine = 0;

        private const int KFOffset = 5; // 3.579545MHz/4MHz=0.89488625補正値 計算するとD-123だけど、MDXデータには既にD-128済み（2音階下）のKeyCodeが格納されているので、D+5するだけでOK
        private int LastWritedKeyCodeData = 0xff;
        private double LastWritedKeyFine = 0xff;
        public void SetTone(int KeyCode, double kf) {
            LastNoteNum = KeyCode;
            LastNoteNumFine = KeyCode + (kf / 64);

            var note = (KeyCode * 64) + kf + KFOffset;
            if (note < 0) { note = 0; }
            if (((64 * 12 * 8) - 1) < note) { note = (64 * 12 * 8) - 1; }

            var KeyFine = note % 64;
            note /= 64;
            var KeyCodeTable = new byte[12] { 0x00, 0x01, 0x02, 0x04, 0x05, 0x06, 0x08, 0x09, 0x0a, 0x0c, 0x0d, 0x0e, };
            var KeyCodeData = (((int)note / 12) << 4) | KeyCodeTable[(int)note % 12];

            if (LastWritedKeyFine != KeyFine) {
                LastWritedKeyFine = KeyFine;
                CCommon.X68Sound.OpmPoke_SetKF(ChannelNum, KeyFine);
            }
            if (LastWritedKeyCodeData != KeyCodeData) {
                LastWritedKeyCodeData = KeyCodeData;
                CCommon.X68Sound.OpmPoke(0x28 + ChannelNum, KeyCodeData);
            }
        }

        private int LastWritedSlotMask = 0xff;
        public void WriteSlotMask(int SlotMask) {
            if (LastWritedSlotMask == SlotMask) { return; }
            LastWritedSlotMask = SlotMask;
            CCommon.X68Sound.OpmPoke(0x08, (SlotMask << 3) + ChannelNum);
        }
        public void NoteOff() {
            WriteSlotMask(0x0);
        }
        public void NoteOn() {
            WriteSlotMask(SLOTMASK & 0x0f);
        }
    }
}
