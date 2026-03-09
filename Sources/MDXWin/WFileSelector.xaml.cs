using Lang;
using MDXOnline004;
using Microsoft.VisualBasic.ApplicationServices;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    public partial class WFileSelector : Window {
        public WFileSelector() {
            InitializeComponent();
        }

        public class CItem {
            public enum EType { Separator, ParentFolder, Folder, File, Text };
            public EType Type;
            public int FolderID;
            public string FileMD5;
            public string Line;
            public string ToolTip=null;
            public System.Windows.Media.ImageSource ViewItem_Line_Image {
                get {
                    return CCommon.CGROM.DrawString(CCommon.FontSize_FileSel, Line);
                }
                set { throw new NotImplementedException(); }
            }
            public string ViewItem_Line_ToolTip {
                get {
                    return ToolTip;
                }
                set { throw new NotImplementedException(); }
            }
        }

        public static string LangSplit(string src) {
            var Items = src.Split('%');
            switch (Items.Length) {
                case 0: return "";
                case 1: return Items[0];
                case 2:
                    switch (MDXWin.CCommon.INI.LangMode) {
                        case CLang.EMode.JPN: return Items[0];
                        case CLang.EMode.ENG: return Items[1];
                        default: return Items[0];
                    }
                default: return Items[0];
            }
        }

        public void UpdateFileList() {
            var CurPath = CCommon.MDXOnlineClient.CurrentFolder.BaseName;

            this.Title = "File selector. " + CurPath+"                            .";

            var Items = new List<CItem>();

            if (1 <= CCommon.MDXOnlineClient.CurrentFolder.ParentFolders.Count) {
                foreach (var Folder in CCommon.MDXOnlineClient.CurrentFolder.ParentFolders) {
                    var Item = new CItem();
                    Item.Type = CItem.EType.ParentFolder;
                    Item.FolderID = Folder.ID;
                    Item.Line = CLang.GetCommand(CLang.ECommand.Folder_Parent) + " " + new CFolderTag(Folder.BaseName).GetTextFromFolderTag();
                    Items.Add(Item);
                }
            }
            {
                var Item = new CItem();
                Item.Type = CItem.EType.ParentFolder;
                Item.FolderID = CCommon.MDXOnlineClient.CurrentFolder.ID;
                Item.Line = CLang.GetCommand(CLang.ECommand.Folder_Current) + " " + new CFolderTag(CCommon.MDXOnlineClient.CurrentFolder.BaseName).GetTextFromFolderTag();
                Items.Add(Item);
            }
            {
                var Item = new CItem();
                Item.Type = CItem.EType.Separator;
                Item.Line = "";
                Items.Add(Item);
            }

            if (1 <= CCommon.MDXOnlineClient.CurrentFolder.InFolders.Count) {
                foreach (var Folder in CCommon.MDXOnlineClient.CurrentFolder.InFolders) {
                    var Item = new CItem();
                    Item.Type = CItem.EType.ParentFolder;
                    Item.FolderID = Folder.ID;
                    Item.Line = " <dir> " + new CFolderTag(Folder.DirName).GetTextFromFolderTag();
                    Items.Add(Item);
                }
                {
                    var Item = new CItem();
                    Item.Type = CItem.EType.Separator;
                    Item.Line = "";
                    Items.Add(Item);
                }
            }

            var Uses = 0L;
            if (1 <= CCommon.MDXOnlineClient.CurrentFolder.Files.Count) {
                var MaxLen = 0;
                foreach (var File in CCommon.MDXOnlineClient.CurrentFolder.Files) {
                    var len = MoonLib.CTextEncode.GetLength_半角カナを1文字として数える(System.IO.Path.GetFileNameWithoutExtension(File.Filename));
                    if (MaxLen < len) { MaxLen = len; }
                }
                foreach (var File in CCommon.MDXOnlineClient.CurrentFolder.Files) {
                    Uses += File.Size;

                    var Item = new CItem();
                    Item.Type = CItem.EType.File;
                    Item.FileMD5 = File.MD5;

                    var PlayTSStr = "ERROR";
                    if (File.PlayTS != System.TimeSpan.FromTicks(0)) {
                        PlayTSStr = File.PlayTS.TotalMinutes.ToString("F0").PadLeft(2) + ":" + File.PlayTS.Seconds.ToString("F0").PadLeft(2, '0');
                    }

                    var fn = System.IO.Path.GetFileNameWithoutExtension(File.Filename);
                    {
                        var len = MoonLib.CTextEncode.GetLength_半角カナを1文字として数える(fn);
                        if (len < MaxLen) { fn += new string(' ', MaxLen - len); }
                    }
                    fn += System.IO.Path.GetExtension(File.Filename).PadRight(4);
                    Item.Line = fn + " " + File.Size.ToString().PadLeft(6) + " " + File.DateTime.ToString("yyyy/MM/dd") + " " + PlayTSStr + " " + File.Title;
                    Item.ToolTip = File.Title;

                    Items.Add(Item);
                }
                {
                    var Item = new CItem();
                    Item.Type = CItem.EType.Separator;
                    Item.Line = "";
                    Items.Add(Item);
                }
            }

            {
                var Item = new CItem();
                Item.Type = CItem.EType.Text;
                Item.Line = CCommon.MDXOnlineClient.CurrentFolder.Files.Count.ToString().PadLeft(6) + " " + Lang.CLang.GetCommand(Lang.CLang.ECommand.Dir_File) + " " + (Uses / 1024).ToString() + "K Byte " + Lang.CLang.GetCommand(Lang.CLang.ECommand.Dir_Used);
                Items.Add(Item);
            }

            FileList.ItemsSource = null;
            FileList.ItemsSource = Items;
            FileList.ScrollIntoView(Items[0]);

            FileList.InvalidateVisual();
        }

        public void UpdateFont() {
            var obj = FileList.ItemsSource;
            FileList.ItemsSource = null;
            FileList.ItemsSource = obj;
        }

        private void Image_MouseUp(object sender, MouseButtonEventArgs e) {
            var selitem = (CItem)FileList.SelectedItem;
            if (selitem == null) { return; }

            switch (e.ChangedButton) {
                case MouseButton.Left:
                    e.Handled = true;
                    switch (selitem.Type) {
                        case CItem.EType.Separator: break;
                        case CItem.EType.ParentFolder: CProgram.CmdLineMacro.Add("cd " + selitem.FolderID); break;
                        case CItem.EType.Folder: CProgram.CmdLineMacro.Add("cd " + selitem.FolderID); break;
                        case CItem.EType.File: CProgram.CmdLineMacro.Add(CCmdLine.GetMusicPlayCommand(selitem.FileMD5)); break;
                        case CItem.EType.Text: break;
                    }
                    break;
                case MouseButton.Right: break;
            }
        }

        public bool ReqClose = false;
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            if (!ReqClose) {
                e.Cancel = true;
                this.Visibility = Visibility.Collapsed;
            }
        }

        private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e) {
            switch (e.Key) {
                case Key.Space:
                    CProgram.CmdLineMacro.Add("PlayNext");
                    e.Handled = true;
                    break;
                case Key.Escape:
                    CProgram.CmdLineMacro.Add("MxStop");
                    e.Handled = true;
                    break;
            }
        }

        public void FileList_SetCursorToFileMD5(string FileMD5) {
            if (FileList.Items == null) { return; }

            foreach (CItem Item in FileList.Items) {
                if (Item.Type == CItem.EType.File) {
                    if (Item.FileMD5.Equals(FileMD5)) {
                        FileList.SelectedItem = Item;
                        FileList.ScrollIntoView(FileList.SelectedItem);
                    }
                }
            }

        }

        private void Image_ContextMenu_Click(object sender, RoutedEventArgs e) {
            var Item = (MenuItem)sender;
            var Target = (string)Item.DataContext;

            CProgram.CmdLineMacro.Add("FlacOut " + Target);
        }

        private void Image_ContextMenuOpening(object sender, ContextMenuEventArgs e) {
            var img = (System.Windows.Controls.Image)e.Source;
            img.ContextMenu = null;

            var selitem = (CItem)FileList.SelectedItem;
            if (selitem == null) { return; }

            switch (selitem.Type) {
                case CItem.EType.Separator: break;
                case CItem.EType.ParentFolder: break;
                case CItem.EType.Folder: break;
                case CItem.EType.File: {
                    ContextMenu CM = new ContextMenu();
                    {
                        var Item = new MenuItem();
                        Item.DataContext = selitem.FileMD5;
                        Item.Click += new RoutedEventHandler(Image_ContextMenu_Click);
                        Item.Header = CLang.GetFileSel(CLang.EFileSel.ContextMenu_FlacOut_This);
                        CM.Items.Add(Item);
                    }
                    {
                        var Item = new MenuItem();
                        Item.DataContext = "ALL";
                        Item.Click += new RoutedEventHandler(Image_ContextMenu_Click);
                        Item.Header = CLang.GetFileSel(CLang.EFileSel.ContextMenu_FlacOut_All);
                        CM.Items.Add(Item);
                    }
                    img.ContextMenu = CM;

                }
                break;
                case CItem.EType.Text: break;
            }
        }
    }
}
