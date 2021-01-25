﻿using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.IconPacks;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
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

namespace PixivWPF.Common
{
    /// <summary>
    /// ImageListGrid.xaml 的交互逻辑
    /// </summary>
    public partial class ImageListGrid : UserControl, INotifyPropertyChanged
    {
        [Description("Get or Set Columns for display Image Tile Grid")]
        [Category("Common Properties")]
        [DefaultValue(5)]
        public int Columns { get; set; }

        [Description("Get or Set Tile Width")]
        [Category("Common Properties")]
        [DefaultValue(128)]
        public int TileWidth { get; set; }

        [Description("Get or Set Tile Height")]
        [Category("Common Properties")]
        [DefaultValue(128)]
        public int TileHeight { get; set; }

        [Description("Get or Set Image Tiles Select Item Index")]
        [Category("Common Properties")]
        public int SelectedIndex
        {
            get { return PART_ImageTiles.SelectedIndex; }
            set { PART_ImageTiles.SelectedIndex = value; }
        }

        [Description("Get or Set Image Tiles Select Item")]
        [Category("Common Properties")]
        public PixivItem SelectedItem
        {
            get { return PART_ImageTiles.SelectedItem is PixivItem ? PART_ImageTiles.SelectedItem as PixivItem : null; }
            set { PART_ImageTiles.SelectedItem = value; }
        }

        [Description("Get or Set Image Tiles Select Items")]
        [Category("Common Properties")]
        public IList<PixivItem> SelectedItems
        {
            get
            {
                if (PART_ImageTiles.SelectedItems == null)
                    return null;
                else
                {
                    IList items = PART_ImageTiles.SelectedItems;
                    var collection = items.Cast<PixivItem>();
                    return (collection.ToList());
                }
            }
        }

        [Description("Get or Set Image Tiles Selection Mode")]
        [Category("Common Properties")]
        public SelectionMode SelectionMode
        {
            get { return PART_ImageTiles.SelectionMode; }
            set { PART_ImageTiles.SelectionMode = value; }
        }

        private ConcurrentDictionary<string, ProgressRingCloud> RingList = new ConcurrentDictionary<string, ProgressRingCloud>();
        private ConcurrentDictionary<string, Canvas> CanvasList = new ConcurrentDictionary<string, Canvas>();
        private ObservableCollection<PixivItem> ItemList = new ObservableCollection<PixivItem>();
        [Description("Get or Set Image Tiles List")]
        [Category("Common Properties")]
        public ObservableCollection<PixivItem> Items
        {
            get { return (ItemList); }
            set
            {
                if(ItemList is ObservableCollection<PixivItem>) ItemList.Clear();
                ItemList = value;
                PART_ImageTiles.ItemsSource = ItemList;
                RaisePropertyChanged("Items");
                NotifyPropertyChanged("Items");
            }
        }

        [Description("Get Tiles Collection")]
        [Category("Common Properties")]
        public ItemCollection Tiles
        {
            get { return (PART_ImageTiles.Items); }
        }

        public IEnumerable ItemsSource
        {
            get { return (PART_ImageTiles.ItemsSource); }
            set
            {
                PART_ImageTiles.ItemsSource = value;
                RaisePropertyChanged("ItemsSource");
                NotifyPropertyChanged("ItemsSource");
            }
        }
        public ItemCollection ItemsCollection
        {
            get { return (PART_ImageTiles.Items); }
        }

        [Description("Get or Set Image Tiles LiveFilter")]
        [Category("Common Properties")]
        public Predicate<object> Filter
        {
            get { return (PART_ImageTiles.Items is ItemCollection ? PART_ImageTiles.Items.Filter : null); }
            set { if (PART_ImageTiles.Items is ItemCollection) PART_ImageTiles.Items.Filter = value; }
        }

        private Visibility badgevisibility = Visibility.Visible;
        public Visibility BadgeVisibility
        {
            get { return badgevisibility; }
            set { badgevisibility = value; }
        }
        public bool DisplayBadge
        {
            get
            {
                if (badgevisibility == Visibility.Visible) return true;
                else return false;
            }
            set
            {
                if (value) badgevisibility = Visibility.Visible;
                else badgevisibility = Visibility.Collapsed;
            }
        }

        [Description("Get or Set Image Tiles Count after filtered/current be displayed")]
        [Category("Common Properties")]
        public int ItemsCount { get { return (PART_ImageTiles.Items != null ? PART_ImageTiles.Items.Count : 0); } }

        private Visibility titlevisibility = Visibility.Visible;
        public Visibility TitleVisibility
        {
            get { return titlevisibility; }
            set { titlevisibility = value; }
        }
        [Description("Get or Set Item Title Visibility")]
        [Category("Common Properties")]
        [DefaultValue(true)]
        public bool DisplayTitle
        {
            get
            {
                if (titlevisibility == Visibility.Visible) return true;
                else return false;
            }
            set
            {
                if (value) titlevisibility = Visibility.Visible;
                else titlevisibility = Visibility.Collapsed;
            }
        }

        #region Scroll Viewer Helper
        private ScrollViewer scrollViewer = null;
        public int CurrentPage { get { return (CurrentScrollPage()); } }
        public int CurrentScrollPage()
        {
            int result = -1;
            try
            {
                if (!(scrollViewer is ScrollViewer)) scrollViewer = PART_ImageTiles.GetVisualChild<ScrollViewer>();
                var offset = scrollViewer.VerticalOffset;
                var height = scrollViewer.ViewportHeight;
                var total = scrollViewer.ExtentHeight;
                result = (int)Math.Round(offset / height) + 1;
            }
            catch (Exception) { }
            return (result);
        }

        public int TotalPages { get { return (TotalScrollPages()); } }
        public int TotalScrollPages()
        {
            int result = -1;
            try
            {
                if (!(scrollViewer is ScrollViewer)) scrollViewer = PART_ImageTiles.GetVisualChild<ScrollViewer>();
                var offset = scrollViewer.VerticalOffset;
                var height = scrollViewer.ViewportHeight;
                var total = scrollViewer.ExtentHeight;
                result = (int)Math.Ceiling(total / height);
            }
            catch (Exception) { }
            return (result);
        }

        public void PageUp()
        {
            try
            {
                if (!(scrollViewer is ScrollViewer)) scrollViewer = PART_ImageTiles.GetVisualChild<ScrollViewer>();
                scrollViewer.UpdateLayout();
                var offset = scrollViewer.VerticalOffset;
                var height = scrollViewer.ViewportHeight;
                var total = scrollViewer.ExtentHeight;
                scrollViewer.ScrollToVerticalOffset(offset - height - 1);
                scrollViewer.UpdateLayout();
            }
            catch (Exception) { }
        }

        public void PageDown()
        {
            try
            {
                if (!(scrollViewer is ScrollViewer)) scrollViewer = PART_ImageTiles.GetVisualChild<ScrollViewer>();
                scrollViewer.UpdateLayout();
                var offset = scrollViewer.VerticalOffset;
                var height = scrollViewer.ViewportHeight;
                var total = scrollViewer.ExtentHeight;
                scrollViewer.ScrollToVerticalOffset(offset + height + 1);
                scrollViewer.UpdateLayout();
            }
            catch (Exception) { }
        }

        public void PageFirst()
        {
            try
            {
                if (!(scrollViewer is ScrollViewer)) scrollViewer = PART_ImageTiles.GetVisualChild<ScrollViewer>();
                scrollViewer.ScrollToHome();
                scrollViewer.UpdateLayout();
            }
            catch (Exception) { }
        }

        public void PageLast()
        {
            try
            {
                if (!(scrollViewer is ScrollViewer)) scrollViewer = PART_ImageTiles.GetVisualChild<ScrollViewer>();
                scrollViewer.ScrollToEnd();
                scrollViewer.UpdateLayout();
            }
            catch (Exception) { }
        }

        public bool IsCurrentBeforeFirst { get { return (PART_ImageTiles.Items != null ? PART_ImageTiles.Items.IsCurrentBeforeFirst : false); } }
        public bool IsCurrentAfterLast { get { return (PART_ImageTiles.Items != null ? PART_ImageTiles.Items.IsCurrentAfterLast : false); } }

        public bool IsCurrentFirst { get { return (PART_ImageTiles.Items != null ? PART_ImageTiles.SelectedIndex == 0 : false); } }
        public bool IsCurrentLast { get { return (PART_ImageTiles.Items != null ? PART_ImageTiles.SelectedIndex == PART_ImageTiles.Items.Count - 1 : false); } }

        public void MoveCurrentToFirst()
        {
            try
            {
                if (PART_ImageTiles is ListView)
                {
                    PART_ImageTiles.Items.MoveCurrentToFirst();
                }
            }
            catch (Exception) { }
        }

        public void MoveCurrentToPrevious()
        {
            try
            {
                if (PART_ImageTiles is ListView)
                {
                    PART_ImageTiles.Items.MoveCurrentToPrevious();
                }
            }
            catch (Exception) { }
        }

        public void MoveCurrentToNext()
        {
            try
            {
                if (PART_ImageTiles is ListView)
                {
                    PART_ImageTiles.Items.MoveCurrentToNext();
                }
            }
            catch (Exception) { }
        }

        public void MoveCurrentToLast()
        {
            try
            {
                if (PART_ImageTiles is ListView)
                {
                    PART_ImageTiles.Items.MoveCurrentToLast();
                }
            }
            catch (Exception) { }
        }

        public void ScrollIntoView(object item)
        {
            try
            {
                if (PART_ImageTiles is ListView)
                {
                    PART_ImageTiles.ScrollIntoView(item);
                }
            }
            catch (Exception) { }
        }
        #endregion

        public event SelectionChangedEventHandler SelectionChanged;
        public delegate void SelectionChangedEventHandler(object sender, SelectionChangedEventArgs e);
        private void PART_ImageTiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Handled) return;
            SelectionChanged?.Invoke(sender, e);
        }

        public new event KeyUpEventHandler KeyUp;
        public delegate void KeyUpEventHandler(object sender, KeyEventArgs e);
        private void PART_ImageTiles_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Handled) return;
            KeyUp?.Invoke(sender, e);
        }

        public new event MouseWheelEventHandler MouseWheel;
        public delegate void MouseWheelEventHandler(object sender, MouseWheelEventArgs e);
        private void PART_ImageTiles_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!e.Handled)
            {
                if (MouseWheel != null) MouseWheel?.Invoke(sender, e);
                else
                {
                    e.Handled = true;
                    var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);
                    eventArg.RoutedEvent = MouseWheelEvent;
                    eventArg.Source = this;
                    var parent = ((Control)sender).Parent as UIElement;
                    parent.RaiseEvent(eventArg);
                }
            }
        }

        public new event PreviewMouseWheelEventHandler PreviewMouseWheel;
        public delegate void PreviewMouseWheelEventHandler(object sender, MouseWheelEventArgs e);
        private void PART_ImageTiles_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!e.Handled)
            {
                if(PreviewMouseWheel!=null) PreviewMouseWheel?.Invoke(sender, e);
                else
                {
                    //e.Handled = true;
                    //if(PART_ImageTiles.Items.Count()>
                    var viewer = PART_ImageTiles.GetVisualChild<ScrollViewer>();
                    if(viewer is ScrollViewer)
                    {
                        e.Handled = viewer.ComputedVerticalScrollBarVisibility == Visibility.Visible ? false : true;
                    }

                    var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);
                    eventArg.RoutedEvent = MouseWheelEvent;
                    eventArg.Source = this;
                    var parent = ((Control)sender).Parent as UIElement;
                    parent.RaiseEvent(eventArg);
                }
            }
        }

        public new event MouseDownEventHandler MouseDown;
        public delegate void MouseDownEventHandler(object sender, MouseButtonEventArgs e);
        private void PART_ImageTiles_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.Handled) return;
            MouseDown?.Invoke(sender, e);
        }

        internal Task lastTask = null;
        internal CancellationTokenSource cancelTokenSource;

        public ImageListGrid()
        {
            InitializeComponent();

            //DataContext = this;

            cancelTokenSource = new CancellationTokenSource();

            ItemList.Clear();
            PART_ImageTiles.ItemsSource = ItemList;
        }

        [Description("Get or Set Wait Ring State")]
        [Category("Common Properties")]
        public bool IsReady
        {
            get { return (PART_ImageTilesWait.Visibility != Visibility.Visible); }
            set
            {
                if (value) Ready();
                else Wait();
            }
        }

        public void Wait()
        {
            PART_ImageTilesWait.Show();
        }

        public void Ready()
        {
            PART_ImageTilesWait.Hide();
        }

        public void Fail()
        {
            PART_ImageTilesWait.Fail();
        }

        public void Cancel()
        {
            if (cancelTokenSource is CancellationTokenSource)
            {
                cancelTokenSource.Cancel(true);
            }
        }

        public void Refresh()
        {
            PART_ImageTiles.Items.Refresh();
            //CollectionViewSource.GetDefaultView(this).Refresh();
        }

        public void Clear(bool batch = true)
        {
            if (ItemList is ObservableCollection<PixivItem> && ItemList.Count > 0)
            {
                try
                {
                    for (var i = 0; i < ItemList.Count; i++)
                    {
                        var item = ItemList[i];
                        var id = GetID(item);
                        if (CanvasList.ContainsKey(id))
                        {
                            var canvas = CanvasList[id];
                            //(canvas.Background as ImageBrush).ImageSource = null;
                            canvas.Background = null;
                            //canvas.UpdateLayout();
                            //canvas.Dispose();
                            if (!batch)
                            {
                                canvas.UpdateLayout();
                                this.DoEvents();
                            }
                        }
                        if(RingList.ContainsKey(id) && RingList[id] is ProgressRingCloud)
                        {
                            var ring = RingList[id];
                            //ring.State = TaskStatus.Canceled;
                            //ring.UpdateState();
                            ring.Hide();
                        }
                        this.DoEvents();
                    }
                    CanvasList.Clear();
                    RingList.Clear();
                    if (batch)
                    {
                        PART_ImageTiles.UpdateLayout();
                        this.DoEvents();
                    }
                    ItemList.Clear();
                }
                catch (Exception ex) { ex.Message.DEBUG(); }
                finally
                {
                    double M = 1024.0 * 1024.0;
                    var before = GC.GetTotalMemory(true);
                    GC.Collect();
                    var after = GC.GetTotalMemory(true);
                    $"Memory Usage: {before / M:F2}M => {after / M:F2}M".DEBUG();
                }
            }
            else
            {
                CanvasList.Clear();
                RingList.Clear();
                ItemList.Clear();
            }
        }

        public async void ClearAsync(bool batch = true)
        {
            if (ItemList is ObservableCollection<PixivItem> && ItemList.Count > 0)
            {
                await new Action(() =>
                {
                    Clear(batch);
                }).InvokeAsync(true);
            }
        }

        public void Filtering(string filter)
        {
            if (PART_ImageTiles.Items.CanFilter)
            {
                if (string.IsNullOrEmpty(filter))
                    PART_ImageTiles.Items.Filter = null;
                else
                    PART_ImageTiles.Items.Filter = filter.GetFilter();
            }
        }

        public void Invalidate(ScrollViewer Viewer = null)
        {
            PART_ImageTiles.InvalidateVisual();
            //PART_ImageTiles.InvalidateArrange();
            //PART_ImageTiles.InvalidateMeasure();
            //PART_ImageTiles.UpdateLayout();

            if (Viewer is ScrollViewer)
            {
                Viewer.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
                //Viewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            }
        }

        private async void RenderCanvas(Canvas canvas, ImageSource source)
        {
            if (canvas is Canvas)
            {
                await new Action(() =>
                {
                    if (canvas.Background != null) canvas.Background = null;
                    canvas.Background = new ImageBrush(source) { Stretch = Stretch.Uniform, TileMode = TileMode.None };
                    if(canvas.Background.CanFreeze) canvas.Background.Freeze();
                }).InvokeAsync(true);
            }
        }

        private string GetID(PixivItem item)
        {
            string id = item.ID;
            string idx = item.Index > 0 ? $"_{item.Index}" : string.Empty;
            if (item.IsWork()) id = $"i_{id}{idx}";
            else if (item.IsUser()) id = $"u_{id}";
            return (id);
        }

        public async void UpdateTilesImage(bool overwrite = false, int parallel = 5, SemaphoreSlim updating_semaphore = null)
        {
            Application.Current.DoEvents();

            foreach (var item in Items.Where(item => item.Source != null))
            {
                var id = GetID(item);
                if (RingList.ContainsKey(id) && item.Source != null)
                {
                    var ring = RingList[id];
                    //ring.State = TaskStatus.RanToCompletion;
                    //ring.UpdateState();
                    ring.Hide();
                }
            }
            this.DoEvents();

            var needUpdate = Items.Where(item => item.Source == null || overwrite);
            if (needUpdate.Count() > 0)
            {
                foreach (var item in ItemList)
                {
                    if (item.TileWidth != TileWidth) item.TileWidth = TileWidth;
                    if (item.TileHeight != TileHeight) item.TileHeight = TileHeight;
                }
                lastTask = await Items.UpdateTilesThumb(lastTask, overwrite, cancelTokenSource, parallel, updating_semaphore);
            }
        }

        private void TileBadge_TargetUpdated(object sender, DataTransferEventArgs e)
        {
            if (sender is Badged && e.Property != null)
            {
                var badge = sender as Badged;
                if (e.Property.Name.Equals("Tag", StringComparison.CurrentCultureIgnoreCase) ||
                    e.Property.Name.Equals("Visibility", StringComparison.CurrentCultureIgnoreCase))
                {
                    var badged = true;
                    if (badge.Tag is bool) badged = (bool)badge.Tag;
                    badge.Visibility = badged ? Visibility.Visible : Visibility.Collapsed;
                }
            }
        }

        private void TileImage_TargetUpdated(object sender, DataTransferEventArgs e)
        {
            if (e.Property == null) return;
            if (sender is ProgressRingCloud && e.Property.Name.Equals("State", StringComparison.CurrentCultureIgnoreCase))
            {
                try
                {
                    var ring = sender as ProgressRingCloud;
                    var tile = ring.Parent is Grid ? ring.Parent as Grid : null;
                    var item = tile is Grid && tile.DataContext is PixivItem ? tile.DataContext as PixivItem : null;
                    var id = GetID(item);
                    var canvas = tile is Grid ? tile.FindByName<Canvas>("PART_ThumbnailCanvas") : null;
                    if (canvas is Canvas && item is PixivItem)
                    {
                        if (ring.State == TaskStatus.RanToCompletion)
                        {
                            RenderCanvas(canvas, item.Source);
                            CanvasList[id] = canvas;
                            RingList[id] = ring;
                        }
                        else if (ring.State == TaskStatus.Canceled)
                        {
                            //tile.DataContext = null;
                            //if (CanvasList.ContainsKey(id)) CanvasList.TryRemove(id, out canvas);
                            //canvas.Background = null;
                            //canvas.UpdateLayout();
                        }
                    }
                    ring.UpdateState();
                }
                catch (Exception) { }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void PART_Thumbnail_Unloaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Image)
                {
                    var image = sender as Image;
                    image.Source = null;
                    image.UpdateLayout();
                }
                else if (sender is Canvas)
                {
                    var canvas = sender as Canvas;
                    canvas.Background = null;
                    canvas.UpdateLayout();
                }
            }
            catch (Exception) { }
        }
    }
}
