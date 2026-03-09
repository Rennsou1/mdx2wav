using NAudio.Dmo;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace MoonLib {
    public class CCGROM {
        public class CFontFace {
            public byte[] Data;
            public CFontFace(byte[] _Data) {
                Data = _Data;
            }
        }
        public class CFonts {
            public bool isLoaded = false;
            public CFontFace[] Face8 = null;
            public CFontFace[] Face12 = null;
            public CFontFace[] Face16 = null;
            public CFontFace[] Face24 = null;
        }
        public CFonts Fonts = new CFonts();

        private CCGROM_Windows CGROM_Windows = new CCGROM_Windows();

        public void Load() {
            var datfn = "CGROM.DAT";
            if (System.IO.File.Exists(datfn)) {
                Fonts.isLoaded = true;
                var CGROM_Converter = new CCGROM_Converter(datfn);
                Fonts.Face8 = CGROM_Converter.ConvToFontFace(8, 0x3a000, 8, 0, 0);
                Fonts.Face12 = CGROM_Converter.ConvToFontFace(12, 0x3b800, 12, 0, 0);
                Fonts.Face16 = CGROM_Converter.ConvToFontFace(16, 0x3a800, 8, 0x00000, 16);
                Fonts.Face24 = CGROM_Converter.ConvToFontFace(24, 0x3d000, 12, 0x40000, 24);
            }
        }

        public bool isLoaded() { return Fonts.isLoaded; }

        private class CFontSpec {
            public int Width, Height, SizeMul;
            public CFontFace[] FontFace = null;
            public CFontSpec(int _Width, int _Height, int _SizeMul, CFontFace[] _FontFace) {
                Width = _Width;
                Height = _Height;
                SizeMul = _SizeMul;
                FontFace = _FontFace;
            }
        }

        private CFontSpec GetFontSpecFromFontSize(int Size) {
            if (!Fonts.isLoaded) { return (null); }
            switch (Size) {
                case 8: return (new CFontSpec(8, 8, 1, Fonts.Face8));
                case 12: return (new CFontSpec(12, 12, 1, Fonts.Face12));
                case 16: return (new CFontSpec(8, 16, 1, Fonts.Face16));
                case 24: return (new CFontSpec(12, 24, 1, Fonts.Face24));
                case 32: return (new CFontSpec(8, 16, 2, Fonts.Face16));
                case 48: return (new CFontSpec(12, 24, 2, Fonts.Face24));
                default: return (null);
            }
        }

        public bool isHaveFontSize(int Size) {
            return 12 < Size; // 12ドット以下のフォントは潰れてしまうので拒否する
        }

        public string GetFontName(int Size) {
            if (Fonts.isLoaded) {
                switch (Size) {
                    case 8: return "8:CGROM.DAT 8x8";
                    case 12: return "12:CGROM.DAT 12x12";
                    case 16: return "16:CGROM.DAT 8x16";
                    case 24: return "24:CGROM.DAT 12x24";
                    case 32: return "32:CGROM.DAT 8x16 ";
                    case 48: return "48:CGROM.DAT 12x24 ";
                }
            }
            return Size.ToString();
        }

        private byte[] ConvertSJIS(string Text) {
            if (Text == null) { return (new byte[0]); }
            var src = MoonLib.CTextEncode.SJIS.GetBytes(Text);
            var dst = new List<byte>();
            for (var idx = 0; idx < src.Length; idx++) {
                var data = src[idx];
                if (data != 0x1b) {
                    dst.Add(data);
                    continue;
                }
                var esc = ((idx + 1) < src.Length) ? src[idx + 1] : 0x00;
                idx++;
                continue;
            }
            return (dst.ToArray());
        }

        private class CGetFontRectRes {
            public bool isKanji;
            public int iWidth, iHeight;
            public CFontFace FontFace;
        }
        private CGetFontRectRes GetFontRect(CFontSpec FontSpec, byte Data1, byte Data2) {
            var res = new CGetFontRectRes();

            var Head = Data1 >> 4;
            res.isKanji = (Head == 0x8) || (Head == 0x9) || (Head == 0xe) || (Head == 0xf);

            res.iWidth = FontSpec.Width;
            res.iHeight = FontSpec.Height;

            if (!res.isKanji) {
                res.FontFace = FontSpec.FontFace[Data1];
            } else {
                res.iWidth *= 2;
                var Data = ((ushort)Data1 << 8) | Data2;
                res.FontFace = FontSpec.FontFace[Data];
            }

            return (res);
        }

        private class CDrawString_LineCache {
            public int Size;
            public string Text;
            public int Padding;
            public uint BackgroundColor;
            public uint ForegroundColor;
            public System.Windows.Media.ImageSource imgsrc;
            public CDrawString_LineCache(int _Size, string _Text, int _Padding, uint _BackgroundColor, uint _ForegroundColor, System.Windows.Media.ImageSource _imgsrc) {
                Size = _Size;
                Text = _Text;
                Padding = _Padding;
                BackgroundColor = _BackgroundColor;
                ForegroundColor = _ForegroundColor;
                imgsrc = _imgsrc;
            }
            public bool isEqual(int _Size, string _Text, int _Padding, uint _BackgroundColor, uint _ForegroundColor) {
                return (Size == _Size) && Text.Equals(_Text) && (Padding == _Padding) && (BackgroundColor == _BackgroundColor) && (ForegroundColor == _ForegroundColor);
            }
        }
        private List<CDrawString_LineCache> DrawString_LineCaches = new List<CDrawString_LineCache>();

        public System.Windows.Media.ImageSource DrawString(int FontSize, string Text, int Padding = 0, uint BackgroundColor = 0x00000000, uint ForegroundColor = 0x11ffffff) {
            if (FontSize == 0) { return null; }

            for (var idx = 0; idx < DrawString_LineCaches.Count; idx++) {
                if (DrawString_LineCaches[idx].isEqual(FontSize, Text, Padding, BackgroundColor, ForegroundColor)) {
                    var res = DrawString_LineCaches[idx];
                    DrawString_LineCaches.Remove(res);
                    DrawString_LineCaches.Add(res);
                    return (res.imgsrc);
                }
            }

            while (0x100 <= DrawString_LineCaches.Count) {
                DrawString_LineCaches.RemoveAt(0);
            }

            var MeasureStringRes = MeasureString(FontSize, Text, Padding);
            var rawbm = new MoonLib.CRawBitmap((MeasureStringRes.Width == 0) ? 1 : MeasureStringRes.Width, MeasureStringRes.Height);
            rawbm.Clear(BackgroundColor);

            var ds = new CDrawSettings(rawbm, FontSize, ForegroundColor, Padding);

            DrawString(ds, 0, 0, Text);

            var imgsrc = rawbm.GetBitmapSource(MoonLib.CDPI.GetDpiX(), MoonLib.CDPI.GetDpiY());
            DrawString_LineCaches.Add(new CDrawString_LineCache(FontSize, Text, Padding, BackgroundColor, ForegroundColor, imgsrc));

            return (imgsrc);
        }

        public class CDrawSettings {
            public MoonLib.CRawBitmap rawbm = null;
            public int FontSize;
            public UInt32 ForegroundColor;
            public bool ShadowEnabled;
            public UInt32 ShadowColor;
            public int Padding;
            public CDrawSettings(MoonLib.CRawBitmap _bm, int _FontSize, UInt32 _ForegroundColor, int _Padding = 0) {
                rawbm = _bm;
                FontSize = _FontSize;
                ForegroundColor = _ForegroundColor;
                ShadowColor = 0xff0000;
                ShadowEnabled = false;
                Padding = _Padding;
            }
            public CDrawSettings(MoonLib.CRawBitmap _bm, int _FontSize, UInt32 _ForegroundColor, UInt32 _ShadowColor, int _Padding = 0) {
                rawbm = _bm;
                FontSize = _FontSize;
                ForegroundColor = _ForegroundColor;
                ShadowEnabled = true;
                ShadowColor = _ShadowColor;
                Padding = _Padding;
            }
        }

        public class CMeasureStringRes {
            public int Width, Height;
        }
        public CMeasureStringRes MeasureString(int FontSize, string Text, int Padding = 0) {
            if ((FontSize == 0) || Text.Equals("")) {
                var res = new CMeasureStringRes();
                res.Width = (Padding * 2) + 1;
                res.Height = (Padding * 2) + FontSize;
                return (res);
            }

            CFontSpec FontSpec = GetFontSpecFromFontSize(FontSize);
            if (FontSpec == null) {
                var res = new CMeasureStringRes();
                res.Width = (Padding * 2) + 0;
                res.Height = (Padding * 2) + FontSize;
                for (var idx = 0; idx < Text.Length; idx++) {
                    res.Width += CGROM_Windows.GetFontWidth(FontSize, Text[idx]);
                }
                if (res.Width == 0) { res.Width = 1; }
                return res;
            } else {
                var sjistxt = ConvertSJIS(Text);

                var res = new CMeasureStringRes();
                res.Width = (Padding * 2) + (FontSpec.Width * sjistxt.Length * FontSpec.SizeMul);
                res.Height = (Padding * 2) + (FontSpec.Height * FontSpec.SizeMul);
                if (res.Width == 0) { res.Width = 1; }
                return res;
            }
        }

        public void DrawString(CDrawSettings DrawSettings, int x, int y, string Text) {
            var pad = DrawSettings.Padding;

            var ForegroundColor = (uint)DrawSettings.ForegroundColor;
            var ShadowColor = (uint)DrawSettings.ShadowColor;
            var ShadowEnabled = DrawSettings.ShadowEnabled;

            var NoShadowIndex = 0;
            if (Text.Equals(MDXWin.CCommon.AppConsole)) {
                ShadowColor = ForegroundColor;
                NoShadowIndex = 6;
            }

            CFontSpec FontSpec = GetFontSpecFromFontSize(DrawSettings.FontSize);
            if (FontSpec == null) {
                for (var idx = 0; idx < Text.Length; idx++) {
                    if (NoShadowIndex != 0) { ShadowEnabled = idx < NoShadowIndex; }
                    var FontFace = CGROM_Windows.GetFontFace(DrawSettings.FontSize, Text[idx]);
                    var srcidx = 0;
                    for (var sy = 0; sy < FontFace.Height; sy++) {
                        if (DrawSettings.rawbm.Height <= (y + sy)) { continue; }
                        for (var sx = 0; sx < FontFace.Width; sx++) {
                            if ((x + sx) < DrawSettings.rawbm.Width) {
                                if (FontFace.Data[srcidx]) {
                                    DrawSettings.rawbm.SetPixel(pad + x + sx, pad + y + sy, ForegroundColor);
                                    if (ShadowEnabled) { DrawSettings.rawbm.SetPixel(pad + x + sx + 1, pad + y + sy, ShadowColor); }
                                }
                            }
                            srcidx++;
                        }
                    }
                    x += FontFace.Width;
                }
                return;
            }

            var sjistxt = ConvertSJIS(Text);
            if (sjistxt.Length == 0) { return; }

            if ((y < 0) || (DrawSettings.rawbm.Height < (y + pad + (FontSpec.Height * FontSpec.SizeMul)))) { return; }

            var dstx = 0;
            for (var idx = 0; idx < sjistxt.Length; idx++) {
                if (NoShadowIndex != 0) { ShadowEnabled = idx < NoShadowIndex; }
                var Data1 = sjistxt[idx + 0];
                var Data2 = ((idx + 1) < sjistxt.Length) ? sjistxt[idx + 1] : (byte)0x00;
                var FontRect = GetFontRect(FontSpec, Data1, Data2);

                if (FontRect.FontFace != null) {
                    var dx = pad + x + dstx;
                    var dy = pad + y + 0;
                    var CheckRight = FontRect.iWidth * FontSpec.SizeMul;
                    if (ShadowEnabled) { CheckRight += 1 * FontSpec.SizeMul; }
                    if ((0 <= dx) && ((dx + CheckRight) <= DrawSettings.rawbm.Width)) {
                        var Data = FontRect.FontFace.Data;
                        var DataOfs = 0;
                        switch (FontSpec.SizeMul) {
                            case 1:
                                for (var ly = 0; ly < FontRect.iHeight; ly++) {
                                    for (var xofs = 0; xofs < (FontRect.iWidth / 8); xofs++) {
                                        var bits = Data[DataOfs++];
                                        for (var lx = 0; lx < 8; lx++) {
                                            if (((bits << lx) & 0x80) != 0) {
                                                DrawSettings.rawbm.SetPixel(dx + (xofs * 8) + lx, dy + ly, ForegroundColor);
                                                if (ShadowEnabled) { DrawSettings.rawbm.SetPixel(dx + (xofs * 8) + lx + 1, dy + ly, ShadowColor); }
                                            }
                                        }
                                    }
                                    if ((FontRect.iWidth % 8) != 0) {
                                        // 半端なデータは、12x12か12x24しかないので、4固定
                                        var xofs = ((FontRect.iWidth + 7) / 8) - 1;
                                        var bits = Data[DataOfs++];
                                        for (var lx = 0; lx < 4; lx++) {
                                            if (((bits << lx) & 0x80) != 0) {
                                                DrawSettings.rawbm.SetPixel(dx + (xofs * 8) + lx, dy + ly, ForegroundColor);
                                                if (ShadowEnabled) { DrawSettings.rawbm.SetPixel(dx + (xofs * 8) + lx + 1, dy + ly, ShadowColor); }
                                            }
                                        }
                                    }
                                }
                                break;
                            case 2:
                                for (var ly = 0; ly < FontRect.iHeight; ly++) {
                                    for (var xofs = 0; xofs < (FontRect.iWidth / 8); xofs++) {
                                        var bits = Data[DataOfs++];
                                        for (var lx = 0; lx < 8; lx++) {
                                            if (((bits << lx) & 0x80) != 0) {
                                                DrawSettings.rawbm.SetPixel4(dx + ((xofs * 8 + lx) * 2), dy + (ly * 2), ForegroundColor);
                                                if (ShadowEnabled) { DrawSettings.rawbm.SetPixel4(dx + ((xofs * 8 + lx + 1) * 2), dy + (ly * 2), ShadowColor); }
                                            }
                                        }
                                    }
                                    if ((FontRect.iWidth % 8) != 0) {
                                        // 半端なデータは、12x12か12x24しかないので、4固定
                                        var xofs = ((FontRect.iWidth + 7) / 8) - 1;
                                        var bits = Data[DataOfs++];
                                        for (var lx = 0; lx < 4; lx++) {
                                            if (((bits << lx) & 0x80) != 0) {
                                                DrawSettings.rawbm.SetPixel4(dx + ((xofs * 8 + lx) * 2), dy + (ly * 2), ForegroundColor);
                                                if (ShadowEnabled) { DrawSettings.rawbm.SetPixel4(dx + ((xofs * 8 + lx + 1) * 2), dy + (ly * 2), ShadowColor); }
                                            }
                                        }
                                    }
                                }
                                break;
                        }
                    }
                }

                if (FontRect.isKanji) { idx++; }
                dstx += FontRect.iWidth * FontSpec.SizeMul;
            }
        }

    }
}