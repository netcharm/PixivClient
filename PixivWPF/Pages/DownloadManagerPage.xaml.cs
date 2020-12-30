﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

using PixivWPF.Common;

namespace PixivWPF.Pages
{
    public class DownloadParams
    {
        public string Url { get; set; } = string.Empty;
        public string ThumbUrl { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = default(DateTime);
        public bool IsSinglePage { get; set; } = false;
        public bool OverwriteExists { get; set; } = true;
    }

    /// <summary>
    /// DownloadManagerPage.xaml 的交互逻辑
    /// </summary>
    public partial class DownloadManagerPage : Page
    {
        private Window window = null;
        public Point Pos { get; set; } = new Point(0, 0);

        private Setting setting = Application.Current.LoadSetting();

        [DefaultValue(true)]
        public bool AutoStart { get; set; } = true;

        [DefaultValue(10)]
        public int SimultaneousJobs { get { return (setting.DownloadSimultaneous); } set { setting.DownloadSimultaneous = value; } }

        [DefaultValue(25)]
        public int MaxSimultaneousJobs { get { return (setting.DownloadMaxSimultaneous); } }

        private TimerCallback tcb = null;
        private Timer timer = null;
        //private bool IsIdle = true;
        private bool IsUpdating = false;

        private SemaphoreSlim CanAddItem = new SemaphoreSlim(1, 1);

        private ObservableCollection<DownloadInfo> items = new ObservableCollection<DownloadInfo>();
        public ObservableCollection<DownloadInfo> Items
        {
            get { return items; }
        }

        public void UpdateTheme()
        {
            UpdateDownloadState();
        }

        private async void UpdateDownloadState(int? illustid = null, bool? exists = null)
        {
            foreach (var item in Items.ToArray())
            {
                if (item is DownloadInfo)
                {
                    await new Action(() =>
                    {
                        item.UpdateDownloadState(illustid, exists);
                    }).InvokeAsync();
                }
            }
        }

        public async void UpdateDownloadStateAsync(int? illustid = null, bool? exists = false)
        {
            await Task.Run(() =>
            {
                UpdateDownloadState(illustid, exists);
            });
        }

        public async void UpdateLikeState(int illustid = -1, bool is_user = false)
        {
            var needUpdate = is_user ? items.Where(i => i.UserID == illustid) : items.Where(i => i.IllustID == illustid);
            foreach (var item in needUpdate.ToArray())
            {
                await new Action(() =>
                {
                    item.UpdateLikeState();
                }).InvokeAsync();
            }
        }

        public async void UpdateLikeStateAsync(int illustid = -1, bool is_user = false)
        {
            await Task.Run(() =>
            {
                UpdateLikeState(illustid, is_user);
            });
        }

        public IEnumerable<string> Unfinished()
        {
            List<string> result = new List<string>();

            var unfinished = items.Where(i => i.State != DownloadState.Finished);
            foreach (var item in unfinished)
            {
                result.Add($"Downloading: {item.Url.ParseID()}");
            }

            return (result);
        }

        public DownloadManagerPage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            setting = Application.Current.LoadSetting();
            if (PART_MaxJobs.Value != SimultaneousJobs) PART_MaxJobs.Value = SimultaneousJobs;
            if (PART_MaxJobs.Maximum != MaxSimultaneousJobs) PART_MaxJobs.Maximum = MaxSimultaneousJobs;
            PART_MaxJobs.ToolTip = $"Max Simultaneous Jobs: {SimultaneousJobs} / {MaxSimultaneousJobs}";

            tcb = timerCallback;
            timer = new Timer(tcb);
            timer.Change(TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(1000));

            DownloadItems.ItemsSource = items;
            window = Window.GetWindow(this);
        }

        private async void timerCallback(object stateInfo)
        {
            try
            {
                await new Action(() =>
                {
                    if (IsLoaded)
                    {
                        setting = Application.Current.LoadSetting();
                        if (PART_MaxJobs.Value != SimultaneousJobs) PART_MaxJobs.Value = SimultaneousJobs;
                        if (PART_MaxJobs.Maximum != MaxSimultaneousJobs) PART_MaxJobs.Maximum = MaxSimultaneousJobs;

                        var jobs_count = items.Where(i => i.State == DownloadState.Downloading || i.State == DownloadState.Writing).Count();
                        var pre_jobs = items.Where(i => i.State == DownloadState.Idle || i.State == DownloadState.Paused);//|| item.State == DownloadState.Failed);
                        foreach (var item in pre_jobs)
                        {
                            if (jobs_count < SimultaneousJobs)
                            {
                                if (item.AutoStart) item.IsStart = true;
                                jobs_count++;
                            }
                        }
                        UpdateStateInfo();
                    }
                }).InvokeAsync();
            }
            catch (Exception) { }
        }

        private async void UpdateStateInfo()
        {
            try
            {
                if (IsUpdating) return;
                await new Action(() =>
                {
                    try
                    {
                        IsUpdating = true;
                        var remove = items.Where(o => o.State == DownloadState.Remove );
                        foreach (var i in remove)
                        {
                            i.Thumbnail = null;
                            items.Remove(i);
                        }

                        var idle = items.Where(o => o.State == DownloadState.Idle );
                        var downloading = items.Where(o => o.State == DownloadState.Downloading);
                        var failed = items.Where(o => o.State == DownloadState.Failed );
                        var finished = items.Where(o => o.State == DownloadState.Finished );
                        var nonexists = items.Where(o => o.State == DownloadState.NonExists );

                        PART_DownloadState.Text = $"Total: {items.Count()}, Idle: {idle.Count()}, Downloading: {downloading.Count()}, Finished: {finished.Count()}, Failed: {failed.Count()}, Non-Exists: {nonexists.Count()}";
                    }
                    catch (Exception) { }
                    finally { IsUpdating = false; }
                }).InvokeAsync();
            }
            catch (Exception) { }
        }

        private bool IsExists(string url)
        {
            bool result = false;
            try
            {
                if (string.IsNullOrEmpty(url))
                    result = true;
                else
                    result = items.Where(i => i.Url.Equals(url, StringComparison.CurrentCultureIgnoreCase)).Count() > 0;
            }
            catch (Exception) { }
            return (result);
        }

        private bool IsExists(DownloadInfo item)
        {
            bool result = false;
            try
            {
                //lock (items)
                {
                    if (string.IsNullOrEmpty(item.Url))
                        result = true;
                    else
                        result = items.Where(i => i.Url.Equals(item.Url, StringComparison.CurrentCultureIgnoreCase)).Count() > 0;
                }
            }
            catch (Exception) { }
            return (result);
        }

        internal void Add(DownloadInfo item)
        {
            if (item is DownloadInfo)// && !IsExists(item))
            {
                //lock (items)
                {
                    items.Add(item);
                    DownloadItems.ScrollIntoView(item);
                }
            }
        }

        internal async void Add(string url, string thumb, DateTime dt, bool is_meta_single_page = false, bool overwrite = true)
        {
            setting = Application.Current.LoadSetting();
            if (string.IsNullOrEmpty(setting.LastFolder))
            {
                if (await CanAddItem.WaitAsync(-1))
                {
                    Application.Current.SaveTarget();
                    if (CanAddItem is SemaphoreSlim && CanAddItem.CurrentCount <= 0) CanAddItem.Release();
                }
            }

            if (await CanAddItem.WaitAsync(TimeSpan.FromSeconds(setting.DownloadHttpTimeout)))
            {
                if (!IsExists(url))
                {
                    var item = new DownloadInfo()
                    {
                        AutoStart = AutoStart,
                        SingleFile = is_meta_single_page,
                        Overwrite = overwrite,
                        ThumbnailUrl = thumb,
                        Url = url,
                        FileTime = dt
                    };
                    Add(item);
                    if (CanAddItem is SemaphoreSlim && CanAddItem.CurrentCount <= 0) CanAddItem.Release();
                }
            }
        }

        internal void Refresh()
        {
            DownloadItems.Items.Refresh();
        }

        internal void Start()
        {
            foreach (var item in items)
            {
                //this.button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                //item.PART_Download.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                //item.Start();
                //item.IsDownloading = true;
            }
        }

        private void DownloadItem_TargetUpdated(object sender, DataTransferEventArgs e)
        {
            if (e.Property != null)
            {
                if (e.Property.Name == "Tag" || e.Property.Name == "Value" || e.Property.Name == "StateChanged")
                {
                    UpdateStateInfo();
                    if (e.Source is DownloadItem)
                    {
                        (e.Source as DownloadItem).UpdateDownloadState();
                    }
                }
            }
        }

        private void PART_ChangeFolder_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.SaveTarget(string.Empty);
        }

        private async void PART_MaxJobs_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                await new Action(() =>
                {
                    if (IsLoaded)
                    {
                        setting = Application.Current.LoadSetting();
                        SimultaneousJobs = Convert.ToInt32(PART_MaxJobs.Value);
                        PART_MaxJobs.ToolTip = $"Max Simultaneous Jobs: {SimultaneousJobs} / {MaxSimultaneousJobs}";
                    }
                }).InvokeAsync();
            }
            catch (Exception) { }
        }

        private async void PART_DownloadAll_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await new Action(() =>
                {
                    if (DownloadItems.SelectedItems is IEnumerable && DownloadItems.SelectedItems.Count > 1)
                    {
                        var targets = new List<DownloadInfo>();
                        foreach (var item in DownloadItems.SelectedItems)
                        {
                            if (item is DownloadInfo)
                                targets.Add(item as DownloadInfo);
                        }
                        var needUpdate = targets.Where(item => item.State != DownloadState.Downloading && item.State != DownloadState.Finished && item.State != DownloadState.NonExists);
                        if (needUpdate.Count() > 0)
                        {
                            var opt = new ParallelOptions();
                            opt.MaxDegreeOfParallelism = (int)SimultaneousJobs;
                            var ret = Parallel.ForEach(needUpdate, opt, (item, loopstate, elementIndex) =>
                            {
                                item.IsStart = true;
                                item.State = DownloadState.Idle;
                            });
                        }
                    }
                    else
                    {
                        var needUpdate = items.Where(item => item.State != DownloadState.Downloading && item.State != DownloadState.Finished && item.State != DownloadState.NonExists);
                        if (needUpdate.Count() > 0)
                        {
                            var opt = new ParallelOptions();
                            opt.MaxDegreeOfParallelism = (int)SimultaneousJobs;
                            var ret = Parallel.ForEach(needUpdate, opt, (item, loopstate, elementIndex) =>
                            {
                                item.IsStart = true;
                                item.State = DownloadState.Idle;
                            });
                        }
                    }
                }).InvokeAsync();
            }
            catch (Exception) { }
        }

        private async void PART_RemoveAll_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await new Action(() =>
                {
                    if (DownloadItems.SelectedItems is IEnumerable && DownloadItems.SelectedItems.Count > 1)
                    {
                        var targets = new List<DownloadInfo>();
                        foreach (var item in DownloadItems.SelectedItems)
                        {
                            if (item is DownloadInfo)
                                targets.Add(item as DownloadInfo);
                        }
                        var remove = targets.Where(o => o.State != DownloadState.Downloading);
                        foreach (var i in remove) { i.State = DownloadState.Remove; }
                    }
                    else
                    {
                        var remove = items.Where(o => o.State != DownloadState.Downloading);
                        foreach (var i in remove) { i.State = DownloadState.Remove; }
                    }
                }).InvokeAsync();
            }
            catch (Exception) { }
        }

        private async void PART_CopyID_Click(object sender, RoutedEventArgs e)
        {
            await new Action(() =>
            {
                var targets = new List<string>();
                var items = DownloadItems.SelectedItems is IEnumerable && DownloadItems.SelectedItems.Count > 1 ? DownloadItems.SelectedItems : DownloadItems.Items;
                foreach (var item in items)
                {
                    if (item is DownloadInfo)
                        targets.Add((item as DownloadInfo).FileName.ParseLink().ParseID());
                }
                Commands.CopyArtworkIDs.Execute(targets);
            }).InvokeAsync(true);
        }

        private async void PART_CopyInfo_Click(object sender, RoutedEventArgs e)
        {
            await new Action(() =>
            {
                var items = DownloadItems.SelectedItems is IEnumerable && DownloadItems.SelectedItems.Count > 1 ? DownloadItems.SelectedItems : DownloadItems.Items;
                List<DownloadInfo> dis = new List<DownloadInfo>();
                foreach (var item in DownloadItems.Items)
                {
                    if (items.Contains(item))
                    {
                        dis.Add(item as DownloadInfo);
                    }
                }
                Commands.CopyDownloadInfo.Execute(dis);
            }).InvokeAsync(true);
        }
    }
}
