using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Policy;
using System.Text;

namespace Lang {
    public class CLang {
        public enum EMode { JPN, ENG };

        private class CItem {
            private string JPN, ENG;
            public CItem(string _JPN, string _ENG) {
                JPN = _JPN;
                ENG = _ENG;
            }
            public string Get() {
                switch (MDXWin.CCommon.INI.LangMode) {
                    case EMode.JPN: return JPN;
                    case EMode.ENG: return ENG;
                    default: throw new Exception();
                }
            }
        }

        public enum EWInternet { TitleLbl, IPLbl, UserNameLbl, AccountHintLbl, PermissionChk, CloseBtn_Yes, CloseBtn_No, ExistsLatestClient, };
        private static Dictionary<EWInternet, CItem> WInternetItems = new() {
            { EWInternet.TitleLbl, new CItem("インターネットを使用してもよろしいですか？", "Do you want to use the Internet?") },
            { EWInternet.IPLbl, new CItem("接続先IPアドレス:", "Destination IP address:") },
            { EWInternet.UserNameLbl, new CItem("ユーザー名:", "User name:") },
            { EWInternet.AccountHintLbl, new CItem("アカウント登録などはありませんので、名前は空のままで大丈夫です。", "You don’t need to register an account. You can leave the name blank.") },
            { EWInternet.PermissionChk, new CItem("インターネットの使用を許可する", "Allow internet use") },
            { EWInternet.CloseBtn_Yes, new CItem("許可して続行する", "Allow and continue") },
            { EWInternet.CloseBtn_No, new CItem("拒否して終了する", "Deny and quit") },
            { EWInternet.ExistsLatestClient, new CItem("MDXWinの新しいバージョンが公開されているかもしれません。", "A new version of MDXWin may be available.") },
        };
        public static string GetWInternet(EWInternet e) {
            if (WInternetItems.ContainsKey(e)) { return WInternetItems[e].Get(); }
            return "WInternet: Language resource undefined.";
        }

        public enum EBoot {
            InputBoxDefText,
            FMPPMD_CannotLoadDLL,
            Network_IlligalFormat, Network_InfoCantGet, Network_ServerStop1, Network_ServerStop2,
            Network_CantLogin1, Network_CantLogin2, Network_CantLogin3,
            Network_Logined1, Network_Logined2,
            MenuItemDef1, MenuItemDef2, MenuItemDef3, MenuItemDef4, MenuItemDef5, MenuItemDef6, MenuItemDef7, MenuItemDef8,
            CGROM_Loaded, CGROM_NotFound, CGROM_NotFoundForVisual1, CGROM_NotFoundForVisual2,
            PDXHQBos_Loaded,
            FatalErrorOnBoot,
            CurrentPath_Restore, CurrentPath_Error,
        };
        private static Dictionary<EBoot, CItem> BootItems = new() {
            { EBoot.InputBoxDefText, new CItem("コマンドを入力してください。（Helpなど）", "Please enter a command. (Help, etc.)") },
            { EBoot.FMPPMD_CannotLoadDLL, new CItem("DLLが読み込めませんでした。（64bits版ではないかもしれません）", "Cannot load DLL. (Is this DLL for 64bits?)") },
            { EBoot.Network_IlligalFormat, new CItem("サーバ情報のフォーマットが違います。", "The format of the server information is incorrect.") },
            { EBoot.Network_InfoCantGet, new CItem("サーバ情報の取得に失敗しました。", "Failed to get the server information.") },
            { EBoot.Network_ServerStop1, new CItem("サーバが止まっているかもしれません。", "The server may be down.") },
            { EBoot.Network_ServerStop2, new CItem("MDXWin本体がバージョンアップしているかもしれません。", "MDXWin.exe may have been upgraded.") },
            { EBoot.Network_CantLogin1, new CItem("サーバにログインできませんでした。", "Failed to log in to the server.") },
            { EBoot.Network_CantLogin2, new CItem("サーバが止まっているかもしれません。", "The server may be down.") },
            { EBoot.Network_CantLogin3, new CItem("インターネット接続が使えるか確認してみてください。", "Please check if you have an internet connection.") },
            { EBoot.Network_Logined1, new CItem("ユーザ名 [", "Logged in as user name [") },
            { EBoot.Network_Logined2, new CItem("] でログインしました。", "]") },
            { EBoot.MenuItemDef1, new CItem("ファイルセレクタ", "File selector") },
            { EBoot.MenuItemDef2, new CItem("ビジュアル", "Visual") },
            { EBoot.MenuItemDef3, new CItem("フォルダを開く", "Open folder") },
            { EBoot.MenuItemDef4, new CItem("ドキュメント類", "Documents") },
            { EBoot.MenuItemDef5, new CItem("シーク", "Seek") },
            { EBoot.MenuItemDef6, new CItem("演奏停止", "Stop") },
            { EBoot.MenuItemDef7, new CItem("次の曲", "Next") },
            { EBoot.MenuItemDef8, new CItem("環境設定", "Preferences") },
            { EBoot.CGROM_Loaded, new CItem("CGROM.DATを読み込みました。", "CGROM.DAT has been loaded.") },
            { EBoot.CGROM_NotFound, new CItem("CGROM.DATが見つからなかったので、Windowsフォントで代替します。", "Using Windows fonts as a substitute because CGROM.DAT was not found.") },
            { EBoot.CGROM_NotFoundForVisual1, new CItem("CGROM.DATが見つからなかったので", "Using Windows substitute fonts,") }, // この項目は横幅に制限があるので変更不可 This item cannot be changed because it has a width limit.
            { EBoot.CGROM_NotFoundForVisual2, new CItem("Windowsフォントで代替します。", "because CGROM.DAT was not found.") }, // この項目は横幅に制限があるので変更不可 This item cannot be changed because it has a width limit.
            { EBoot.PDXHQBos_Loaded, new CItem("を読み込みました。", "loaded.") },
            { EBoot.CurrentPath_Restore, new CItem("前回のカレントパスを復元しました。", "Restored the previous current path.") },
            { EBoot.CurrentPath_Error, new CItem("カレントパスの復元に失敗しました。ルートパスから再開しました。", "Failed to restore the current path. Restarted from the root path.") },
            { EBoot.FatalErrorOnBoot, new CItem("起動中に例外が発生しました。報告頂けたらありがたいです。", "An exception occurred during startup. I would appreciate it if you could report it.") },
        };
        public static string GetBoot(EBoot e) {
            if (BootItems.ContainsKey(e)) { return BootItems[e].Get(); }
            return "Boot: Language resource undefined.";
        }

        public enum EConsole {
            MDX_NoTitle,
            MDX_Exception1, MDX_Exception2, MDX_ExceptionOnPlay,
            MDX_Exception_Dialog,
            AudioThread_UndefBitDepth, AudioThread_MDXNotLoad, AudioThread_StillHaveSamplesInBuffer, AudioThread_SamplePeakClipped, AudioThread_EnabledWaveFileWriter, AudioThread_MDXFileHaveErrorSoSmallVolume,
            CGROM_DoubleSize, CGROM_UseWindowsFont,
        };
        private static Dictionary<EConsole, CItem> ConsoleItems = new() {
            { EConsole.MDX_NoTitle, new CItem("タイトルはありません。", "No title.") },
            { EConsole.MDX_Exception1, new CItem("エラーのあるMDXファイルです。", "This is an MDX file with errors.") },
            { EConsole.MDX_Exception2, new CItem("異常な発音があるかもしれないので、小さめの音量で再生します。", "Play with a low volume because there may be some abnormal sounds.") },
            { EConsole.MDX_ExceptionOnPlay, new CItem("エラーが発生したので演奏を中断しました。", "The performance was interrupted due to an error.") },
            { EConsole.MDX_Exception_Dialog, new CItem("＜中止＞ 再実行 ＜無視＞", "<Stop> Retry <Ignore>") },
            { EConsole.AudioThread_UndefBitDepth, new CItem("未定義ビット深度", "Undefined bit depth") },
            { EConsole.AudioThread_MDXNotLoad, new CItem("Thread error. MDXファイルが読み込まれていない。", "Thread error. No MDX file is loaded.") },
            { EConsole.AudioThread_StillHaveSamplesInBuffer, new CItem("Thread error. バッファにサンプルが残っている。", "Thread error. There are samples remaining in the buffer.") },
            { EConsole.AudioThread_SamplePeakClipped, new CItem("音割れが発生したので音量を下げました。", "I lowered the volume because of sound distortion.") },
            { EConsole.AudioThread_MDXFileHaveErrorSoSmallVolume, new CItem("エラーのあるMDXファイルです。異常音が出るかもしれないので小さめの音量で再生します。", "Play with a low volume because this is an MDX file with errors.") },
            { EConsole.CGROM_DoubleSize, new CItem("(2倍角)", "(double)") },
            { EConsole.CGROM_UseWindowsFont, new CItem("Windows代替フォント", "Windows substitute font") },
        };
        public static string GetConsole(EConsole e) {
            if (ConsoleItems.ContainsKey(e)) { return ConsoleItems[e].Get(); }
            return "Console: Language resource undefined.";
        }

        public enum ECommand {
            ParamError,
            SetFunc_FormatError, SetFunc_NumIsNotNum, SetFunc_NumIsOver,
            CantLoadMDX,
            PlayMode_Undef, PlayMode_Empty,
            ExecUndefFunc1, ExecUndefFunc2, Func_OverIndex,
            SetIgnoreException,
            ADPCMMode_NearestNeighborInterpolation, ADPCMMode_LinearInterpolation, ADPCMMode_CubicSplineInterpolation,
            Folder_Parent, Folder_Current, Dir_File, Dir_Used,
            WrongParam, OutOfRange, NoLoadedMDX,
            VisualMul_Help, FS_Error, FS_Help, ADPCM_Help, MXLoop_Help, MXMute_WrongCh, MXSeek_Help, MXSeek_NoNum, SampleRate_Help, WaveWrite_NowPlaying, Volume_Help, NoCommandOrNoFilename,
            OpenDocs_NotOpened,
        };
        private static Dictionary<ECommand, CItem> CommandItems = new() {
            { ECommand.ParamError, new CItem("パラメータが間違っています。", "The parameter is wrong.") },
            { ECommand.SetFunc_FormatError, new CItem("フォーマットが違います。", "The format is incorrect.") },
            { ECommand.SetFunc_NumIsNotNum, new CItem("機能番号が数字ではありません。", "The function number is not a digit.") },
            { ECommand.SetFunc_NumIsOver, new CItem("機能番号が範囲外です。", "The function number is out of range.") },
            { ECommand.CantLoadMDX, new CItem("ファイルが読み込めません。", "Cannot read file.") },
            { ECommand.PlayMode_Undef, new CItem("未定義モードです。 ", "Undefined mode.") },
            { ECommand.PlayMode_Empty, new CItem("プレイリストが空です。", "Playlist is empty.") },
            { ECommand.ExecUndefFunc1, new CItem("未定義のショートカット機能です。", "Undefined shortcut function.") },
            { ECommand.ExecUndefFunc2, new CItem("Helpコマンドを実行して、SetFuncコマンドを参照してください。", "Please run the Help command and refer to the SetFunc command.") },
            { ECommand.Func_OverIndex, new CItem("範囲外のコマンドの情報を取得しようとしました。", "Tried to get information for a command out of range.") },
            { ECommand.SetIgnoreException, new CItem("続行可能な例外を無視するように設定しました。", "Set to ignore continuable exceptions.") },
            { ECommand.ADPCMMode_NearestNeighborInterpolation, new CItem("最近傍補間", "Nearest neighbor interpolation") },
            { ECommand.ADPCMMode_LinearInterpolation, new CItem("線形補間", "Linear interpolation") },
            { ECommand.ADPCMMode_CubicSplineInterpolation, new CItem("三次スプライン補間", "Cubic spline interpolation") },
            { ECommand.Folder_Parent, new CItem("親", "Parent") },
            { ECommand.Folder_Current, new CItem("現在", "Current") },
            { ECommand.Dir_File, new CItem("ファイル", "file(s)") },
            { ECommand.Dir_Used, new CItem("使用", "used") },
            { ECommand.WrongParam, new CItem("パラメータが間違っています。", "The parameter is incorrect.") },
            { ECommand.OutOfRange, new CItem("設定範囲外です。", "Out of setting range.") },
            { ECommand.NoLoadedMDX, new CItem("MDXファイルが読み込まれていません。", "No MDX file is loaded.") },
            { ECommand.VisualMul_Help, new CItem("表示倍率を指定してください。", "Please specify the display magnification.") },
            { ECommand.FS_Error, new CItem("フォントサイズの設定に失敗しました。", "Failed to set the font size.") },
            { ECommand.FS_Help, new CItem("フォントサイズを指定してください。", "Please specify the font size.") },
            { ECommand.ADPCM_Help, new CItem("アップサンプリングモードを指定してください。", "Please specify the upsampling mode.") },
            { ECommand.MXLoop_Help, new CItem("ループ回数を指定してください。", "Please specify the number of loops.") },
            { ECommand.MXMute_WrongCh, new CItem("チャンネルの指定が異常です。", "The channel specification is invalid.") },
            { ECommand.MXSeek_Help, new CItem("秒数または割合（数値%）を指定してください。", "Please specify the seconds or percentage. (When using percentages, add the % character at the end.)") },
            { ECommand.MXSeek_NoNum, new CItem("数値ではありません。", "It’s not a numeric value.") },
            { ECommand.SampleRate_Help, new CItem("レンダリング周波数を指定してください。", "Please specify the rendering frequency.") },
            { ECommand.WaveWrite_NowPlaying, new CItem("再生中は設定を変更できません。", "You cannot change the settings while playing.") },
            { ECommand.Volume_Help, new CItem("音量を指定してください。", "Please specify the volume.") },
            { ECommand.NoCommandOrNoFilename, new CItem("コマンドまたはファイル名が違います。", "It’s not a valid command or file name.") },
            { ECommand.OpenDocs_NotOpened, new CItem("対象のファイルがありません。","The target file does not exist.") },
        };
        public static string GetCommand(ECommand e) {
            if (CommandItems.ContainsKey(e)) { return CommandItems[e].Get(); }
            return "Command: Language resource undefined.";
        }

        public static List<MDXWin.CConsole.CLog.CItem> GetCommandHelp() {
            var res = new List<MDXWin.CConsole.CLog.CItem>();
            switch (MDXWin.CCommon.INI.LangMode) {
                case EMode.JPN:
                    res.Add(new MDXWin.CConsole.CLog.CItem("・システム関連"));
                    res.Add(new MDXWin.CConsole.CLog.CItem("SWITCH             環境設定ウィンドウを開きます。", MDXWin.CConsole.CLog.CItem.EMode.Command, "SWITCH"));
                    res.Add(new MDXWin.CConsole.CLog.CItem("CD [フォルダID]    現在のディレクトリを表示または変更します。", MDXWin.CConsole.CLog.CItem.EMode.Command, "CD"));
                    res.Add(new MDXWin.CConsole.CLog.CItem("DIR                ディレクトリ中のファイルやサブディレクトリの一覧を表示します。", MDXWin.CConsole.CLog.CItem.EMode.Command, "DIR"));
                    res.Add(new MDXWin.CConsole.CLog.CItem("OPENFOLDER         現在のフォルダを既定のブラウザで開きます。", MDXWin.CConsole.CLog.CItem.EMode.Command, "OPENFOLDER"));
                    res.Add(new MDXWin.CConsole.CLog.CItem("OPENDOCS           再生中の付属ドキュメント類を既定のブラウザで開きます。", MDXWin.CConsole.CLog.CItem.EMode.Command, "OPENDOCS"));
                    res.Add(new MDXWin.CConsole.CLog.CItem("EXIT               アプリケーションを終了します。", MDXWin.CConsole.CLog.CItem.EMode.Command, "EXIT"));
                    res.Add(new MDXWin.CConsole.CLog.CItem("FILESEL [ON|OFF]   ファイルセレクタを表示または非表示に変更します。", MDXWin.CConsole.CLog.CItem.EMode.Command, "FILESEL"));
                    res.Add(new MDXWin.CConsole.CLog.CItem("IGEXCEPT [ON|OFF]  演奏続行可能なエラーを無視するよう設定します。", MDXWin.CConsole.CLog.CItem.EMode.Command, "IGEXCEPT"));
                    res.Add(new MDXWin.CConsole.CLog.CItem("PLAYMODE [Mode]    再生順序を設定します。Single=単曲停止, Repeat=単曲繰り返し, Normal=順再生, Random=ランダム再生", MDXWin.CConsole.CLog.CItem.EMode.Command, "PLAYMODE"));
                    res.Add(new MDXWin.CConsole.CLog.CItem("PLAYNEXT           次の曲を再生します。", MDXWin.CConsole.CLog.CItem.EMode.Command, "PLAYNEXT"));
                    res.Add(new MDXWin.CConsole.CLog.CItem("SETFUNC [番号, 名前, コマンド] メニューのショートカット機能を設定します。(8個まで)", MDXWin.CConsole.CLog.CItem.EMode.Command, "SETFUNC"));
                    res.Add(new MDXWin.CConsole.CLog.CItem("                   SetFunc 1, 停止, mxstop のように指定してください。"));
                    res.Add(new MDXWin.CConsole.CLog.CItem("VISUAL [ON|OFF]    現在の再生状況を表示または非表示に変更します。", MDXWin.CConsole.CLog.CItem.EMode.Command, "VISUAL"));
                    res.Add(new MDXWin.CConsole.CLog.CItem("VISUALMUL [0.5～4] 再生状況ウィンドウの表示倍率を設定します。", MDXWin.CConsole.CLog.CItem.EMode.Command, "VISUALMUL"));
                    res.Add(new MDXWin.CConsole.CLog.CItem("SPEANA [ON|OFF]    再生状況ウィンドウのスペアナを表示または非表示に変更します。", MDXWin.CConsole.CLog.CItem.EMode.Command, "SPEANA"));
                    res.Add(new MDXWin.CConsole.CLog.CItem("OSCILLO [ON|OFF]   再生状況ウィンドウのオシロスコープを表示または非表示に変更します。", MDXWin.CConsole.CLog.CItem.EMode.Command, "OSCILLO"));

                    res.Add(new MDXWin.CConsole.CLog.CItem(""));
                    res.Add(new MDXWin.CConsole.CLog.CItem("・フォント関連"));
                    res.Add(new MDXWin.CConsole.CLog.CItem("CONSOLEFS [数値]   この画面のフォントサイズを設定します。", MDXWin.CConsole.CLog.CItem.EMode.Command, "CONSOLEFS"));
                    res.Add(new MDXWin.CConsole.CLog.CItem("FILESELFS [数値]   ファイルセレクタのフォントサイズを設定します。", MDXWin.CConsole.CLog.CItem.EMode.Command, "FILESELFS"));
                    res.Add(new MDXWin.CConsole.CLog.CItem("                   16=8x16, 24=12x24, 32=8x16(2倍角), 48=12x24(2倍角)"));
                    res.Add(new MDXWin.CConsole.CLog.CItem(""));
                    res.Add(new MDXWin.CConsole.CLog.CItem("・MXDRV関連"));
                    res.Add(new MDXWin.CConsole.CLog.CItem("ADPCM [0|1|2]      ADPCMのアップサンプリングモードを設定します。", MDXWin.CConsole.CLog.CItem.EMode.Command, "ADPCM"));
                    res.Add(new MDXWin.CConsole.CLog.CItem("                   0=最近傍補間, 1=線形補間, 2=三次スプライン補間."));
                    res.Add(new MDXWin.CConsole.CLog.CItem("BOSPDXHQ [ON|OFF]  高音質版bos.pdxの使用を設定します。", MDXWin.CConsole.CLog.CItem.EMode.Command, "BOSPDXHQ"));
                    res.Add(new MDXWin.CConsole.CLog.CItem("MXFADE             フェードアウトします。", MDXWin.CConsole.CLog.CItem.EMode.Command, "MXFADE"));
                    res.Add(new MDXWin.CConsole.CLog.CItem("MXLOOP [数値]      ループ回数を設定します。", MDXWin.CConsole.CLog.CItem.EMode.Command, "MXLOOP"));
                    res.Add(new MDXWin.CConsole.CLog.CItem("MXMUTE [Channel]   チャンネルミュートを切り替えます。[1-16|A-H|P-W|ALL]", MDXWin.CConsole.CLog.CItem.EMode.Command, "MXMUTE"));
                    res.Add(new MDXWin.CConsole.CLog.CItem("MXOPM [ON|OFF]     OPM出力を設定します。", MDXWin.CConsole.CLog.CItem.EMode.Command, "MXOPM"));
                    res.Add(new MDXWin.CConsole.CLog.CItem("MXPCM [ON|OFF]     PCM(PCM8)出力を設定します。", MDXWin.CConsole.CLog.CItem.EMode.Command, "MXPCM"));
                    res.Add(new MDXWin.CConsole.CLog.CItem("MXP [ファイルID]   指定されたファイルIDを再生します。", MDXWin.CConsole.CLog.CItem.EMode.Command, "MXP"));
                    res.Add(new MDXWin.CConsole.CLog.CItem("MXSEEK [秒数]      再生中のMDXファイルの再生位置を変更します。", MDXWin.CConsole.CLog.CItem.EMode.Command, "MXSEEK"));
                    res.Add(new MDXWin.CConsole.CLog.CItem("MXSTAT             再生中のMDXファイルの情報を表示します。", MDXWin.CConsole.CLog.CItem.EMode.Command, "MXSTAT"));
                    res.Add(new MDXWin.CConsole.CLog.CItem("MXSTOP             再生を停止します。", MDXWin.CConsole.CLog.CItem.EMode.Command, "MXSTOP"));
                    res.Add(new MDXWin.CConsole.CLog.CItem("SAMPLERATE [Hz]    レンダリング周波数を設定します。(62500～384000Hz)", MDXWin.CConsole.CLog.CItem.EMode.Command, "SAMPLERATE"));
                    res.Add(new MDXWin.CConsole.CLog.CItem(""));
                    res.Add(new MDXWin.CConsole.CLog.CItem("・音声出力関連"));
                    res.Add(new MDXWin.CConsole.CLog.CItem("VOLUME [数値]        出力音量を設定します。dB単位で70～100の範囲です。", MDXWin.CConsole.CLog.CItem.EMode.Command, "VOLUME"));
                    res.Add(new MDXWin.CConsole.CLog.CItem("FLACOUT [ファイルID] 現在の演奏設定で、FLACファイルを出力します。ALLを指定すると、現在のフォルダ内全て変換します。", MDXWin.CConsole.CLog.CItem.EMode.Command, "FLACOUT"));
                    res.Add(new MDXWin.CConsole.CLog.CItem(""));
                    res.Add(new MDXWin.CConsole.CLog.CItem("コマンド及びファイル名は大文字小文字を区別しません。"));
                    res.Add(new MDXWin.CConsole.CLog.CItem("パラメータはひとつだけなので、スペースを含むファイル名でもダブルクォーテーション不要です。"));
                    break;
                case EMode.ENG:
                    res.Add(new MDXWin.CConsole.CLog.CItem("+ System class"));
                    res.Add(new MDXWin.CConsole.CLog.CItem("SWITCH             Open the preferences window.", MDXWin.CConsole.CLog.CItem.EMode.Command, "SWITCH"));
                    res.Add(new MDXWin.CConsole.CLog.CItem("CD [FolderID]      Displays or changes the current directory.", MDXWin.CConsole.CLog.CItem.EMode.Command, "CD"));
                    res.Add(new MDXWin.CConsole.CLog.CItem("DIR                Displays a list of files and subdirectories in the directory.", MDXWin.CConsole.CLog.CItem.EMode.Command, "DIR"));
                    res.Add(new MDXWin.CConsole.CLog.CItem("OPENFOLDER         Open the current folder in your default browser.", MDXWin.CConsole.CLog.CItem.EMode.Command, "OPENFOLDER"));
                    res.Add(new MDXWin.CConsole.CLog.CItem("OPENDOCS           Open the attached documents in your default browser.", MDXWin.CConsole.CLog.CItem.EMode.Command, "OPENDOCS"));
                    res.Add(new MDXWin.CConsole.CLog.CItem("EXIT               Exits the application.", MDXWin.CConsole.CLog.CItem.EMode.Command, "EXIT"));
                    res.Add(new MDXWin.CConsole.CLog.CItem("FILESEL [ON|OFF]   Shows or hides the file selector window.", MDXWin.CConsole.CLog.CItem.EMode.Command, "FILESEL"));
                    res.Add(new MDXWin.CConsole.CLog.CItem("IGEXCEPT [ON|OFF]  Sets to ignore errors that can be continued playing.", MDXWin.CConsole.CLog.CItem.EMode.Command, "IGEXCEPT"));
                    res.Add(new MDXWin.CConsole.CLog.CItem("PLAYMODE [Mode]    Sets the playback order. Single=Single stop, Repeat=Single repeat, Normal=Sequential play, Random=Random play", MDXWin.CConsole.CLog.CItem.EMode.Command, "PLAYMODE"));
                    res.Add(new MDXWin.CConsole.CLog.CItem("PLAYNEXT           Plays the next song.", MDXWin.CConsole.CLog.CItem.EMode.Command, "PLAYNEXT"));
                    res.Add(new MDXWin.CConsole.CLog.CItem("SETFUNC [Num, Name, Command] Configures shortcut functions in the menu. (Up to 8 items)", MDXWin.CConsole.CLog.CItem.EMode.Command, "SETFUNC"));
                    res.Add(new MDXWin.CConsole.CLog.CItem("                             Example: SetFunc 1, STOP, mxstop"));
                    res.Add(new MDXWin.CConsole.CLog.CItem("VISUAL [ON|OFF]    Shows or hides the current playback status.", MDXWin.CConsole.CLog.CItem.EMode.Command, "VISUAL"));
                    res.Add(new MDXWin.CConsole.CLog.CItem("VISUALMUL [0.5～4] Sets the display magnification of the playback status window.", MDXWin.CConsole.CLog.CItem.EMode.Command, "VISUALMUL"));
                    res.Add(new MDXWin.CConsole.CLog.CItem("SPEANA [ON|OFF]    Shows or hides the spectrum analyzer in the playback status window.", MDXWin.CConsole.CLog.CItem.EMode.Command, "SPEANA"));
                    res.Add(new MDXWin.CConsole.CLog.CItem("OSCILLO [ON|OFF]   Shows or hides the Oscilloscope in the playback status window.", MDXWin.CConsole.CLog.CItem.EMode.Command, "OSCILLO"));
                    res.Add(new MDXWin.CConsole.CLog.CItem(""));
                    res.Add(new MDXWin.CConsole.CLog.CItem("+ Font class"));
                    res.Add(new MDXWin.CConsole.CLog.CItem("CONSOLEFS [Size]   Sets the font size for this screen.", MDXWin.CConsole.CLog.CItem.EMode.Command, "CONSOLEFS"));
                    res.Add(new MDXWin.CConsole.CLog.CItem("FILESELFS [Size]   Sets the font size for the file selector.", MDXWin.CConsole.CLog.CItem.EMode.Command, "FILESELFS"));
                    res.Add(new MDXWin.CConsole.CLog.CItem("                   16=8x16, 24=12x24, 32=8x16 (double), 48=12x24 (double)"));
                    res.Add(new MDXWin.CConsole.CLog.CItem(""));
                    res.Add(new MDXWin.CConsole.CLog.CItem("+ MXDRV class"));
                    res.Add(new MDXWin.CConsole.CLog.CItem("ADPCM [0|1|2]      Sets the upsampling mode for ADPCM.", MDXWin.CConsole.CLog.CItem.EMode.Command, "ADPCM"));
                    res.Add(new MDXWin.CConsole.CLog.CItem("                   0=Nearest neighbor interpolation, 1=Linear interpolation, 2=Cubic spline interpolation."));
                    res.Add(new MDXWin.CConsole.CLog.CItem("BOSPDXHQ [ON|OFF]  Sets to use the high-quality version of bos.pdx.", MDXWin.CConsole.CLog.CItem.EMode.Command, "BOSPDXHQ"));
                    res.Add(new MDXWin.CConsole.CLog.CItem("MXFADE             Start fades out.", MDXWin.CConsole.CLog.CItem.EMode.Command, "MXFADE"));
                    res.Add(new MDXWin.CConsole.CLog.CItem("MXLOOP [Num]       Sets the number of loops.", MDXWin.CConsole.CLog.CItem.EMode.Command, "MXLOOP"));
                    res.Add(new MDXWin.CConsole.CLog.CItem("MXMUTE [Channel]   Toggles channel mute. [1-16|A-H|P-W|ALL]", MDXWin.CConsole.CLog.CItem.EMode.Command, "MXMUTE"));
                    res.Add(new MDXWin.CConsole.CLog.CItem("MXOPM [ON|OFF]     Sets to enable OPM output.", MDXWin.CConsole.CLog.CItem.EMode.Command, "MXOPM"));
                    res.Add(new MDXWin.CConsole.CLog.CItem("MXPCM [ON|OFF]     Sets to enable PCM(PCM8) output.", MDXWin.CConsole.CLog.CItem.EMode.Command, "MXPCM"));
                    res.Add(new MDXWin.CConsole.CLog.CItem("MXP [FileID]       Plays the file ID.", MDXWin.CConsole.CLog.CItem.EMode.Command, "MXP"));
                    res.Add(new MDXWin.CConsole.CLog.CItem("MXSEEK [Seconds]   Changes the playback position of the playing MDX file.", MDXWin.CConsole.CLog.CItem.EMode.Command, "MXSEEK"));
                    res.Add(new MDXWin.CConsole.CLog.CItem("MXSTAT             Displays information about the playing MDX file.", MDXWin.CConsole.CLog.CItem.EMode.Command, "MXSTAT"));
                    res.Add(new MDXWin.CConsole.CLog.CItem("MXSTOP             Stops playback.", MDXWin.CConsole.CLog.CItem.EMode.Command, "MXSTOP"));
                    res.Add(new MDXWin.CConsole.CLog.CItem("SAMPLERATE [Hz]    Sets the rendering frequency. (62500 to 384000Hz)", MDXWin.CConsole.CLog.CItem.EMode.Command, "SAMPLERATE"));
                    res.Add(new MDXWin.CConsole.CLog.CItem(""));
                    res.Add(new MDXWin.CConsole.CLog.CItem("+ Audio output class"));
                    res.Add(new MDXWin.CConsole.CLog.CItem("VOLUME [dB]          Sets the output volume. (70 to 100dB)", MDXWin.CConsole.CLog.CItem.EMode.Command, "VOLUME"));
                    res.Add(new MDXWin.CConsole.CLog.CItem("FLACOUT [FileID|ALL] Outputs a FLAC file using the current settings. Specifying ALL will convert everything in the current folder.", MDXWin.CConsole.CLog.CItem.EMode.Command, "FLACOUT"));
                    res.Add(new MDXWin.CConsole.CLog.CItem(""));
                    res.Add(new MDXWin.CConsole.CLog.CItem("Commands and file names are not case sensitive."));
                    res.Add(new MDXWin.CConsole.CLog.CItem("Only one parameter is allowed, so double quotes are not required for file names with spaces."));
                    break;
                default: throw new Exception();
            }
            return res;
        }

        public enum EMainWindow {
            MenuFile, MenuEditEnv, MenuExit, MenuHelp,
        };
        private static Dictionary<EMainWindow, CItem> MainWindowItems = new() {
            { EMainWindow.MenuHelp, new CItem("ヘルプ(_H)", "Help(_H)") },
        };
        public static string GetMainWindow(EMainWindow e) {
            if (MainWindowItems.ContainsKey(e)) { return MainWindowItems[e].Get(); }
            return "MainWindow: Language resource undefined.";
        }

        public enum EMDXOnline {
            CantParseEmptyPath, CantMoveToUpFromRoot, CantFoundDirectory,
        };
        private static Dictionary<EMDXOnline, CItem> MDXOnlineItems = new() {
            { EMDXOnline.CantParseEmptyPath, new CItem("空のパスは処理できません。", "Cannot process an empty path.") },
            { EMDXOnline.CantMoveToUpFromRoot, new CItem("ルートより上のディレクトリには移動できません。", "Cannot move to a directory above the root.") },
            { EMDXOnline.CantFoundDirectory, new CItem("ディレクトリが見つかりません。", "Directory not found.") },
        };
        public static string GetMDXOnline(EMDXOnline e) {
            if (MDXOnlineItems.ContainsKey(e)) { return MDXOnlineItems[e].Get(); }
            return "MDXOnline: Language resource undefined.";
        }

        public enum EMXDRV {
            CanIgnoreException_Ignored, CanIgnoreException_Stopped, PDLLoadError,
            RepeatParamError,
            RepeatEndNotStarted, RepeatExitNotStarted, FadeoutParamError, UndefCommand, CommandSwitchError, SetOPMNoiseChError, UndefPCMVoiceNote, FadeoutStarted,
            SyncSignalOverChOutOfRange_Wait, SyncSignalOverChOutOfRange_Send, SyncSignalWaitOnWaiting, SyncSignalNoWaitOnNoWaiting,
            NoMDXFormatFile_StartWith0x00, NotFoundPDX, PerformEnded, UsePDXHQBos, EnableOldVolumeTable,
            MDXPDXReader_StringOverflow,
            Voice_NotFoundVoiceData, Voice_VolumeError,
            PDX_UndefPCMFormat, PDX_IlligalVolumeForOldMDXWinVolumeTable, PDX_VolumeError, PDX_IlligalPCMNum,
            PDXHQBos_OnlySupportMonoCh, PDXHQBos_OnlySupport1624bits, PDXHQBos_UnpackZipError, PDXHQBos_InternalError_CPCMWithNoRenderPCM, PDXHQBos_InternalError_BosPdxWithAnotherPCMFormat,
            UnsupportPhaseLFOType, UnsupportAmpLFOType,
            X68Sound_UnsupportFreqConversion,
        };
        private static Dictionary<EMXDRV, CItem> MXDRVItems = new() {
            { EMXDRV.CanIgnoreException_Ignored, new CItem("続行可能な例外を無視しました。", "Ignored a continuable exception.") },
            { EMXDRV.CanIgnoreException_Stopped, new CItem("続行可能な例外で停止しました。", "Stopped by a continuable exception.") },
            { EMXDRV.PDLLoadError, new CItem("PDLファイルの読み込みに失敗しました。", "Failed to read the PDL file.") },
            { EMXDRV.RepeatParamError, new CItem("リピート開始パラメータ異常", "Repeat start parameter error") },
            { EMXDRV.RepeatEndNotStarted, new CItem("リピート開始していないのにリピート終端が来た", "Repeat end without repeat start") },
            { EMXDRV.RepeatExitNotStarted, new CItem("リピート開始していないのにリピート脱出が来た", "Repeat escape without repeat start") },
            { EMXDRV.FadeoutParamError, new CItem("フェードアウトパラメータ異常", "Fade out parameter error") },
            { EMXDRV.UndefCommand, new CItem("未定義命令", "Undefined instruction") },
            { EMXDRV.CommandSwitchError, new CItem("ここには来ないはず。CommandのSwitch例外", "Should not come here. Command switch exception") },
            { EMXDRV.SetOPMNoiseChError, new CItem("OPMノイズ周波数設定チャネル異常", "OPM noise frequency setting channel error") },
            { EMXDRV.UndefPCMVoiceNote, new CItem("未登録のPCM番号です。", "Unregistered PCM number.") },
            { EMXDRV.FadeoutStarted, new CItem("フェードアウトを開始しました。", "Started fade out.") },
            { EMXDRV.SyncSignalOverChOutOfRange_Wait, new CItem("同期信号 待機 Ch範囲外", "Sync signal wait [standby] out of channel range") },
            { EMXDRV.SyncSignalOverChOutOfRange_Send, new CItem("同期信号 解除 Ch範囲外", "Sync signal release [Cancellation] out of channel range") },
            { EMXDRV.SyncSignalWaitOnWaiting, new CItem("同期信号を待機中なのに、待機命令が来た。", "Wait command while waiting for sync signal.") },
            { EMXDRV.SyncSignalNoWaitOnNoWaiting, new CItem("同期信号を待っていないのに、解除信号が来た。", "Release signal without waiting for sync signal.") },
            { EMXDRV.NoMDXFormatFile_StartWith0x00, new CItem("MDXファイルではない。（先頭バイトが0x00）", "Not an MDX file. (First byte is 0x00)") },
            { EMXDRV.NotFoundPDX, new CItem("PDXファイルが見つかりませんでした。", "PDX file not found.") },
            { EMXDRV.PerformEnded, new CItem("演奏が終了しています。", "Playback has ended.") },
            { EMXDRV.UsePDXHQBos, new CItem("高音質版bos.pdxを使用しています。", "Using high-quality version of bos.pdx.") },
            { EMXDRV.EnableOldVolumeTable, new CItem("古いMDXWinの音量テーブルが有効になっています。", "Old MDXWin volume table is enabled.") },
            { EMXDRV.MDXPDXReader_StringOverflow, new CItem("最大文字数オーバー", "Exceeded maximum number of characters.") },
            { EMXDRV.Voice_NotFoundVoiceData, new CItem("音色データが見つかりません。", "Voice data not found.") },
            { EMXDRV.Voice_VolumeError, new CItem("ボリューム値異常", "Abnormal volume value") },
            { EMXDRV.PDX_UndefPCMFormat , new CItem("未対応PCMフォーマット", "Unsupported PCM format") },
            { EMXDRV.PDX_IlligalVolumeForOldMDXWinVolumeTable, new CItem("(このメッセージは表示されないはずです) 古いMDXWin用に作った自作MDXファイルのPCM8の@vコマンドは、vコマンドを事前に@vに変換しているはず", "(This message should not appear.) The @v command of PCM8 in the homemade MDX file for the old MDXWin should have converted the v command to @v beforehand.") },
            { EMXDRV.PDX_VolumeError, new CItem("ボリューム値異常", "Abnormal volume value") },
            { EMXDRV.PDX_IlligalPCMNum, new CItem("異常なPCM番号です。", "Abnormal PCM number.") },
            { EMXDRV.PDXHQBos_OnlySupportMonoCh, new CItem("モノラルチャネルのみ対応しています。", "Only supports monaural channels.") },
            { EMXDRV.PDXHQBos_OnlySupport1624bits, new CItem("16/24bitsのみ対応しています。", "Only supports 16/24bits.") },
            { EMXDRV.PDXHQBos_UnpackZipError, new CItem("ZIP展開に失敗しました。", "Failed to unzip ZIP.") },
            { EMXDRV.PDXHQBos_InternalError_CPCMWithNoRenderPCM, new CItem("RenderPCMを作成しないで、CPCMを作成しようとした。", "Tried to create CPCM without creating RenderPCM.") },
            { EMXDRV.PDXHQBos_InternalError_BosPdxWithAnotherPCMFormat, new CItem("bos.pdxをADPCM以外で再生しようとした。", "Tried to play bos.pdx with something other than ADPCM.") },
            { EMXDRV.UnsupportPhaseLFOType, new CItem("未対応のソフトPhaseLFOタイプです。", "Unsupported software PhaseLFO type.") },
            { EMXDRV.UnsupportAmpLFOType, new CItem("未対応のソフトAmpLFOタイプです。", "Unsupported software AmpLFO type.") },
            { EMXDRV.X68Sound_UnsupportFreqConversion, new CItem("周波数変換未対応", "Frequency conversion not supported") },
        };
        public static string GetMXDRV(EMXDRV e) {
            if (MXDRVItems.ContainsKey(e)) { return MXDRVItems[e].Get(); }
            return "MXDRV: Language resource undefined.";
        }

        // 2023/12/10 現在未使用
        public enum EMDXDis {
            VoiceNotFound,
            CS_SetTempo, CS_WriteReg, CS_SetVoice, CS_SetPanpot, CS_SetVolume, CS_VolumeMinus, CS_VolumePlus, CS_SetQ, CS_DisableKeyOff, CS_RepeatStart, CS_RepeatEnd, CS_RepeatExit, CS_SetDetune, CS_SetPorta, CS_DataEnd, CS_SetKeyOnDelay, CS_SendSync, CS_WaitSync, CS_SetADPCM, CS_SetPhaseLFO, CS_SetAmpLFO, CS_SetOPMLFO, CS_SetLFODelay, CS_EnablePCM8, CS_StartFadeout, CS_Error,
            RepeatParamError, FadeoutParamError, UndefCmd_InterruptDis, CommandSwitchError,
        };
        private static Dictionary<EMDXDis, CItem> MDXDisItems = new() {
            { EMDXDis.VoiceNotFound, new CItem("音色が見つからなかった。", "Voice not found.") },
            { EMDXDis.CS_SetTempo, new CItem("テンポ設定", "Set tempo") },
            { EMDXDis.CS_WriteReg, new CItem("OPMレジスタ設定", "Set OPM register") },
            { EMDXDis.CS_SetVoice, new CItem("音色設定", "Set voice") },
            { EMDXDis.CS_SetPanpot, new CItem("出力位相設定", "Set panpot") },
            { EMDXDis.CS_SetVolume, new CItem("音量設定", "Set volume") },
            { EMDXDis.CS_VolumeMinus, new CItem("音量減小", "Decrease volume") },
            { EMDXDis.CS_VolumePlus, new CItem("音量増大", "Increase volume") },
            { EMDXDis.CS_SetQ, new CItem("発音長指定", "Set quantize") },
            { EMDXDis.CS_DisableKeyOff, new CItem("キーオフ無効", "Disable key off") },
            { EMDXDis.CS_RepeatStart, new CItem("リピート開始", "Repeat start") },
            { EMDXDis.CS_RepeatEnd, new CItem("リピート終端", "Repeat end") },
            { EMDXDis.CS_RepeatExit, new CItem("リピート脱出", "Repeat escape") },
            { EMDXDis.CS_SetDetune, new CItem("デチューン", "Set detune") },
            { EMDXDis.CS_SetPorta, new CItem("ポルタメント", "Set portamento") },
            { EMDXDis.CS_DataEnd, new CItem("データエンド", "Data end") },
            { EMDXDis.CS_SetKeyOnDelay, new CItem("キーオンディレイ", "Key on delay") },
            { EMDXDis.CS_SendSync, new CItem("同期信号送出", "Send sync signal") },
            { EMDXDis.CS_WaitSync, new CItem("同期信号待機", "Wait for sync signal") },
            { EMDXDis.CS_SetADPCM, new CItem("ADPCM/ノイズ周波数設定", "Set ADPCM or noise frequency") },
            { EMDXDis.CS_SetPhaseLFO, new CItem("音程LFO制御", "Set PhaseLFO") },
            { EMDXDis.CS_SetAmpLFO, new CItem("音量LFO制御", "Set AmpLFO") },
            { EMDXDis.CS_SetOPMLFO, new CItem("OPMLFO制御", "Set OPMLFO") },
            { EMDXDis.CS_SetLFODelay, new CItem("LFOディレイ設定(SoftLFOのみ)", "Set LFO delay (only for SoftLFO)") },
            { EMDXDis.CS_EnablePCM8, new CItem("PCM8拡張モード移行", "Switch to PCM8 extended mode") },
            { EMDXDis.CS_StartFadeout, new CItem("フェードアウト", "Start fade out") },
            { EMDXDis.CS_Error, new CItem("ここには来ないはず:", "Should not come here:") },
            { EMDXDis.RepeatParamError, new CItem("リピート開始パラメータ異常", "Repeat start parameter error") },
            { EMDXDis.FadeoutParamError, new CItem("フェードアウトパラメータ異常", "Fade out parameter error") },
            { EMDXDis.UndefCmd_InterruptDis, new CItem("未定義命令 解析中断", "Undefined instruction analysis abort") },
            { EMDXDis.CommandSwitchError, new CItem("CommandのSwitch例外", "Command switch exception") },
        };
        public static string GetMDXDis(EMDXDis e) {
            if (MDXDisItems.ContainsKey(e)) { return MDXDisItems[e].Get(); }
            return "MDXDis: Language resource undefined.";
        }

        public enum EDirect3D {
            Hardware_CantInit, Wrap_CantInit, Software_CantInit, Reference_CantInit, CantInit
        };
        private static Dictionary<EDirect3D, CItem> Direct3DItems = new() {
            { EDirect3D.Hardware_CantInit, new CItem("Direct3D Hardware を初期化できませんでした。 ", "Failed to initialize Direct3D Hardware.") },
            { EDirect3D.Wrap_CantInit, new CItem("Direct3D Warp を初期化できませんでした。 ", "Failed to initialize Direct3D Warp.") },
            { EDirect3D.Software_CantInit, new CItem("Direct3D Software を初期化できませんでした。 ", "Failed to initialize Direct3D Software.") },
            { EDirect3D.Reference_CantInit, new CItem("Direct3D Reference を初期化できませんでした。 ", "Failed to initialize Direct3D Reference.") },
            { EDirect3D.CantInit, new CItem("Direct3Dを初期化できませんでした。", "Failed to initialize Direct3D.") },
        };
        public static string GetDirect3D(EDirect3D e) {
            if (Direct3DItems.ContainsKey(e)) { return Direct3DItems[e].Get(); }
            return "Direct3D: Language resource undefined.";
        }

        public enum EFolderTag {
            Root, TagNameU, TagNameC, TagNameA, TagNameM, TagNameT, TagNameP, TagNameB, TagNameH, TagNameD,
        };
        private static Dictionary<EFolderTag, CItem> FolderTagItems = new() {
            { EFolderTag.Root, new CItem("ルート","Root") },
            { EFolderTag.TagNameU, new CItem("使用ドライバ","Use driver") },
            { EFolderTag.TagNameC, new CItem("カテゴリ","Category") },
            { EFolderTag.TagNameA, new CItem("作者名","Author") },
            { EFolderTag.TagNameM, new CItem("メーカー","Maker") },
            { EFolderTag.TagNameT, new CItem("タイトル","Title") },
            { EFolderTag.TagNameP, new CItem("パッケージ","Package") },
            { EFolderTag.TagNameB, new CItem("備考","Comment") },
            { EFolderTag.TagNameH, new CItem("ハードウェア","Hardware") },
            { EFolderTag.TagNameD, new CItem("重複","Dup") },
        };
        public static string GetFolderTag(EFolderTag e) {
            if (FolderTagItems.ContainsKey(e)) { return FolderTagItems[e].Get(); }
            return "FolderTag: Language resource undefined.";
        }

        public enum ESwitchX {
            LongDescDefault,
            HeaderKeyword, HeaderSelections, HeaderDefault, HeaderDesc,
            Exit, Exit_SaveAndExit, Exit_ExitOnly, Exit_SaveAndExit_Result, Exit_ExitOnly_Result,

            ConsoleFSDesc, ConsoleFSLongDesc,
            ConsoleFS16Desc, ConsoleFS24Desc, ConsoleFS32Desc, ConsoleFS48Desc,
            IgExceptDesc, IgExceptLongDesc,
            IgExceptOFFDesc, IgExceptONDesc,
            PlayModeDesc, PlayModeLongDesc,
            PlayModeSingleDesc, PlayModeRepeatDesc, PlayModeNormalDesc, PlayModeRandomDesc,
            ADPCMDesc, ADPCMLongDesc,
            ADPCM0Desc, ADPCM1Desc, ADPCM2Desc,
            BosPdxHQDesc, BosPdxHQLongDesc,
            BosPdxHQOFFDesc, BosPdxHQONDesc,
            VolumeDesc, VolumeLongDesc,
            Volume70Desc, Volume75Desc, Volume80Desc, Volume85Desc, Volume90Desc, Volume95Desc, Volume100Desc,
            MXOPMDesc, MXOPMLongDesc,
            MXOPMOFFDesc, MXOPMONDesc,
            MXPCMDesc, MXPCMLongDesc,
            MXPCMOFFDesc, MXPCMONDesc,
            MXLoopDesc, MXLoopLongDesc,
            MXLoop0Desc, MXLoop1Desc, MXLoop2Desc, MXLoop3Desc, MXLoop4Desc,
            VisualDesc, VisualLongDesc,
            VisualOFFDesc, VisualONDesc,
            VisualMulDesc, VisualMulLongDesc,
            VisualMul05Desc, VisualMul1Desc, VisualMul15Desc, VisualMul2Desc, VisualMul25Desc, VisualMul3Desc, VisualMul35Desc, VisualMul4Desc,
            SpeAnaDesc, SpeAnaLongDesc,
            SpeAnaOFFDesc, SpeAnaONDesc,
            OscilloDesc, OscilloLongDesc,
            OscilloOFFDesc, OscilloONDesc,
            FileSelDesc, FileSelLongDesc,
            FileSelOFFDesc, FileSelONDesc,
            FileSelFSDesc, FileSelFSLongDesc,
            FileSelFS16Desc, FileSelFS24Desc, FileSelFS32Desc, FileSelFS48Desc,
        };
        private static Dictionary<ESwitchX, CItem> SwitchXItems = new() {
            { ESwitchX.LongDescDefault, new CItem("各項目にマウスカーソルを移動すると、ここに説明が表示されます。","When you hover your mouse over each item, a description will appear here.") },
            { ESwitchX.HeaderKeyword, new CItem("ｷｰﾜｰﾄﾞ","Keyword") },
            { ESwitchX.HeaderSelections, new CItem("設定項目","Selections") },
            { ESwitchX.HeaderDefault, new CItem("初期値","Default") },
            { ESwitchX.HeaderDesc, new CItem("説明","Description") },
            { ESwitchX.Exit, new CItem("終了","Exit") },

            { ESwitchX.Exit_SaveAndExit, new CItem("autoexec.txtに書き出して閉じる","Export to autoexec.txt and close.") },
            { ESwitchX.Exit_ExitOnly, new CItem("保存しないで閉じる","Close without saving.") },
            { ESwitchX.Exit_SaveAndExit_Result, new CItem("設定をautoexec.txtに書き出しました。","Exported the settings to autoexec.txt.") },
            { ESwitchX.Exit_ExitOnly_Result, new CItem("設定を保存しないで閉じました。","Closed without saving settings.") },

            { ESwitchX.ConsoleFSDesc, new CItem("コンソール画面のフォントサイズ","Console screen font size") },
            { ESwitchX.ConsoleFSLongDesc, new CItem("この設定を変えるとswitch.xのフォントサイズも変わります。","Changing this setting will also change the font size of switch.x.") },
            { ESwitchX.ConsoleFS16Desc, new CItem("16(8x16)","16(8x16)") },
            { ESwitchX.ConsoleFS24Desc, new CItem("24(12x24)","24(12x24)") },
            { ESwitchX.ConsoleFS32Desc, new CItem("32(8x16倍角)","32(8x16x2)") },
            { ESwitchX.ConsoleFS48Desc, new CItem("48(12x24倍角)","48(12x24x2)") },

            { ESwitchX.IgExceptDesc, new CItem("演奏続行可能なエラーを無視する","Ignore errors that allow playback to continue") },
            { ESwitchX.IgExceptLongDesc, new CItem("存在しないPCM番号、リピートや同期コマンドエラー、未知のソフトLFOなどを無視します。","Ignore minor errors that occur during performance.") },
            { ESwitchX.IgExceptOFFDesc, new CItem("OFF (停止する)","OFF (Stop)") },
            { ESwitchX.IgExceptONDesc, new CItem("ON (無視する)","ON (Ignore)") },

            { ESwitchX.PlayModeDesc, new CItem("プレイリスト再生順序","Playlist play order") },
            { ESwitchX.PlayModeLongDesc, new CItem("音楽ファイルが演奏終了したときに、同一フォルダ内の音楽ファイルを順次再生する順序を設定します。","Set the order in which music files in the same folder are played sequentially when the music file finishes playing.") },
            { ESwitchX.PlayModeSingleDesc, new CItem("単曲停止","Single") },
            { ESwitchX.PlayModeRepeatDesc, new CItem("単曲繰り返し","Repeat") },
            { ESwitchX.PlayModeNormalDesc, new CItem("順再生","Normal") },
            { ESwitchX.PlayModeRandomDesc, new CItem("ランダム再生","Random") },

            { ESwitchX.ADPCMDesc, new CItem("ADPCMアップサンプリングモード","ADPCM upsampling mode") },
            { ESwitchX.ADPCMLongDesc, new CItem("数字が下がると荒々しく力強い音感に、数字が上がると滑らかな丸い音感になります。","Higher numbers result in higher quality sound, but use more CPU.") },
            { ESwitchX.ADPCM0Desc, new CItem("0 (最近傍)","0 (Nearest neighbor)") },
            { ESwitchX.ADPCM1Desc, new CItem("1 (線形)","1 (Liner)") },
            { ESwitchX.ADPCM2Desc, new CItem("2 (三次スプライン)","2 (Cubic spline)") },

            { ESwitchX.BosPdxHQDesc, new CItem("高音質版bos.pdxを使用する","Use the high quality version of bos.pdx") },
            { ESwitchX.BosPdxHQLongDesc, new CItem("bos.pdxをリニアPCMセットに置き換えます。ドラムの音量バランスが大きく崩れます。","Replace bos.pdx with linear PCM set. The balance of the drum will be severely disrupted.") },
            { ESwitchX.BosPdxHQOFFDesc, new CItem("OFF (本来のPDXを使用)","OFF (Use original PDX)") },
            { ESwitchX.BosPdxHQONDesc, new CItem("ON (BosPdxHQを使用)","ON (Use BosPdxHQ)") },

            { ESwitchX.VolumeDesc, new CItem("出力音量","Output volume") },
            { ESwitchX.VolumeLongDesc, new CItem("音量をdB単位で設定します。値が大きくなるほど音量が大きくなります。","Set the volume in dB. The higher the value, the louder the volume.") },
            { ESwitchX.Volume70Desc, new CItem("70dB","70dB") },
            { ESwitchX.Volume75Desc, new CItem("75dB","75dB") },
            { ESwitchX.Volume80Desc, new CItem("80dB","80dB") },
            { ESwitchX.Volume85Desc, new CItem("85dB","85dB") },
            { ESwitchX.Volume90Desc, new CItem("90dB","90dB") },
            { ESwitchX.Volume95Desc, new CItem("95dB","95dB") },
            { ESwitchX.Volume100Desc, new CItem("100dB","100dB") },

            { ESwitchX.MXOPMDesc, new CItem("OPMを出力する","Output OPM") },
            { ESwitchX.MXOPMLongDesc, new CItem("OPM(YM2151)出力をミュートできます。(MDXのみ)","OPM (YM2151) output can be muted. (MDX only)") },
            { ESwitchX.MXOPMOFFDesc, new CItem("OFF (OPM無効)","OFF (Disable)") },
            { ESwitchX.MXOPMONDesc, new CItem("ON (OPM有効)","ON (Enable)") },

            { ESwitchX.MXPCMDesc, new CItem("PCMを出力する","Output PCM") },
            { ESwitchX.MXPCMLongDesc, new CItem("ADPCM(MSM6258)出力をミュートできます。(MDXのみ)","ADPCM (MSM6258) output can be muted. (MDX only)") },
            { ESwitchX.MXPCMOFFDesc, new CItem("OFF (ADPCM無効)","OFF (Disable)") },
            { ESwitchX.MXPCMONDesc, new CItem("ON (ADPCM有効)","ON (Enable)") },

            { ESwitchX.MXLoopDesc, new CItem("ループ回数設定","Loop count setting") },
            { ESwitchX.MXLoopLongDesc, new CItem("無限ループする曲がフェードアウトするまでのループ回数を設定できます。","You can set the number of times an infinitely looped song will last before it fades out.") },
            { ESwitchX.MXLoop0Desc, new CItem("0 (無限ループ)","0 (Infinity)") },
            { ESwitchX.MXLoop1Desc, new CItem("1回","1 time") },
            { ESwitchX.MXLoop2Desc, new CItem("2回","2 times") },
            { ESwitchX.MXLoop3Desc, new CItem("3回","3 times") },
            { ESwitchX.MXLoop4Desc, new CItem("4回","4 times") },

            { ESwitchX.VisualDesc, new CItem("再生状況ウィンドウを表示する","Display the playback status window") },
            { ESwitchX.VisualLongDesc, new CItem("MMDSP風のパラメータビュアを表示します。","Displays an MMDSP-like parameter viewer.") },
            { ESwitchX.VisualOFFDesc, new CItem("OFF (非表示)","OFF (Hide)") },
            { ESwitchX.VisualONDesc, new CItem("ON (表示)","ON (Show)") },

            { ESwitchX.VisualMulDesc, new CItem("再生状況ウィンドウの表示倍率","Display magnification of playback status") },
            { ESwitchX.VisualMulLongDesc, new CItem("再生状況ウィンドウをダブルクリックするとフルスクリーン表示になります。","Double-click the playback status window to display it in full screen.") },
            { ESwitchX.VisualMul05Desc, new CItem("0.5倍","x0.5") },
            { ESwitchX.VisualMul1Desc, new CItem("1倍","x1") },
            { ESwitchX.VisualMul15Desc, new CItem("1.5倍","x1.5") },
            { ESwitchX.VisualMul2Desc, new CItem("2倍","x2") },
            { ESwitchX.VisualMul25Desc, new CItem("2.5倍","x2.5") },
            { ESwitchX.VisualMul3Desc, new CItem("3倍","x3") },
            { ESwitchX.VisualMul35Desc, new CItem("3.5倍","x3.5") },
            { ESwitchX.VisualMul4Desc, new CItem("4倍","x4") },

            { ESwitchX.SpeAnaDesc, new CItem("再生状況にスペアナを表示する","Show spectrum analyzer in playback status") },
            { ESwitchX.SpeAnaLongDesc, new CItem("スペアナを非表示にすると僅かにCPU使用率が下がります。","Hiding the spectrum analyzer will slightly reduce CPU usage.") },
            { ESwitchX.SpeAnaOFFDesc, new CItem("OFF (非表示)","OFF (Disable)") },
            { ESwitchX.SpeAnaONDesc, new CItem("ON (表示)","ON (Enable)") },

            { ESwitchX.OscilloDesc, new CItem("再生状況にオシロスコープを表示","Show oscilloscope in playback status") },
            { ESwitchX.OscilloLongDesc, new CItem("オシロスコープを非表示にすると僅かにCPU使用率が下がります。","Hiding the oscilloscope will slightly reduce CPU usage.") },
            { ESwitchX.OscilloOFFDesc, new CItem("OFF (非表示)","OFF (Disable)") },
            { ESwitchX.OscilloONDesc, new CItem("ON (表示)","ON (Enable)") },

            { ESwitchX.FileSelDesc, new CItem("ファイルセレクタを表示する","Show file selector window") },
            { ESwitchX.FileSelLongDesc, new CItem("簡易ファイルセレクタを表示します。","Displays a simple file selector.") },
            { ESwitchX.FileSelOFFDesc, new CItem("OFF (非表示)","OFF (Hide)") },
            { ESwitchX.FileSelONDesc, new CItem("ON (表示)","ON (Show)") },

            { ESwitchX.FileSelFSDesc, new CItem("ファイルセレクタフォントサイズ","File selector font size") },
            { ESwitchX.FileSelFSLongDesc, new CItem("簡易ファイルセレクタのフォントサイズを設定します。","Set the font size of the simple file selector.") },
            { ESwitchX.FileSelFS16Desc, new CItem("16(8x16)","16(8x16)") },
            { ESwitchX.FileSelFS24Desc, new CItem("24(12x24)","24(12x24)") },
            { ESwitchX.FileSelFS32Desc, new CItem("32(8x16倍角)","32(8x16x2)") },
            { ESwitchX.FileSelFS48Desc, new CItem("48(12x24倍角)","48(12x24x2)") },
        };
        public static string GetSwitchX(ESwitchX e) {
            if (SwitchXItems.ContainsKey(e)) { return SwitchXItems[e].Get(); }
            return "SwitchX: Language resource undefined.";
        }

        public enum EFlacOut {
            NotFoundFlacExe, InfinityLoop,
            Completed,Render,Rendered,Canceled,
        };
        private static Dictionary<EFlacOut, CItem> FlacOutItems = new() {
            { EFlacOut.NotFoundFlacExe, new CItem("FLACファイルに書き出すときは、MDXWinと同じフォルダにflac.exeが必要です。","When exporting to a FLAC file, flac.exe is required in the same folder as MDXWin.") },
            { EFlacOut.InfinityLoop, new CItem("「無限ループする」設定の時は、FLACファイルに書き出せません。","When set to infinite loop, it is not possible to export to a FLAC file.") },
            { EFlacOut.Completed, new CItem("完了しました。","Completed.") },
            { EFlacOut.Render, new CItem("FLAC出力","FlacOut") },
            { EFlacOut.Rendered, new CItem("終了","End") },
            { EFlacOut.Canceled, new CItem("中断しました。","Interrupted.") },
        };
        public static string GetFlacOut(EFlacOut e) {
            if (FlacOutItems.ContainsKey(e)) { return FlacOutItems[e].Get(); }
            return "FlacOut: Language resource undefined.";
        }

        public enum EFileSel {
            ContextMenu_FlacOut_This,
            ContextMenu_FlacOut_All,
        };
        private static Dictionary<EFileSel, CItem> FileSelItems = new() {
            { EFileSel.ContextMenu_FlacOut_This, new CItem("このファイルをFLACに変換する","Convert this file to FLAC") },
            { EFileSel.ContextMenu_FlacOut_All, new CItem("フォルダ内全てのファイルをFLACに変換する","Convert all files in this folder to FLAC") },
        };
        public static string GetFileSel(EFileSel e) {
            if (FileSelItems.ContainsKey(e)) { return FileSelItems[e].Get(); }
            return "FileSel: Language resource undefined.";
        }

        public enum EOpenBrowser {
            TitleMain, TitleSub,
            AlwaysOpenChk,
            Clipboard, Open, Cancel,
            CopyToClipboard,
        };
        private static Dictionary<EOpenBrowser, CItem> OpenBrowserItems = new() {
            { EOpenBrowser.TitleMain, new CItem("以下のURLを既定のブラウザで開いてもよろしいですか？","Are you sure you want to open the URL below in your default browser?") },
            { EOpenBrowser.TitleSub, new CItem("もし上手く開けないときは、ブラウザのアドレスにURLをコピー＆ペーストしてください。","If you are unable to open the page, please copy and paste the URL into your browser.") },
            { EOpenBrowser.AlwaysOpenChk, new CItem("以降「既定のブラウザで開く」を常に許可する。","Always allow open in default browser.") },
            { EOpenBrowser.Clipboard, new CItem("クリップボードにコピー","Copy to clipboard") },
            { EOpenBrowser.Open, new CItem("既定のブラウザで開く", "Open with default browser") },
            { EOpenBrowser.Cancel, new CItem("キャンセル","Cancel") },
            { EOpenBrowser.CopyToClipboard, new CItem("クリップボードにコピーしました。","Copy to the clipboard.") },
        };
        public static string GetOpenBrowser(EOpenBrowser e) {
            if (OpenBrowserItems.ContainsKey(e)) { return OpenBrowserItems[e].Get(); }
            return "OpenBrowser: Language resource undefined.";
        }

    }
}
