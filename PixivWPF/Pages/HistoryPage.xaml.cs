﻿using PixivWPF.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PixivWPF.Pages
{
    /// <summary>
    /// HistoryPage.xaml 的交互逻辑
    /// </summary>
    public partial class HistoryPage : Page
    {
        private Window window = null;
        public Point Pos { get; set; } = new Point(0, 0);

        private string result_filter = string.Empty;

        public string Contents { get; set; } = string.Empty;

        private void UpdateDownloadState(int? illustid = null, bool? exists = null)
        {
            HistoryItems.UpdateDownloadStateAsync(illustid, exists);
        }

        public async void UpdateDownloadStateAsync(int? illustid = null, bool? exists = false)
        {
            await Task.Run(() =>
            {
                UpdateDownloadState(illustid, exists);
            });
        }

        public async void UpdateLikeStateAsync(int illustid = -1, bool is_user = false)
        {
            await new Action(() => {
                UpdateLikeState(illustid, is_user);
            }).InvokeAsync();
        }

        public void UpdateLikeState(int illustid = -1, bool is_user = false)
        {
                HistoryItems.UpdateLikeState(illustid, is_user);
        }

        public void AddToHistory(PixivItem item)
        {
            if (HistoryItems.Items is ObservableCollection<PixivItem>)
            {
                Application.Current.HistoryAdd(item, HistoryItems.Items);
                UpdateDetail();
            }
        }

        public void AddToHistory(Pixeez.Objects.Work illust)
        {
            if(HistoryItems.Items is ObservableCollection<PixivItem>)
            {
                Application.Current.HistoryAdd(illust, HistoryItems.Items);
                UpdateDetail();
            }            
        }

        public void AddToHistory(Pixeez.Objects.User user)
        {
            if (HistoryItems.Items is ObservableCollection<PixivItem>)
            {
                Application.Current.HistoryAdd(user, HistoryItems.Items);
                UpdateDetail();
            }
        }

        public void AddToHistory(Pixeez.Objects.UserBase user)
        {
            if (HistoryItems.Items is ObservableCollection<PixivItem>)
            {
                Application.Current.HistoryAdd(user, HistoryItems.Items);
                UpdateDetail();
            }
        }

        private void ShowHistory(bool overwrite = false)
        {
            try
            {
                HistoryItems.Wait();
                var setting = Application.Current.LoadSetting();
                if (HistoryItems.ItemsCount <= 0 || Keyboard.Modifiers == ModifierKeys.Control)
                {
                    HistoryItems.Clear(setting.BatchClearThumbnails);
                    this.DoEvents();
                    HistoryItems.Items.AddRange(Application.Current.HistorySource());
                    this.DoEvents();
                }
                else
                {
                    UpdateLikeState();
                    this.DoEvents();
                    UpdateDownloadState();
                    this.DoEvents();
                }
                HistoryItems.UpdateTilesImage(overwrite);
            }
            catch (Exception ex)
            {
                HistoryItems.Fail();
                if (ex is NullReferenceException)
                {
                    //"No Result".ShowMessageBox("WARNING");
                    "No Result".ShowToast("WARNING[HISTORY]");
                }
                else
                {
                    ex.Message.ShowMessageBox("ERROR[HISTORY]");
                }
            }
            finally
            {
                HistoryItems.Ready();
                this.DoEvents();
            }
        }

        internal void UpdateThumb()
        {
            try
            {
                var overwrite = Keyboard.Modifiers == ModifierKeys.Alt ? true : false;
                HistoryItems.UpdateTilesImage(overwrite);
                this.DoEvents();
            }
            catch (Exception ex) { ex.ERROR(); }
        }

        internal void UpdateDetail()
        {
            ShowHistory();
            if (window != null)
            {
                window.SizeToContent = SizeToContent.WidthAndHeight;
                if (window is ContentWindow) (window as ContentWindow).AdjustWindowPos();
            }
        }

        public void SetFilter(string filter)
        {
            try
            {
                HistoryItems.Filter = filter.GetFilter();
            }
            catch (Exception ex)
            {
                ex.Message.DEBUG();
            }
        }

        public void SetFilter(FilterParam filter)
        {
            try
            {
                if (filter is FilterParam)
                {
                    HistoryItems.Filter = filter.GetFilter();
                }
                else
                {
                    HistoryItems.Filter = null;
                }
            }
            catch (Exception ex)
            {
                ex.Message.DEBUG();
            }
        }

        public dynamic GetTilesCount()
        {
            return ($"History: {HistoryItems.ItemsCount} of {HistoryItems.Items.Count}");
        }

        public void ChangeIllustLikeState()
        {
            try
            {
                Commands.ChangeIllustLikeState.Execute(HistoryItems);
            }
            catch (Exception ex) { ex.ERROR(); }
        }

        public void ChangeUserLikeState()
        {
            try
            {
                Commands.ChangeUserLikeState.Execute(HistoryItems);
            }
            catch (Exception ex) { ex.ERROR(); }
        }

        public void OpenIllust()
        {
            Commands.OpenDownloaded.Execute(HistoryItems);
        }

        public void OpenCachedImage()
        {
            try
            {
                Commands.OpenCachedImage.Execute(HistoryItems);
            }
            catch (Exception ex) { ex.ERROR(); }
        }

        public void OpenWork()
        {
            Commands.OpenWork.Execute(HistoryItems);
        }

        public void OpenUser()
        {
            Commands.OpenUser.Execute(HistoryItems);
        }

        public void SaveIllust()
        {
            try
            {
                Commands.SaveIllust.Execute(HistoryItems);
            }
            catch (Exception ex) { ex.ERROR(); }
        }

        public void SaveIllustAll()
        {
            try
            {
                Commands.SaveIllustAll.Execute(HistoryItems);
            }
            catch (Exception ex) { ex.ERROR(); }
        }

        public void FirstIllust()
        {
            HistoryItems.MoveCurrentToFirst();
            HistoryItems.ScrollIntoView(HistoryItems.SelectedItem);
        }

        public void LastIllust()
        {
            HistoryItems.MoveCurrentToLast();
            HistoryItems.ScrollIntoView(HistoryItems.SelectedItem);
        }

        public void PrevIllust()
        {
            if (this is HistoryPage && HistoryItems.ItemsCount > 0)
            {
                if (HistoryItems.IsCurrentFirst)
                    HistoryItems.MoveCurrentToLast();
                else
                    HistoryItems.MoveCurrentToPrevious();
                HistoryItems.ScrollIntoView(HistoryItems.SelectedItem);
            }
        }

        public void NextIllust()
        {
            if (this is HistoryPage && HistoryItems.ItemsCount > 0)
            {
                if (HistoryItems.IsCurrentLast)
                    HistoryItems.MoveCurrentToFirst();
                else
                    HistoryItems.MoveCurrentToNext();
                HistoryItems.ScrollIntoView(HistoryItems.SelectedItem);
            }
        }

        public void ScrollPageUp()
        {
            if (HistoryItems is ImageListGrid)
            {
                HistoryItems.PageUp();
            }
        }

        public void ScrollPageDown()
        {
            if (HistoryItems is ImageListGrid)
            {
                HistoryItems.PageDown();
            }
        }

        public void ScrollPageFirst()
        {
            if (HistoryItems is ImageListGrid)
            {
                HistoryItems.PageFirst();
            }
        }

        public void ScrollPageLast()
        {
            if (HistoryItems is ImageListGrid)
            {
                HistoryItems.PageLast();
            }
        }

        internal void Dispose()
        {
            try
            {
                HistoryItems.Clear(batch: false, force: true);
                Contents = null;
            }
            catch (Exception ex) { ex.ERROR("DisposeHistory"); }
        }

        public HistoryPage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            window = Window.GetWindow(this);

            if (window != null)
            {
                var wa = System.Windows.Forms.Screen.GetWorkingArea(new System.Drawing.Point((int)window.Left, (int)window.Top));
                window.MaxHeight = Math.Min(960, wa.Height);
            }

            try
            {
                UpdateDetail();
            }
            catch (Exception ex) { ex.ERROR(); }
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            try
            {
                Dispose();
            }
            catch (Exception ex) { ex.ERROR(); }
        }

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (window is ContentWindow) (window as ContentWindow).AdjustWindowPos();
        }

        private void Page_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.XButton1 == MouseButtonState.Pressed)
            {
                NextIllust();
                e.Handled = true;
            }
            else if (e.XButton2 == MouseButtonState.Pressed)
            {
                PrevIllust();
                e.Handled = true;
            }
        }

        #region History Result related routines
        private void ActionCopyIllustID_Click(object sender, RoutedEventArgs e)
        {
            Commands.CopyArtworkIDs.Execute(HistoryItems);
        }

        private void ActionCopyWeblink_Click(object sender, RoutedEventArgs e)
        {
            UpdateLikeState();

            if (sender.GetUid().Equals("ActionIllustWebLink", StringComparison.CurrentCultureIgnoreCase))
            {
                Commands.CopyArtworkWeblinks.Execute(HistoryItems);
            }
            else if (sender.GetUid().Equals("ActionAuthorWebLink", StringComparison.CurrentCultureIgnoreCase))
            {
                Commands.CopyArtistWeblinks.Execute(HistoryItems);
            }

            e.Handled = true;
        }

        private void ActionOpenSelected_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is MenuItem && (sender as MenuItem).Parent is ContextMenu)
                {
                    var host = ((sender as MenuItem).Parent as ContextMenu).PlacementTarget;
                    if (host == HistoryItems)
                    {
                        Commands.OpenItem.Execute(HistoryItems);
                    }
                }
            }
            catch (Exception ex) { ex.ERROR(); }
        }

        private void ActionJumpSelected_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var item = HistoryItems.GetSelected(WithSelectionOrder:true).LastOrDefault();
                if (item.IsWork())
                {
                    Application.Current.GetMainWindow().JumpTo(item.ID);
                }
            }
            catch (Exception ex) { ex.ERROR(); }
        }

        private void ActionSendToOtherInstance_Click(object sender, RoutedEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.None)
                Commands.SendToOtherInstance.Execute(HistoryItems);
            else
                Commands.ShellSendToOtherInstance.Execute(HistoryItems);
        }

        private void ActionRefresh_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem)
            {
                var overwrite = Keyboard.Modifiers == ModifierKeys.Alt ? true : false;

                var m = sender as MenuItem;
                var host = (m.Parent as ContextMenu).PlacementTarget;
                if (m.Uid.Equals("ActionRefresh", StringComparison.CurrentCultureIgnoreCase))
                {
                    if (host == HistoryItems)
                    {
                        ShowHistory(overwrite);
                    }
                }
                else if (m.Uid.Equals("ActionRefreshThumb", StringComparison.CurrentCultureIgnoreCase))
                {
                    if (host == HistoryItems)
                    {
                        HistoryItems.UpdateTilesImage(overwrite);
                    }
                }
            }
        }

        private void ActionSaveIllusts_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem)
            {
                Commands.SaveIllust.Execute(HistoryItems);
            }
        }

        private void ActionSaveIllustsAll_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem)
            {
                Commands.SaveIllustAll.Execute(HistoryItems);
            }
        }

        private void ActionOpenDownloaded_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem)
            {
                foreach (PixivItem item in HistoryItems.SelectedItems)
                {
                    Commands.OpenDownloaded.Execute(item);
                }
            }
        }

        private void ActionSpeech_Click(object sender, RoutedEventArgs e)
        {
            var text = string.Empty;
            CultureInfo culture = null;
            if (sender is MenuItem)
            {
                var mi = sender as MenuItem;
                if (mi.Parent is ContextMenu)
                {
                    var host = (mi.Parent as ContextMenu).PlacementTarget;
                    if (host == HistoryItems)
                    {
                        foreach (PixivItem item in HistoryItems.SelectedItems)
                        {
                            text += $"{item.Subject},\r\n";
                        }
                    }
                }
            }

            if (!string.IsNullOrEmpty(text)) text.Play(culture);
        }

        private void ActionBookmarkIllust_Click(object sender, RoutedEventArgs e)
        {
            string uid = (sender as dynamic).Uid;
            try
            {
                if (uid.Equals("ActionLikeIllust", StringComparison.CurrentCultureIgnoreCase) ||
                    uid.Equals("ActionLikeIllustPrivate", StringComparison.CurrentCultureIgnoreCase) ||
                    uid.Equals("ActionUnLikeIllust", StringComparison.CurrentCultureIgnoreCase))
                {
                    IList<PixivItem> items = new List<PixivItem>();
                    var host = ((sender as MenuItem).Parent as ContextMenu).PlacementTarget;
                    if (host == HistoryItems) items = HistoryItems.GetSelectedIllusts();
                    try
                    {
                        if (uid.Equals("ActionLikeIllust", StringComparison.CurrentCultureIgnoreCase))
                        {
                            items.LikeIllust();
                        }
                        else if (uid.Equals("ActionLikeIllustPrivate", StringComparison.CurrentCultureIgnoreCase))
                        {
                            items.LikeIllust(false);
                        }
                        else if (uid.Equals("ActionUnLikeIllust", StringComparison.CurrentCultureIgnoreCase))
                        {
                            items.UnLikeIllust();
                        }
                    }
                    catch (Exception ex) { ex.ERROR(); }
                }
            }
            catch (Exception ex) { ex.ERROR(); }
        }

        private void ActionFollowAuthor_Click(object sender, RoutedEventArgs e)
        {
            string uid = (sender as dynamic).Uid;
            try
            {
                if (uid.Equals("ActionLikeUser", StringComparison.CurrentCultureIgnoreCase) ||
                    uid.Equals("ActionLikeUserPrivate", StringComparison.CurrentCultureIgnoreCase) ||
                    uid.Equals("ActionUnLikeUser", StringComparison.CurrentCultureIgnoreCase))
                {
                    IList<PixivItem> items = new List<PixivItem>();
                    var host = ((sender as MenuItem).Parent as ContextMenu).PlacementTarget;
                    if (host == HistoryItems) items = HistoryItems.GetSelected();
                    try
                    {
                        if (uid.Equals("ActionLikeUser", StringComparison.CurrentCultureIgnoreCase))
                        {
                            items.LikeUser();
                        }
                        else if (uid.Equals("ActionLikeUserPrivate", StringComparison.CurrentCultureIgnoreCase))
                        {
                            items.LikeUser(false);
                        }
                        else if (uid.Equals("ActionUnLikeUser", StringComparison.CurrentCultureIgnoreCase))
                        {
                            items.UnLikeUser();
                        }
                    }
                    catch (Exception ex) { ex.ERROR(); }
                }
            }
            catch (Exception ex) { ex.ERROR(); }
        }

        private void HistoryIllusts_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (e.LeftButton == MouseButtonState.Pressed) Commands.Open.Execute(HistoryItems);
            }
            catch (Exception ex) { ex.ERROR(); }
        }
        #endregion

    }
}
