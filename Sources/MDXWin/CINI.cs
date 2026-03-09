using SharpDX.Direct2D1;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace MDXWin {
    internal class CINI {
        private string inifn = "MDXWin.ini";

        public bool InitialBoot = false;

        private string _UserName = "";
        public string UserName {
            get { return _UserName; }
            set {
                value = value.Replace("&", "").Replace("=", "").Replace("<", "").Replace(">", "").Replace("|", "").Replace(@"\", "").Replace("/", "");
                if (!_UserName.Equals(value)) {
                    _UserName = value;
                    Save();
                }
            }
        }

        private int _CurrentFolderID = 0;
        public int CurrentFolderID {
            get { return _CurrentFolderID; }
            set {
                if (_CurrentFolderID != value) {
                    _CurrentFolderID = value;
                    Save();
                }
            }
        }

        private Lang.CLang.EMode _LangMode = Lang.CLang.EMode.JPN;
        public Lang.CLang.EMode LangMode {
            get { return _LangMode; }
            set {
                if (!_LangMode.Equals(value)) {
                    _LangMode = value;
                    Save();
                }
            }
        }

        private bool _AcceptDefaultBrowser = false;
        public bool AcceptDefaultBrowser {
            get { return _AcceptDefaultBrowser; }
            set {
                if (!_AcceptDefaultBrowser.Equals(value)) {
                    _AcceptDefaultBrowser = value;
                    Save();
                }
            }
        }

        public CINI() {
            _LangMode = System.Globalization.CultureInfo.CurrentCulture.Name.Equals("ja-JP") ? Lang.CLang.EMode.JPN : Lang.CLang.EMode.ENG; // この時点ではINIファイルに保存しないので、直接変数に代入する。

            if (!System.IO.File.Exists(inifn)) { InitialBoot = true; return; }

            var TempUserName = "";
            var TempCurrentFolderID = 0;
            var TempLangMode = LangMode;
            var TempAcceptDefaultBrowser = false;

            using (var rfs = new System.IO.StreamReader(inifn)) {
                while (!rfs.EndOfStream) {
                    var Line = rfs.ReadLine();
                    var pos = Line.IndexOf('=');
                    if (pos == -1) { continue; }
                    var Key = Line.Substring(0, pos);
                    var Value = Line.Substring(pos + 1);
                    switch (Key) {
                        case "UserName": TempUserName = Value; break;
                        case "CurrentFolderID": TempCurrentFolderID = int.Parse(Value); break;
                        case "LangMode": {
                            switch (Value) {
                                case "JPN": TempLangMode = Lang.CLang.EMode.JPN; break;
                                case "ENG": TempLangMode = Lang.CLang.EMode.ENG; break;
                            }
                        }
                        break;
                        case "AcceptDefaultBrowser": TempAcceptDefaultBrowser = bool.Parse(Value); break;
                    }
                }
            }

            if (!TempUserName.Equals("")) { UserName = TempUserName; }
            if (!TempCurrentFolderID.Equals("")) { CurrentFolderID = TempCurrentFolderID; }
            LangMode = TempLangMode;
            AcceptDefaultBrowser = TempAcceptDefaultBrowser;
        }

        private void Save() {
            for (var loop = 0; loop < 10; loop++) {
                try {
                    using (var wfs = new System.IO.StreamWriter(inifn)) {
                        wfs.WriteLine("UserName=" + UserName);
                        wfs.WriteLine("CurrentFolderID=" + CurrentFolderID.ToString());
                        wfs.WriteLine("LangMode=" + LangMode.ToString());
                        wfs.WriteLine("AcceptDefaultBrowser=" + AcceptDefaultBrowser.ToString());
                    }
                } catch {
                    if (loop == 9) {
                        CCommon.Console.WriteLine(inifn + " write error.");
                        break;
                    }
                    System.Threading.Thread.Sleep(100);
                }
            }
        }

    }
}
