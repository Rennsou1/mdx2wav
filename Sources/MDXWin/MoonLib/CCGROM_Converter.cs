using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MoonLib {
    internal class CCGROM_Converter {
        private CCGROM_Reader CGROM_Reader;
        public CCGROM_Converter(string Filename) {
            CGROM_Reader = new CCGROM_Reader(Filename);
        }

        private class CCGROM_Reader {
            byte[] srcbuf;

            public CCGROM_Reader(string Filename) {
                using (var rfs = new System.IO.StreamReader(Filename)) {
                    using (var br = new System.IO.BinaryReader(rfs.BaseStream)) {
                        srcbuf = br.ReadBytes((int)rfs.BaseStream.Length);
                    }
                }
            }

            public byte[] GetAsciiBytes(int cnum, int w, int h, int ofs) {
                var xofscnt = (w + 7) / 8;
                var res = new byte[xofscnt * h];
                var resofs = 0;
                for (var y = 0; y < h; y++) {
                    for (var xofs = 0; xofs < xofscnt; xofs++) {
                        var pos = ofs + (y * xofscnt) + xofs + cnum * xofscnt * h;
                        res[resofs++] = srcbuf[pos];
                    }
                }
                return (res);
            }

            public class CGetKanjiBytesReq {
                public int ofs;
                public int pos = 0;
                public int w, h;
                public CGetKanjiBytesReq(int _ofs, int _w, int _h) {
                    ofs = _ofs;
                    w = _w;
                    h = _h;
                }
            }
            public byte[] GetKanjiBytes(CGetKanjiBytesReq Req) {
                var res = new byte[(Req.w / 8) * Req.h];
                var resofs = 0;
                for (var y = 0; y < Req.h; y++) {
                    for (var x = 0; x < Req.w / 8; x++) {
                        res[resofs++] = srcbuf[Req.ofs + (Req.h * (Req.w / 8)) * Req.pos + x + (y * (Req.w / 8))];
                    }
                }
                return (res);
            }
        }

        private bool ReadKanji_isIgnoreData(int Data) {
            if ((Data & 0xff) == 0x7f) { return (true); }
            if ((Data & 0xff) == 0xfd) { return (true); }
            if ((Data & 0xff) == 0xfe) { return (true); }
            if ((Data & 0xff) == 0xff) { return (true); }
            return (false);
        }

        public CCGROM.CFontFace[] ConvToFontFace(int Height, int AsciiOffset, int AsciiWidth, int KanjiOffset, int KanjiWidth) {
            var res = new CCGROM.CFontFace[0x10000];

            for (var idx = 0; idx < 0x100; idx++) {
                res[idx] = new CCGROM.CFontFace(CGROM_Reader.GetAsciiBytes(idx, AsciiWidth, Height, AsciiOffset));
            }

            if (KanjiWidth != 0) {
                var Req = new CCGROM_Reader.CGetKanjiBytesReq(KanjiOffset, KanjiWidth, Height);

                for (var PrimeData = 0x8100; PrimeData < 0x8500; PrimeData += 0x100) {
                    for (var SecondData = 0x40; SecondData < 0x100; SecondData += 0x10) {
                        for (var idx = 0; idx < 0x10; idx++) {
                            var Data = PrimeData + SecondData + idx;
                            if (ReadKanji_isIgnoreData(Data)) { continue; }
                            res[Data] = new CCGROM.CFontFace(CGROM_Reader.GetKanjiBytes(Req));
                            Req.pos++;
                        }
                    }
                }
                Req.pos -= 15;
                for (var PrimeData = 0x8800; PrimeData < 0x8900; PrimeData += 0x100) {
                    for (var SecondData = 0x90; SecondData < 0x100; SecondData += 0x10) {
                        for (var idx = 0; idx < 0x10; idx++) {
                            var Data = PrimeData + SecondData + idx;
                            if (ReadKanji_isIgnoreData(Data)) { continue; }
                            res[Data] = new CCGROM.CFontFace(CGROM_Reader.GetKanjiBytes(Req));
                            Req.pos++;
                        }
                    }
                }
                for (var PrimeData = 0x8900; PrimeData < 0xa000; PrimeData += 0x100) {
                    for (var SecondData = 0x40; SecondData < 0x100; SecondData += 0x10) {
                        for (var idx = 0; idx < 0x10; idx++) {
                            var Data = PrimeData + SecondData + idx;
                            if (ReadKanji_isIgnoreData(Data)) { continue; }
                            res[Data] = new CCGROM.CFontFace(CGROM_Reader.GetKanjiBytes(Req));
                            Req.pos++;
                        }
                    }
                }
                for (var PrimeData = 0xe000; PrimeData < 0xeb00; PrimeData += 0x100) {
                    for (var SecondData = 0x40; SecondData < 0x100; SecondData += 0x10) {
                        for (var idx = 0; idx < 0x10; idx++) {
                            var Data = PrimeData + SecondData + idx;
                            if (ReadKanji_isIgnoreData(Data)) { continue; }
                            res[Data] = new CCGROM.CFontFace(CGROM_Reader.GetKanjiBytes(Req));
                            Req.pos++;
                        }
                    }
                }
            }

            return (res);
        }
    }
}
