using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace MoonLib {
    internal class CCGROM_Windows {
        private class CFont {
            public int Height;
            public Dictionary<char, bool[]> FontFaces = new Dictionary<char, bool[]>();
            public CFont(int _Height) {
                Height = _Height;
            }
        }
        private Dictionary<int, CFont> Fonts = new Dictionary<int, CFont>();

        private class CBitmapToByteArray_res {
            public int Stride;
            public byte[] Data;
        }
        private static CBitmapToByteArray_res BitmapToByteArray(System.Drawing.Bitmap bitmap, int Channels) {
            System.Drawing.Imaging.BitmapData bmpdata = null;

            try {
                bmpdata = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, bitmap.PixelFormat);
                int numbytes = bmpdata.Stride * bitmap.Height;

                var res = new CBitmapToByteArray_res();
                res.Stride = bmpdata.Stride;
                res.Data = new byte[numbytes];

                IntPtr ptr = bmpdata.Scan0;

                System.Runtime.InteropServices.Marshal.Copy(ptr, res.Data, 0, numbytes);

                return res;
            } finally {
                if (bmpdata != null) { bitmap.UnlockBits(bmpdata); }
            }
        }

        private static void DrawText(System.Drawing.Graphics g, int x, int y, int ofsx, int ofsy, char ch, int FontSize) {
            using (var fnt = new System.Drawing.Font("MS Gothic", FontSize)) {
                x += ofsx * FontSize / 64;
                y += ofsy * FontSize / 64;
                g.DrawString(ch.ToString(), fnt, System.Drawing.Brushes.White, x, y);
            }
        }

        private static char Han2Zen(char ch) {
            {
                var str = ch.ToString();
                str = Regex.Replace(str, "[0-9]", p => ((char)(p.Value[0] - '0' + '０')).ToString());
                str = Regex.Replace(str, "[a-z]", p => ((char)(p.Value[0] - 'a' + 'ａ')).ToString());
                str = Regex.Replace(str, "[A-Z]", p => ((char)(p.Value[0] - 'A' + 'Ａ')).ToString());
                ch = str[0];
            }

            switch (ch) {
                case ' ': ch = '　'; break;
                case '!': ch = '！'; break;
                case '"': ch = '”'; break;
                case '#': ch = '＃'; break;
                case '$': ch = '＄'; break;
                case '%': ch = '％'; break;
                case '&': ch = '＆'; break;
                case '(': ch = '（'; break;
                case ')': ch = '）'; break;
                case '*': ch = '＊'; break;
                case '+': ch = '＋'; break;
                case ',': ch = '，'; break;
                case '-': ch = '－'; break;
                case '.': ch = '．'; break;
                case '/': ch = '／'; break;
                case ':': ch = '：'; break;
                case ';': ch = '；'; break;
                case '<': ch = '＜'; break;
                case '=': ch = '＝'; break;
                case '>': ch = '＞'; break;
                case '?': ch = '？'; break;
                case '@': ch = '＠'; break;
                case '[': ch = '［'; break;
                case '\\': ch = '￥'; break;
                case ']': ch = '］'; break;
                case '^': ch = '＾'; break;
                case '_': ch = '＿'; break;
                case '`': ch = '’'; break;
                case '{': ch = '｛'; break;
                case '|': ch = '｜'; break;
                case '}': ch = '｝'; break;
                case '~': ch = '～'; break;
            }

            return ch;
        }
        
        public int GetFontWidth(int FontSize, char ch) {
            if ((FontSize == 8) || (FontSize == 12)) { ch = Han2Zen(ch); }

            var Width = FontSize;
            if (ch < 0x100) { Width /= 2; }

            return Width;
        }

        public class CGetFontFace_Res {
            public int Width, Height;
            public bool[] Data = null;
        }
        public CGetFontFace_Res GetFontFace(int FontSize, char ch) {
            var res = new CGetFontFace_Res();

            if ((FontSize == 8) || (FontSize == 12)) { ch = Han2Zen(ch); }

            {
                res.Width = FontSize;
                if (ch < 0x100) { res.Width /= 2; }
                res.Height = FontSize;
            }

            if (!Fonts.ContainsKey(FontSize)) { Fonts[FontSize] = new CFont(FontSize); }
            if (Fonts[FontSize].FontFaces.ContainsKey(ch)) { res.Data = Fonts[FontSize].FontFaces[ch]; }

            if (res.Data == null) {
                var bm = new System.Drawing.Bitmap(res.Width, res.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                bm.SetResolution(72, 72);

                using (var g = System.Drawing.Graphics.FromImage(bm)) {
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;
                    DrawText(g, 0, 0, -10, 0, ch, FontSize);
                }

                res.Data = new bool[res.Width * res.Height];

                var bmdata = BitmapToByteArray(bm, 3);
                for (var x = 0; x < res.Width; x++) {
                    for (var y = 0; y < res.Height; y++) {
                        res.Data[(y * res.Width) + x] = bmdata.Data[(y * bmdata.Stride) + (x * 3)] != 0x00;
                    }
                }

                Fonts[FontSize].FontFaces[ch] = res.Data;
            }

            return res;
        }
    }
}
