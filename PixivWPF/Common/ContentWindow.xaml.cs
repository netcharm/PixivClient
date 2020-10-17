﻿using MahApps.Metro.Controls;
using PixivWPF.Pages;
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
        public Queue<WindowState> LastWindowStates { get; set; } = new Queue<WindowState>();

        public void SetDropBoxState(bool state)
        {
            CommandToggleDropbox.IsChecked = state;
        }

        public void UpdateTheme(MetroWindow win = null)
        {
            if (win != null)
                CommonHelper.UpdateTheme(win);
            else
                CommonHelper.UpdateTheme();
        }

        private ObservableCollection<string> auto_suggest_list = new ObservableCollection<string>();
        public ObservableCollection<string> AutoSuggestList
        {
            get { return (auto_suggest_list); }
        }

        public ContentWindow()
        {
            InitializeComponent();

            //this.GlowBrush = null;

            SearchBox.ItemsSource = AutoSuggestList;

            //Topmost = true;
            ShowActivated = true;
            //Activate();

            LastWindowStates.Enqueue(WindowState.Normal);
            UpdateTheme(this);
        }

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (Content is IllustDetailPage ||
                Content is IllustImageViewerPage ||
                Content is SearchResultPage ||
                Content is HistoryPage)
            {
                CommandPageRefresh.Show();
                if (!(Content is IllustImageViewerPage))
                {
                    CommandPageRefreshThumb.Show();
                    CommandFilter.Show();
                }
            }
            else
            {
                CommandPageRefresh.Hide();
                CommandFilter.Hide();
            }            

            if (this.DropBoxExists() == null)
                CommandToggleDropbox.IsChecked = false;
            else
                CommandToggleDropbox.IsChecked = true;

            this.AdjustWindowPos();
        }

        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                if (this.Content is DownloadManagerPage)
                {
                    (this.Content as DownloadManagerPage).Pos = new Point(this.Left, this.Top);
                }
                if (Application.Current.GetLoginWindow() != null) e.Cancel = true;
            }
            catch (Exception ex) { ex.Message.ShowMessageBox("ERROR[CLOSEWIN]"); }
        }

        private void MetroWindow_DragOver(object sender, DragEventArgs e)
        {
            var fmts = e.Data.GetFormats(true);
            if (new List<string>(fmts).Contains("Text"))
            {
                e.Effects = DragDropEffects.Link;
            }
        }

        private void MetroWindow_Drop(object sender, DragEventArgs e)
        {
            var links = e.ParseDragContent();
            foreach (var link in links)
            {
                Commands.OpenSearch.Execute(link);
            }
        }

        private void MetroWindow_KeyUp(object sender, KeyEventArgs e)
        {
            Commands.KeyProcessor.Execute(new KeyValuePair<dynamic, KeyEventArgs>(Content, e));
        }

        private void MetroWindow_MouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = false;
            if(e.ChangedButton == MouseButton.Middle)
            {
                if (Title.Equals("DropBox", StringComparison.CurrentCultureIgnoreCase))
                {
                    Hide();
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

        private void MetroWindow_StateChanged(object sender, EventArgs e)
        {
            LastWindowStates.Enqueue(this.WindowState);
            if (LastWindowStates.Count > 2) LastWindowStates.Dequeue();
        }

        private void CommandPageRefresh_Click(object sender, RoutedEventArgs e)
        {
            if(sender == CommandPageRefresh)
                Commands.RefreshPage.Execute(Content);
            else if(sender == CommandPageRefreshThumb)
                Commands.RefreshPageThumb.Execute(Content);
        }

        private void CommandLogin_Click(object sender, RoutedEventArgs e)
        {
            Commands.Login.Execute(sender);
        }

        private void CommandDownloadManager_Click(object sender, RoutedEventArgs e)
        {
            Commands.OpenDownloadManager.Execute(true);
        }

        private void CommandToggleDropbox_Click(object sender, RoutedEventArgs e)
        {
            Commands.OpenDropBox.Execute(sender);
        }

        private void CommandHistory_Click(object sender, RoutedEventArgs e)
        {
            Commands.OpenHistory.Execute(null);
        }

        private void CommandSearch_Click(object sender, RoutedEventArgs e)
        {
            Commands.OpenSearch.Execute(SearchBox.Text);
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
                    Commands.OpenSearch.Execute(query);
                }
            }
        }

        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                e.Handled = true;
                Commands.OpenSearch.Execute(SearchBox.Text);
            }
        }

        private void LiveFilter_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is MenuItem)) return;
            if (sender == LiveFilterFavoritedRange) return;

            var menus_type = new List<MenuItem>() {
                LiveFilterUser, LiveFilterWork
            };
            var menus_fav = new List<MenuItem>() {
                LiveFilterFavorited, LiveFilterNotFavorited, //LiveFilterFavoritedRange,
                LiveFilterFavorited_00100, LiveFilterFavorited_00200, LiveFilterFavorited_00500,
                LiveFilterFavorited_01000, LiveFilterFavorited_02000, LiveFilterFavorited_05000,
                LiveFilterFavorited_10000, LiveFilterFavorited_20000, LiveFilterFavorited_50000,
            };
            var menus_follow = new List<MenuItem>() {
                LiveFilterFollowed, LiveFilterNotFollowed,
            };
            var menus_down = new List<MenuItem>() {
                LiveFilterDownloaded, LiveFilterNotDownloaded,
            };
            var menus_sanity = new List<MenuItem>() {
                LiveFilterAllAge, LiveFilterR15, LiveFilterR18,
            };

            var menus = new List<IEnumerable<MenuItem>>() { menus_type, menus_fav, menus_follow, menus_down, menus_sanity };

            var idx = "LiveFilter".Length;

            string filter_type = string.Empty;
            string filter_fav = string.Empty;
            string filter_follow = string.Empty;
            string filter_down = string.Empty;
            string filter_sanity = string.Empty;

            var menu = sender as MenuItem;

            if (menu == LiveFilterNone)
            {
                LiveFilterNone.IsChecked = true;
                foreach (var fmenus in menus)
                {
                    foreach (var fmenu in fmenus)
                    {
                        fmenu.IsChecked = false;
                        fmenu.IsEnabled = true;
                    }
                }
            }
            else
            {
                LiveFilterNone.IsChecked = false;
                #region filter by item type 
                foreach (var fmenu in menus_type)
                {
                    if (menus_down.Contains(menu))
                    {
                        if (fmenu == menu) fmenu.IsChecked = !fmenu.IsChecked;
                        else fmenu.IsChecked = false;
                    }
                    if (fmenu.IsChecked) filter_type = fmenu.Name.Substring(idx);
                }
                if (menu == LiveFilterUser)
                {
                    foreach (var ffmenu in menus_fav)
                        ffmenu.IsEnabled = false;
                    foreach (var ffmenu in menus_down)
                        ffmenu.IsEnabled = false;
                    foreach (var ffmenu in menus_sanity)
                        ffmenu.IsEnabled = false;
                }
                else if (menu == LiveFilterWork)
                {
                    foreach (var ffmenu in menus_fav)
                        ffmenu.IsEnabled = true;
                    foreach (var ffmenu in menus_down)
                        ffmenu.IsEnabled = true;
                    foreach (var ffmenu in menus_sanity)
                        ffmenu.IsEnabled = true;
                }
                #endregion
                #region filter by favorited state
                foreach (var fmenu in menus_fav)
                {
                    if (menus_fav.Contains(menu))
                    {
                        if (fmenu == menu) fmenu.IsChecked = !fmenu.IsChecked;
                        else fmenu.IsChecked = false;
                    }
                    if (fmenu.IsChecked)
                    {
                        filter_fav = fmenu.Name.Substring(idx);
                        if (menu.Name.StartsWith("LiveFilterFavorited_")) LiveFilterFavoritedRange.IsChecked = true;
                        else LiveFilterFavoritedRange.IsChecked = false;
                    }
                }
                #endregion
                #region filter by followed state
                foreach (var fmenu in menus_follow)
                {
                    if (menus_follow.Contains(menu))
                    {
                        if (fmenu == menu) fmenu.IsChecked = !fmenu.IsChecked;
                        else fmenu.IsChecked = false;
                    }
                    if (fmenu.IsChecked) filter_follow = fmenu.Name.Substring(idx);
                }
                #endregion
                #region filter by downloaded state
                foreach (var fmenu in menus_down)
                {
                    if (menus_down.Contains(menu))
                    {
                        if (fmenu == menu) fmenu.IsChecked = !fmenu.IsChecked;
                        else fmenu.IsChecked = false;
                    }
                    if (fmenu.IsChecked) filter_down = fmenu.Name.Substring(idx);
                }
                #endregion
                #region filter by sanity state
                foreach (var fmenu in menus_sanity)
                {
                    if (menus_sanity.Contains(menu))
                    {
                        if (fmenu == menu) fmenu.IsChecked = !fmenu.IsChecked;
                        else fmenu.IsChecked = false;
                    }
                    if (fmenu.IsChecked) filter_sanity = fmenu.Name.Substring(idx);
                }
                #endregion
            }
            if (Content is IllustDetailPage)
                (Content as IllustDetailPage).SetFilter(filter_type, filter_fav, filter_follow, filter_down, filter_sanity);
            else if (Content is SearchResultPage)
                (Content as SearchResultPage).SetFilter(filter_type, filter_fav, filter_follow, filter_down, filter_sanity);
            else if (Content is HistoryPage)
                (Content as HistoryPage).SetFilter(filter_type, filter_fav, filter_follow, filter_down, filter_sanity);
        }

    }
}
