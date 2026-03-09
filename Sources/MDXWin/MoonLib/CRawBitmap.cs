using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace MoonLib {
    public class CRawBitmap {
        public int Width, Height;
        private Int32[] buf;

        public CRawBitmap(int _Width, int _Height) {
            Width = _Width;
            Height = _Height;
            buf = new Int32[Width * Height];
        }

        public CRawBitmap(Uri uri) {
            var decoder = System.Windows.Media.Imaging.BitmapDecoder.Create(uri, System.Windows.Media.Imaging.BitmapCreateOptions.IgnoreColorProfile | System.Windows.Media.Imaging.BitmapCreateOptions.IgnoreImageCache, System.Windows.Media.Imaging.BitmapCacheOption.OnLoad);
            var bitmap = new System.Windows.Media.Imaging.FormatConvertedBitmap(decoder.Frames[0], System.Windows.Media.PixelFormats.Bgr24, null, 0);

            Width = bitmap.PixelWidth;
            Height = bitmap.PixelHeight;
            buf = new Int32[Width * Height];

            byte[] tmpbuf = new byte[Width * 3 * Height];
            bitmap.CopyPixels(tmpbuf, Width * 3, 0);
            for (var idx = 0; idx < Width * Height; idx++) {
                Int32 col = 0;
                col |= tmpbuf[idx * 3 + 0] << 0;
                col |= tmpbuf[idx * 3 + 1] << 8;
                col |= tmpbuf[idx * 3 + 2] << 16;
                buf[idx] = col;
            }
        }

        public Int32[] GetRawBuffer() {
            return (buf);
        }

        public System.Windows.Media.Imaging.BitmapSource GetBitmapSource(double DpiX, double DpiY) {
            var tmp = Marshal.AllocCoTaskMem(buf.Length * 4);
            Marshal.Copy(buf, 0, tmp, buf.Length);
            var imgsrc = System.Windows.Media.Imaging.BitmapSource.Create(Width, Height, DpiX, DpiY, System.Windows.Media.PixelFormats.Bgr32, null, tmp, buf.Length * 4, Width * 4);
            Marshal.FreeCoTaskMem(tmp);
            imgsrc.Freeze();
            return (imgsrc);
        }

        public void FillRect(int x, int y, int w, int h, UInt32 Color) {
            var ofs = (y * Width) + x;
            for (var ly = 0; ly < h; ly++) {
                for (var lx = 0; lx < w; lx++) {
                    buf[ofs + lx] = (Int32)Color;
                }
                ofs += Width;
            }
        }

        public void Draw横棒(int x, int y, int w, UInt32 Color) {
            var ofs = (y * Width) + x;
            for (var lx = 0; lx < w; lx++) {
                buf[ofs + lx] = (Int32)Color;
            }
        }

        public void Draw縦棒(int x, int y, int h, UInt32 Color) {
            var ofs = (y * Width) + x;
            for (var ly = 0; ly < h; ly++) {
                buf[ofs] = (Int32)Color;
                ofs += Width;
            }
        }

        public void FillRectAdd(int x, int y, int w, int h, UInt32 Color) {
            var sr = Color & 0xff0000;
            var sg = Color & 0x00ff00;
            var sb = Color & 0x0000ff;
            for (var ly = y; ly < (y + h); ly++) {
                for (var lx = x; lx < (x + w); lx++) {
                    var d = buf[ly * Width + lx];
                    var r = sr + (d & 0xff0000);
                    var g = sg + (d & 0x00ff00);
                    var b = sb + (d & 0x0000ff);
                    if (0xff0000 < r) { r = 0xff0000; }
                    if (0x00ff00 < g) { g = 0x00ff00; }
                    if (0x0000ff < b) { b = 0x0000ff; }
                    buf[ly * Width + lx] = (Int32)(r | g | b);
                }
            }
        }

        public void DrawBox(int x, int y, int w, int h, UInt32 Color) {
            w--;
            h--;
            for (var ly = y; ly < (y + h); ly++) {
                buf[ly * Width + x] = (Int32)Color;
                buf[ly * Width + x + w] = (Int32)Color;
            }
            for (var lx = x; lx < (x + w); lx++) {
                buf[y * Width + lx] = (Int32)Color;
                buf[(y + h) * Width + lx] = (Int32)Color;
            }
            buf[(y + h) * Width + x + w] = (Int32)Color;
        }

        public void Clear(UInt32 Color) {
            Array.Fill<Int32>(buf, (Int32)Color);
        }

        public void SetPixel(int x, int y, UInt32 Color) {
            buf[y * Width + x] = (Int32)Color;
        }
        public void SetPixel4(int x, int y, UInt32 Color) {
            buf[(y + 0) * Width + x + 0] = (Int32)Color;
            buf[(y + 0) * Width + x + 1] = (Int32)Color;
            buf[(y + 1) * Width + x + 0] = (Int32)Color;
            buf[(y + 1) * Width + x + 1] = (Int32)Color;
        }

        public void CopyTo(CRawBitmap dstbm) {
            if ((Width != dstbm.Width) || (Height != dstbm.Height)) { throw new Exception("CopyToの元と先のサイズが違います。"); }

            Array.Copy(buf, dstbm.buf, dstbm.buf.Length);
            return;
            for (var idx = 0; idx < Width * Height; idx++) {
                dstbm.buf[idx] = buf[idx];
            }
        }

        public void CopyTo縦横2倍(CRawBitmap dstbm) {
            if ((Width * 2 != dstbm.Width) || (Height * 2 != dstbm.Height)) { throw new Exception("CopyToの元と先のサイズが2倍ではありません。"); }

            for (var y = 0; y < Height; y++) {
                var srcofs = y * Width;
                var dstofs = (y * 2) * dstbm.Width;
                for (var x = 0; x < Width; x++) {
                    var col = buf[srcofs + x];
                    dstbm.buf[dstofs + (dstbm.Width * 0) + (x * 2) + 0] = col;
                    dstbm.buf[dstofs + (dstbm.Width * 0) + (x * 2) + 1] = col;
                    dstbm.buf[dstofs + (dstbm.Width * 1) + (x * 2) + 0] = col;
                    dstbm.buf[dstofs + (dstbm.Width * 1) + (x * 2) + 1] = col;
                }
            }
        }

        public void Blit(int x, int y, CRawBitmap srcbm, System.Windows.Int32Rect srcrect) {
            for (var ly = 0; ly < srcrect.Height; ly++) {
                for (var lx = 0; lx < srcrect.Width; lx++) {
                    buf[(x + lx) + ((y + ly) * Width)] = srcbm.buf[(srcrect.X + lx) + ((srcrect.Y + ly) * srcbm.Width)];
                }
            }
        }

        public void Scroll_左に1ドット() {
            for (var ly = 0; ly < Height; ly++) {
                for (var lx = 0; lx < Width - 1; lx++) {
                    var ofs = ly * Width + lx;
                    buf[ofs] = buf[ofs + 1];
                }
            }
        }

        public void DarkRect(int x, int y, int w, int h, byte Alpha) {
            if (Alpha == 0x80) {
                for (var ly = y; ly < (y + h); ly++) {
                    for (var lx = x; lx < (x + w); lx++) {
                        buf[ly * Width + lx] = (buf[ly * Width + lx] >> 1) & 0x007f7f7f;
                    }
                }
            } else {
                if (Alpha == 0x40) {
                    for (var ly = y; ly < (y + h); ly++) {
                        for (var lx = x; lx < (x + w); lx++) {
                            buf[ly * Width + lx] = (buf[ly * Width + lx] >> 2) & 0x003f3f3f;
                        }
                    }
                } else {
                    for (var ly = y; ly < (y + h); ly++) {
                        for (var lx = x; lx < (x + w); lx++) {
                            var d = buf[ly * Width + lx];
                            var r = (((d & 0xff0000) * Alpha) >> 8) & 0xff0000;
                            var g = (((d & 0x00ff00) * Alpha) >> 8) & 0x00ff00;
                            var b = (((d & 0x0000ff) * Alpha) >> 8) & 0x0000ff;
                            buf[ly * Width + lx] = (Int32)(r | g | b);
                        }
                    }
                }
            }
        }

        private static int[] WVisual_CVisNote_DrawBM_ColDark = null; // ColDark = 0x5353AD;
        private static int[] WVisual_CVisNote_DrawBM_ColBright = null; // ColBright = 0x8787FF;

        public void WVisual_CVisNote_DrawBM_Overwrite(int y, int SrcWidth, byte[] NoteBM, byte[] FineBM) {
            if (WVisual_CVisNote_DrawBM_ColDark == null) {
                WVisual_CVisNote_DrawBM_ColDark = new int[0x100];
                for (var Level = 0x00; Level < 0x100; Level++) {
                    var r = Level * 0x53 / 0x100;
                    var g = Level * 0x53 / 0x100;
                    var b = Level * 0xAD / 0x100;
                    WVisual_CVisNote_DrawBM_ColDark[Level] = (Int32)(r << 16 | g << 8 | b);
                }
            }
            if (WVisual_CVisNote_DrawBM_ColBright == null) {
                WVisual_CVisNote_DrawBM_ColBright = new int[0x100];
                for (var Level = 0x00; Level < 0x100; Level++) {
                    var r = Level * 0x87 / 0x100;
                    var g = Level * 0x87 / 0x100;
                    var b = Level * 0xFF / 0x100;
                    WVisual_CVisNote_DrawBM_ColBright[Level] = (Int32)(r << 16 | g << 8 | b);
                }
            }

            const int H = 10;

            for (var x = 0; x < SrcWidth; x++) {
                {
                    var Level = FineBM[x];
                    if (16 <= Level) {
                        var col = WVisual_CVisNote_DrawBM_ColDark[Level];
                        var wofs = (y * Width) + x;
                        for (var _y = 0; _y < H; _y++) {
                            buf[wofs] = col;
                            wofs += Width;
                        }
                    }
                }
                {
                    var Level = NoteBM[x];
                    if (16 <= Level) {
                        var col = WVisual_CVisNote_DrawBM_ColBright[Level];
                        var wofs = (y * Width) + x;
                        for (var _y = 0; _y < H; _y++) {
                            buf[wofs] = col;
                            wofs += Width;
                        }
                    }
                }
            }
        }

        public void WVisual_CVisNote_DrawBM_Add(int y, int SrcWidth, byte[] NoteBM, byte[] FineBM) {
            const int H = 10;

            for (var x = 0; x < SrcWidth; x++) {
                var Fine = FineBM[x];
                var Note = NoteBM[x];
                if ((Fine == 0) && (Note == 0)) { continue; }

                var FineR = Fine * 0x53 / 0x100;
                var FineG = Fine * 0x53 / 0x100;
                var FineB = Fine * 0xAD / 0x100;

                var NoteR = Note * 0x87 / 0x100;
                var NoteG = Note * 0x87 / 0x100;
                var NoteB = Note * 0xFF / 0x100;

                var wofs = (y * Width) + x;
                for (var _y = 0; _y < H; _y++) {
                    var pix = buf[wofs];
                    var r = (pix >> 16) & 0xff;
                    var g = (pix >> 8) & 0xff;
                    var b = (pix >> 0) & 0xff;
                    if (Fine != 0) { r += FineR; g += FineG; b += FineB; }
                    if (Note != 0) { r += NoteR; g += NoteG; b += NoteB; }
                    if (0xff < r) { r = 0xff; }
                    if (0xff < g) { g = 0xff; }
                    if (0xff < b) { b = 0xff; }
                    buf[wofs] = (Int32)(r << 16 | g << 8 | b);
                    wofs += Width;
                }
            }
        }

    }
}