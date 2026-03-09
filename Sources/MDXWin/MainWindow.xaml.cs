using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static System.Windows.Forms.LinkLabel;

namespace MDXWin {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        public MainWindow() {
            InitializeComponent();
            CProgram.BootLog.Add("Initialize component");

            CProgram.ExecBoot_FromMainWindow();
        }


        private void Window_Loaded(object sender, RoutedEventArgs e) {
            this.Title =  "MDXWin Main console.";
            CProgram.BootLog.Add("Window loaded");

            {
                SetMenuFunc(1, Lang.CLang.GetBoot(Lang.CLang.EBoot.MenuItemDef1), "FileSel Toggle");
                SetMenuFunc(2, Lang.CLang.GetBoot(Lang.CLang.EBoot.MenuItemDef2), "Visual Toggle");
                SetMenuFunc(3, Lang.CLang.GetBoot(Lang.CLang.EBoot.MenuItemDef3), "OpenFolder");
                SetMenuFunc(4, Lang.CLang.GetBoot(Lang.CLang.EBoot.MenuItemDef4), "OpenDocs");
                SetMenuFunc(5, Lang.CLang.GetBoot(Lang.CLang.EBoot.MenuItemDef5), "MxSeek 30%");
                SetMenuFunc(6, Lang.CLang.GetBoot(Lang.CLang.EBoot.MenuItemDef6), "MxStop");
                SetMenuFunc(7, Lang.CLang.GetBoot(Lang.CLang.EBoot.MenuItemDef7), "PlayNext");
                SetMenuFunc(8, Lang.CLang.GetBoot(Lang.CLang.EBoot.MenuItemDef8), "Switch");
                CProgram.BootLog.Add("Set default menu func");

                MenuHelp.Header = Lang.CLang.GetMainWindow(Lang.CLang.EMainWindow.MenuHelp);
                CProgram.BootLog.Add("Set lang");
            }

            InputBox.Focus();
            InputBox.Text = Lang.CLang.GetBoot(Lang.CLang.EBoot.InputBoxDefText);
            CProgram.BootLog.Add("Set InputBox");

            CCommon.Console = new CConsole(this);
            CProgram.BootLog.Add("new CConsole");

            CProgram.ExecBoot_MainWindow_Window_Loaded(this);
        }

        private void Window_Closed(object sender, EventArgs e) {
            CProgram.Window_Closed();
        }

        private void MenuHelp_Click(object sender, RoutedEventArgs e) {
            CProgram.CmdLineMacro.Add("Help");
        }

        private bool InputBox_ClearDefText() {
            if (InputBox.Text.Equals(Lang.CLang.GetBoot(Lang.CLang.EBoot.InputBoxDefText))) {
                InputBox.Text = "";
                return (true);
            }
            return (false);
        }
        private void InputBox_PreviewMouseDown(object sender, MouseButtonEventArgs e) {
            InputBox_ClearDefText();
        }

        private string LastInputCommandLine = "";

        private void InputBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e) {
            InputBox_ClearDefText();

            switch (e.Key) {
                case Key.Enter:
                    e.Handled = true;

                    var txt = InputBox.Text;
                    InputBox.Text = "";

                    CProgram.CmdLineMacro.Add(txt);
                    LastInputCommandLine = txt;
                    break;
                case Key.Up:
                case Key.F3: {
                    InputBox_ClearDefText();
                    InputBox.Text = LastInputCommandLine;
                    InputBox.Focus();
                    InputBox.Select(InputBox.Text.Length, 0);
                }
                break;
            }
        }

        private void LogListCtxMenu_CopyToClipboard_Click(object sender, RoutedEventArgs e) {
            if (LogList.SelectedItem == null) { return; }
            var Item = (CConsole.CLog.CItem)LogList.SelectedItem;
            var Line = Item.Line;
            if (Line.StartsWith("A>")) { Line = Line.Substring(2); }
            System.Windows.Clipboard.SetText(Line);
        }

        private const int MenuFuncsCount = 8;
        private string[] MenuFuncCommands = new string[MenuFuncsCount] { "", "", "", "", "", "", "", "" };

        public List<KeyValuePair<string, string>> GetMenuFuncs() {
            var res = new List<KeyValuePair<string, string>>();
            for (var idx = 1; idx <= MenuFuncsCount; idx++) {
                var func = GetMenuFunc(idx);
                res.Add(new KeyValuePair<string, string>("SetFunc", idx.ToString() + "," + func.Name + "," + func.Command));
            }
            return res;
        }

        private void ExecMenuFunc(int idx) {
            var cmd = MenuFuncCommands[idx - 1];
            if (cmd.Equals("")) {
                CCommon.Console.WriteLine最後から一行前に追加(Lang.CLang.GetCommand(Lang.CLang.ECommand.ExecUndefFunc1));
                CCommon.Console.WriteLine最後から一行前に追加(Lang.CLang.GetCommand(Lang.CLang.ECommand.ExecUndefFunc2));
                return;
            }
            CProgram.CmdLineMacro.Add(cmd);
        }

        private class CGetMenuFuncRes {
            public string Name, Command;
        }
        private CGetMenuFuncRes GetMenuFunc(int FuncNum) { // FuncNum:1～MenuFuncsCount
            var res = new CGetMenuFuncRes();
            switch (FuncNum) {
                case 1: res.Name = (string)MenuFunc1.Header; break;
                case 2: res.Name = (string)MenuFunc2.Header; break;
                case 3: res.Name = (string)MenuFunc3.Header; break;
                case 4: res.Name = (string)MenuFunc4.Header; break;
                case 5: res.Name = (string)MenuFunc5.Header; break;
                case 6: res.Name = (string)MenuFunc6.Header; break;
                case 7: res.Name = (string)MenuFunc7.Header; break;
                case 8: res.Name = (string)MenuFunc8.Header; break;
                default: throw new Exception(Lang.CLang.GetCommand(Lang.CLang.ECommand.Func_OverIndex));
            }
            res.Command = MenuFuncCommands[FuncNum - 1];
            return (res);
        }
        private void SetMenuFunc(int FuncNum, string Name, string Command) { // FuncNum:1～MenuFuncsCount
            switch (FuncNum) {
                case 1: MenuFunc1.Header = Name; break;
                case 2: MenuFunc2.Header = Name; break;
                case 3: MenuFunc3.Header = Name; break;
                case 4: MenuFunc4.Header = Name; break;
                case 5: MenuFunc5.Header = Name; break;
                case 6: MenuFunc6.Header = Name; break;
                case 7: MenuFunc7.Header = Name; break;
                case 8: MenuFunc8.Header = Name; break;
                default: throw new Exception(Lang.CLang.GetCommand(Lang.CLang.ECommand.Func_OverIndex));
            }
            MenuFuncCommands[FuncNum - 1] = Command;
        }
        public void SetMenuFunc(string param) {
            int FuncNum;
            {
                var pos = param.IndexOf(',');
                if (pos == -1) { Console.WriteLine(Lang.CLang.GetCommand(Lang.CLang.ECommand.SetFunc_FormatError)); return; }
                if (!int.TryParse(param.Substring(0, pos), out FuncNum)) { Console.WriteLine(Lang.CLang.GetCommand(Lang.CLang.ECommand.SetFunc_NumIsNotNum)); return; }
                param = param.Substring(pos + 1);
                if ((FuncNum < 1) || (MenuFuncsCount < FuncNum)) { Console.WriteLine(Lang.CLang.GetCommand(Lang.CLang.ECommand.SetFunc_NumIsOver)); return; }
            }

            string FuncName;
            {
                var pos = param.IndexOf(',');
                if (pos == -1) { Console.WriteLine(Lang.CLang.GetCommand(Lang.CLang.ECommand.SetFunc_FormatError)); return; }
                FuncName = param.Substring(0, pos);
                param = param.Substring(pos + 1);
            }

            var Command = param;

            SetMenuFunc(FuncNum, FuncName, Command);
        }

        private void MenuFunc1_Click(object sender, RoutedEventArgs e) { ExecMenuFunc(1); }
        private void MenuFunc2_Click(object sender, RoutedEventArgs e) { ExecMenuFunc(2); }
        private void MenuFunc3_Click(object sender, RoutedEventArgs e) { ExecMenuFunc(3); }
        private void MenuFunc4_Click(object sender, RoutedEventArgs e) { ExecMenuFunc(4); }
        private void MenuFunc5_Click(object sender, RoutedEventArgs e) { ExecMenuFunc(5); }
        private void MenuFunc6_Click(object sender, RoutedEventArgs e) { ExecMenuFunc(6); }
        private void MenuFunc7_Click(object sender, RoutedEventArgs e) { ExecMenuFunc(7); }
        private void MenuFunc8_Click(object sender, RoutedEventArgs e) { ExecMenuFunc(8); }

        private void LogList_Image_MouseUp(object sender, MouseButtonEventArgs e) {
            if (e.ChangedButton == MouseButton.Left) {
                if (LogList.SelectedItem == null) { return; }
                var Item = (CConsole.CLog.CItem)LogList.SelectedItem;
                switch (Item.Mode) {
                    case CConsole.CLog.CItem.EMode.Text: break;
                    case CConsole.CLog.CItem.EMode.Command:
                        InputBox_ClearDefText();
                        InputBox.Text = Item.HintText + " ";
                        InputBox.Focus();
                        InputBox.Select(InputBox.Text.Length, 0);
                        break;
                    case CConsole.CLog.CItem.EMode.Dir:
                        CProgram.CmdLineMacro.Add("cd " + Item.HintText);
                        CProgram.CmdLineMacro.Add("dir");
                        break;
                    case CConsole.CLog.CItem.EMode.File:
                        CProgram.CmdLineMacro.Add(CCmdLine.GetMusicPlayCommand(Item.HintText));
                        break;
                }
            }
        }
    }
}