using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MXDRV {
    public class CVoiceName {
        private static List<string> LoadMML(string MDXFilename) {
            var fn = "";
            if (System.IO.File.Exists(System.IO.Path.ChangeExtension(MDXFilename, ".mml"))) { fn = System.IO.Path.ChangeExtension(MDXFilename, ".mml"); }
            if (System.IO.File.Exists(System.IO.Path.ChangeExtension(MDXFilename, ".mus"))) { fn = System.IO.Path.ChangeExtension(MDXFilename, ".mus"); }

            var res = new List<string>();
            if (fn.Equals("")) { return res; }

            using (var sr = new System.IO.StreamReader(fn)) {
                while (!sr.EndOfStream) {
                    res.Add(sr.ReadLine());
                }
            }

            return res;
        }

        public static Dictionary<int, string> GetVoiceNamesFromMML(string MDXFilename) {
            var res = new Dictionary<int, string>();

            var MML = LoadMML(MDXFilename);

            var LastLine = "";

            foreach (var Line in MML) {
                if (Line.StartsWith("@") && !Line.StartsWith("@@") && !Line.StartsWith("@w") && (Line.IndexOf('=') != -1)) {
                    var VoiceNum = int.Parse(Line.Substring(1, Line.IndexOf('=') - 1));

                    var curpos = -1;
                    if (Line.IndexOf(';') != -1) { curpos = Line.IndexOf(';'); }
                    if (Line.IndexOf('*') != -1) { curpos = Line.IndexOf('*'); }
                    if (curpos != -1) {
                        res[VoiceNum] = Line.Substring(curpos + 1).Trim();
                    } else {
                        if (LastLine.StartsWith("//")) { LastLine = LastLine.Substring(2); }
                        if (LastLine.StartsWith('/')) { LastLine = LastLine.Substring(1); }
                        if (LastLine.StartsWith('*')) { LastLine = LastLine.Substring(1); }
                        if (LastLine.EndsWith('/')) { LastLine = LastLine.Substring(0, LastLine.Length - 1); }
                        if (LastLine.EndsWith('*')) { LastLine = LastLine.Substring(0, LastLine.Length - 1); }
                        if (LastLine.StartsWith(";")) { LastLine = LastLine.Substring(1); }
                        if (LastLine.StartsWith(": ")) { LastLine = LastLine.Substring(2); }
                        if (LastLine.EndsWith('}')) { LastLine = LastLine.Substring(0, LastLine.Length - 1); }
                        LastLine = LastLine.Replace("AR D1R D2R  RR D1L  TL  RS MUL DT1 DT2 AMS-EN", "");
                        LastLine = LastLine.Replace("AR D1R D2R  RR D1L  TL  RS MUL DT1 DT2 AMS", "");
                        LastLine = LastLine.Replace("AR  DR  SR  RR  SL  OL  KS  ML DT1 DT2 AME", "");
                        LastLine = LastLine.Replace("AR  DR  SR  RR  SL  OL  KS  ML DT1 DT2 AMS-EN", "");
                        LastLine = LastLine.Replace("AR D1R D2R  RR D1L  TL  RS MUL DT1 DT2 AME", "");
                        LastLine = LastLine.Replace("AR 1DR 2DR  RR 1DL  TL  RS MUL DT1 DT2 AME", "");
                        LastLine = LastLine.Replace("AR 1DR 2DR  RR 1DL  TL  RS MUL DT1 DT2 AME", "");
                        LastLine = LastLine.Replace("AR   DR   SR   RR   SL   OL   KS   ML  DT1  DT2  AME", "");
                        LastLine = LastLine.Replace("-----", "");
                        LastLine = LastLine.Replace("----", "");
                        LastLine = LastLine.Replace("#pcmfile tes01.pdx", "");
                        LastLine = LastLine.Replace("o3q6", "");
                        LastLine = LastLine.Replace("y55,0MH3,200,127,0,7,0,0MHONo5e16", "");

                        LastLine = LastLine.Trim();
                        if ((LastLine.StartsWith('[') && LastLine.EndsWith(']')) || (LastLine.StartsWith('(') && LastLine.EndsWith(')'))) {
                            LastLine = LastLine.Substring(1, LastLine.Length - 2); LastLine = LastLine.Trim();
                        }

                        while (true) {
                            if (LastLine.IndexOf("  ") == -1) { break; }
                            LastLine = LastLine.Replace("  ", "");
                        }

                        if (LastLine.Equals("") || LastLine.Equals("音色定義") || LastLine.Equals("") || LastLine.Equals("") || LastLine.Equals("") || LastLine.Equals("}")) { continue; }

                        res[VoiceNum] = LastLine;
                    }
                }
                LastLine = Line;
            }

            return res;
        }

        public static Dictionary<int, string> GetVoiceNamesFromBosPDX() {
            return new Dictionary<int, string>() {
                {(0x00<<8)|0x00, "Acoustic Bass"} ,
                {(0x00<<8)|0x01, "Crash Cymbal"} ,
                {(0x00<<8)|0x02, "Ride Cymbal"} ,
                {(0x00<<8)|0x03, "Close HiHat"} ,
                {(0x00<<8)|0x04, "Open HiHat"} ,
                {(0x00<<8)|0x05, "Hand Clap"} ,
                {(0x00<<8)|0x06, "High Tom"} ,
                {(0x00<<8)|0x07, "Mid Tom"} ,
                {(0x00<<8)|0x08, "Low Tom"} ,
                {(0x00<<8)|0x09, "Brush Snare"} ,
                {(0x00<<8)|0x0a, "Voice One-Two"} ,
                {(0x00<<8)|0x0b, "Bass & Snare"} ,
                {(0x00<<8)|0x0c, "Bass & Snare (Long)"} ,
                {(0x00<<8)|0x0d, "Snare (Light Open)"} ,
                {(0x00<<8)|0x0e, "Bass & Snare (Vol.Low)"} ,
                {(0x00<<8)|0x0f, "High Tom (Vol.Low)"} ,
                {(0x00<<8)|0x10, "Mid Tom (Vol.Low)"} ,
                {(0x00<<8)|0x11, "Low Tom (Vol.Low)"} ,
                {(0x00<<8)|0x12, "Bass & Snare (Hard)"} ,
                {(0x00<<8)|0x13, "Long Noise High"} ,
                {(0x00<<8)|0x14, "E.Tom1"} ,
                {(0x00<<8)|0x15, "E.Tom2"} ,
                {(0x00<<8)|0x16, "E.Tom3"} ,
                {(0x00<<8)|0x17, "E.Tom4"} ,
                {(0x00<<8)|0x18, "Bass & Crash Cymbal"} ,
                {(0x00<<8)|0x19, "Long Noise Low"} ,
                {(0x00<<8)|0x1a, "Close HiHat (Vol.Loud)"} ,
                {(0x00<<8)|0x1b, "Open HiHat (Vol.Loud)"} ,
                {(0x00<<8)|0x1c, "Long Tom1"} ,
                {(0x00<<8)|0x1d, "Long Tom2"} ,
                {(0x00<<8)|0x1e, "Long Tom3"} ,
                {(0x00<<8)|0x1f, "Long Tom4"} ,
                {(0x00<<8)|0x20, "Tom1 (Metallic)"} ,
                {(0x00<<8)|0x21, "Tom2 (Metallic)"} ,
                {(0x00<<8)|0x22, "Tom3 (Metallic)"} ,
                {(0x00<<8)|0x23, "Tom4 (Metallic)"} ,
                {(0x00<<8)|0x24, "Long Tom1 (Vol.Low)"} ,
                {(0x00<<8)|0x25, "Long Tom2 (Vol.Low)"} ,
                {(0x00<<8)|0x26, "Long Tom3 (Vol.Low)"} ,
                {(0x00<<8)|0x27, "Long Tom4 (Vol.Low)"} ,
                {(0x00<<8)|0x28, "Orch.Hit G"} ,
                {(0x00<<8)|0x29, "Orch.Hit G+"} ,
                {(0x00<<8)|0x2a, "Orch.Hit A"} ,
                {(0x00<<8)|0x2b, "Orch.Hit A+"} ,
                {(0x00<<8)|0x2c, "Orch.Hit B"} ,
                {(0x00<<8)|0x2d, "Orch.Hit C"} ,
                {(0x00<<8)|0x2e, "Orch.Hit C+"} ,
                {(0x00<<8)|0x2f, "Orch.Hit D"} ,
                {(0x00<<8)|0x30, "Orch.Hit D+"} ,
                {(0x00<<8)|0x31, "Orch.Hit E"} ,
                {(0x00<<8)|0x32, "Orch.Hit F"} ,
                {(0x00<<8)|0x33, "Orch.Hit F+"} ,
                {(0x00<<8)|0x34, "Orch.Hit G"} ,
                {(0x00<<8)|0x35, "Orch.Hit G+"} ,
                {(0x00<<8)|0x36, "Orch.Hit A"} ,
                {(0x00<<8)|0x37, "Orch.Hit A+"} ,
                {(0x00<<8)|0x38, "Orch.Hit B"} ,
                {(0x00<<8)|0x39, "Orch.Hit C"} ,
                {(0x00<<8)|0x3a, "E.Tom1 (Vol.Low)"} ,
                {(0x00<<8)|0x3b, "E.Tom2 (Vol.Low)"} ,
                {(0x00<<8)|0x3c, "E.Tom3 (Vol.Low)"} ,
                {(0x00<<8)|0x3d, "E.Tom4 (Vol.Low)"} ,
                {(0x00<<8)|0x3e, "Power Snare Brush1 (Vol.Loud)"} ,
                {(0x00<<8)|0x3f, "Power Snare Brush2 (Vol.Mid)"} ,
                {(0x00<<8)|0x40, "Power Snare Brush3 (Vol.Low)"} ,
                {(0x00<<8)|0x41, "Power Bass Snare Brush1 (Vol.Loud)"} ,
                {(0x00<<8)|0x42, "Power Bass Snare Brush2 (Vol.Mid)"} ,
                {(0x00<<8)|0x43, "Power Bass Snare Brush3 (Vol.Low)"} ,
                {(0x00<<8)|0x44, "Rock Snare1 (Vol.Loud)"} ,
                {(0x00<<8)|0x45, "Rock Snare2 (Vol.Mid)"} ,
                {(0x00<<8)|0x46, "Rock Snare3 (Vol.Low)"} ,
            };
        }

        private static List<string> LoadPDL(string PDXFilename) {
                var fn = "";
            if (System.IO.File.Exists(System.IO.Path.ChangeExtension(PDXFilename, ".pdl"))) { fn = System.IO.Path.ChangeExtension(PDXFilename, ".pdl"); }
            if (System.IO.File.Exists(System.IO.Path.ChangeExtension(PDXFilename, ".spl"))) { fn = System.IO.Path.ChangeExtension(PDXFilename, ".spl"); }
            if (System.IO.File.Exists(System.IO.Path.ChangeExtension(PDXFilename, ".xpl"))) { fn = System.IO.Path.ChangeExtension(PDXFilename, ".xpl"); }

            var res = new List<string>();
                if (fn.Equals("")) { return res; }

                using (var sr = new System.IO.StreamReader(fn)) {
                    while (!sr.EndOfStream) {
                        res.Add(sr.ReadLine());
                    }
                }

                return res;
        }

        public static Dictionary<int, string> GetVoiceNamesFromPDL(string PDXFilename) {
            var res = new Dictionary<int, string>();

            var PDL = LoadPDL(PDXFilename);

            for (var idx = 0; idx < PDL.Count; idx++) {
                var Line = PDL[idx];
            }

            var VoiceNum = 0;
            foreach (var _Line in PDL) {
                var Line = _Line;

                if (Line.StartsWith('*')) { Line = Line.Substring(1); } // 先頭アスタリスクは、PCMファイル名ではなく音色名を明記している？

                var StartPos = Line.IndexOf('.');
                if (StartPos == -1) { StartPos = 0; } // ファイル名に#が含まれることがあるため。'.'記号が見つからなかったときは先頭から検索
                if (Line.IndexOf('/', StartPos) != -1) { Line = Line.Substring(0, Line.IndexOf('/', StartPos)); }
                if (Line.IndexOf('#', StartPos) != -1) { Line = Line.Substring(0, Line.IndexOf('#', StartPos)); }
                Line = Line.Trim();

                if (Line.StartsWith('@')) {
                    VoiceNum = int.Parse(Line.Substring(1));
                    continue;
                }
                var pos = Line.IndexOf('=');
                if (pos == -1) { continue; }
                var Note = Line.Substring(0, pos).Trim();
                var Name = Line.Substring(pos + 1).Trim();
                int NoteInt;
                if (int.TryParse(Note, out NoteInt)) {
                    res[(VoiceNum << 8) | NoteInt] = Name;
                } else {
                    // ノート名指定 動作未確認
                    NoteInt = MusDriver.CCommon.StringToKeyCode(Note);
                    if (NoteInt != -1) {
                        res[(VoiceNum << 8) | NoteInt] = Name;
                    }
                }
            }

            return res;
        }
    }
}
