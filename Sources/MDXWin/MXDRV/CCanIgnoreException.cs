using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MXDRV {
    public class CCanIgnoreException { // 続行可能な例外
        public static bool IgnoreException = true;

        private static List<string> Log = new List<string>();
        private static void SetLog(string Line) { lock (Log) { Log.Add(Line); } }
        public static List<string> GetLog() {
            var res = new List<string>();
            lock (Log) {
                foreach (var Line in Log) {
                    res.Add(Line);
                }
                Log.Clear();
            }
            return (res);
        }

        public static void Throw(string Caption, string Detail) {
            if (IgnoreException) {
                SetLog(Lang.CLang.GetMXDRV(Lang.CLang.EMXDRV.CanIgnoreException_Ignored) + " " + Caption + " " + Detail);
            } else {
                SetLog(Lang.CLang.GetMXDRV(Lang.CLang.EMXDRV.CanIgnoreException_Stopped) + " " + Caption + " " + Detail);
                throw new Exception("CanIgnoreException|" + Caption + "|" + Detail);
            }
        }

        public class CStack {
            public string Caption, Detail;
            public CStack(string _Caption, string _Detail) {
                Caption = _Caption;
                Detail = _Detail;
            }
        }
        public static CStack Stack = null;
        public static CStack GetStack() {
            var res = Stack;
            Stack = null;
            return res;
        }
    }
}
