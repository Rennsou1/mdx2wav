using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MoonLib {
    public class CTextEncode {
        public static Encoding SJIS;
        public static void Setup() {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            SJIS = System.Text.Encoding.GetEncoding(932);
        }

        public class COptimizer {
            public static string Conv半角カナを全角カナに(string str) {
                var dic = new Dictionary<string, string>() {
                    { "｢", "「"}, { "｣", "」"}, { "､", "、"}, { "･", "・"}, { "ｦ", "ヲ"},
                    { "ｧ", "ァ"}, { "ｨ", "ィ"}, { "ｩ", "ゥ"}, { "ｪ", "ェ"}, { "ｫ", "ォ"},
                    { "ｬ", "ャ"}, { "ｭ", "ュ"}, { "ｮ", "ョ"}, { "ｯ", "ッ"}, { "ｰ", "ー"},
                    { "ｱ", "ア"}, { "ｲ", "イ"}, { "ｳ", "ウ"}, { "ｴ", "エ"}, { "ｵ", "オ"},
                    { "ｶ", "カ"}, { "ｷ", "キ"}, { "ｸ", "ク"}, { "ｹ", "ケ"}, { "ｺ", "コ"},
                    { "ｻ", "サ"}, { "ｼ", "シ"}, { "ｽ", "ス"}, { "ｾ", "セ"}, { "ｿ", "ソ"},
                    { "ﾀ", "タ"}, { "ﾁ", "チ"}, { "ﾂ", "ツ"}, { "ﾃ", "テ"}, { "ﾄ", "ト"},
                    { "ﾅ", "ナ"}, { "ﾆ", "ニ"}, { "ﾇ", "ヌ"}, { "ﾈ", "ネ"}, { "ﾉ", "ノ"},
                    { "ﾊ", "ハ"}, { "ﾋ", "ヒ"}, { "ﾌ", "フ"}, { "ﾍ", "ヘ"}, { "ﾎ", "ホ"},
                    { "ﾏ", "マ"}, { "ﾐ", "ミ"}, { "ﾑ", "ム"}, { "ﾒ", "メ"}, { "ﾓ", "モ"},
                    { "ﾔ", "ヤ"}, { "ﾕ", "ユ"}, { "ﾖ", "ヨ"},
                    { "ﾗ", "ラ"}, { "ﾘ", "リ"}, { "ﾙ", "ル"}, { "ﾚ", "レ"}, { "ﾛ", "ロ"},
                    { "ﾜ", "ワ"}, { "ﾝ", "ン"}, { "ﾞ", "゛"}, { "ﾟ", "゜"},
                    { "ｶﾞ", "ガ"}, { "ｷﾞ", "ギ"}, { "ｸﾞ", "グ"}, { "ｹﾞ", "ゲ"}, { "ｺﾞ", "ゴ"},
                    { "ｻﾞ", "ザ"}, { "ｼﾞ", "ジ"}, { "ｽﾞ", "ズ"}, { "ｾﾞ", "ゼ"}, { "ｿﾞ", "ゾ"},
                    { "ﾀﾞ", "ダ"}, { "ﾁﾞ", "ヂ"}, { "ﾂﾞ", "ヅ"}, { "ﾃﾞ", "デ"}, { "ﾄﾞ", "ド"},
                    { "ﾊﾞ", "バ"}, { "ﾋﾞ", "ビ"}, { "ﾌﾞ", "ブ"}, { "ﾍﾞ", "ベ"}, { "ﾎﾞ", "ボ"},
                    { "ﾊﾟ", "パ"}, { "ﾋﾟ", "ピ"}, { "ﾌﾟ", "プ"}, { "ﾍﾟ", "ペ"}, { "ﾎﾟ", "ポ"},
                };

                Regex re = new Regex(@"[ｦ-ﾝ]ﾞ|[ｦ-ﾝ]ﾟ|[ｦ-ﾝ]");
                var res = re.Replace(str, match => {
                    if (dic.ContainsKey(match.Value)) {
                        return dic[match.Value];
                    } else {
                        return match.Value;
                    }
                });
                return res;
            }

            public static string Conv全角英数記号を半角英数記号に(string str) {
                var dic = new Dictionary<char, char>() {
                    {'０','0'},{'１','1'},{'２','2'},{'３','3'},{'４','4'},{'５','5'},{'６','6'},{'７','7'},{'８','8'},{'９','9'},
                    {'Ａ','A'},{'Ｂ','B'},{'Ｃ','C'},{'Ｄ','D'},{'Ｅ','E'},{'Ｆ','F'},{'Ｇ','G'},{'Ｈ','H'},{'Ｉ','I'},{'Ｊ','J'},{'Ｋ','K'},{'Ｌ','L'},{'Ｍ','M'},{'Ｎ','N'},{'Ｏ','O'},{'Ｐ','P'},{'Ｑ','Q'},{'Ｒ','R'},{'Ｓ','S'},{'Ｔ','T'},{'Ｕ','U'},{'Ｖ','V'},{'Ｗ','W'},{'Ｘ','X'},{'Ｙ','Y'},{'Ｚ','Z'},
                    {'ａ','a'},{'ｂ','b'},{'ｃ','c'},{'ｄ','d'},{'ｅ','e'},{'ｆ','f'},{'ｇ','g'},{'ｈ','h'},{'ｉ','i'},{'ｊ','j'},{'ｋ','k'},{'ｌ','l'},{'ｍ','m'},{'ｎ','n'},{'ｏ','o'},{'ｐ','p'},{'ｑ','q'},{'ｒ','r'},{'ｓ','s'},{'ｔ','t'},{'ｕ','u'},{'ｖ','v'},{'ｗ','w'},{'ｘ','x'},{'ｙ','y'},{'ｚ','z'},
                    {'，',','},{'．','.'},{'：',':'},{'；',';'},{'？','?'},{'！','!'},{'＾','^'},{'（','('},{'）',')'},{'［','['},{'］',']'},{'｛','{'},{'｝','}'},{'〈','<'},{'〉','>'},{'＋','+'},{'－','-'},{'＝','='},{'＜','<'},{'＞','>'},{'″','"'},{'＄','$'},{'％','%'},{'＃','#'},{'＆','&'},{'＊','*'},{'＠','@'},{'‘','\''},{'’','\''},
                };

                var res = "";
                foreach (var ch in str) {
                    if (dic.ContainsKey(ch)) {
                        res += dic[ch];
                    } else {
                        res += ch;
                    }
                }
                return res;
            }

            public static string Opt改行除去(string str) {
                return str.Replace("\r", "").Replace("\n", "");
            }
            public static string Conv全角スペースを半角スペースに(string str) {
                return str.Replace("　", " ");
            }

            private static string Opt連続除去(string str, string tag, int Repeat = 1) {
                var find = "";
                for (var idx = 0; idx < Repeat + 1; idx++) {
                    find += tag;
                }
                var rep = "";
                for (var idx = 0; idx < Repeat; idx++) {
                    rep += tag;
                }
                for (var loop = 0; loop < 1000; loop++) {
                    if (str.IndexOf(find) == -1) { break; }
                    str = str.Replace(find, rep);
                }
                return str;
            }

            public static string 音楽タイトル用トリミング(string str) {
                str = Opt改行除去(str);
                str = Conv半角カナを全角カナに(str);
                str = Conv全角スペースを半角スペースに(str);

                str = Conv全角英数記号を半角英数記号に(str);

                str = Opt連続除去(str, " ");
                str = Opt連続除去(str, "-", 2);
                str = Opt連続除去(str, "=", 2);
                str = Opt連続除去(str, "*", 2);
                str = Opt連続除去(str, "～", 2);
                str = Opt連続除去(str, "･");
                str = Opt連続除去(str, ":");
                str = Opt連続除去(str, "<");
                str = Opt連続除去(str, ">");
                str = Opt連続除去(str, "…");

                str = str.Replace("…‥･", "-");
                str = str.Replace("･‥…･", "-");
                str = str.Replace("‥…", "-");
                str = str.Replace("…‥", "-");
                str = str.Replace("", "u");
                str = str.Replace("", "i");
                str = str.Replace("", "a");

                str = str.Replace("・", " 外字 ");
                str = Opt連続除去(str, " ");
                str = Opt連続除去(str, "外字 ");

                return str;
            }
        }

        public static int GetLength_半角カナを1文字として数える(string str) {
            var res = 0;
            for (var idx = 0; idx < str.Length; idx++) {
                res += (str[idx] < 0x100) ? 1 : 2;
            }
            return res;
        }
    }
}
