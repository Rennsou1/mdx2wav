using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MDXWin {
    public class CConsole {
        public class CLog {
            public class CItem {
                public string Line;
                public System.Windows.Media.ImageSource ViewItem_Line_Image {
                    get {
                        return CCommon.CGROM.DrawString(CCommon.FontSize_Console, Line);
                    }
                    set { throw new NotImplementedException(); }
                }
                public enum EMode { Text, Command, Dir, File };
                public EMode Mode;
                public string HintText = "";
                public CItem(string _Line = "") {
                    if (_Line == null) { throw new Exception(); }
                    Line = _Line;
                    Mode = EMode.Text;
                    HintText = "";
                }
                public CItem(string _Line, EMode _Mode, string _HintText) {
                    if (_Line == null) { throw new Exception(); }
                    Line = _Line;
                    Mode = _Mode;
                    HintText = _HintText;
                }
            }
            public List<CItem> Items = new List<CItem>();

            MainWindow Parent;
            public CLog(MainWindow _Parent) {
                Parent = _Parent;
                Parent.LogList.ItemsSource = Items;
            }

            public void Refresh(bool カーソルを最下行に移動する = true) {
                Parent.LogList.ItemsSource = null;
                Parent.LogList.ItemsSource = Items;
                if (カーソルを最下行に移動する) { Parent.LogList.ScrollIntoView(Items[Items.Count - 1]); }
            }
        }
        private CLog Log;

        public CConsole(MainWindow Parent) {
            Log = new CLog(Parent);
        }
        public void Echo(string Line) {
            var Item = Log.Items[Log.Items.Count - 1];
            Item.Line += Line;
            Log.Items[Log.Items.Count - 1] = Item;
            Refresh();
        }
        public void WriteLine(List<string> Lines) {
            if ((Lines == null) || (Lines.Count == 0)) { return; }
            foreach (var Line in Lines) {
                Log.Items.Add(new CLog.CItem(Line));
            }
            Refresh();
        }
        public void WriteLine(List<CLog.CItem> Lines) {
            if ((Lines == null) || (Lines.Count == 0)) { return; }
            foreach (var Line in Lines) {
                Log.Items.Add(Line);
            }
            Refresh();
        }
        public void WriteLine(string Line = "") { WriteLine(new List<string>() { Line }); }
        
        public void WriteLine最後から一行前に追加(List<string> Lines) {
            if ((Lines == null) || (Lines.Count == 0)) { return; }
            foreach (var Line in Lines) {
                var idx = (Log.Items.Count <= 0) ? 0 : (Log.Items.Count - 1);
                Log.Items.Insert(idx, new CLog.CItem(Line));
            }
            Refresh();
        }
        public void WriteLine最後から一行前に追加(string Line = "") { WriteLine最後から一行前に追加(new List<string>() { Line }); }

        public void WriteLine最後の行を置き換え(string Line = "") {
            Log.Items[Log.Items.Count - 1] = new CLog.CItem(Line);
            Refresh();
        }

        public void Refresh(bool カーソルを最下行に移動する = true) {
            Log.Refresh(カーソルを最下行に移動する);
        }
    }
}
