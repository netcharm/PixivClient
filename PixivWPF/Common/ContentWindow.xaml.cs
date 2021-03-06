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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace PixivWPF.Common
{
    /// <summary>
    /// ImageViewerWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ContentWindow : MetroWindow
    {
        private Queue<WindowState> LastWindowStates { get; set; } = new Queue<WindowState>();
        public void RestoreWindowState()
        {
            if (LastWindowStates is Queue<WindowState> && LastWindowStates.Count > 0)
                WindowState = LastWindowStates.Dequeue();
        }

        public void SetDropBoxState(bool state)
        {
            CommandDropbox.IsChecked = state;
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

        public bool InSearching
        {
            get { return (SearchBox.IsKeyboardFocusWithin); }
            set
            {
                if (SearchBox.IsKeyboardFocusWithin && !value)
                {
                    SearchBox.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                    Keyboard.ClearFocus();
                    if (Content is Page) (Content as Page).Focus();
                }
                else if (!SearchBox.IsKeyboardFocusWithin && value)
                {
                    SearchBox.Focus();
                    Keyboard.Focus(SearchBox);
                }
            }
        }

        private Storyboard PreftchingStateRing = null;
        public void SetPrefetchingProgress(double progress, string tooltip = "", TaskStatus state = TaskStatus.Created)
        {
            new Action(() =>
            {
                if (PreftchingProgress.IsHidden()) PreftchingProgress.Show();
                if (string.IsNullOrEmpty(tooltip)) PreftchingProgress.ToolTip = null;
                else PreftchingProgress.ToolTip = tooltip;

                PreftchingProgressInfo.Text = $"{Math.Floor(progress):F0}%";

                if (PreftchingStateRing == null) PreftchingStateRing = (Storyboard)PreftchingProgressState.FindResource("PreftchingStateRing");
                if (state == TaskStatus.Created)
                {
                    PreftchingProgressInfo.Hide();
                    PreftchingProgressState.Hide();
                }
                else if (state == TaskStatus.WaitingToRun)
                {
                    PreftchingProgressInfo.Show();
                    PreftchingProgressState.Show();
                    if (PreftchingStateRing != null) PreftchingStateRing.Begin();
                }
                else if (state == TaskStatus.Running)
                {
                    // do something
                }
                else
                {
                    if (PreftchingStateRing != null) PreftchingStateRing.Stop();
                    PreftchingProgressState.Hide();
                }
                if(progress < 0) PreftchingProgressInfo.Hide();
            }).Invoke(async: false);
        }

        public void JumpTo(string id)
        {
            try
            {
                if (!string.IsNullOrEmpty(id))
                {
                    var win = this.GetMainWindow();
                    if (win is MainWindow && (win as MainWindow).Contents is TilesPage)
                    {
                        (win as MainWindow).Contents.JumpTo(id);
                    }
                }
            }
            catch (Exception ex) { ex.ERROR("RecentJumpTo"); }
        }

        public ContentWindow()
        {
            InitializeComponent();
            //this.GlowBrush = null;

            Title = $"{GetType().Name}_{GetHashCode()}";
            Application.Current.UpdateContentWindows(this);

            SearchBox.ItemsSource = AutoSuggestList;

            //Topmost = true;
            ShowActivated = true;
            //Activate();

            LastWindowStates.Enqueue(WindowState.Normal);
            UpdateTheme(this);
        }

        public ContentWindow(string title)
        {
            InitializeComponent();
            //this.GlowBrush = null;

            Title = string.IsNullOrEmpty(title) ? $"{GetType().Name}_{GetHashCode()}" : title;
            Application.Current.UpdateContentWindows(this, Title);

            SearchBox.ItemsSource = AutoSuggestList;

            //Topmost = true;
            ShowActivated = true;
            //Activate();

            LastWindowStates.Enqueue(WindowState.Normal);
            UpdateTheme(this);
        }

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            //Application.Current.UpdateContentWindows(this);
            $"{Title} Loading...".INFO();

            CommandRefresh.Hide();
            CommandFilter.Hide();

            if (Content is BrowerPage)
            {
                CommandPageRead.Show();
                CommandRefreshThumb.ToolTip = "Refresh";
                PreftchingProgress.Hide();
            }
            else
                CommandPageRead.Hide();

            if (Content is IllustDetailPage ||
                Content is IllustImageViewerPage ||
                Content is SearchResultPage ||
                Content is HistoryPage)
            {
                CommandRefresh.Show();
                if (!(Content is IllustImageViewerPage))
                {
                    CommandRefreshThumb.Show();
                    PreftchingProgress.Show();
                    CommandFilter.Show();
                }
            }

            if (Content is BatchProcessPage)
            {
                LeftWindowCommands.Hide();
                RightWindowCommands.Hide();
                ShowMinButton = true;
                ShowMaxRestoreButton = false;
                ResizeMode = ResizeMode.CanMinimize;
            }

            if (Application.Current.DropBoxExists() == null)
                CommandDropbox.IsChecked = false;
            else
                CommandDropbox.IsChecked = true;

            this.AdjustWindowPos();

            Commands.SaveOpenedWindows.Execute(null);
        }

        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                if (Application.Current.GetLoginWindow() != null) { e.Cancel = true; return; }

                this.WindowState = WindowState.Minimized;
                //this.Hide();

                if (Content is DownloadManagerPage)
                    (Content as DownloadManagerPage).Pos = new Point(this.Left, this.Top);
                else if (Content is HistoryPage)
                    (Content as HistoryPage).Pos = new Point(this.Left, this.Top);

                if (Content is IllustDetailPage)
                    (Content as IllustDetailPage).Dispose();
                else if (Content is IllustImageViewerPage)
                    (Content as IllustImageViewerPage).Dispose();
                else if (Content is HistoryPage)
                    (Content as HistoryPage).Dispose();
                else if (Content is SearchResultPage)
                    (Content as SearchResultPage).Dispose();
                else if (Title.Equals("DropBox", StringComparison.CurrentCultureIgnoreCase))
                {
                    if (Content is Image) (Content as Image).Dispose();
                    false.SetDropBoxState();
                }

                if (Content is Page)
                {
                    (Content as Page).DataContext = null;
                }
            }
            catch (Exception ex) { ex.ERROR("CLOSEWIN"); }
            finally
            {
                if (!e.Cancel)
                {
                    var name = Content is Page ? (Content as Page).Name ?? (Content as Page).GetType().Name : Title;
                    Content = null;
                    Application.Current.GC(name: name, wait: true);
                }
                Application.Current.RemoveContentWindows(this);
            }
        }

        private void MetroWindow_StateChanged(object sender, EventArgs e)
        {
            LastWindowStates.Enqueue(WindowState);
            if (LastWindowStates.Count > 2) LastWindowStates.Dequeue();
        }

        private void MetroWindow_Activated(object sender, EventArgs e)
        {
            //Application.Current.ReleaseKeyboardModifiers(force: false, use_keybd_event: true);
            Application.Current.ReleaseKeyboardModifiers(force: false, use_sendkey: true);
            this.DoEvents();
        }

        private void MetroWindow_Deactivated(object sender, EventArgs e)
        {
            //Application.Current.ReleaseKeyboardModifiers(updown: true);
            this.DoEvents();
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

        private void MetroWindow_MouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = false;
            if (e.ChangedButton == MouseButton.Middle)
            {
                e.Handled = true;
                if (Title.Equals("Download Manager", StringComparison.CurrentCultureIgnoreCase))
                {
                    Hide();
                }
                else
                {
                    Close();
                }
            }
        }

        private void CommandRefresh_Click(object sender, RoutedEventArgs e)
        {
            if (sender == CommandRefresh)
                Commands.RefreshPage.Execute(Content);
            else if (sender == CommandRefreshThumb)
                Commands.RefreshPageThumb.Execute(Content);
        }

        private void CommandRecents_Click(object sender, RoutedEventArgs e)
        {
            var setting = Application.Current.LoadSetting();
            var recents = Application.Current.HistoryRecentIllusts(setting.MostRecents);
            RecentsList.Items.Clear();
            //var contents = recents.Select(item => $"ID: {item.ID}, {item.Illust.Title}").ToList();
            foreach (var item in recents)
            {
                RecentsList.Items.Add($"ID: {item.ID}, {new string(item.Illust.Title.Take(32).ToArray())} ");
            }
            RecentsPopup.IsOpen = true;
        }

        private void CommandRecentsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                var item = e.AddedItems[0];
                if (item is string)
                {
                    var contents = item as string;
                    var id = Regex.Replace(contents, @"ID:\s*?(\d+),.*?$", "$1", RegexOptions.IgnoreCase);
                    JumpTo(id);
                }
                RecentsPopup.IsOpen = false;
            }
        }

        private void CommandPageRead_Click(object sender, RoutedEventArgs e)
        {
            if (Content is BrowerPage)
            {
                (Content as BrowerPage).ReadText();
            }
        }

        private void CommandLogin_Click(object sender, RoutedEventArgs e)
        {
            Commands.Login.Execute(sender);
        }

        private void CommandLog_Click(object sender, RoutedEventArgs e)
        {
            var log_type = string.Empty;
            if (sender == CommandLog_Info) log_type = "INFO";
            else if (sender == CommandLog_Debug) log_type = "DEBUG";
            else if (sender == CommandLog_Error) log_type = "ERROR";
            else if (sender == CommandLog_Folder) log_type = "FOLDER";
            Commands.OpenLogs.Execute(log_type);
        }

        private void CommandLog_DropDownOpened(object sender, EventArgs e)
        {
            CommandLog.ContextMenu.IsOpen = true;
        }

        private void CommandDownloadManager_Click(object sender, RoutedEventArgs e)
        {
            Commands.OpenDownloadManager.Execute(true);
        }

        private void CommandDownloadManager_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            Commands.OpenDownloadManager.Execute(false);
        }

        private void CommandDropbox_Click(object sender, RoutedEventArgs e)
        {
            Commands.OpenDropBox.Execute(sender);
        }

        private void CommandDropbox_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
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

        private void SearchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            SearchBox.IsDropDownOpen = false;
        }

        private void SearchBox_TextChanged(object sender, RoutedEventArgs e)
        {
            if (SearchBox.Text.Length > 0)
            {
                auto_suggest_list.Clear();

                var content = SearchBox.Text.ParseLink().ParseID();
                if (!string.IsNullOrEmpty(content))
                {
                    content.GetSuggestList(SearchBox.Text).ToList().ForEach(t => auto_suggest_list.Add(t));
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

        private void LiveFilter_ToolTipOpening(object sender, ToolTipEventArgs e)
        {
            if (Content is IllustDetailPage)
                CommandFilter.ToolTip = $"{(Content as IllustDetailPage).GetTilesCount()}";
            else if (Content is SearchResultPage)
                CommandFilter.ToolTip = $"{(Content as SearchResultPage).GetTilesCount()}";
            else if (Content is HistoryPage)
                CommandFilter.ToolTip = $"{(Content as HistoryPage).GetTilesCount()}";
            else CommandFilter.ToolTip = $"Live Filter";
        }

        private void LiveFilter_Click(object sender, RoutedEventArgs e)
        {
            if (LiveFilterSanity_OptIncludeUnder.IsChecked)
            {
                LiveFilterSanity_NoR18.IsChecked = LiveFilterSanity_R18.IsChecked = false;
                LiveFilterSanity_NoR18.IsEnabled = LiveFilterSanity_R18.IsEnabled = false;
            }
            else
            {
                LiveFilterSanity_NoR18.IsEnabled = LiveFilterSanity_R18.IsEnabled = true;
            }
        }

        private void LiveFilterItem_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is MenuItem)) return;
            if (sender == LiveFilterFavoritedRange) return;

            #region pre-define filter menus list
            var menus_type = new List<MenuItem>() {
                LiveFilterUser, LiveFilterWork
            };
            var menus_fast = new List<MenuItem>() {
                LiveFilterFast_None,
                LiveFilterFast_Portrait, LiveFilterFast_Landscape, LiveFilterFast_Square,
                LiveFilterFast_Size1K, LiveFilterFast_Size2K, LiveFilterFast_Size4K, LiveFilterFast_Size8K,
                LiveFilterFast_SinglePage, LiveFilterFast_NotSinglePage,
                LiveFilterFast_InHistory, LiveFilterFast_NotInHistory,
                LiveFilterFast_CurrentAuthor
            };
            var menus_fav_no = new List<MenuItem>() {
                LiveFilterFavorited_00000,
                LiveFilterFavorited_00100, LiveFilterFavorited_00200, LiveFilterFavorited_00500,
                LiveFilterFavorited_01000, LiveFilterFavorited_02000, LiveFilterFavorited_05000,
                LiveFilterFavorited_10000, LiveFilterFavorited_20000, LiveFilterFavorited_50000,
            };
            var menus_fav = new List<MenuItem>() {
                LiveFilterFavorited, LiveFilterNotFavorited,
            };
            var menus_follow = new List<MenuItem>() {
                LiveFilterFollowed, LiveFilterNotFollowed,
            };
            var menus_down = new List<MenuItem>() {
                LiveFilterDownloaded, LiveFilterNotDownloaded,
            };
            var menus_sanity = new List<MenuItem>() {
                LiveFilterSanity_Any,
                LiveFilterSanity_All, LiveFilterSanity_NoAll,
                LiveFilterSanity_R12, LiveFilterSanity_NoR12,
                LiveFilterSanity_R15, LiveFilterSanity_NoR15,
                LiveFilterSanity_R17, LiveFilterSanity_NoR17,
                LiveFilterSanity_R18, LiveFilterSanity_NoR18,
            };

            var menus = new List<IEnumerable<MenuItem>>() { menus_type, menus_fav_no, menus_fast, menus_fav, menus_follow, menus_down, menus_sanity };
            #endregion

            var idx = "LiveFilter".Length;

            string filter_type = string.Empty;
            string filter_fav_no = string.Empty;
            string filter_fast = string.Empty;
            string filter_fav = string.Empty;
            string filter_follow = string.Empty;
            string filter_down = string.Empty;
            string filter_sanity = string.Empty;

            var menu = sender as MenuItem;

            LiveFilterFavoritedRange.IsChecked = false;
            LiveFilterFast.IsChecked = false;
            LiveFilterSanity.IsChecked = false;

            if (menu == LiveFilterNone)
            {
                LiveFilterNone.IsChecked = true;
                #region un-check all filter conditions
                foreach (var fmenus in menus)
                {
                    foreach (var fmenu in fmenus)
                    {
                        fmenu.IsChecked = false;
                        fmenu.IsEnabled = true;
                    }
                }
                #endregion
            }
            else
            {
                LiveFilterNone.IsChecked = false;
                #region filter by item type 
                foreach (var fmenu in menus_type)
                {
                    if (menus_type.Contains(menu))
                    {
                        if (fmenu == menu) fmenu.IsChecked = !fmenu.IsChecked;
                        else fmenu.IsChecked = false;
                    }
                    if (fmenu.IsChecked) filter_type = fmenu.Name.Substring(idx);
                }
                if (menu == LiveFilterUser && menu.IsChecked)
                {
                    foreach (var fmenu in menus_fav)
                        fmenu.IsEnabled = false;
                    foreach (var fmenu in menus_down)
                        fmenu.IsEnabled = false;
                    foreach (var fmenu in menus_sanity)
                        fmenu.IsEnabled = false;
                }
                else
                {
                    foreach (var fmenu in menus_fav)
                        fmenu.IsEnabled = true;
                    foreach (var fmenu in menus_down)
                        fmenu.IsEnabled = true;
                    foreach (var fmenu in menus_sanity)
                        fmenu.IsEnabled = true;
                }
                #endregion
                #region filter by favirited number
                LiveFilterFavoritedRange.IsChecked = false;
                foreach (var fmenu in menus_fav_no)
                {
                    if (menus_fav_no.Contains(menu))
                    {
                        if (fmenu == menu) fmenu.IsChecked = !fmenu.IsChecked;
                        else fmenu.IsChecked = false;
                    }
                    if (fmenu.IsChecked)
                    {
                        filter_fav_no = fmenu.Name.Substring(idx);
                        if (fmenu.Name.StartsWith("LiveFilterFavorited_"))
                            LiveFilterFavoritedRange.IsChecked = true;
                    }
                }
                #endregion
                #region filter by fast simple filter
                LiveFilterFast.IsChecked = false;
                foreach (var fmenu in menus_fast)
                {
                    if (menus_fast.Contains(menu))
                    {
                        if (fmenu == menu) fmenu.IsChecked = !fmenu.IsChecked;
                        else fmenu.IsChecked = false;
                    }
                    if (fmenu.IsChecked)
                    {
                        var param = string.Empty;
                        filter_fast = $"{fmenu.Name.Substring(idx)}_{param}".Trim().TrimEnd('_');
                        if (fmenu.Name.StartsWith("LiveFilterFast_"))
                            LiveFilterFast.IsChecked = true;
                    }
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
                    if (fmenu.IsChecked) filter_fav = fmenu.Name.Substring(idx);
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
                LiveFilterSanity.IsChecked = false;
                foreach (var fmenu in menus_sanity)
                {
                    if (menus_sanity.Contains(menu))
                    {
                        if (fmenu == menu) fmenu.IsChecked = !fmenu.IsChecked;
                        else fmenu.IsChecked = false;
                    }
                    if (fmenu.IsChecked)
                    {
                        filter_sanity = fmenu.Name.Substring(idx);
                        if (fmenu.Name.StartsWith("LiveFilterSanity_"))
                            LiveFilterSanity.IsChecked = true;
                    }
                }
                if (LiveFilterSanity_OptIncludeUnder.IsChecked)
                {
                    LiveFilterSanity_NoR18.IsChecked = LiveFilterSanity_R18.IsChecked = false;
                    LiveFilterSanity_NoR18.IsEnabled = LiveFilterSanity_R18.IsEnabled = false;
                }
                else
                {
                    LiveFilterSanity_NoR18.IsEnabled = LiveFilterSanity_R18.IsEnabled = true;
                }
                #endregion
            }

            var filter = new FilterParam()
            {
                Type = filter_type,
                FavoitedRange = filter_fav_no,
                Fast = filter_fast,
                Favorited = filter_fav,
                Followed = filter_follow,
                Downloaded = filter_down,
                Sanity = filter_sanity,
                SanityOption_IncludeUnder = LiveFilterSanity_OptIncludeUnder.IsChecked
            };

            if (Content is IllustDetailPage)
                (Content as IllustDetailPage).SetFilter(filter);
            else if (Content is SearchResultPage)
                (Content as SearchResultPage).SetFilter(filter);
            else if (Content is HistoryPage)
                (Content as HistoryPage).SetFilter(filter);
        }

        private void PreftchingProgress_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (Content is IllustDetailPage)
                (Content as IllustDetailPage).StopPrefetching();
            else if (Content is IllustImageViewerPage)
                (Content as IllustImageViewerPage).StopPrefetching();
            else if (Content is SearchResultPage)
                (Content as SearchResultPage).StopPrefetching();
            else if (Content is HistoryPage)
                (Content as HistoryPage).StopPrefetching();
        }
    }
}
