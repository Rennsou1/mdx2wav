using Lang;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MDXWin {
    /// <summary>
    /// Interaction logic for WOpenBrowser.xaml
    /// </summary>
    public partial class WOpenBrowser : Window {
        public bool 許可 = false;

        public WOpenBrowser(string Url) {
            InitializeComponent();

            TitleMainLbl.Text = CLang.GetOpenBrowser(CLang.EOpenBrowser.TitleMain);
            TitleSubLbl.Text = CLang.GetOpenBrowser(CLang.EOpenBrowser.TitleSub);
            UrlText.Text = Url;
            AlwaysOpenChk.Content = CLang.GetOpenBrowser(CLang.EOpenBrowser.AlwaysOpenChk);

            ClipboardBtn.Content = CLang.GetOpenBrowser(CLang.EOpenBrowser.Clipboard);
            OpenBtn.Content = CLang.GetOpenBrowser(CLang.EOpenBrowser.Open);
            CancelBtn.Content = CLang.GetOpenBrowser(CLang.EOpenBrowser.Cancel);
        }

        public static bool 許可を得る必要がある() {
            return !CCommon.INI.AcceptDefaultBrowser;
        }

        private void ClipboardBtn_Click(object sender, RoutedEventArgs e) {
            System.Windows.Clipboard.SetText(UrlText.Text);
            CCommon.Console.WriteLine(CLang.GetOpenBrowser(CLang.EOpenBrowser.CopyToClipboard));
            許可 = false;
            CCommon.INI.AcceptDefaultBrowser = AlwaysOpenChk.IsChecked.Value;
            Close();
        }

        private void OpenBtn_Click(object sender, RoutedEventArgs e) {
            許可 = true;
            CCommon.INI.AcceptDefaultBrowser = AlwaysOpenChk.IsChecked.Value;
            Close();
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e) {
            許可 = false;
            Close();
        }

        public static void Exec(string Url) {
            using (var process = new System.Diagnostics.Process()) {
                process.StartInfo.FileName = Url;
                process.StartInfo.UseShellExecute = true;
                process.Start();
            }
        }
    }
}
