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
    public partial class WFatalError : Window {
        public WFatalError(string Line1, string Line2 = "", string Line3 = "") {
            InitializeComponent();

            Text1Lbl.Text = Line1;
            Text2Lbl.Text = Line2;
            Text3Lbl.Text = Line3;

            ShowDialog();
        }

        private void Button_Click(object sender, RoutedEventArgs e) {
            this.Close();
        }
    }
}
