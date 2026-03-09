using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MXDRV {
    internal class CBuffer {
        private byte[] buf;
        private int bufpos = 0;

        private void Load(byte[] _buf, int 終端余剰サイズ, byte 終端余剰データ) {
            if (終端余剰サイズ == 0) {
                buf = _buf;
            } else {
                buf = new byte[_buf.Length + 終端余剰サイズ];
                for (var idx = 0; idx < _buf.Length; idx++) {
                    buf[idx] = _buf[idx];
                }
                for (var idx = _buf.Length; idx < buf.Length; idx++) {
                    buf[idx] = 終端余剰データ;
                }
            }
        }
        public CBuffer(byte[] buf, int 終端余剰サイズ = 0, byte 終端余剰データ = 0x00) {
            Load(buf, 終端余剰サイズ, 終端余剰データ);
        }
        public CBuffer(string fn, int 終端余剰サイズ = 0, byte 終端余剰データ = 0x00) {
            Load(System.IO.File.ReadAllBytes(fn), 終端余剰サイズ, 終端余剰データ);
        }

        public byte[] GetRawData() { return (buf); }

        public int GetLength() { return (buf.Length); }

        public int GetPosition() { return (bufpos); }
        public bool SetPosition(int _bufpos) {
            bufpos = _bufpos;
            return (bufpos < buf.Length);
        }

        public byte ReadU8() {
            return (buf[bufpos++]);
        }
        public ushort ReadU16() {
            var temp = new byte[2] { buf[bufpos + 1], buf[bufpos + 0] };
            bufpos += 2;
            var res = BitConverter.ToUInt16(temp);
            return (res);
        }
        public uint ReadU32() {
            var temp = new byte[4] { buf[bufpos + 3], buf[bufpos + 2], buf[bufpos + 1], buf[bufpos + 0] };
            bufpos += 4;
            var res = BitConverter.ToUInt32(temp);
            return (res);
        }
        public sbyte ReadS8() {
            return ((sbyte)buf[bufpos++]);
        }
        public short ReadS16() {
            var temp = new byte[2] { buf[bufpos + 1], buf[bufpos + 0] };
            bufpos += 2;
            var res = BitConverter.ToInt16(temp);
            return (res);
        }
        public int ReadS32() {
            var temp = new byte[4] { buf[bufpos + 3], buf[bufpos + 2], buf[bufpos + 1], buf[bufpos + 0] };
            bufpos += 4;
            var res = BitConverter.ToInt32(temp);
            return (res);
        }
        public void ReadBytes(byte[] dstbuf) {
            Array.Copy(buf, bufpos, dstbuf, 0, dstbuf.Length);
            bufpos += dstbuf.Length;
        }

        public string ReadStringSJIS(byte TermData, bool 制御文字をスペースに変換する = false) {
            var buf = new List<byte>();
            while (true) {
                var data = ReadU8();
                if (data == TermData) { break; }
                if (制御文字をスペースに変換する) {
                    if (data < 0x20) { data = 0x20; }
                }
                buf.Add(data);
            }
            return MoonLib.CTextEncode.SJIS.GetString(buf.ToArray());
        }

        public string GetDebugStr(int len) {
            var res = "$";
            for (var idx = 0; idx < len; idx++) {
                if (buf.Length <= (bufpos + idx)) {
                    res += "EOF";
                    break;

                }
                res += buf[bufpos + idx].ToString("x2");
            }
            return (res);
        }
    }
}
