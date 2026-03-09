using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO.Compression;
using System.Linq;
using System.Text;
using static Lang.CLang;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace MDXOnline004 {
    public class CClient {

        public const string Version = "Internet contents manager (Protocol:" + CCommon.ProtocolVersion + ") 2023/12/04";

        private System.Net.Http.HttpClient http = new System.Net.Http.HttpClient();

        private string HttpGetUserName = "";

        public static string HttpGetLang = "";

        public static bool IncludeFMPPMD = false;

        public static string UriEscapeDataString(string s) {
            return (Uri.EscapeDataString(s.Replace("&", "&amp;")));
        }
        public static string UriUnescapeDataString(string s) {
            return (Uri.UnescapeDataString(s.Replace("&amp;", "&")));
        }

        public class CServerINI {
            public string UseIP = "";
            public string IPv4Addr = "";
            public string IPv6Addr = "";
            public string Port = "";
            public string LatestClient = "";
            public string DefaultFolderID = "";
        }
        public static CServerINI Settings;
        public static bool LoadSettings() {
            Settings = new CServerINI();

            var cl = new System.Net.Http.HttpClient();
            cl.Timeout = System.TimeSpan.FromSeconds(5);
            var s = cl.GetStringAsync(@"https://mdxonline.s3.us-west-2.amazonaws.com/MDXOnline" + CCommon.ProtocolVersion + ".ini").Result;
            using (var sr = new System.IO.StringReader(s)) {
                while (true) {
                    var Line = sr.ReadLine();
                    if (Line == null) { break; }
                    var Items = Line.Split('=');
                    if (Items.Length != 2) { continue; }
                    switch (Items[0]) {
                        case "UseIP": Settings.UseIP = Items[1]; break;
                        case "IPv4Addr": Settings.IPv4Addr = Items[1]; break;
                        case "IPv6Addr": Settings.IPv6Addr = Items[1]; break;
                        case "Port": Settings.Port = Items[1]; break;
                        case "LatestClient": Settings.LatestClient = Items[1]; break;
                        case "DefaultFolderID": Settings.DefaultFolderID = Items[1]; break;
                    }
                }
            }

            if (Settings.UseIP.Equals("") || Settings.IPv4Addr.Equals("") || Settings.IPv6Addr.Equals("") || Settings.Port.Equals("") || Settings.LatestClient.Equals("") || Settings.DefaultFolderID.Equals("")) { return (false); }

            return (true);
        }

        public static bool isLatestClient(string Current) {
            return Settings.LatestClient.Equals(Current);
        }

        private static string GetBaseUrl() {
            switch (Settings.UseIP) {
                case "IPv4": return ("http://" + Settings.IPv4Addr + ":" + Settings.Port + "/MDXOnline" + CCommon.ProtocolVersion);
                case "IPv6": return ("http://[" + Settings.IPv6Addr + "]:" + Settings.Port + "/MDXOnline" + CCommon.ProtocolVersion);
                default: throw new Exception("未定義のUseIPモードです。 UseIP:" + Settings.UseIP);
            }
        }

        private MDXWin.CConsole Console;

        public string TempPath;

        public CClient(MDXWin.CConsole _Console) {
            http.Timeout = System.TimeSpan.FromSeconds(5);
            Console = _Console;
            TempPath = System.IO.Path.GetTempPath() + "MDXWinCaches";
            System.IO.Directory.CreateDirectory(TempPath);
        }

        public class CHttpGetRes {
            public System.TimeSpan ProgressTS;
            public byte[] Data;

            public string GetProgressStr() {
                var bps = Data.Length / ProgressTS.TotalSeconds;
                var res = "Download " + (Data.Length / 1024d).ToString("F3") + " KBytes. " + ProgressTS.TotalSeconds.ToString("F3") + " secs.";
                if (System.TimeSpan.FromSeconds(1) <= ProgressTS) { res += " " + (bps / 1024).ToString("F3") + " KBytes/Secs."; }
                return (res);
            }
        }
        public CHttpGetRes HttpGet(string url, bool UseGZip = false) {
            url = GetBaseUrl() + "?" + url + "&UserName=" + UriEscapeDataString(HttpGetUserName) + "&Lang=" + UriEscapeDataString(HttpGetLang);

            var request = new System.Net.Http.HttpRequestMessage {
                Method = System.Net.Http.HttpMethod.Get,
                RequestUri = new Uri(url),
            };
            request.Headers.Add("Accept-Encoding", "gzip");

            var res = new CHttpGetRes();

            var StartDT = System.DateTime.Now;
            var httpres = http.SendAsync(request).Result;
            res.ProgressTS = System.DateTime.Now - StartDT;
            if (!httpres.IsSuccessStatusCode) {
                var reason = (httpres.ReasonPhrase != null) ? httpres.ReasonPhrase : "no reason";
                throw new Exception("Not success status code. StatusCode=" + httpres.StatusCode.ToString() + " " + reason);
            }

            if (httpres.Content.Headers.ContentEncoding.Count==0) {
                var rs = httpres.Content.ReadAsStream();
                res.Data = new byte[rs.Length];
                rs.Read(res.Data);
            } else {
                if (httpres.Content.Headers.ContentEncoding.Contains("gzip")) {
                    using (var ms = new System.IO.MemoryStream()) {
                        using (var gzip = new GZipStream(httpres.Content.ReadAsStream(), CompressionMode.Decompress, true)) {
                            gzip.CopyTo(ms);
                        }
                        res.Data = ms.ToArray();
                    }
                } else {
                    throw new Exception("未知の圧縮方式です。 httpres.Content.Headers.ContentEncoding:" + httpres.Content.Headers.ContentEncoding.ToString());
                }
            }

            return (res);
        }

        public string HttpGet_Login(string UserName) {
            HttpGetUserName = UserName;
            var httpres = HttpGet("Command=" + CCommon.HttpCommand_Login);
            var res = System.Text.Encoding.UTF8.GetString(httpres.Data);
            return (res);
        }

        private Dictionary<int, CFolder> HttpGetFolderCache = new Dictionary<int, CFolder>();

        public CFolder CurrentFolder;

        public string HttpGetFolder(int FolderID) {
            try {
                CFolder res;
                if (HttpGetFolderCache.ContainsKey(FolderID)) {
                    res = HttpGetFolderCache[FolderID];
                } else {
                    var httpres = HttpGet("Command=" + CCommon.HttpCommand_GetFolder + "&FolderID=" + FolderID.ToString() + "&Mode=" + CCommon.HttpCommand_GetFolder_Mode_UTF8);
                    res = new CFolder(httpres.Data);
                    if (!IncludeFMPPMD) {
                        CFolder.CInFolder remove = null;
                        foreach (var InFolder in res.InFolders) {
                            if (InFolder.DirName.Equals("$UFMPPMD")) { remove = InFolder; }
                        }
                        if (remove != null) { res.InFolders.Remove(remove); }
                    }
                    HttpGetFolderCache[FolderID] = res;
                }
                CurrentFolder = res;
                return "";
            } catch (Exception ex) { return ex.Message; }
        }

        public CZip HttpGetFullFilesZip(string MD5) {
            try {
                var CacheFilename = TempPath + @"\" + MD5 + @".FullFiles.zip";
                if (System.IO.File.Exists(CacheFilename)) {
                    return new CZip(CacheFilename);
                } else {
                    var httpres = HttpGet("Command=" + CCommon.HttpCommand_GetFullFilesZip + "&ArchiveID=" + MD5);
                    using (var rfs = new System.IO.StreamWriter(CacheFilename)) { rfs.BaseStream.Write(httpres.Data); }
                    return new CZip(httpres.Data);
                }
            } catch { return null; }
        }

        public CZip HttpGetMusicFileOnlyZip(string MD5) {
            try {
                var CacheFilename = TempPath + @"\" + MD5 + @".MusicFileOnly.zip";
                if (System.IO.File.Exists(CacheFilename)) {
                    return new CZip(CacheFilename);
                } else {
                    var httpres = HttpGet("Command=" + CCommon.HttpCommand_GetMusicFileOnlyZip + "&ArchiveID=" + MD5);
                    using (var rfs = new System.IO.StreamWriter(CacheFilename)) { rfs.BaseStream.Write(httpres.Data); }
                    return new CZip(httpres.Data);
                }
            } catch { return null; }
        }

        public byte[] HttpGet_GetBosPdxHQ() {
            var httpres = HttpGet("Command=" + CCommon.HttpCommand_GetBosPdxHQ);
            Console.WriteLine(httpres.GetProgressStr());
            return (httpres.Data);
        }

        public string GetBrowserURL_GetFolder(int FolderID, string Lang) {
            return GetBaseUrl() + @"?Command=GetFolder&FolderID=" + FolderID.ToString() + @"&Mode=HTML&Lang=" + Lang;
        }
        public string GetBrowserURL_GetDocsHTML(string ArchiveID, string Lang) {
            return GetBaseUrl() + @"?Command=GetDocsHTML&ArchiveID=" + ArchiveID + @"#default";
        }

        public CZip HttpGetCompositeZip(string MD5) {
            var MusicZip = HttpGetMusicFileOnlyZip(MD5);
            if (MusicZip == null) { return null; }

            var PCMFilenames = new string[4] { MusicZip.Settings.PCM0Filename, MusicZip.Settings.PCM1Filename, MusicZip.Settings.PCM2Filename, MusicZip.Settings.PCM3Filename };
            var PCMMD5s = new string[4] { MusicZip.Settings.PCM0MD5, MusicZip.Settings.PCM1MD5, MusicZip.Settings.PCM2MD5, MusicZip.Settings.PCM3MD5 };
            for (var idx = 0; idx < PCMMD5s.Length; idx++) {
                var PCMMD5 = PCMMD5s[idx];
                if (PCMMD5.Equals("")) { continue; }
                var PCMZip = HttpGetFullFilesZip(PCMMD5);
                if (PCMZip == null) { return null; }
                foreach (var File in PCMZip.Files) {
                    if (File.Key.Equals(CZip.SettingsIniFilename)) { continue; }
                    MusicZip.Files[System.IO.Path.GetFileNameWithoutExtension(PCMFilenames[idx]) + System.IO.Path.GetExtension(File.Key)] = File.Value;
                }
            }

            return MusicZip;
        }
    }
}
