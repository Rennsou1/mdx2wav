using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MDXWin {
    public partial class WInternet : Window {
        public bool 許可 = false;

        private void ApplyLang() {
            TitleLbl.Text = Lang.CLang.GetWInternet(Lang.CLang.EWInternet.TitleLbl);
            IPLbl.Text = Lang.CLang.GetWInternet(Lang.CLang.EWInternet.IPLbl);
            UserNameLbl.Text = Lang.CLang.GetWInternet(Lang.CLang.EWInternet.UserNameLbl);
            AccountHintLbl.Text = Lang.CLang.GetWInternet(Lang.CLang.EWInternet.AccountHintLbl);
            PermissionChk.Content = Lang.CLang.GetWInternet(Lang.CLang.EWInternet.PermissionChk);
            CloseBtn.Content = 許可 ? Lang.CLang.GetWInternet(Lang.CLang.EWInternet.CloseBtn_Yes) : Lang.CLang.GetWInternet(Lang.CLang.EWInternet.CloseBtn_No);
        }

        private void ApplyCheckBox() {
            Language_JPNCheck.IsChecked = false;
            Language_ENGCheck.IsChecked = false;
            switch (CCommon.INI.LangMode) {
                case Lang.CLang.EMode.JPN: Language_JPNCheck.IsChecked = true; break;
                case Lang.CLang.EMode.ENG: Language_ENGCheck.IsChecked = true; break;
            }
            ApplyLang();
        }

        public WInternet() {
            InitializeComponent();

            ApplyLang();

            switch (MDXOnline004.CClient.Settings.UseIP) {
                case "IPv4": IPAddrLbl.Text = MDXOnline004.CClient.Settings.IPv4Addr;break;
                case "IPv6": IPAddrLbl.Text = "[" + MDXOnline004.CClient.Settings.IPv6Addr + "]"; break;
                default: throw new Exception("未定義のUseIPモードです。 UseIP:" + MDXOnline004.CClient.Settings.UseIP);
            }

            UserNameText.Focus();
            UserNameText.Text = "";

            ApplyCheckBox();
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e) {
            Close();
        }

        private void PermissionChk_Click(object sender, RoutedEventArgs e) {
            許可 = (PermissionChk.IsChecked != null) ? PermissionChk.IsChecked.Value : false;
            ApplyLang();
        }

        private void Language_JPNCheck_Click(object sender, RoutedEventArgs e) {
            CCommon.INI.LangMode = Lang.CLang.EMode.JPN;
            ApplyCheckBox();
        }

        private void Language_ENGCheck_Click(object sender, RoutedEventArgs e) {
            CCommon.INI.LangMode = Lang.CLang.EMode.ENG;
            ApplyCheckBox();
        }
    }
}
