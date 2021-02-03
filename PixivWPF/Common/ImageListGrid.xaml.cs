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
        #region Tiles Layout
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
        #endregion

        #region Tiles Selection
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
        #endregion

        #region Tiles
        private ConcurrentDictionary<string, ProgressRingCloud> RingList = new ConcurrentDictionary<string, ProgressRingCloud>();
        private ConcurrentDictionary<string, Canvas> CanvasList = new ConcurrentDictionary<string, Canvas>();
        private ConcurrentDictionary<string, Image> ImageList = new ConcurrentDictionary<string, Image>();
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

        [Description("Get or Set Image Tiles Count after filtered/current be displayed")]
        [Category("Common Properties")]
        public int ItemsCount { get { return (PART_ImageTiles.Items != null ? PART_ImageTiles.Items.Count : 0); } }
        #endregion

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
            catch (Exception ex) { ex.ERROR(); }
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
            catch (Exception ex) { ex.ERROR(); }
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
                scrollViewer.ScrollToVerticalOffset(offset - height);
                scrollViewer.UpdateLayout();
            }
            catch (Exception ex) { ex.ERROR(); }
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
                scrollViewer.ScrollToVerticalOffset(offset + height);
                scrollViewer.UpdateLayout();
            }
            catch (Exception ex) { ex.ERROR(); }
        }

        public void PageFirst()
        {
            try
            {
                if (!(scrollViewer is ScrollViewer)) scrollViewer = PART_ImageTiles.GetVisualChild<ScrollViewer>();
                scrollViewer.ScrollToHome();
                scrollViewer.UpdateLayout();
            }
            catch (Exception ex) { ex.ERROR(); }
        }

        public void PageLast()
        {
            try
            {
                if (!(scrollViewer is ScrollViewer)) scrollViewer = PART_ImageTiles.GetVisualChild<ScrollViewer>();
                scrollViewer.ScrollToEnd();
                scrollViewer.UpdateLayout();
            }
            catch (Exception ex) { ex.ERROR(); }
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
            catch (Exception ex) { ex.ERROR(); }
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
            catch (Exception ex) { ex.ERROR(); }
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
            catch (Exception ex) { ex.ERROR(); }
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
            catch (Exception ex) { ex.ERROR(); }
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
            catch (Exception ex) { ex.ERROR(); }
        }
        #endregion

        #region Events Handler
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
            e.Handled = Keyboard.Modifiers == ModifierKeys.None ? e.Handled : true;
        }

        public new event MouseWheelEventHandler MouseWheel;
        public delegate void MouseWheelEventHandler(object sender, MouseWheelEventArgs e);
        private void PART_ImageTiles_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Handled) return;
            if (MouseWheel != null)
                MouseWheel?.Invoke(sender, e);
            else
            {
                try
                {
                    if (!(scrollViewer is ScrollViewer)) scrollViewer = PART_ImageTiles.GetVisualChild<ScrollViewer>();
                    e.Handled = false;// scrollViewer.ComputedVerticalScrollBarVisibility == Visibility.Visible ? true : false;

                    var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);
                    eventArg.RoutedEvent = MouseWheelEvent;
                    eventArg.Source = this;
                    RaiseEvent(eventArg);
                }
                catch (Exception ex) { ex.Message.DEBUG("TILES"); }
            }
        }

        public new event PreviewMouseWheelEventHandler PreviewMouseWheel;
        public delegate void PreviewMouseWheelEventHandler(object sender, MouseWheelEventArgs e);
        private void PART_ImageTiles_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Handled) return;
            if (PreviewMouseWheel != null)
                PreviewMouseWheel?.Invoke(sender, e);
            else
            {
                try
                {
                    if (!(scrollViewer is ScrollViewer)) scrollViewer = PART_ImageTiles.GetVisualChild<ScrollViewer>();
                    e.Handled = false;// scrollViewer.ComputedVerticalScrollBarVisibility == Visibility.Visible ? true : false;

                    var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);
                    eventArg.RoutedEvent = MouseWheelEvent;
                    eventArg.Source = this;
                    RaiseEvent(eventArg);
                }
                catch (Exception ex) { ex.ERROR("TILES"); }
            }
        }

        public new event MouseDownEventHandler MouseDown;
        public delegate void MouseDownEventHandler(object sender, MouseButtonEventArgs e);
        private void PART_ImageTiles_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.Handled) return;
            MouseDown?.Invoke(sender, e);
        }
        #endregion

        #region Properties Handler
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        internal Task lastTask = null;
        internal CancellationTokenSource cancelTokenSource;
        private SemaphoreSlim CanUpdateTiles = new SemaphoreSlim(1,1);

        #region Calc GC Memory
        public bool AutoGC { get; set; } = false;
        public bool WaitGC { get; set; } = false;
        public bool CalcSystemMemoryUsage { get; set; } = false;
        #endregion

        #region Wait State
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
        #endregion

        #region Tiles Helper
        private string GetID(PixivItem item, bool prefix = true)
        {
            string id = item.ID;
            string idx = item.Index > 0 ? $"_{item.Index}" : string.Empty;
            if (prefix)
            {
                if (item.IsWork()) id = $"i_{id}{idx}";
                else if (item.IsUser()) id = $"u_{id}";
            }
            return (id);
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

        public void Refresh()
        {
            PART_ImageTiles.Items.Refresh();
            //CollectionViewSource.GetDefaultView(this).Refresh();
        }

        private async void RenderCanvas(Canvas canvas, ImageSource source, bool batch = false)
        {
            if (canvas is Canvas)
            {
                await new Action(() =>
                {
                    if (source == null)
                    {
                        if (canvas.Background != null)
                        {
                            //canvas.Width = 1;
                            //canvas.Height = 1;
                            //canvas.Background = NullBackground;
                            canvas.Background = null;
                            if (batch) canvas.InvalidateVisual();
                            else canvas.UpdateLayout();
                            canvas.DoEvents();
                        }
                    }
                    else
                    {
                        canvas.Width = TileWidth;
                        canvas.Height = TileHeight;
                        canvas.Background = new ImageBrush(source) { Stretch = Stretch.Uniform, TileMode = TileMode.None };
                        if (canvas.Background.CanFreeze) canvas.Background.Freeze();
                    }
                }).InvokeAsync(true);
            }
        }

        private async void RenderImage(Image image, ImageSource source, bool batch = false)
        {
            if (image is Image)
            {
                await new Action(() =>
                {
                    if (source == null)
                    {
                        if (image.Source != null)
                        {
                            image.Source = null;
                            if (batch) image.InvalidateVisual();
                            else image.UpdateLayout();
                            image.DoEvents();
                        }
                    }
                    else
                    {
                        image.Source = source;
                        if (image.Source.CanFreeze) image.Source.Freeze();
                    }
                }).InvokeAsync(true);
            }
        }

        public async void Clear(bool batch = true)
        {
            if (UpdateTileTask.IsBusy) UpdateTileTask.CancelAsync();
            if (await CanUpdateTiles.WaitAsync(TimeSpan.FromSeconds(2)))
            {
                var count = ItemList is ObservableCollection<PixivItem> ? ItemList.Count : 0;
                var items = ItemList is ObservableCollection<PixivItem> ? ItemList.ToList() : new List<PixivItem>();
                try
                {
                    if (lastTask is Task) lastTask.Dispose();
                    if (count > 0)
                    {
                        for (var i = 0; i < items.Count; i++)
                        {
                            var item = items[i];
                            var id = GetID(item);

                            if (RingList.ContainsKey(id))
                            {
                                ProgressRingCloud ring = null;
                                if (RingList.TryRemove(id, out ring) && ring is ProgressRingCloud) ring.Dispose();
                            }
                            if (ImageList.ContainsKey(id))
                            {
                                Image image = null;
                                if (ImageList.TryRemove(id, out image) && image is Image) RenderImage(image, null, batch);
                            }
                            if (CanvasList.ContainsKey(id))
                            {
                                Canvas canvas = null;
                                if (CanvasList.TryRemove(id, out canvas) && canvas is Canvas) RenderCanvas(canvas, null, batch);
                            }
                            ItemList.Remove(item);
                            this.DoEvents();
                        }
                        items.Clear();
                        this.DoEvents();
                        if (batch) PART_ImageTiles.UpdateLayout();
                        else PART_ImageTiles.InvalidateVisual();
                        this.DoEvents();
                    }
                }
                catch (Exception ex) { ex.ERROR(this.Name ?? string.Empty); }
                finally
                {
                    if (CanUpdateTiles is SemaphoreSlim && CanUpdateTiles.CurrentCount <= 0) CanUpdateTiles.Release();
                    if (AutoGC && count > 0) Application.Current.GC(this.Name, WaitGC, CalcSystemMemoryUsage);
                    this.DoEvents();
                }
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

        private BackgroundWorker UpdateTileTask = new BackgroundWorker() { WorkerReportsProgress = true, WorkerSupportsCancellation = true };

        private async void UpdateTileTask_DoWork(object sender, DoWorkEventArgs e)
        {
            var overwrite = e.Argument is bool ? (bool)e.Argument : false;
            var needUpdate = ItemList.Where(item => item.Source == null || overwrite);
            var total = needUpdate.Count();
            if (total > 0)
            {
                try
                {
                    var setting = Application.Current.LoadSetting();
                    var parallel = setting.ThumbDownloadParallel;

                    if (parallel <= 0) parallel = 1;
                    else if (parallel >= total) parallel = total;

                    SemaphoreSlim tasks = new SemaphoreSlim(parallel, parallel);
                    foreach (var item in needUpdate)
                    {
                        if (await tasks.WaitAsync(-1))
                        {
                            if (UpdateTileTask.CancellationPending) { tasks.Release(); break; }
                            new Action(async () =>
                            {
                                try
                                {
                                    item.State = TaskStatus.Running;
                                    var img = await item.Thumb.LoadImageFromUrl(overwrite, size:Application.Current.GetDefaultThumbSize());
                                    if (UpdateTileTask.CancellationPending) { img.Source = null; img = null; return; }
                                    if (item.Source == null) item.Source = img.Source;
                                    if (item.Source is ImageSource)
                                        item.State = TaskStatus.RanToCompletion;
                                    else
                                        item.State = TaskStatus.Faulted;
                                    img.Source = null;
                                    img = null;
                                }
                                catch(Exception ex) { ex.ERROR("DOWNLOAD"); }
                                finally { if (tasks is SemaphoreSlim && tasks.CurrentCount <= parallel) tasks.Release(); }
                            }).Invoke(async: false);
                        }
                    }

                    //Random rnd = new Random();
                    //var opt = new ParallelOptions();
                    ////opt.TaskScheduler = TaskScheduler.Current;
                    //opt.MaxDegreeOfParallelism = parallel;
                    //
                    //var ret = Parallel.ForEach(needUpdate, opt, (item, loopstate, elementIndex) =>
                    //{
                    //    try
                    //    {
                    //        if(UpdateTileTask.CancellationPending) loopstate.Break();
                    //
                    //        if (item.Count <= 1) item.BadgeValue = string.Empty;
                    //        if (item.Source == null || overwrite)
                    //        {
                    //            var task = new Action(async ()=>
                    //            {
                    //                item.State = TaskStatus.Running;
                    //                var img = await item.Thumb.LoadImageFromUrl(overwrite, size:Application.Current.GetDefaultThumbSize());
                    //                //if(UpdateTileTask.CancellationPending) { loopstate.Break(); img.Source = null; img = null; }
                    //                if (item.Source == null) item.Source = img.Source;
                    //                if(item.Source is ImageSource)
                    //                    item.State = TaskStatus.RanToCompletion;
                    //                else
                    //                    item.State = TaskStatus.Faulted;
                    //                img.Source = null;
                    //                img = null;
                    //            }).InvokeAsync(true);
                    //            Task.WaitAll(new Task[] { task });
                    //        }
                    //    }
                    //    catch (Exception ex)
                    //    {
                    //        $"Download Thumbnail Failed:\n{ex.Message}".ERROR("DOWNLOAD");
                    //        item.State = TaskStatus.Faulted;
                    //    }
                    //    finally
                    //    {
                    //        if (item.Source == null && item.Thumb.IsCached())
                    //        {
                    //            var thumb = item.Thumb.GetImageCachePath().LoadImageFromFile(size:Application.Current.GetDefaultThumbSize());
                    //            item.Source = thumb.Source;
                    //            thumb.Source = null;
                    //            thumb = null;
                    //        }
                    //        this.DoEvents();
                    //        if(UpdateTileTask.CancellationPending) loopstate.Break();
                    //    }
                    //});
                    //if (ret.IsCompleted) { }
                }
                catch (Exception ex)
                {
                    ex.ERROR($"{this.Name ?? "ImageListGrid"}_UPDATETILES");
                }
                finally
                {
                    this.DoEvents();
                    if (CanUpdateTiles is SemaphoreSlim && CanUpdateTiles.CurrentCount <= 0) CanUpdateTiles.Release();
                }
            }
        }

        public async void UpdateTilesImage(bool overwrite = false, int parallel = 5, SemaphoreSlim updating_semaphore = null)
        {
            this.DoEvents();
            if (ItemList.Count <= 0) return;
            var setting = Application.Current.LoadSetting();
            if (UpdateTileTask.IsBusy) UpdateTileTask.CancelAsync();
            if (await CanUpdateTiles.WaitAsync(TimeSpan.FromMilliseconds(250)))
            {
                try
                {
                    if (!UpdateTileTask.IsBusy && !UpdateTileTask.CancellationPending) UpdateTileTask.RunWorkerAsync(overwrite);
                }
                catch(Exception ex) { ex.ERROR(this.Name ?? "UpdateTilesTask"); }
                finally 
                {
                    if (CanUpdateTiles is SemaphoreSlim && CanUpdateTiles.CurrentCount <= 0) CanUpdateTiles.Release();
                }
            }

            //if (await CanUpdateTiles.WaitAsync(TimeSpan.FromSeconds(2)))
            //{
            //    foreach (var item in Items.Where(item => item.Source != null))
            //    {
            //        var id = GetID(item);
            //        if (RingList.ContainsKey(id)) RingList[id].Hide();
            //    }
            //    UpdateLayout();
            //    this.DoEvents();

            //    if (lastTask is Task) lastTask.Dispose();
            //    var needUpdate = Items.Where(item => item.Source == null || overwrite);
            //    if (needUpdate.Count() > 0)
            //    {
            //        foreach (var item in ItemList)
            //        {
            //            if (item.TileWidth != TileWidth) item.TileWidth = TileWidth;
            //            if (item.TileHeight != TileHeight) item.TileHeight = TileHeight;
            //        }
            //        lastTask = await Items.UpdateTilesThumb(lastTask, overwrite, cancelTokenSource, parallel, updating_semaphore);
            //    }
            //    if (CanUpdateTiles is SemaphoreSlim && CanUpdateTiles.CurrentCount <= 0) CanUpdateTiles.Release();
            //}
        }
        #endregion

        public ImageListGrid()
        {
            InitializeComponent();

            //DataContext = this;

            cancelTokenSource = new CancellationTokenSource();

            if (UpdateTileTask is BackgroundWorker)
            {
                UpdateTileTask.WorkerReportsProgress = true;
                UpdateTileTask.DoWork += UpdateTileTask_DoWork;
            }

            ItemList.Clear();
            PART_ImageTiles.ItemsSource = ItemList;
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
                    if (item == null) item = tile is Grid && tile.Tag is PixivItem ? tile.Tag as PixivItem : null;
                    if (item is PixivItem)
                    {
                        ring.UpdateState();
                        var id = GetID(item);
                        long lid = 0;
                        if (long.TryParse(GetID(item, false), out lid) && lid > 0)
                        {
                            var canvas = tile is Grid ? tile.FindByName<Canvas>("PART_ThumbnailCanvas") : null;
                            var image = tile is Grid ? tile.FindByName<Image>("PART_Thumbnail") : null;
                            if (ring.State == TaskStatus.RanToCompletion)
                            {
                                if (canvas is Canvas)
                                {
                                    RenderCanvas(canvas, item.Source);
                                    CanvasList[id] = canvas;
                                }
                                else if (image is Image)
                                {
                                    RenderImage(image, item.Source);
                                    ImageList[id] = image;
                                }
                            }
                        }
                        RingList[id] = ring;
                    }
                }
                catch (Exception ex) { ex.ERROR("THUMBRINGSTATE"); }
            }
        }

        private void PART_Thumbnail_Unloaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Image)
                {
                    var image = sender as Image;
                    if (image.Source != null)
                    {
                        image.Source = null;
                        image.UpdateLayout();
                    }
                }
                else if (sender is Canvas)
                {
                    var canvas = sender as Canvas;
                    if (canvas.Background != null)
                    {
                        canvas.Background = null;
                        canvas.UpdateLayout();
                    }
                }
            }
            catch (Exception ex) { ex.ERROR("THUMBUNLOAD"); }
        }
    }
}
