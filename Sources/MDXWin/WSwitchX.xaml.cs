using Lang;
using Microsoft.VisualBasic.Devices;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
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
using static System.Net.Mime.MediaTypeNames;

namespace MDXWin {
    /// <summary>
    /// Interaction logic for WSwitchX.xaml
    /// </summary>
    public partial class WSwitchX : Window {
        private static MainWindow MainWindow;

        private static int CurrentFontSize;

        private const int ImagePadding = 2;

        private const UInt32 ColBG = 0x000000;
        private const UInt32 ColText = 0xffffff;
        private const UInt32 ColTitleFrame = 0x00dddd;
        private const UInt32 ColMainFrame = 0xdddd00;
        private const UInt32 ColDescFrame = 0x00dddd;
        private const UInt32 ColSelectBG = 0xdddd00;
        private const UInt32 ColSelectText = 0x000000;

        private const string TitleMsg = "ＳＷＩＴＣＨ　ｆｏｒ　ＭＤＸＷｉｎ　Version 0.04 Copyright 2022-23 Moonlight.";

        private class CCallback {
            private WSwitchX Window;
            public CCallback(WSwitchX _Window) {
                Window = _Window;
            }
            public void ApplyLongDesc(string Text) {
                Window.FutterLongDescImg.Source = CCommon.CGROM.DrawString(CurrentFontSize, Text);
            }
        }
        private static CCallback Callback;

        private static System.Windows.Controls.Image GetBaseImage(Thickness Margin, int FontSize, string Text, uint BackgroundColor = 0x00000000, uint ForegroundColor = 0x11ffffff) {
            var img = new System.Windows.Controls.Image();
            img.UseLayoutRounding = true;
            img.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            img.VerticalAlignment = VerticalAlignment.Top;
            img.Stretch = Stretch.None;
            img.Margin = Margin;
            img.Source = CCommon.CGROM.DrawString(FontSize, Text, ImagePadding, BackgroundColor, ForegroundColor);
            return img;
        }

        private class CItemsMargins {
            public const int Right = 16;
            public const int Y = 4;
            public Thickness Keyword = new Thickness(0, Y, Right, Y);
            public Thickness Selections = new Thickness(0, Y, Right, Y);
            public Thickness Default = new Thickness(0, Y, Right, Y);
            public Thickness Desc = new Thickness(0, Y, Right, Y);
        }
        private static CItemsMargins ItemsMargins = new CItemsMargins();

        private class CItem {
            public string Keyword;
            public string Default;
            public string CurrentParam;
            public string Desc;
            public string LongDesc;

            public class CSelection {
                public string Param, Desc;
                public System.Windows.Controls.Image Image = null;
                public CSelection(string _Param, string _Desc) {
                    Param = _Param;
                    Desc = _Desc;
                }
            }
            public List<CSelection> Selections = new List<CSelection>();

            public CItem(string _Keyword, string _Desc, string _LongDesc) {
                Keyword = _Keyword;
                var Value = GetValue(Keyword);
                Default = Value.Default;
                CurrentParam = Value.Current;
                Desc = _Desc;
                LongDesc = _LongDesc;
            }

            private void MouseEnter(object sender, System.Windows.Input.MouseEventArgs e) {
                Callback.ApplyLongDesc(LongDesc);
            }

            private void MouseUp(object sender, MouseButtonEventArgs e) {
                var img = (System.Windows.Controls.Image)sender;
                var SelectIndex = (int)img.DataContext;

                for (var idx = 0; idx < Selections.Count; idx++) {
                    var BackgroundColor = ColBG;
                    var ForegroundColor = ColText;
                    if (idx == SelectIndex) {
                        BackgroundColor = ColSelectBG;
                        ForegroundColor = ColSelectText;
                    }
                    Selections[idx].Image.Source = CCommon.CGROM.DrawString(CurrentFontSize, Selections[idx].Desc, ImagePadding, BackgroundColor, ForegroundColor);
                }

                var Selection = Selections[SelectIndex];
                CurrentParam = Selection.Param;
                CProgram.CmdLineMacro.Add(Keyword + " " + Selection.Param);

                if (Keyword.Equals("ConsoleFS")) {
                    Window.Close();
                    CProgram.CmdLineMacro.Add("switch");
                }
            }

            private WSwitchX Window;
            private int RowIndex;
            private void AddChildrenToGrid(int Column, Thickness Margin, string Text) {
                var img = GetBaseImage(Margin, CurrentFontSize, Text);
                Grid.SetRow(img, RowIndex);
                Grid.SetColumn(img, Column);
                img.MouseEnter += MouseEnter;
                Window.ItemsGrid.Children.Add(img);
            }
            public void AddElements(WSwitchX _Window) {
                Window = _Window;

                RowIndex = Window.ItemsGrid.RowDefinitions.Count;
                {
                    var RowDefinition = new RowDefinition();
                    RowDefinition.Height = GridLength.Auto;
                    Window.ItemsGrid.RowDefinitions.Add(RowDefinition);
                }
                AddChildrenToGrid(0, ItemsMargins.Keyword, Keyword);
                {
                    var sp = new StackPanel();
                    sp.Orientation = System.Windows.Controls.Orientation.Horizontal;
                    sp.MouseEnter += MouseEnter;
                    for (var idx = 0; idx < Selections.Count; idx++) {
                        var Selection = Selections[idx];
                        var BackgroundColor = ColBG;
                        var ForegroundColor = ColText;
                        if (Selection.Param.Equals(CurrentParam)) {
                            BackgroundColor = ColSelectBG;
                            ForegroundColor = ColSelectText;
                        }
                        var img = GetBaseImage(ItemsMargins.Selections, CurrentFontSize, Selection.Desc, BackgroundColor, ForegroundColor);
                        img.DataContext = idx;
                        img.MouseUp += MouseUp;
                        Selection.Image = img;
                        sp.Children.Add(img);
                    }
                    Grid.SetRow(sp, RowIndex);
                    Grid.SetColumn(sp, 1);
                    Window.ItemsGrid.Children.Add(sp);
                }
                AddChildrenToGrid(2, ItemsMargins.Default, Default);
                AddChildrenToGrid(3, ItemsMargins.Desc, Desc);
            }
        }
        private List<CItem> Items = new List<CItem>();

        private void Items_Init_ins_AddChildrenToGrid(WSwitchX Window, int Column, int RowIndex, Thickness Margin, string Text) {
            var img = GetBaseImage(Margin, CurrentFontSize, Text);
            Grid.SetRow(img, RowIndex);
            Grid.SetColumn(img, Column);
            Window.ItemsGrid.Children.Add(img);
        }
        private void MouseEnter_ClearLongDesc(object sender, System.Windows.Input.MouseEventArgs e) {
            Callback.ApplyLongDesc("");
        }
        private void Items_Init(WSwitchX Window) {
            {
                var RowIndex = Window.ItemsGrid.RowDefinitions.Count;
                {
                    var RowDefinition = new RowDefinition();
                    RowDefinition.Height = GridLength.Auto;
                    Window.ItemsGrid.RowDefinitions.Add(RowDefinition);
                }
                Items_Init_ins_AddChildrenToGrid(Window, 0, RowIndex, ItemsMargins.Keyword, CLang.GetSwitchX(CLang.ESwitchX.HeaderKeyword));
                Items_Init_ins_AddChildrenToGrid(Window, 1, RowIndex, ItemsMargins.Selections, CLang.GetSwitchX(CLang.ESwitchX.HeaderSelections));
                Items_Init_ins_AddChildrenToGrid(Window, 2, RowIndex, ItemsMargins.Default, CLang.GetSwitchX(CLang.ESwitchX.HeaderDefault));
                Items_Init_ins_AddChildrenToGrid(Window, 3, RowIndex, ItemsMargins.Desc, CLang.GetSwitchX(CLang.ESwitchX.HeaderDesc));
            }

            {
                var RowDefinition = new RowDefinition();
                RowDefinition.Height = new GridLength(4, GridUnitType.Pixel);
                Window.ItemsGrid.RowDefinitions.Add(RowDefinition);
            }

            foreach (var Item in Items) {
                Item.AddElements(this);
            }

            {
                var RowDefinition = new RowDefinition();
                RowDefinition.Height = new GridLength(8, GridUnitType.Pixel);
                Window.ItemsGrid.RowDefinitions.Add(RowDefinition);
            }

            {
                var RowIndex = Window.ItemsGrid.RowDefinitions.Count;
                {
                    var RowDefinition = new RowDefinition();
                    RowDefinition.Height = GridLength.Auto;
                    Window.ItemsGrid.RowDefinitions.Add(RowDefinition);
                }
                Items_Init_ins_AddChildrenToGrid(Window, 0, RowIndex, ItemsMargins.Keyword, CLang.GetSwitchX(CLang.ESwitchX.Exit));
                var Texts = new string[] { };
                {
                    var sp = new StackPanel();
                    sp.Orientation = System.Windows.Controls.Orientation.Horizontal;
                    sp.UseLayoutRounding = true;
                    {
                        var img = GetBaseImage(ItemsMargins.Selections, CurrentFontSize, CLang.GetSwitchX(CLang.ESwitchX.Exit_SaveAndExit));
                        img.DataContext = Window;
                        img.MouseEnter += MouseEnter_ClearLongDesc;
                        img.MouseUp += Exit_SaveAndExit_MouseUp;
                        sp.Children.Add(img);
                    }
                    {
                        var img = GetBaseImage(ItemsMargins.Selections, CurrentFontSize, CLang.GetSwitchX(CLang.ESwitchX.Exit_ExitOnly));
                        img.DataContext = Window;
                        img.MouseEnter += MouseEnter_ClearLongDesc;
                        img.MouseUp += Exit_ExitOnly_MouseUp;
                        sp.Children.Add(img);
                    }
                    Grid.SetRow(sp, RowIndex);
                    Grid.SetColumn(sp, 1);
                    Grid.SetColumnSpan(sp, 3);
                    Window.ItemsGrid.Children.Add(sp);
                }
            }

            {
                var Frame = new System.Windows.Shapes.Rectangle();
                Frame.Stroke = GetBrush(ColMainFrame);
                Frame.StrokeThickness = 2;
                Frame.Margin = new Thickness(-6, 0, -6, -6);
                Frame.UseLayoutRounding = true;
                Grid.SetColumn(Frame, 0);
                Grid.SetColumnSpan(Frame, 4);
                Grid.SetRow(Frame, 1);
                Grid.SetRowSpan(Frame, Window.ItemsGrid.RowDefinitions.Count - 1);
                Window.ItemsGrid.Children.Add(Frame);
            }
        }

        public WSwitchX(MainWindow _MainWindow) {
            InitializeComponent();
            MainWindow=_MainWindow;
            Callback = new CCallback(this);
        }

        private SolidColorBrush GetBrush(UInt32 Color32) {
            var r = (byte)((Color32 >> 16) & 0xff);
            var g = (byte)((Color32 >> 8) & 0xff);
            var b = (byte)((Color32 >> 0) & 0xff);
            return new SolidColorBrush(System.Windows.Media.Color.FromRgb(r, g, b));
        }

        private class CGetValue {
            public string Default, Current;
            public CGetValue(string _Default, string _Current) {
                Default = _Default;
                Current = _Current;
            }
        }
        private static CGetValue GetValue(string Keyword) {
            var HDPISettings = new CCommon.CHDPISettings();
            switch (Keyword) {
                case "ConsoleFS": return new CGetValue(HDPISettings.FontSize_Console.ToString(), CCommon.FontSize_Console.ToString());
                case "IgExcept": return new CGetValue("ON", MXDRV.CCanIgnoreException.IgnoreException ? "ON" : "OFF");
                case "PlayMode": return new CGetValue("Normal", CPlayList.PlayMode.ToString());
                case "ADPCM": return new CGetValue("2", CCommon.AudioThread.Settings.GetADPCMModeInt().ToString());
                case "BosPdxHQ": return new CGetValue("ON", MXDRV.CCommon.UseBosPdxHQ ? "ON" : "OFF");
                case "Volume": return new CGetValue("85", CCommon.AudioThread.Settings.VolumeDB.ToString());
                case "MXOPM": return new CGetValue("ON", CCommon.AudioThread.Settings.OPMEnabled ? "ON" : "OFF");
                case "MXPCM": return new CGetValue("ON", CCommon.AudioThread.Settings.PCMEnabled ? "ON" : "OFF");
                case "MXLoop": return new CGetValue("2", CCommon.AudioThread.Settings.LoopCount.ToString());
                case "Visual": return new CGetValue("OFF", CProgram.GetVisualVisible() ? "ON" : "OFF");
                case "VisualMul": return new CGetValue(HDPISettings.VisualMul.ToString(), CCommon.VisualMul.ToString());
                case "SpeAna": return new CGetValue("ON", CCommon.VisualSpeAna ? "ON" : "OFF");
                case "Oscillo": return new CGetValue("ON", CCommon.VisualOscillo ? "ON" : "OFF");
                case "FileSel": return new CGetValue("OFF", CProgram.GetFileSelectorVisible() ? "ON" : "OFF");
                case "FileSelFS": return new CGetValue(HDPISettings.FontSize_FileSel.ToString(), CCommon.FontSize_FileSel.ToString());
                default: throw new Exception("Internal error.");
            }
        }
        private void Window_Loaded(object sender, RoutedEventArgs e) {
            this.Title = "switch.x";

            this.Background = GetBrush(ColBG);
            this.Foreground = GetBrush(ColText);

            CurrentFontSize = CCommon.FontSize_Console;

            HeaderFrame.Stroke = GetBrush(ColTitleFrame);
            HeaderTitleImg.Source = CCommon.CGROM.DrawString(CurrentFontSize, TitleMsg);

            FutterFrame.Stroke = GetBrush(ColDescFrame);
            FutterLongDescImg.Source = CCommon.CGROM.DrawString(CurrentFontSize, CLang.GetSwitchX(CLang.ESwitchX.LongDescDefault));

            {
                var Item = new CItem("ConsoleFS", CLang.GetSwitchX(CLang.ESwitchX.ConsoleFSDesc), CLang.GetSwitchX(CLang.ESwitchX.ConsoleFSLongDesc));
                Item.Selections.Add(new CItem.CSelection("16", CLang.GetSwitchX(CLang.ESwitchX.ConsoleFS16Desc)));
                Item.Selections.Add(new CItem.CSelection("24", CLang.GetSwitchX(CLang.ESwitchX.ConsoleFS24Desc)));
                Item.Selections.Add(new CItem.CSelection("32", CLang.GetSwitchX(CLang.ESwitchX.ConsoleFS32Desc)));
                Item.Selections.Add(new CItem.CSelection("48", CLang.GetSwitchX(CLang.ESwitchX.ConsoleFS48Desc)));
                Items.Add(Item);
            }
            {
                var Item = new CItem("IgExcept", CLang.GetSwitchX(CLang.ESwitchX.IgExceptDesc), CLang.GetSwitchX(CLang.ESwitchX.IgExceptLongDesc));
                Item.Selections.Add(new CItem.CSelection("OFF", CLang.GetSwitchX(CLang.ESwitchX.IgExceptOFFDesc)));
                Item.Selections.Add(new CItem.CSelection("ON", CLang.GetSwitchX(CLang.ESwitchX.IgExceptONDesc)));
                Items.Add(Item);
            }
            {
                var Item = new CItem("PlayMode", CLang.GetSwitchX(CLang.ESwitchX.PlayModeDesc), CLang.GetSwitchX(CLang.ESwitchX.PlayModeLongDesc));
                Item.Selections.Add(new CItem.CSelection("Single", CLang.GetSwitchX(CLang.ESwitchX.PlayModeSingleDesc)));
                Item.Selections.Add(new CItem.CSelection("Repeat", CLang.GetSwitchX(CLang.ESwitchX.PlayModeRepeatDesc)));
                Item.Selections.Add(new CItem.CSelection("Normal", CLang.GetSwitchX(CLang.ESwitchX.PlayModeNormalDesc)));
                Item.Selections.Add(new CItem.CSelection("Random", CLang.GetSwitchX(CLang.ESwitchX.PlayModeRandomDesc)));
                Items.Add(Item);
            }
            {
                var Item = new CItem("ADPCM", CLang.GetSwitchX(CLang.ESwitchX.ADPCMDesc), CLang.GetSwitchX(CLang.ESwitchX.ADPCMLongDesc));
                Item.Selections.Add(new CItem.CSelection("0", CLang.GetSwitchX(CLang.ESwitchX.ADPCM0Desc)));
                Item.Selections.Add(new CItem.CSelection("1", CLang.GetSwitchX(CLang.ESwitchX.ADPCM1Desc)));
                Item.Selections.Add(new CItem.CSelection("2", CLang.GetSwitchX(CLang.ESwitchX.ADPCM2Desc)));
                Items.Add(Item);
            }
            {
                var Item = new CItem("BosPdxHQ", CLang.GetSwitchX(CLang.ESwitchX.BosPdxHQDesc), CLang.GetSwitchX(CLang.ESwitchX.BosPdxHQLongDesc));
                Item.Selections.Add(new CItem.CSelection("OFF", CLang.GetSwitchX(CLang.ESwitchX.BosPdxHQOFFDesc)));
                Item.Selections.Add(new CItem.CSelection("ON", CLang.GetSwitchX(CLang.ESwitchX.BosPdxHQONDesc)));
                Items.Add(Item);
            }
            {
                var Item = new CItem("Volume", CLang.GetSwitchX(CLang.ESwitchX.VolumeDesc), CLang.GetSwitchX(CLang.ESwitchX.VolumeLongDesc));
                Item.Selections.Add(new CItem.CSelection("70", CLang.GetSwitchX(CLang.ESwitchX.Volume70Desc)));
                Item.Selections.Add(new CItem.CSelection("75", CLang.GetSwitchX(CLang.ESwitchX.Volume75Desc)));
                Item.Selections.Add(new CItem.CSelection("80", CLang.GetSwitchX(CLang.ESwitchX.Volume80Desc)));
                Item.Selections.Add(new CItem.CSelection("85", CLang.GetSwitchX(CLang.ESwitchX.Volume85Desc)));
                Item.Selections.Add(new CItem.CSelection("90", CLang.GetSwitchX(CLang.ESwitchX.Volume90Desc)));
                Item.Selections.Add(new CItem.CSelection("95", CLang.GetSwitchX(CLang.ESwitchX.Volume95Desc)));
                Item.Selections.Add(new CItem.CSelection("100", CLang.GetSwitchX(CLang.ESwitchX.Volume100Desc)));
                Items.Add(Item);
            }
            {
                var Item = new CItem("MXOPM", CLang.GetSwitchX(CLang.ESwitchX.MXOPMDesc), CLang.GetSwitchX(CLang.ESwitchX.MXOPMLongDesc));
                Item.Selections.Add(new CItem.CSelection("OFF", CLang.GetSwitchX(CLang.ESwitchX.MXOPMOFFDesc)));
                Item.Selections.Add(new CItem.CSelection("ON", CLang.GetSwitchX(CLang.ESwitchX.MXOPMONDesc)));
                Items.Add(Item);
            }
            {
                var Item = new CItem("MXPCM", CLang.GetSwitchX(CLang.ESwitchX.MXPCMDesc), CLang.GetSwitchX(CLang.ESwitchX.MXPCMLongDesc));
                Item.Selections.Add(new CItem.CSelection("OFF", CLang.GetSwitchX(CLang.ESwitchX.MXPCMOFFDesc)));
                Item.Selections.Add(new CItem.CSelection("ON", CLang.GetSwitchX(CLang.ESwitchX.MXPCMONDesc)));
                Items.Add(Item);
            }
            {
                var Item = new CItem("MXLoop", CLang.GetSwitchX(CLang.ESwitchX.MXLoopDesc), CLang.GetSwitchX(CLang.ESwitchX.MXLoopLongDesc));
                Item.Selections.Add(new CItem.CSelection("0", CLang.GetSwitchX(CLang.ESwitchX.MXLoop0Desc)));
                Item.Selections.Add(new CItem.CSelection("1", CLang.GetSwitchX(CLang.ESwitchX.MXLoop1Desc)));
                Item.Selections.Add(new CItem.CSelection("2", CLang.GetSwitchX(CLang.ESwitchX.MXLoop2Desc)));
                Item.Selections.Add(new CItem.CSelection("3", CLang.GetSwitchX(CLang.ESwitchX.MXLoop3Desc)));
                Item.Selections.Add(new CItem.CSelection("4", CLang.GetSwitchX(CLang.ESwitchX.MXLoop4Desc)));
                Items.Add(Item);
            }
            {
                var Item = new CItem("Visual", CLang.GetSwitchX(CLang.ESwitchX.VisualDesc), CLang.GetSwitchX(CLang.ESwitchX.VisualLongDesc));
                Item.Selections.Add(new CItem.CSelection("OFF", CLang.GetSwitchX(CLang.ESwitchX.VisualOFFDesc)));
                Item.Selections.Add(new CItem.CSelection("ON", CLang.GetSwitchX(CLang.ESwitchX.VisualONDesc)));
                Items.Add(Item);
            }
            {
                var Item = new CItem("VisualMul", CLang.GetSwitchX(CLang.ESwitchX.VisualMulDesc), CLang.GetSwitchX(CLang.ESwitchX.VisualMulLongDesc));
                Item.Selections.Add(new CItem.CSelection("0.5", CLang.GetSwitchX(CLang.ESwitchX.VisualMul05Desc)));
                Item.Selections.Add(new CItem.CSelection("1", CLang.GetSwitchX(CLang.ESwitchX.VisualMul1Desc)));
                Item.Selections.Add(new CItem.CSelection("1.5", CLang.GetSwitchX(CLang.ESwitchX.VisualMul15Desc)));
                Item.Selections.Add(new CItem.CSelection("2", CLang.GetSwitchX(CLang.ESwitchX.VisualMul2Desc)));
                Item.Selections.Add(new CItem.CSelection("2.5", CLang.GetSwitchX(CLang.ESwitchX.VisualMul25Desc)));
                Item.Selections.Add(new CItem.CSelection("3", CLang.GetSwitchX(CLang.ESwitchX.VisualMul3Desc)));
                Item.Selections.Add(new CItem.CSelection("3.5", CLang.GetSwitchX(CLang.ESwitchX.VisualMul35Desc)));
                Item.Selections.Add(new CItem.CSelection("4", CLang.GetSwitchX(CLang.ESwitchX.VisualMul4Desc)));
                Items.Add(Item);
            }
            {
                var Item = new CItem("SpeAna", CLang.GetSwitchX(CLang.ESwitchX.SpeAnaDesc), CLang.GetSwitchX(CLang.ESwitchX.SpeAnaLongDesc));
                Item.Selections.Add(new CItem.CSelection("OFF", CLang.GetSwitchX(CLang.ESwitchX.SpeAnaOFFDesc)));
                Item.Selections.Add(new CItem.CSelection("ON", CLang.GetSwitchX(CLang.ESwitchX.SpeAnaONDesc)));
                Items.Add(Item);
            }
            {
                var Item = new CItem("Oscillo", CLang.GetSwitchX(CLang.ESwitchX.OscilloDesc), CLang.GetSwitchX(CLang.ESwitchX.OscilloLongDesc));
                Item.Selections.Add(new CItem.CSelection("OFF", CLang.GetSwitchX(CLang.ESwitchX.OscilloOFFDesc)));
                Item.Selections.Add(new CItem.CSelection("ON", CLang.GetSwitchX(CLang.ESwitchX.OscilloONDesc)));
                Items.Add(Item);
            }
            {
                var Item = new CItem("FileSel", CLang.GetSwitchX(CLang.ESwitchX.FileSelDesc), CLang.GetSwitchX(CLang.ESwitchX.FileSelLongDesc));
                Item.Selections.Add(new CItem.CSelection("OFF", CLang.GetSwitchX(CLang.ESwitchX.FileSelOFFDesc)));
                Item.Selections.Add(new CItem.CSelection("ON", CLang.GetSwitchX(CLang.ESwitchX.FileSelONDesc)));
                Items.Add(Item);
            }
            {
                var Item = new CItem("FileSelFS", CLang.GetSwitchX(CLang.ESwitchX.FileSelFSDesc), CLang.GetSwitchX(CLang.ESwitchX.FileSelFSLongDesc));
                Item.Selections.Add(new CItem.CSelection("16", CLang.GetSwitchX(CLang.ESwitchX.FileSelFS16Desc)));
                Item.Selections.Add(new CItem.CSelection("24", CLang.GetSwitchX(CLang.ESwitchX.FileSelFS24Desc)));
                Item.Selections.Add(new CItem.CSelection("32", CLang.GetSwitchX(CLang.ESwitchX.FileSelFS32Desc)));
                Item.Selections.Add(new CItem.CSelection("48", CLang.GetSwitchX(CLang.ESwitchX.FileSelFS48Desc)));
                Items.Add(Item);
            }

            Items_Init(this);
        }

        private static void Exit_SaveAndExit_MouseUp(object sender, MouseButtonEventArgs e) {
            var img = (System.Windows.Controls.Image)sender;
            var Window = (WSwitchX)img.DataContext;

            var Lines = new List<string[]>();

            if (System.IO.File.Exists(CCommon.AutoexecIniFilename)) {
                using (var rfs = new System.IO.StreamReader(CCommon.AutoexecIniFilename)) {
                    while (!rfs.EndOfStream) {
                        Lines.Add(rfs.ReadLine().Split(' '));
                    }
                }
            }

            var DeleteThisLine = "DeleteThisLine";

            for (var idx = 0; idx < Lines.Count; idx++) {
                var Line = Lines[idx];
                if (Line.Length == 0) { continue; }
                if (!Line[0].Equals("SetFunc", StringComparison.CurrentCultureIgnoreCase)) { continue; }
                Lines[idx] = new string[] { DeleteThisLine };
            }

            foreach (var Item in Window.Items) {
                var Keyword = Item.Keyword;
                var Value = GetValue(Keyword);
                var Updated = false;
                for (var idx = 0; idx < Lines.Count; idx++) {
                    var Line = Lines[idx];
                    if (Line.Length == 0) { continue; }
                    if (!Line[0].Equals(Keyword, StringComparison.CurrentCultureIgnoreCase)) { continue; }
                    if (!Item.CurrentParam.Equals(Value.Default)) {
                        Lines[idx] = new string[] { Keyword, Item.CurrentParam };
                    } else {
                        Lines[idx] = new string[] { DeleteThisLine };
                    }
                    Updated = true;
                }
                if (!Updated) {
                    if (!Item.CurrentParam.Equals(Value.Default)) {
                        Lines.Add(new string[] { Keyword, Item.CurrentParam });
                    }
                }
            }

            foreach (var Line in MainWindow.GetMenuFuncs()) {
                Lines.Add(new string[] { Line.Key, Line.Value });
            }

            using (var wfs = new System.IO.StreamWriter(CCommon.AutoexecIniFilename)) {
                foreach (var Line in Lines) {
                    if ((Line.Length == 1) && (Line[0].Equals(DeleteThisLine))) { continue; }
                    wfs.WriteLine(string.Join(' ', Line));
                }
            }

            CCommon.Console.WriteLine最後から一行前に追加(CLang.GetSwitchX(CLang.ESwitchX.Exit_SaveAndExit_Result));
            Window.Close();
        }
        private static void Exit_ExitOnly_MouseUp(object sender, MouseButtonEventArgs e) {
            var img = (System.Windows.Controls.Image)sender;
            var Window = (WSwitchX)img.DataContext;
            CCommon.Console.WriteLine最後から一行前に追加(CLang.GetSwitchX(CLang.ESwitchX.Exit_ExitOnly_Result));
            Window.Close();
        }
    }
}