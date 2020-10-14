﻿using MahApps.Metro.Controls;
using PixivWPF.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Media;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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
    /// IllustImageViewerPage.xaml 的交互逻辑
    /// </summary>
    public partial class IllustImageViewerPage : Page
    {
        private Window window = null;

        public ImageItem Contents { get; set; } = null;
        private string PreviewImageUrl = string.Empty;
        private string OriginalImageUrl = string.Empty;
        private bool IsOriginal = false;

        internal void UpdateTheme()
        {
            btnViewPrevPage.Enable(btnViewPrevPage.IsEnabled, btnViewPrevPage.IsVisible);
            btnViewNextPage.Enable(btnViewNextPage.IsEnabled, btnViewNextPage.IsVisible);
        }

        internal async void UpdateDetail(ImageItem item)
        {
            try
            {
                PreviewWait.Show();
                if (item is ImageItem) Contents = item;

                if (Contents.Illust is Pixeez.Objects.Work)
                {
                    var illust = Contents.Illust as Pixeez.Objects.Work;

                    if (illust.PageCount > 1)
                    {
                        ActionViewPrevPage.Show();
                        ActionViewNextPage.Show();
                        ActionViewPageSep.Show();

                        btnViewPrevPage.Enable(Contents.Index > 0);
                        btnViewNextPage.Enable(Contents.Index < Contents.Count - 1);
                    }
                    else
                    {
                        btnViewNextPage.Hide();
                        btnViewPrevPage.Hide();
                        ActionViewPrevPage.Hide();
                        ActionViewNextPage.Hide();
                        ActionViewPageSep.Hide();
                    }

                    PreviewImageUrl = illust.GetPreviewUrl(Contents.Index, true);
                    var img = await PreviewImageUrl.LoadImageFromUrl();
                    if (img == null || img.Width < 360)
                    {
                        PreviewImageUrl = Contents.Illust.GetOriginalUrl(Contents.Index);
                        var large = await PreviewImageUrl.LoadImageFromUrl();
                        if (large != null) img = large;
                    }
                    Preview.Source = img;

                    if (Preview.Source != null)
                    {
                        var aspect = Preview.Source.AspectRatio();
                        PreviewSize.Text = $"{Preview.Source.Width:F0}x{Preview.Source.Height:F0}, {aspect.Item1:G5}:{aspect.Item2:G5}";
                        Page_SizeChanged(null, null);
                    }

                    if (window == null)
                    {
                        window = this.GetActiveWindow();
                        if (window is Window)
                        {
                            //window.KeyUp += Preview_KeyUp;
                            window.PreviewKeyUp += Page_PreviewKeyUp;
                        }
                    }
                    else
                    {
                        if (Regex.IsMatch(Contents.Subject, @" - \d+\/\d+$", RegexOptions.IgnoreCase))
                            window.Title = $"Preview ID: {Contents.ID}, {Contents.Subject}";
                        else
                            window.Title = $"Preview ID: {Contents.ID}, {Contents.Subject}";// - 1/1";
                    }
                }
            }
            catch (Exception ex)
            {
                ex.Message.ShowMessageBox("ERROR");
            }
            finally
            {
                PreviewWait.Hide();
            }
        }

        private void ChangeIllustPage(int offset)
        {
            if (Contents is ImageItem)
            {
                var illust = Contents.Illust;
                int index_p = Contents.Index;
                if (index_p < 0) index_p = 0;
                var index_n = Contents.Index+offset;
                if (index_n < 0) index_n = 0;
                if (index_n >= Contents.Count - 1) index_n = Contents.Count - 1;
                if (index_n == index_p) return;

                var i = illust.IllustItem();
                if (i is ImageItem)
                {
                    i.NextURL = Contents.NextURL;
                    i.Thumb = illust.GetThumbnailUrl(index_n);
                    i.Index = index_n;
                    i.BadgeValue = (index_n + 1).ToString();
                    i.Subject = $"{illust.Title} - {index_n + 1}/{illust.PageCount}";
                    i.DisplayTitle = false;
                    i.Tag = Contents.Tag;
                }
                UpdateDetail(i);
            }
        }

        private void SaveIllust()
        {
            if (Contents is ImageItem)
                Commands.SaveIllust.Execute(Contents);
        }

        internal void KeyAction(KeyEventArgs e)
        {
            Preview_KeyUp(Preview, e);
        }

        public IllustImageViewerPage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            window = Window.GetWindow(this);
            if (window is Window)
            {
                #region ToolButton MouseOver action
                btnViewPrevPage.MouseOverAction();
                btnViewNextPage.MouseOverAction();
                btnViewOriginalPage.MouseOverAction();
                btnViewFullSize.MouseOverAction();
                btnSavePage.MouseOverAction();
                #endregion

                var titleheight = window is MetroWindow ? (window as MetroWindow).TitleBarHeight : 0;
                window.Width += window.BorderThickness.Left + window.BorderThickness.Right;
                window.Height -= window.BorderThickness.Top + window.BorderThickness.Bottom + (32 - titleheight % 32);

                if (Contents is ImageItem) UpdateDetail(Contents);
            }
        }

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (btnViewFullSize.IsChecked.Value)
            {
                PreviewBox.Width = Preview.Source.Width;
                PreviewBox.Height = Preview.Source.Height;
            }
            else
            {
                PreviewBox.Width = PreviewScroll.ActualWidth;
                PreviewBox.Height = PreviewScroll.ActualHeight;
            }
        }

        private void Page_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            int offset = 0;
            int factor = 1;
            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {
                factor = 10;
            }
            if (e.Key == Key.Right || e.Key == Key.Down || e.Key == Key.PageDown)
                offset = 1 * factor;
            else if (e.Key == Key.Left || e.Key == Key.Up || e.Key == Key.PageUp)
                offset = -1 * factor;
            else if (e.Key == Key.Home)
                offset = -10000;
            else if (e.Key == Key.End)
                offset = 10000;
            else if (e.Key == Key.S && (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)))
            {
                SaveIllust();
                return;
            }
            ChangeIllustPage(offset);
        }

        private void Preview_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            int offset = 0;
            int factor = 1;
            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {
                factor = 10;
            }
            if (e.Delta < 0)
                offset = 1 * factor;
            else if (e.Delta > 0)
                offset = -1 * factor;
            ChangeIllustPage(offset);
        }

        private Point start;
        private Point origin;

        private void Preview_MouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = false;
            if (e.Device is MouseDevice)
            {
                if (e.ChangedButton == MouseButton.Left)
                {
                    if (e.ClickCount >= 2)
                    {
                        ActionViewOriginalPage_Click(sender, e);
                        e.Handled = true;
                    }
                    else
                    {
                        start = e.GetPosition(PreviewScroll);
                        origin = new Point(PreviewScroll.HorizontalOffset, PreviewScroll.VerticalOffset);
                    }
                }
                else if (e.ChangedButton == MouseButton.XButton1)
                {
                    if (e.ClickCount == 1)
                    {
                        Preview_MouseWheel(sender, new MouseWheelEventArgs(e.Device as MouseDevice, 0, -60));
                        e.Handled = true;
                    }
                }
                else if (e.ChangedButton == MouseButton.XButton2)
                {
                    if (e.ClickCount == 1)
                    {
                        Preview_MouseWheel(sender, new MouseWheelEventArgs(e.Device as MouseDevice, 0, 60));
                        e.Handled = true;
                    }
                }
            }
        }

        private void Preview_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (PreviewBox.Stretch == Stretch.None)
                {
                    Point factor = new Point(PreviewScroll.ExtentWidth/PreviewScroll.ActualWidth, PreviewScroll.ExtentHeight/PreviewScroll.ActualHeight);
                    Vector v = start - e.GetPosition(PreviewScroll);
                    PreviewScroll.ScrollToHorizontalOffset(origin.X + v.X * factor.X);
                    PreviewScroll.ScrollToVerticalOffset(origin.Y + v.Y * factor.Y);
                }
            }
        }

        private void Preview_KeyUp(object sender, KeyEventArgs e)
        {
            int offset = 0;
            int factor = 1;
            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {
                factor = 10;
            }
            if (e.Key == Key.Right || e.Key == Key.Down || e.Key == Key.PageDown)
                offset = 1 * factor;
            else if (e.Key == Key.Left || e.Key == Key.Up || e.Key == Key.PageUp)
                offset = -1 * factor;
            else if (e.Key == Key.Home)
                offset = -10000;
            else if (e.Key == Key.End)
                offset = 10000;
            //else if (e.Key == Key.S && (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)))
            else if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Control)
            {
                SaveIllust();
                return;
            }
            else if (e.Key == Key.O && Keyboard.Modifiers == ModifierKeys.Control)
            {
                ActionIllustInfo_Click(ActionOpenCachedWith, e);
                return;
            }
            else if (e.Key == Key.C && Keyboard.Modifiers == ModifierKeys.Control)
            {
                ActionIllustInfo_Click(ActionCopyPreview, e);
                return;
            }
            ChangeIllustPage(offset);
        }

        private void ActionIllustInfo_Click(object sender, RoutedEventArgs e)
        {
            if (Contents is ImageItem)
            {
                if (sender == ActionCopyIllustID)
                    Commands.CopyIllustIDs.Execute(Contents);
                else if (sender == ActionOpenIllust)
                    Commands.Open.Execute(Contents.Illust);
                else if (sender == ActionOpenAuthor)
                    Commands.OpenUser.Execute(Contents.User);
                else if (sender == ActionOpenCachedWith)
                {
                    Commands.ShellOpenFile.Execute(IsOriginal ? OriginalImageUrl.GetImageCachePath() : PreviewImageUrl.GetImageCachePath());
                }
                else if (sender == ActionCopyPreview)
                {
                    Commands.CopyImage.Execute(IsOriginal ? OriginalImageUrl.GetImageCachePath() : PreviewImageUrl.GetImageCachePath());
                }
                else if (sender == ActionSendIllustToInstance)
                {
                    if (Keyboard.Modifiers == ModifierKeys.None)
                        Commands.SendToOtherInstance.Execute(Contents);
                    else
                        Commands.ShellSendToOtherInstance.Execute(Contents);
                }
                else if (sender == ActionSendAuthorToInstance)
                {
                    var id = $"uid:{Contents.UserID}";
                    if (Keyboard.Modifiers == ModifierKeys.None)
                        Commands.SendToOtherInstance.Execute(id);
                    else
                        Commands.ShellSendToOtherInstance.Execute(id);
                }
            }
        }

        private void ActionViewPrevPage_Click(object sender, RoutedEventArgs e)
        {
            int offset = 0;
            int factor = 1;
            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {
                factor = 10;
            }
            offset = -1 * factor;
            ChangeIllustPage(offset);
        }

        private void ActionViewNextPage_Click(object sender, RoutedEventArgs e)
        {
            int offset = 0;
            int factor = 1;
            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {
                factor = 10;
            }
            offset = 1 * factor;
            ChangeIllustPage(offset);
        }

        private void ActionSaveIllust_Click(object sender, RoutedEventArgs e)
        {
            SaveIllust();
        }

        private void ActionViewFullSize_Click(object sender, RoutedEventArgs e)
        {
            if (btnViewFullSize.IsChecked.Value)
            {
                PreviewBox.HorizontalAlignment = HorizontalAlignment.Center;
                PreviewBox.VerticalAlignment = VerticalAlignment.Center;
                PreviewBox.Stretch = Stretch.None;
                PreviewScroll.PanningMode = PanningMode.Both;
                PreviewScroll.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
                PreviewScroll.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
                InfoBar.Margin = new Thickness(16, 16, 16, 32);
                ActionBar.Margin = new Thickness(0, 0, 16, 16);
            }
            else
            {
                PreviewBox.HorizontalAlignment = HorizontalAlignment.Stretch;
                PreviewBox.VerticalAlignment = VerticalAlignment.Stretch;
                PreviewBox.Stretch = Stretch.Uniform;
                PreviewScroll.PanningMode = PanningMode.None;
                PreviewScroll.HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;
                PreviewScroll.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
                InfoBar.Margin = new Thickness(16);
                ActionBar.Margin = new Thickness(0);
            }
            Page_SizeChanged(null, null);
        }

        private async void ActionViewOriginalPage_Click(object sender, RoutedEventArgs e)
        {
            if (Contents is ImageItem)
            {
                var preview = Preview.Source;
                try
                {
                    PreviewWait.Show();
                    if (Contents.Illust is Pixeez.Objects.Work)
                    {
                        var illust = Contents.Illust as Pixeez.Objects.Work;
                        OriginalImageUrl = illust.GetOriginalUrl(Contents.Index);
                        var original = await OriginalImageUrl.LoadImageFromUrl();
                        if (original != null)
                        {
                            Preview.Source = original;
                            IsOriginal = true;
                        }
                        else
                        {
                            Preview.Source = preview;
                            IsOriginal = false;
                        }
                        if (Preview.Source != null)
                        {
                            var aspect = Preview.Source.AspectRatio();
                            PreviewSize.Text = $"{Preview.Source.Width:F0}x{Preview.Source.Height:F0}, {aspect.Item1:G5}:{aspect.Item2:G5}";
                            Page_SizeChanged(sender, null);
                            PreviewWait.Hide();
                        }
                    }
                }
                catch (Exception)
                {
                    Preview.Source = preview;
                }
                finally
                {
                }
            }
        }
    }
}
