using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MXDRV {
    internal class CMDXTitlePDXFilenameReader {
        private void ReadMDXFilename(CBuffer Buffer) {
            byte last = 0x00;
            byte lastlast = 0x00;
            var buf = new List<byte>();
            while (true) {
                var data = Buffer.ReadU8();
                if ((lastlast == 0x0d) && (last == 0x0a) && (data == 0x1a)) { break; }
                lastlast = last;
                last = data;
                if ((data == 0x0d) || (data == 0x0a) || (data == 0x1a)) { continue; }
                if (data < 0x20) { data = 0x20; }
                buf.Add(data);
                if (1024 <= buf.Count) { throw new Exception(Lang.CLang.GetMXDRV(Lang.CLang.EMXDRV. MDXPDXReader_StringOverflow)+" " + buf.Count); }
            }
            MDXTitleSJIS = buf.ToArray();
            MDXTitle = MoonLib.CTextEncode.SJIS.GetString(MDXTitleSJIS);
        }

        private void ReadPDXFilename(CBuffer Buffer) {
            var buf = new List<byte>();
            while (true) {
                var data = Buffer.ReadU8();
                if (data == 0x00) { break; }
                if (data < 0x20) { data = 0x20; }
                buf.Add(data);
                if (64 <= buf.Count) { throw new Exception(Lang.CLang.GetMXDRV(Lang.CLang.EMXDRV.MDXPDXReader_StringOverflow) +" " + buf.Count); }
            }
            PDXFilename = MoonLib.CTextEncode.SJIS.GetString(buf.ToArray());

            if (!PDXFilename.Equals("")) {
                if (!System.IO.Path.GetExtension(PDXFilename).Equals(".pdx", StringComparison.OrdinalIgnoreCase)) { PDXFilename += ".pdx"; } // マルチドットファイル名かもしれないので、拡張子変更ではなく拡張子を追加する。
                PDXFilename = PDXFilename.Replace(".mdx", "");
            }
        }

        public byte[] MDXTitleSJIS;
        public string MDXTitle, PDXFilename;

        public CMDXTitlePDXFilenameReader(CBuffer Buffer) {
            ReadMDXFilename(Buffer);
            MDXTitle = MDXTitle.Trim();
            ReadPDXFilename(Buffer);
            PDXFilename = PDXFilename.Trim();
        }
    }
}
