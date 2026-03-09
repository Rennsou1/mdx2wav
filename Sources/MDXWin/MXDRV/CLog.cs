using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace MXDRV {
    public class CLog {
        public System.IO.StreamWriter sw = null;
        private List<string> Lines = new List<string>();

        public void WriteLine(List<string> _Lines) {
            foreach (var Line in _Lines) {
                if (sw != null) {
                    sw.WriteLine(Line);
                    Debug.WriteLine(Line);
                }
                Lines.Add(Line);
            }
        }
        public void WriteLine(string Line) {
            var _Lines = new List<string>();
            _Lines.Add(Line);
            WriteLine(_Lines);
        }

        public List<string> GetLines() {
            var res = Lines;
            Lines = new List<string>();
            return (res);
        }
    }
}
