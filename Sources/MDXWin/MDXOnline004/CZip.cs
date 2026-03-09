using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace MDXOnline004 {
    public class CZip {
        public Dictionary<string, byte[]> Files = new Dictionary<string, byte[]>();
        public CSettings Settings;

        public const string SettingsIniFilename = "Settings.ini";

        public void Init(byte[] ZipData) {
            using (var ms = new System.IO.MemoryStream(ZipData)) {
                using (var zip = new ZipArchive(ms)) {
                    foreach (var Entry in zip.Entries) {
                        using (var s = Entry.Open()) {
                            var buf = new byte[(int)Entry.Length];
                            s.ReadExactly(buf);
                            Files[Entry.FullName] = buf;
                        }
                    }
                }
            }

            Settings = new CSettings(Files[SettingsIniFilename]); // Settings.iniは必ず入っているはず
        }
        public CZip(byte[] ZipData) {
            Init(ZipData);
        }
        public CZip(string ZipFilename) {
            Init(System.IO.File.ReadAllBytes(ZipFilename));
        }

        public byte[] GetDataFromExt(string ext) {
            foreach (var File in Files) {
                if (System.IO.Path.GetExtension(File.Key).Equals(ext, StringComparison.CurrentCultureIgnoreCase)) { return (File.Value); }
            }
            return (null);
        }

        public byte[] GetDataFromFilename(string fn) {
            foreach (var File in Files) {
                if (File.Key.Equals(fn, StringComparison.CurrentCultureIgnoreCase)) { return (File.Value); }
            }
            return (null);
        }

        public class CSettings {
            public string MD5 = "";
            public string Path = "";
            public long Size = 0;
            public DateTime DateTime = DateTime.FromBinary(0);
            public string Title = "";
            public TimeSpan PlayTS = TimeSpan.FromTicks(0);
            public double ReplayGain = 0;
            public string Exception = "";
            public string PCMMode = "";
            public string PCM0Filename = "";
            public string PCM0MD5 = "";
            public string PCM1Filename = "";
            public string PCM1MD5 = "";
            public string PCM2Filename = "";
            public string PCM2MD5 = "";
            public string PCM3Filename = "";
            public string PCM3MD5 = "";

            private void Load(Stream src) {
                using (var sr = new StreamReader(src)) {
                    while (!sr.EndOfStream) {
                        var Line = sr.ReadLine();
                        var pos = Line.IndexOf('=');
                        if (pos == -1) { continue; }
                        var Key = Line.Substring(0, pos);
                        var Value = Line.Substring(pos + 1);
                        switch (Key) {
                            case "MD5": MD5 = Value; break;
                            case "Path": Path = Value; break;
                            case "Size": Size = long.Parse(Value); break;
                            case "DateTime": DateTime = DateTime.FromBinary(long.Parse(Value)); break;
                            case "Title": Title = Value; break;
                            case "PlayTS": PlayTS = TimeSpan.FromTicks(long.Parse(Value)); break;
                            case "ReplayGain": ReplayGain = double.Parse(Value); break;
                            case "Exception": Exception = Value; break;
                            case "PCMMode": PCMMode = Value; break;
                            case "PCM0Filename": PCM0Filename = Value; break;
                            case "PCM0MD5": PCM0MD5 = Value; break;
                            case "PCM1Filename": PCM1Filename = Value; break;
                            case "PCM1MD5": PCM1MD5 = Value; break;
                            case "PCM2Filename": PCM2Filename = Value; break;
                            case "PCM2MD5": PCM2MD5 = Value; break;
                            case "PCM3Filename": PCM3Filename = Value; break;
                            case "PCM3MD5": PCM3MD5 = Value; break;
                            default: break; // 未知の項目は無視する
                        }
                    }
                }
            }

            public CSettings(Stream src) {
                Load(src);
            }
            public CSettings(string fn) {
                using (var sr = new StreamReader(fn)) {
                    Load(sr.BaseStream);
                }
            }
            public CSettings(byte[] buf) {
                using (var ms = new MemoryStream(buf)) {
                    Load(ms);
                }
            }
        }
    }
}
