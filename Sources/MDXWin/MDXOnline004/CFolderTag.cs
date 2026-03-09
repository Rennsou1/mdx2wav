using Lang;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MDXOnline004 {
    public class CFolderTag {
        private string MasterPath = "";

        public string UseDriver = "";
        public string Category = "";
        public string MakerJpnEng = "";
        public string TitleJpnEng = "";
        public List<string> Authors = new List<string>();
        public List<string> Bikos = new List<string>();
        public string Package = "";
        public string Hardware = "";
        public string Dup = "";
        public List<string> UndefTags = new List<string>();
        public List<string> NoTags = new List<string>();

        public CFolderTag() {
        }

        public CFolderTag(string Path) {
            if (Path.Equals("")) { throw new Exception("CFolderTag: Empty path. Path=" + Path); }
            MasterPath = Path;

            foreach (var item in System.IO.Path.GetFileName(Path).Split(',')) {
                if (!item.StartsWith("$")) {
                    NoTags.Add(item);
                    continue;
                }

                var tag = item.Substring(0, 2);
                var body = item.Substring(2);
                if (body.Equals("")) {
                    UndefTags.Add(tag + "中身が空");
                } else {
                    if (body.Contains('$') | body.Contains(',')) { throw new Exception("タグの中身に不正な文字があります。 " + Path); }

                    switch (tag) {
                        case "$U":
                            if (!UseDriver.Equals("")) { throw new Exception("UseDriver tag が複数存在しています。 " + Path); }
                            UseDriver = body;
                            break;
                        case "$C":
                            if (!Category.Equals("")) { throw new Exception("Category tag が複数存在しています。 " + Path); }
                            Category = body;
                            break;
                        case "$M":
                            if (!MakerJpnEng.Equals("")) { throw new Exception("MakerJpnEng tag が複数存在しています。 " + Path); }
                            MakerJpnEng = body;
                            break;
                        case "$T":
                            if (!TitleJpnEng.Equals("")) { throw new Exception("TitleJpnEng tag が複数存在しています。 " + Path); }
                            TitleJpnEng = body;
                            break;
                        case "$A": Authors.Add(body); break;
                        case "$B": Bikos.Add(body); break;
                        case "$P":
                            if (!Package.Equals("")) { throw new Exception("Package tag が複数存在しています。 " + Path); }
                            Package = body;
                            break;
                        case "$H":
                            if (!Hardware.Equals("")) { throw new Exception("Hardware tag が複数存在しています。 " + Path); }
                            Hardware = body;
                            break;
                        case "$D":
                            if (!Dup.Equals("")) { throw new Exception("Dup tag が複数存在しています。 " + Path); }
                            Dup = body;
                            break;
                        default: UndefTags.Add(item); break;
                    }
                }
            }
        }

        public string GetTagsStr() {
            var Items = new List<string>();

            Items.AddRange(NoTags);

            if (!UseDriver.Equals("")) { Items.Add("$U" + UseDriver); }
            if (!Category.Equals("")) { Items.Add("$C" + Category); }
            foreach (var Item in Authors) { Items.Add("$A" + Item); }
            if (!MakerJpnEng.Equals("")) { Items.Add("$M" + MakerJpnEng); }
            if (!TitleJpnEng.Equals("")) { Items.Add("$T" + TitleJpnEng); }
            if (!Package.Equals("")) { Items.Add("$P" + Package); }
            foreach (var Item in Bikos) { Items.Add("$B" + Item); }
            if (!Hardware.Equals("")) { Items.Add("$H" + Hardware); }
            if (!Dup.Equals("")) { Items.Add("$D" + Dup); }

            Items.AddRange(UndefTags);

            return string.Join(',', Items);
        }

        public string GetApplyPath() {
            if (MasterPath.Equals("")) { throw new Exception("MasterPathが無い（無引数でnewした）ときはフォルダの名前を変えられません。"); }
            return System.IO.Path.GetDirectoryName(MasterPath) + @"\" + GetTagsStr();
        }

        public void ApplyTags() {
            if (MasterPath.Equals("")) { throw new Exception("MasterPathが無い（無引数でnewした）ときはフォルダの名前を変えられません。"); }
            var NewPath = GetApplyPath();
            System.IO.Directory.Move(MasterPath, NewPath);
            MasterPath = NewPath;
        }

        public bool hasAuthorタグ以外() {
            if (NoTags.Count != 0) { return true; }

            if (!UseDriver.Equals("")) { return true; }
            if (!Category.Equals("")) { return true; }
            // if (Authors.Count != 0) { return true; }
            if (!MakerJpnEng.Equals("")) { return true; }
            if (!TitleJpnEng.Equals("")) { return true; }
            if (!Package.Equals("")) { return true; }
            if (Bikos.Count != 0) { return true; }
            if (!Hardware.Equals("")) { return true; }
            if (!Dup.Equals("")) { return true; }

            if (UndefTags.Count != 0) { return true; }

            return false;
        }

        public bool EqualAuthorTags(CFolderTag Tag) {
            if (Authors.Count != Tag.Authors.Count) { return false; }
            foreach(var name in Authors) {
                if (!Tag.Authors.Contains(name)) { return false; }
            }
            return true;
        }

        private string LangJoin(bool Join, string src) {
            var Items = src.Split('%');
            switch (Items.Length) {
                case 0: return "";
                case 1: return Items[0];
                case 2:
                    switch (MDXWin.CCommon.INI.LangMode) {
                        case CLang.EMode.JPN: return Join?( Items[0] + " (" + Items[1] + ")"):Items[0];
                        case CLang.EMode.ENG: return Join?( Items[1] + " (" + Items[0] + ")"):Items[1];
                        default: return Items[0];
                    }
                default: return Items[0];
            }
        }

        public string GetTextFromFolderTag(bool Join=true) {
            var Items = new List<string>();

            Items.AddRange(NoTags);
            if (!UseDriver.Equals("")) { Items.Add(CLang.GetFolderTag(CLang.EFolderTag.TagNameU) + @": " + LangJoin(Join,UseDriver)); }
            if (!Category.Equals("")) { Items.Add(CLang.GetFolderTag(CLang.EFolderTag.TagNameC) + @": " + LangJoin(Join, Category)); }
            foreach (var Item in Authors) { Items.Add(CLang.GetFolderTag(CLang.EFolderTag.TagNameA) + @": " + LangJoin(Join, Item)); }
            if (!MakerJpnEng.Equals("")) { Items.Add(CLang.GetFolderTag(CLang.EFolderTag.TagNameM) + @": " + LangJoin(Join, MakerJpnEng)); }
            if (!TitleJpnEng.Equals("")) { Items.Add(CLang.GetFolderTag(CLang.EFolderTag.TagNameT) + @": " + LangJoin(Join, TitleJpnEng)); }
            if (!Package.Equals("")) { Items.Add(CLang.GetFolderTag(CLang.EFolderTag.TagNameP) + @": " + LangJoin(Join, Package)); }
            foreach (var Item in Bikos) { Items.Add(CLang.GetFolderTag(CLang.EFolderTag.TagNameB) + @": " + LangJoin(Join, Item)); }
            if (!Hardware.Equals("")) { Items.Add(CLang.GetFolderTag(CLang.EFolderTag.TagNameH) + @": " + LangJoin(Join, Hardware)); }
            if (!Dup.Equals("")) { Items.Add(CLang.GetFolderTag(CLang.EFolderTag.TagNameD) + @": " + LangJoin(Join, Dup)); }
            //Items.AddRange(UndefTags);

            var res = string.Join(", ", Items);
            return res.Equals("")? CLang.GetFolderTag(CLang.EFolderTag.Root) : res;
        }

    }
}
