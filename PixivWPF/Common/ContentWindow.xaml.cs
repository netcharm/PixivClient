﻿using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace PixivWPF.Common
{
    /// <summary>
    /// ImageViewerWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ContentWindow : MetroWindow
    {
        public void UpdateTheme()
        {
            foreach (Window win in Application.Current.Windows)
            {
                if (win.Content is Pages.IllustDetailPage)
                {
                    var page = win.Content as Pages.IllustDetailPage;
                    page.UpdateTheme();
                }
            }

        }

        private ObservableCollection<string> auto_suggest_list = new ObservableCollection<string>() {"a", "b" };
        public ObservableCollection<string> AutoSuggestList
        {
            get { return (auto_suggest_list); }
        }

        public ContentWindow()
        {
            InitializeComponent();

            SearchBox.ItemsSource = AutoSuggestList;

            CommandToggleTheme.ItemsSource = Common.Theme.Accents;
            CommandToggleTheme.SelectedIndex = Common.Theme.Accents.IndexOf(Common.Theme.CurrentAccent);

            //Topmost = true;
            ShowActivated = true;
            //Activate();
        }

        private void CommandToggleTheme_Click(object sender, RoutedEventArgs e)
        {
            Common.Theme.Toggle();
            this.UpdateTheme();
        }

        private void CommandToggleTheme_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CommandToggleTheme.SelectedIndex >= 0 && CommandToggleTheme.SelectedIndex < CommandToggleTheme.Items.Count)
            {
                Common.Theme.CurrentAccent = Common.Theme.Accents[CommandToggleTheme.SelectedIndex];
            }
        }

        private void CommandLogin_Click(object sender, RoutedEventArgs e)
        {
            var accesstoken = Setting.Token();
            var dlgLogin = new PixivLoginDialog() { AccessToken = accesstoken };
            var ret = dlgLogin.ShowDialog();
            accesstoken = dlgLogin.AccessToken;
            Setting.Token(accesstoken);
        }

        private void CommandSearch_Click(object sender, RoutedEventArgs e)
        {
            CommonHelper.Cmd_Search.Execute(SearchBox.Text);
        }

        private void SearchBox_TextChanged(object sender, RoutedEventArgs e)
        {
            if (SearchBox.Text.Length > 0)
            {
                auto_suggest_list.Clear();

                var content = SearchBox.Text.ParseLink().ParseID();
                if (!string.IsNullOrEmpty(content))
                {
                    content.GetSuggestList().ToList().ForEach(t => auto_suggest_list.Add(t));
                    SearchBox.Items.Refresh();
                    SearchBox.IsDropDownOpen = true;
                }

                e.Handled = true;
            }
        }

        private void SearchBox_DropDownOpened(object sender, EventArgs e)
        {
            var textBox = Keyboard.FocusedElement as TextBox;
            if (textBox != null && textBox.Text.Length == 1 && textBox.SelectionLength == 1)
            {
                textBox.SelectionLength = 0;
                textBox.SelectionStart = 1;
            }
        }

        private void SearchBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            e.Handled = true;
            var items = e.AddedItems;
            if (items.Count > 0)
            {
                var item = items[0];
                if (item is string)
                {
                    var query = (string)item;
                    CommonHelper.Cmd_Search.Execute(query);
                }
            }
        }

        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                e.Handled = true;
                CommonHelper.Cmd_Search.Execute(SearchBox.Text);
            }
        }

        private void CommandDownloadManager_Click(object sender, RoutedEventArgs e)
        {
            CommonHelper.ShowDownloadManager(true);
        }

        private void CommandToggleDropbox_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Primitives.ToggleButton)
            {
                ContentWindow box = null;
                foreach (Window win in Application.Current.Windows)
                {
                    if (win.Title.Equals("Dropbox", StringComparison.CurrentCultureIgnoreCase))
                    {
                        if (win is ContentWindow)
                        {
                            box = win as ContentWindow;
                            break;
                        }
                    }
                }

                var btn = sender as System.Windows.Controls.Primitives.ToggleButton;
                if (box == null && !btn.IsChecked.Value)
                {
                    btn.IsChecked = true;
                }
                btn.IsChecked = CommonHelper.ShowDropBox(btn.IsChecked.Value);
            }
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (this.Content is Pages.DownloadManagerPage) return;
            if (this.Tag is Pages.DownloadManagerPage) return;

            e.Handled = true;
            if (e.Key == Key.Escape) Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
        }

        private void Window_DragOver(object sender, DragEventArgs e)
        {
            var fmts = e.Data.GetFormats(true);
            if (new List<string>(fmts).Contains("Text"))
            {
                e.Effects = DragDropEffects.Link;
            }
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            var links = e.ParseDragContent();
            foreach (var link in links)
            {
                CommonHelper.Cmd_Search.Execute(link);
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if(e.ChangedButton == MouseButton.XButton1)
            {
                if (Title.Equals("DropBox", StringComparison.CurrentCultureIgnoreCase))
                {
                    //Hide();
                }
                else if (Title.Equals("Download Manager", StringComparison.CurrentCultureIgnoreCase))
                {
                    Hide();
                }
                else
                {
                    Close();
                }
                e.Handled = true;
            }
        }

    }
}
