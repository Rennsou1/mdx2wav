using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MDXOnline004 {
    public class CFolder {
        public int ID;
        public string BaseName;
        public class CParentFolder {
            public int ID;
            public string BaseName;
            public int FilesCount;
        }
        public List<CParentFolder> ParentFolders = new List<CParentFolder>();

        public class CInFolder {
            public int ID;
            public string DirName;
            public int FilesCount;
        }
        public List<CInFolder> InFolders = new List<CInFolder>();

        public class CFile {
            public string MD5;
            public string Filename;
            public long Size;
            public DateTime DateTime;
            public string Title;
            public string TitleRaw;
            public TimeSpan PlayTS;
            public string PCMMode;
        }
        public List<CFile> Files = new List<CFile>();

        private void Load(Stream src) {
            using (var rfs = new StreamReader(src)) {
                ID = int.Parse(rfs.ReadLine());
                BaseName = rfs.ReadLine();

                while (true) {
                    var ID = rfs.ReadLine();
                    if (ID.Equals("")) { break; }
                    var ParentFolder = new CParentFolder();
                    ParentFolder.ID = int.Parse(ID);
                    ParentFolder.BaseName = rfs.ReadLine();
                    ParentFolder.FilesCount = int.Parse(rfs.ReadLine());
                    ParentFolders.Add(ParentFolder);
                }

                while (true) {
                    var ID = rfs.ReadLine();
                    if (ID.Equals("")) { break; }
                    var InFolder = new CInFolder();
                    InFolder.ID = int.Parse(ID);
                    InFolder.DirName = rfs.ReadLine();
                    InFolder.FilesCount = int.Parse(rfs.ReadLine());
                    InFolders.Add(InFolder);
                }

                while (true) {
                    var MD5 = rfs.ReadLine();
                    if (MD5.Equals("")) { break; }
                    var File = new CFile();
                    File.MD5 = MD5;
                    File.Filename = rfs.ReadLine();
                    File.Size = long.Parse(rfs.ReadLine());
                    File.DateTime = System.DateTime.FromBinary(long.Parse(rfs.ReadLine()));
                    File.Title = rfs.ReadLine();
                    File.TitleRaw = rfs.ReadLine();
                    File.PlayTS = System.TimeSpan.FromTicks(long.Parse(rfs.ReadLine()));
                    File.PCMMode = rfs.ReadLine();
                    Files.Add(File);
                }
            }
        }

        public CFolder(Stream src) {
            Load(src);
        }
        public CFolder(string fn) {
            using (var sr = new StreamReader(fn)) {
                Load(sr.BaseStream);
            }
        }
        public CFolder(byte[] buf) {
            using (var ms = new MemoryStream(buf)) {
                Load(ms);
            }
        }

        public List<string> GetAllMD5s() {
            var res=new List<string>();
            foreach(var File in Files) {
                res.Add(File.MD5);
            }
            return res;
        }
    }
}
