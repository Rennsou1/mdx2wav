using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MXDRV {
    internal class CLZXDec {
        public static uint isLZXCompressed(CBuffer Buffer) {
            Buffer.SetPosition(Buffer.GetPosition() + 4);
            if (Buffer.ReadU32() != 0x4c5a5820) { return (0); }
            Console.WriteLine((char)Buffer.ReadU8() + "" + (char)Buffer.ReadU8() + (char)Buffer.ReadU8() + (char)Buffer.ReadU8());
            Buffer.SetPosition(Buffer.GetPosition() - 4 + 4 + 6);
            return (Buffer.ReadU32());
        }

        private static int u8tos8(int u8) {
            var buf = new byte[1] { (byte)u8 };
            return (-(0x100 - u8));
        }

        private static int u16tos16(int AH, int AL) {
            var buf = new byte[4] { (byte)AL, (byte)AH, 0xff, 0xff };
            return (BitConverter.ToInt32(buf));
        }

        private static int DebugHeaderSize = 0;
        private static string debugbufstr(byte[] buf, int len) {
            if (len < 128) { return (""); }
            var res = "len:0x" + (DebugHeaderSize + len).ToString("x") + ", ";
            for (var num = 0; num < 32; num++) {
                res += "0x";
                for (var idx = 0; idx < 4; idx++) {
                    res += buf[len - 128 + num * 4 + idx].ToString("x2");
                }
                res += ", ";
            }
            return (res);
        }

        private class CBitReader {
            private bool DebugOut;
            private int Last;
            private byte Data;
            public CBitReader(bool _DebugOut) {
                DebugOut = _DebugOut;
                Last = 0;
                Data = 0x00;
            }
            public bool GetBitBool(CBuffer br, string Label) {
                if (Last == 0) {
                    Data = br.ReadU8();
                    Last = 8;
                    if (DebugOut) Console.WriteLine("Read BitStream 0x" + (DebugHeaderSize + br.GetPosition() - 1).ToString("x4") + " 0x" + Data.ToString("x2"));
                }
                Last--;
                var res = (Data & 0x80) != 0;
                Data <<= 1;
                if (DebugOut) Console.WriteLine("ReadBit " + Label + " " + res);
                return (res);
            }
            public int GetBitInt(CBuffer br, string Label) {
                return (GetBitBool(br, Label) ? 1 : 0);
            }
        }

        public static int ReadS32実データ16bit(CBuffer br) {
            var high = br.ReadU8();
            var low = br.ReadU8();
            var buf = new byte[4] { low, high, 0xff, 0xff };
            return (BitConverter.ToInt32(buf));
        }

        private static byte[] LZXDecodeBody(CBuffer br, int decompsize) {
            var wbuf = new byte[decompsize + 16]; // バッファオーバーランに気を付けて
            var wbufidx = 0;

            while (true) {
                var ID = br.ReadU32();
                if (ID == 0x7FFFFF4C) { break; }
                br.SetPosition(br.GetPosition() - 2); // 16bit単位でIDを検索する
            }

            // ad5   E1(1110 0001) 00 00 03 FD 00 C2 04 D4 FC 07 38 FC 12 09 61

            // mezon B1(1011 0001) 00 FF FF 03 FC D8(1101 1000) 07 FF FF 80 09 0A DC F4 03

            // 1 Store 00
            // 01 CopyBlockLong ffff 8bytes from:0xffff(-1)
            // 1 Store 03
            // 00 01 CopyBlockShort 3bytes from:0xfc(-4)

            // 1 Store 07
            // 1 Store FF
            // 01 CopyBlockLong ff80 9bytes from:0xfff0(-16)

            var DebugOut = true;

            var BitReader = new CBitReader(DebugOut);

            while (true) {
                if (DebugOut) Console.WriteLine();

                // L26
                if (BitReader.GetBitBool(br, "L26")) {
                    var tmp = br.ReadU8();
                    if (DebugOut) Console.WriteLine("SimpleStore 0x" + (br.GetPosition() - 1).ToString("x") + " 0x" + tmp.ToString("x"));
                    wbuf[wbufidx++] = tmp;
                    continue;
                }

                // L36
                if (!BitReader.GetBitBool(br, "L36")) {
                    // L00 指定バイト数前の領域を指定バイト数書き込む
                    var CL = BitReader.GetBitInt(br, "L00-1");
                    CL = (CL << 1) + BitReader.GetBitInt(br, "L00-2") + 2;

                    var AL = br.ReadU8();
                    var ALs8 = u8tos8(AL);
                    if (DebugOut) Console.WriteLine("CopyBlockShort AL:0x" + AL.ToString("x") + " s8:" + ALs8 + " CL:" + CL.ToString());

                    var CopyFromPos = wbufidx + ALs8;
                    for (var idx = 0; idx < CL; idx++) {
                        wbuf[wbufidx++] = wbuf[CopyFromPos++];
                    }
                    continue;
                } else {
                    var AX = ReadS32実データ16bit(br);
                    if (DebugOut) Console.WriteLine("CopyBlock AX:0x" + AX.ToString("x4"));
                    var CL = AX & 7;
                    AX >>= 3;
                    if (CL != 0) { // L1A 指定された前方バッファから指定バイト数コピーする
                        CL += 2;
                        var CopyFromPos = wbufidx + AX;
                        if (DebugOut) Console.WriteLine("CopyBlockMiddle EAX:" + AX.ToString() + " CL:" + CL);
                        for (var idx = 0; idx < CL; idx++) {
                            wbuf[wbufidx++] = wbuf[CopyFromPos++];
                        }
                        continue;
                    }

                    CL = br.ReadU8();
                    if (CL != 0) { // L1C 指定された前方バッファから指定バイト数コピーする
                        CL++;
                        var CopyFromPos = wbufidx + AX;
                        if (DebugOut) Console.WriteLine("CopyBlockLong EAX:" + AX.ToString() + " CL:" + CL);
                        for (var idx = 0; idx < CL; idx++) {
                            wbuf[wbufidx++] = wbuf[CopyFromPos++];
                        }
                        continue;
                    }

                    if (decompsize != wbufidx) { throw new Exception("size miss."); }

                    var res = new byte[decompsize];
                    for(var idx = 0; idx <res.Length ; idx++) {
                        res[idx] = wbuf[idx];
                    }
                    return (res);
                }
            }
        }

        public static byte[] LZXDecode(CBuffer src) {
            var DecompSize = isLZXCompressed(src);
            if (DecompSize == 0) { return (null); }

            DebugHeaderSize = 0;

            return (LZXDecodeBody(src, (int)DecompSize));
        }
    }
}
