using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MDXWin {
    public class CPlayList {
        public enum EPlayMode { Single, Repeat, Normal, Random };
        private static EPlayMode _PlayMode = EPlayMode.Normal;
        public static EPlayMode PlayMode { set { _PlayMode = value; } get { return _PlayMode; } }

        public string[] MD5s = null;
        public int PlayIndex = -1;
        public CPlayList(List<string> _MD5s, string PlayingMD5) {
            if (_MD5s != null) {
                MD5s = _MD5s.ToArray();
                for (var idx = 0; idx < MD5s.Length; idx++) {
                    if (MD5s[idx].Equals(PlayingMD5)) { PlayIndex = idx; }
                }
                if (PlayIndex == -1) { MD5s = null; }
            }
        }
        public void Clear() {
            MD5s = null;
            PlayIndex = -1;
        }
        public string Next() {
            if ((MD5s == null) || (PlayIndex == -1)) { return (""); }
            switch (PlayMode) {
                case EPlayMode.Single: return ("");
                case EPlayMode.Repeat: return (MD5s[PlayIndex]);
                case EPlayMode.Normal:
                    PlayIndex = (PlayIndex + 1) % MD5s.Length;
                    return (MD5s[PlayIndex]);
                case EPlayMode.Random:
                    PlayIndex = new System.Random().Next(MD5s.Length);
                    return (MD5s[PlayIndex]);
                default: throw new Exception("Undefined play mode.");
            }
        }
    }
}
