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

        public PixivItem Contents { get; set; } = null;
        private string PreviewImageUrl = string.Empty;
        private string OriginalImageUrl = string.Empty;
        private bool IsOriginal
        {
            get { return (btnViewOriginalPage.IsChecked ?? false); }
        }
        private bool IsFullSize
        {
            get { return (btnViewFullSize.IsChecked ?? false); }
        }
        public CustomImageSource PreviewImage { get; private set; } = new CustomImageSource();

        internal void UpdateTheme()
        {
            btnViewPrevPage.Enable(btnViewPrevPage.IsEnabled, btnViewPrevPage.IsVisible);
            btnViewNextPage.Enable(btnViewNextPage.IsEnabled, btnViewNextPage.IsVisible);

            btnViewOriginalPage.Enable(btnViewOriginalPage.IsEnabled, btnViewOriginalPage.IsVisible);
            btnViewFullSize.Enable(btnViewFullSize.IsEnabled, btnViewFullSize.IsVisible);
            btnOpenCache.Enable(btnOpenCache.IsEnabled, btnOpenCache.IsVisible);
            btnSavePage.Enable(btnViewNextPage.IsEnabled, btnSavePage.IsVisible);
        }

        private void ChangeIllustPage(int offset)
        {
            if (Contents.IsWork())
            {
                int factor = Keyboard.Modifiers == ModifierKeys.Shift ? 10 : 1;

                var illust = Contents.Illust;
                int index_p = Contents.Index;
                if (index_p < 0) index_p = 0;
                var index_n = Contents.Index + (offset * factor);
                if (index_n < 0) index_n = 0;
                if (index_n >= Contents.Count - 1) index_n = Contents.Count - 1;
                if (index_n == index_p) return;

                var i = illust.WorkItem();
                if (i is PixivItem)
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

        private async Task<CustomImageSource> GetPreviewImage(bool overwrite = false)
        {
            CustomImageSource img = new CustomImageSource();
            try
            {
                var setting = Application.Current.LoadSetting();

                PreviewWait.Show();

                var c_item = Contents;
                if (IsOriginal)
                {
                    var original = await OriginalImageUrl.LoadImageFromUrl(overwrite);
                    if (original.Source != null) img = original;
                }
                else
                {
                    var preview = await PreviewImageUrl.LoadImageFromUrl(overwrite);
                    if (setting.SmartPreview &&
                        (preview.Source == null ||
                         preview.Source.Width < setting.PreviewUsingLargeMinWidth ||
                         preview.Source.Height < setting.PreviewUsingLargeMinHeight))
                    {
                        var original = await OriginalImageUrl.LoadImageFromUrl();
                        if (original.Source != null) img = original;
                    }
                    else img = preview;
                }

                if (c_item.IsSameIllust(Contents))
                {
                    if (img.Source != null)
                    {
                        Preview.Dispose();
                        Preview.Source = img.Source;
                        var dpiX = DPI.Default.X;
                        var dpiY = DPI.Default.Y;
                        var width = img.Source.Width;
                        var height = img.Source.Height;
                        var aspect = Preview.Source.AspectRatio();
                        if (!setting.AutoConvertDPI && img.Source is BitmapSource)
                        {
                            var bmp = img.Source as BitmapSource;
                            width = bmp.PixelWidth;
                            height = bmp.PixelHeight;
                            dpiX = bmp.DpiX;
                            dpiY = bmp.DpiY;
                        }
                        PreviewSize.Text = $"{width:F0}x{height:F0}, ASPECT={aspect.Item1:F2}:{aspect.Item2:F2}, DPI={dpiX:F0}:{dpiY:F0}";
                        Page_SizeChanged(null, null);
                        PreviewWait.Hide();
                    }
                    else PreviewWait.Fail();
                }
            }
            catch (Exception) { }
            finally
            {
                img.Source = null;
                if (Preview.Source == null) PreviewWait.Fail();
            }
            return (img);
        }

        internal async void UpdateDetail(PixivItem item, bool overwrite = false)
        {
            try
            {
                if (item.IsWork())
                {
                    Contents = item;
                    var illust = Contents.Illust as Pixeez.Objects.Work;

                    PreviewImageUrl = illust.GetPreviewUrl(Contents.Index, true);
                    OriginalImageUrl = illust.GetOriginalUrl(Contents.Index);

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

                    PreviewImage = await GetPreviewImage(overwrite);

                    if (window == null)
                    {
                        window = this.GetActiveWindow();
                        if (window is Window) window.PreviewKeyUp += Page_PreviewKeyUp;
                    }
                    else
                    {
                        window.Title = $"Preview ID: {Contents.ID}, {Contents.Subject}";
                    }
                }
            }
            catch (Exception ex)
            {
                ex.Message.ShowMessageBox("ERROR");
            }
            finally
            {
                Preview.Focus();
            }
        }

        internal void KeyAction(KeyEventArgs e)
        {
            Page_PreviewKeyUp(Preview, e);
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
                btnOpenIllust.MouseOverAction();
                btnOpenCache.MouseOverAction();
                btnSavePage.MouseOverAction();
                #endregion

                //PreviewWait.ReloadEnabled = true;
                //PreviewWait.ReloadAction = new Action(() => {
                //    UpdateDetail(Contents, Keyboard.Modifiers == ModifierKeys.Alt || Keyboard.Modifiers == ModifierKeys.Control);
                //});

                var titleheight = window is MetroWindow ? (window as MetroWindow).TitleBarHeight : 0;
                window.Width += window.BorderThickness.Left + window.BorderThickness.Right;
                window.Height -= window.BorderThickness.Top + window.BorderThickness.Bottom + (32 - titleheight % 32);

                if (Contents is PixivItem) UpdateDetail(Contents);
            }
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            if (PreviewImage is CustomImageSource) PreviewImage.Source = null;
            Preview.Source = null;
            this.DataContext = null;
        }

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            try
            {
                if (Preview.Source is ImageSource && Preview.Source.Width > 0 && Preview.Source.Height > 0)
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
            }
            catch (Exception) { }
        }

        private void Page_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            e.Handled = false;

            int offset = 0;
            if (e.IsKey(Key.Right) || e.IsKey(Key.Down) || e.IsKey(Key.PageDown))
                offset = 1;
            else if (e.IsKey(Key.Left) || e.IsKey(Key.Up) || e.IsKey(Key.PageUp))
                offset = -1;
            else if (e.IsKey(Key.Home))
                offset = -10000;
            else if (e.IsKey(Key.End))
                offset = 10000;
            else if (e.IsKey(Key.C, ModifierKeys.Control, false))
            {
                ActionIllustInfo_Click(ActionCopyPreview, e);
                e.Handled = true;
            }
            else if (e.IsKey(Key.O, ModifierKeys.Control, false))
            {
                if (Contents.IsDownloaded)
                    Commands.OpenDownloaded.Execute(Contents);
                else
                    ActionIllustInfo_Click(ActionOpenCachedWith, e);
                e.Handled = true;
            }
            else if (e.IsKey(Key.S, ModifierKeys.Control))
            {
                if (Contents.IsWork()) Commands.SaveIllust.Execute(Contents);
                e.Handled = true;
            }
            else
            {
                Commands.KeyProcessor.Execute(new KeyValuePair<object, KeyEventArgs>(Contents, e));
                e.Handled = true;
            }

            if (!e.Handled) ChangeIllustPage(offset);
        }

        private void Preview_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            ChangeIllustPage(-Math.Sign(e.Delta));
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
                        ActionViewOriginal_Click(ActionViewOriginal, e);
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
                        ChangeIllustPage(1);
                        e.Handled = true;
                    }
                }
                else if (e.ChangedButton == MouseButton.XButton2)
                {
                    if (e.ClickCount == 1)
                    {
                        ChangeIllustPage(-1);
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

        private void ActionIllustInfo_Click(object sender, RoutedEventArgs e)
        {
            if (Contents is PixivItem)
            {
                if (sender == ActionCopyIllustID)
                    Commands.CopyArtworkIDs.Execute(Contents);
                else if (sender == ActionOpenIllust || sender == btnOpenIllust)
                    Commands.Open.Execute(Contents.Illust);
                else if (sender == ActionOpenAuthor)
                    Commands.OpenUser.Execute(Contents.User);
                else if (sender == ActionOpenCachedWith || sender == btnOpenCache)
                {
                    if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt))
                        Commands.OpenCachedImage.Execute(IsOriginal ? OriginalImageUrl.GetImageCachePath() : PreviewImageUrl.GetImageCachePath());
                    else
                        Commands.OpenCachedImage.Execute(PreviewImage);
                }
                else if (sender == ActionCopyPreview)
                {
                    if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt))
                        Commands.CopyImage.Execute(IsOriginal ? OriginalImageUrl.GetImageCachePath() : PreviewImageUrl.GetImageCachePath());
                    else
                        Commands.CopyImage.Execute(PreviewImage);
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
                else if (sender == ActionRefreshPreview || sender == PreviewWait)
                {
                    UpdateDetail(Contents, Keyboard.Modifiers == ModifierKeys.Alt || Keyboard.Modifiers == ModifierKeys.Control);
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
            if (Contents.IsWork()) Commands.SaveIllust.Execute(Contents);
        }

        private void ActionViewFullSize_Click(object sender, RoutedEventArgs e)
        {
            if (sender == ActionViewFullSize)
            {
                btnViewFullSize.IsChecked = !btnViewFullSize.IsChecked.Value;
                CommonHelper.MouseLeave(btnViewFullSize);
            }

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
            ActionViewFullSize.IsChecked = IsFullSize;
            Page_SizeChanged(null, null);
        }

        private async void ActionViewOriginal_Click(object sender, RoutedEventArgs e)
        {
            if (Contents.IsWork())
            {
                try
                {
                    if (sender == ActionViewOriginal)
                    {
                        btnViewOriginalPage.IsChecked = !btnViewOriginalPage.IsChecked.Value;
                        CommonHelper.MouseLeave(btnViewOriginalPage);
                    }
                    ActionViewOriginal.IsChecked = IsOriginal;

                    PreviewImage = await GetPreviewImage();
                }
                catch (Exception) { }
            }
        }

    }
}
