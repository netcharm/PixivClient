﻿using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using PixivWPF.Pages;
using Prism.Commands;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Media;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WPFNotification.Core.Configuration;
using WPFNotification.Model;
using WPFNotification.Services;


namespace PixivWPF.Common
{
    public enum PixivPage
    {
        None,
        TrendingTags,
        WorkSet,
        Recommanded,
        Latest,
        My,
        MyWork,
        User,
        UserWork,
        Feeds,
        Favorite,
        FavoritePrivate,
        Follow,
        FollowPrivate,
        Bookmark,
        MyBookmark,
        RankingDay,
        RankingDayMale,
        RankingDayFemale,
        RankingDayR18,
        RankingDayMaleR18,
        RankingDayFemaleR18,
        RankingDayManga,
        RankingWeek,
        RankingWeekOriginal,
        RankingWeekRookie,
        RankingWeekR18,
        RankingWeekR18G,
        RankingMonth,
        RankingYear
    }

    public class SimpleCommand : ICommand
    {
        public Predicate<object> CanExecuteDelegate { get; set; }
        public Action<object> ExecuteDelegate { get; set; }

        public bool CanExecute(object parameter)
        {
            if (CanExecuteDelegate != null)
                return CanExecuteDelegate(parameter);
            return true; // if there is no can execute default to true
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public void Execute(object parameter)
        {
            ExecuteDelegate?.Invoke(parameter);
        }
    }

    public class DPI
    {
        public int X { get; }
        public int Y { get; }

        public DPI()
        {
            var dpiXProperty = typeof(SystemParameters).GetProperty("DpiX", BindingFlags.NonPublic | BindingFlags.Static);
            var dpiYProperty = typeof(SystemParameters).GetProperty("Dpi", BindingFlags.NonPublic | BindingFlags.Static);

            X = (int)dpiXProperty.GetValue(null, null);
            Y = (int)dpiYProperty.GetValue(null, null);
        }
    }

    public static class CommonHelper
    {
        private const int WIDTH_MIN = 720;
        private const int HEIGHT_MIN = 520;
        private const int HEIGHT_DEF = 900;
        private const int HEIGHT_MAX = 1008;
        private static Setting setting = Setting.Load();
        private static CacheImage cache = new CacheImage();
        public static Dictionary<long?, Pixeez.Objects.Work> cacheIllust = new Dictionary<long?, Pixeez.Objects.Work>();
        public static Dictionary<long?, Pixeez.Objects.UserBase> cacheUser = new Dictionary<long?, Pixeez.Objects.UserBase>();

        public static DateTime SelectedDate { get; set; } = DateTime.Now;
        internal static char[] trim_char = new char[] { ' ', ',', '.', '/', '\\', '\r', '\n', ':', ';' };
        internal static string[] trim_str = new string[] { Environment.NewLine };

        public static ICommand Cmd_DatePicker { get; } = new DelegateCommand<Point?>(obj => {
            if (obj.HasValue)
            {              
                var page = new DateTimePicker();
                var viewer = new MetroWindow();
                viewer.Icon = BitmapFrame.Create(new Uri("pack://application:,,,/PixivWPF;component/Resources/pixiv-icon.ico"));
                viewer.ShowMinButton = false;
                viewer.ShowMaxRestoreButton = false;
                viewer.ResizeMode = ResizeMode.NoResize;
                viewer.Width = 320;
                viewer.Height = 240;
                viewer.Top = obj.Value.Y + 4;
                viewer.Left = obj.Value.X - 64;
                viewer.Content = page;
                viewer.Title = $"Pick Date";
                viewer.KeyUp += page.Page_KeyUp;
                viewer.MouseDown += page.Page_MouseDown;
                viewer.ShowDialog();
            }
        });

        public static ICommand Cmd_SaveIllust { get; } = new DelegateCommand<object>(obj => {
            if (obj is ImageItem)
            {
                var item = obj as ImageItem;
                var illust = item.Illust;
                var dt = illust.GetDateTime();
                var is_meta_single_page = illust.PageCount==1 ? true : false;
                if (item.Tag is Pixeez.Objects.MetaPages)
                {
                    var pages = item.Tag as Pixeez.Objects.MetaPages;
                    var url = pages.GetOriginalUrl();
                    if (!string.IsNullOrEmpty(url))
                    {
                        url.SaveImage(pages.GetThumbnailUrl(), dt, is_meta_single_page);
                    }
                }
                else if (item.Tag is Pixeez.Objects.Page)
                {
                    var pages = item.Tag as Pixeez.Objects.Page;
                    var url = pages.GetOriginalUrl();
                    if (!string.IsNullOrEmpty(url))
                    {
                        url.SaveImage(pages.GetThumbnailUrl(), dt, is_meta_single_page);
                    }
                }
                else if (item.Illust is Pixeez.Objects.Work)
                {
                    var url = illust.GetOriginalUrl();
                    if (!string.IsNullOrEmpty(url))
                    {
                        url.SaveImage(illust.GetThumbnailUrl(), dt, is_meta_single_page);
                    }
                }
            }
        });

        public static ICommand Cmd_SaveIllustAll { get; } = new DelegateCommand<object>(async obj => {
            if (obj is ImageItem)
            {
                var item = obj as ImageItem;
                var illust = item.Illust;
                var dt = illust.GetDateTime();
                var is_meta_single_page = illust.PageCount==1 ? true : false;

                if (illust != null)
                {
                    if (illust is Pixeez.Objects.IllustWork)
                    {
                        var illustset = illust as Pixeez.Objects.IllustWork;
                        var total = illustset.meta_pages.Count();
                        if (is_meta_single_page)
                        {
                            var url = illust.GetOriginalUrl();
                            url.SaveImage(illust.GetThumbnailUrl(), dt, is_meta_single_page);
                        }
                        else
                        {
                            foreach (var pages in illustset.meta_pages)
                            {
                                var url = pages.GetOriginalUrl();
                                url.SaveImage(pages.GetThumbnailUrl(), dt, is_meta_single_page);
                            }
                        }
                    }
                    else if (illust is Pixeez.Objects.NormalWork)
                    {
                        if (is_meta_single_page)
                        {
                            var url = illust.GetOriginalUrl();
                            var illustset = illust as Pixeez.Objects.NormalWork;
                            url.SaveImage(illust.GetThumbnailUrl(), dt, is_meta_single_page);
                        }
                        else
                        {
                            var tokens = await ShowLogin();
                            var illusts = await tokens.GetWorksAsync(illust.Id.Value);
                            foreach (var w in illusts)
                            {
                                if (w.Metadata != null && w.Metadata.Pages != null)
                                {
                                    w.Cache();
                                    foreach (var p in w.Metadata.Pages)
                                    {
                                        var u = p.GetOriginalUrl();
                                        u.SaveImage(p.GetThumbnailUrl(), dt, is_meta_single_page);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        });

        public static ICommand Cmd_OpenDownloaded { get; } = new DelegateCommand<object>(obj => {
            if (obj is ImageItem)
            {
                var item = obj as ImageItem;
                var illust = item.Illust;

                if (item.Index >= 0)
                {
                    string fp = string.Empty;
                    item.IsDownloaded = illust.IsDownloaded(out fp, item.Index);
                    if (!string.IsNullOrEmpty(fp) && File.Exists(fp))
                    {
                        System.Diagnostics.Process.Start(fp);
                    }
                }
                else
                {
                    string fp = string.Empty;
                    item.IsDownloaded = illust.IsPartDownloaded(out fp);
                    if (!string.IsNullOrEmpty(fp) && File.Exists(fp))
                    {
                        System.Diagnostics.Process.Start(fp);
                    }
                }
            }
        });

        public static ICommand Cmd_OpenIllust { get; } = new DelegateCommand<object>(obj => {
            if (obj is ImageListGrid)
            {
                var list = obj as ImageListGrid;
                foreach (var item in list.SelectedItems)
                {
                    if (list.Name.Equals("RelativeIllusts", StringComparison.CurrentCultureIgnoreCase) ||
                        list.Name.Equals("ResultIllusts", StringComparison.CurrentCultureIgnoreCase) ||
                        list.Name.Equals("FavoriteIllusts", StringComparison.CurrentCultureIgnoreCase))
                    {
                        item.IsDownloaded = item.Illust == null ? false : item.Illust.IsPartDownloaded();

                        if (item.Illust == null && item.Tag is Pixeez.Objects.User)
                            Cmd_OpenIllust.Execute(item.Tag as Pixeez.Objects.User);
                        else
                            Cmd_OpenIllust.Execute(item);
                    }
                    else
                    {
                        foreach (Window win in Application.Current.Windows)
                        {
                            if (win.Title.Contains($"ID: {item.ID}, {item.Subject}"))
                            {
                                win.Activate();
                                return;
                            }
                        }

                        item.IsDownloaded = item.Illust == null ? false : item.Illust.IsDownloaded(item.Index);

                        var page = new IllustImageViewerPage();
                        page.UpdateDetail(item);
                        var viewer = new ContentWindow();
                        viewer.Content = page;
                        viewer.Title = $"ID: {item.ID}, {item.Subject}";
                        viewer.Width = WIDTH_MIN;
                        viewer.Height = HEIGHT_DEF;
                        viewer.MinWidth = WIDTH_MIN;
                        viewer.MinHeight = HEIGHT_MIN;
                        viewer.Show();
                    }
                }
            }
            else if (obj is ImageItem)
            {
                var item = obj as ImageItem;
                item.IsDownloaded = item.Illust == null ? false : item.Illust.IsPartDownloaded();

                switch (item.ItemType)
                {
                    case ImageItemType.Work:
                        Cmd_OpenIllust.Execute(item.Tag as Pixeez.Objects.Work);
                        break;
                    case ImageItemType.Page:
                    case ImageItemType.Pages:
                        foreach (Window win in Application.Current.Windows)
                        {
                            if (win.Title.StartsWith($"ID: {item.ID}, {item.Subject} - "))
                            {
                                win.Activate();
                                return;
                            }
                        }
                        var page = new IllustImageViewerPage();
                        page.UpdateDetail(item);
                        var viewer = new ContentWindow();
                        viewer.Content = page;
                        viewer.Title = $"ID: {item.ID}, {item.Subject} - {item.BadgeValue}/{item.Count}";
                        viewer.Width = WIDTH_MIN;
                        viewer.Height = HEIGHT_DEF;
                        viewer.MinWidth = WIDTH_MIN;
                        viewer.MinHeight = HEIGHT_MIN;
                        viewer.Show();
                        break;
                    case ImageItemType.User:
                        Cmd_OpenIllust.Execute(item.Tag as Pixeez.Objects.User);
                        break;
                    default:
                        Cmd_OpenIllust.Execute(item.Tag as Pixeez.Objects.Work);
                        break;
                }
            }
            else if (obj is Pixeez.Objects.Work)
            {
                var illust = obj as Pixeez.Objects.Work;
                foreach (Window win in Application.Current.Windows)
                {
                    if (win.Title.Contains($"ID: {illust.Id}, {illust.Title}"))
                    {
                        win.Activate();
                        return;
                    }
                }

                var item = illust.IllustItem();
                if (item is ImageItem)
                {
                    var viewer = new ContentWindow();
                    var page = new IllustDetailPage();

                    page.UpdateDetail(item);

                    viewer.Title = $"ID: {illust.Id}, {illust.Title}";
                    viewer.Width = WIDTH_MIN;
                    viewer.Height = HEIGHT_DEF;
                    viewer.MinWidth = WIDTH_MIN;
                    viewer.MinHeight = HEIGHT_MIN;
                    viewer.Content = page;
                    viewer.Show();
                }
            }
            else if (obj is Pixeez.Objects.UserBase)
            {
                dynamic user = obj;

                foreach (Window win in Application.Current.Windows)
                {
                    if (win.Title.Contains($"User: {user.Name} / {user.Id} / {user.Account}"))
                    {
                        win.Activate();
                        return;
                    }
                }

                var viewer = new ContentWindow();
                var page = new IllustDetailPage();

                page.UpdateDetail(user);
                viewer.Title = $"User: {user.Name} / {user.Id} / {user.Account}";

                viewer.Width = WIDTH_MIN;
                viewer.Height = HEIGHT_DEF;
                viewer.MinWidth = WIDTH_MIN;
                viewer.MinHeight = HEIGHT_MIN;
                viewer.Content = page;
                viewer.Show();
            }
            else if (obj is string)
            {
                Cmd_Search.Execute(obj as string);
            }
        });

        public static ICommand Cmd_CopyIllustIDs { get; } = new DelegateCommand<object>(obj =>
        {
            if (obj is ImageListGrid)
            {
                var list = obj as ImageListGrid;
                var ids = new  List<string>();
                foreach (var item in list.SelectedItems)
                {
                    if (list.Name.Equals("RelativeIllusts", StringComparison.CurrentCultureIgnoreCase) ||
                        list.Name.Equals("ResultIllusts", StringComparison.CurrentCultureIgnoreCase) ||
                        list.Name.Equals("FavoriteIllusts", StringComparison.CurrentCultureIgnoreCase))
                    {
                        ids.Add($"{item.ID}");
                    }
                }
                Clipboard.SetText(string.Join("\n", ids));
            }
            else if (obj is ImageItem)
            {
                var item = obj as ImageItem;
                if (item.Illust is Pixeez.Objects.Work)
                {
                    Clipboard.SetText(item.ID);
                }
            }
            else if(obj is string)
            {
                var id = (obj as string).ParseLink().ParseID();
                if(!string.IsNullOrEmpty(id)) Clipboard.SetText(id);
            }
        });

        public static string ParseID(this string searchContent)
        {
            var patten =  @"((UserID)|(IllustID)|(User)|(Tag)|(Caption)|(Fuzzy)|(Fuzzy Tag)):(.*?)$";
            string result = searchContent;
            if (!string.IsNullOrEmpty(result))
            {
                result = Regex.Replace(result, patten, "$9", RegexOptions.IgnoreCase).Trim().Trim(trim_char);
            }
            return (result);
        }

        public static string ParseLink(this string link)
        {
            string result = link;

            if (!string.IsNullOrEmpty(link))
            {
                if (Regex.IsMatch(result, @"(.*?illust_id=)(\d+)(.*)", RegexOptions.IgnoreCase))
                    result = Regex.Replace(result, @"(.*?illust_id=)(\d+)(.*)", "IllustID: $2", RegexOptions.IgnoreCase).Trim().Trim(trim_char);
                else if(Regex.IsMatch(result, @"(.*?\/artworks\/)(\d+)(.*)", RegexOptions.IgnoreCase))
                    result = Regex.Replace(result, @"(.*?\/artworks\/)(\d+)(.*)", "IllustID: $2", RegexOptions.IgnoreCase).Trim().Trim(trim_char);
                else if (Regex.IsMatch(result, @"(.*?\/pixiv\.navirank\.com\/id\/)(\d+)(.*)", RegexOptions.IgnoreCase))
                    result = Regex.Replace(result, @"(.*?\/id\/)(\d+)(.*)", "IllustID: $2", RegexOptions.IgnoreCase).Trim().Trim(trim_char);

                else if (Regex.IsMatch(result, @"^(.*?\?id=)(\d+)(.*)$", RegexOptions.IgnoreCase))
                    result = Regex.Replace(result, @"^(.*?\?id=)(\d+)(.*)$", "UserID: $2", RegexOptions.IgnoreCase).Trim().Trim(trim_char);
                else if (Regex.IsMatch(result, @"(.*?\/pixiv\.navirank\.com\/user\/)(\d+)(.*)", RegexOptions.IgnoreCase))
                    result = Regex.Replace(result, @"(.*?\/user\/)(\d+)(.*)", "UserID: $2", RegexOptions.IgnoreCase).Trim().Trim(trim_char);

                else if (Regex.IsMatch(result, @"^(.*?tag_full&word=)(.*)$", RegexOptions.IgnoreCase))
                {
                    result = Regex.Replace(result, @"^(.*?tag_full&word=)(.*)$", "Tag: $2", RegexOptions.IgnoreCase).Trim().Trim(trim_char);
                    result = Uri.UnescapeDataString(result);
                }
                else if (Regex.IsMatch(result, @"(.*?\/pixiv\.navirank\.com\/tag\/)(.*?)", RegexOptions.IgnoreCase))
                    result = Regex.Replace(result, @"(.*?\/tag\/)(.*?)", "Tag: $2", RegexOptions.IgnoreCase).Trim().Trim(trim_char).HtmlDecodeFix();


                else if (Regex.IsMatch(result, @"^(.*?\/img-.*?\/)(\d+)(_p\d+.*?\.((png)|(jpg)|(jpeg)|(gif)|(bmp)))$", RegexOptions.IgnoreCase))
                    result = Regex.Replace(result, @"^(.*?\/img-.*?\/)(\d+)(_p\d+.*?\.((png)|(jpg)|(jpeg)|(gif)|(bmp)))$", "IllustID: $2", RegexOptions.IgnoreCase).Trim().Trim(trim_char);

                else if (Regex.IsMatch(result, @"^(\d+)(_((p)|(ugoira))*\d+)"))
                    result = Regex.Replace(result, @"^(\d+)(_((p)|(ugoira))*\d+)", "$1", RegexOptions.IgnoreCase).Trim().Trim(trim_char);

                else if (!Regex.IsMatch(result, @"((UserID)|(User)|(IllustID)|(Tag)|(Caption)|(Fuzzy)|(Fuzzy Tag)):", RegexOptions.IgnoreCase))
                {
                    result = $"Caption: {result}";
                }
            }

            return (result);
        }

        public static IEnumerable<string> ParseDragContent(this DragEventArgs e)
        {
            List<string> links = new List<string>();

            var fmts = new List<string>(e.Data.GetFormats(true));

            if (fmts.Contains("text/html"))
            {
                using (var ms = (MemoryStream)e.Data.GetData("text/html"))
                {

                    var html = System.Text.Encoding.Unicode.GetString(ms.ToArray()).Trim();

                    var mr = new List<MatchCollection>();
                    mr.Add(Regex.Matches(html, @"href=""(http(s{0,1}):\/\/www\.pixiv\.net\/member_illust\.php\?mode=.*?illust_id=\d+.*?)"""));
                    mr.Add(Regex.Matches(html, @"href=""(http(s{0,1}):\/\/www\.pixiv\.net\/(.*?\/){0,1}artworks\/\d+.*?)"""));
                    mr.Add(Regex.Matches(html, @"((src)|(href))=""(.*?\.pximg\.net\/img-.*?\/(\d+)_p\d+.*?\.((png)|(jpg)|(jpeg)|(gif)|(bmp)))"""));
                    mr.Add(Regex.Matches(html, @"href=""(http(s{0,1}):\/\/www\.pixiv\.net\/member.*?\.php\?id=\d+).*?"""));
                    mr.Add(Regex.Matches(html, @"(http(s{0,1}):\/\/www\.pixiv\.net\/member.*?\.php\?id=\d+).*?"));

                    mr.Add(Regex.Matches(html, @"href=""(http(s{0,1}):\/\/pixiv\.navirank\.com\/id\/\d+).*?"""));
                    mr.Add(Regex.Matches(html, @"href=""(http(s{0,1}):\/\/pixiv\.navirank\.com\/user\/\d+).*?"""));
                    mr.Add(Regex.Matches(html, @"href=""(http(s{0,1}):\/\/pixiv\.navirank\.com\/tag\/.*?)"""));

                    foreach (var mi in mr)
                    {
                        if (mi.Count > 50)
                        {
                            ShowMessageBox("There are too many links, which may cause the program to crash and cancel the operation.", "WARNING");
                            continue;
                        }
                        foreach (Match m in mi)
                        {
                            var link = m.Groups[1].Value.Trim().Trim(trim_char);
                            if (!string.IsNullOrEmpty(link) && !links.Contains(link)) links.Add(link);
                        }
                    }
                }
            }
            else if (fmts.Contains("Text"))
            {
                var html = ((string)e.Data.GetData("Text")).Trim();

                var mr = new List<MatchCollection>();
                mr.Add(Regex.Matches(html, @"(http(s{0,1}):\/\/www\.pixiv\.net\/member.*?\.php\?id=\d+).*?$"));
                mr.Add(Regex.Matches(html, @"(http(s{0,1}):\/\/www\.pixiv\.net\/member.*?\.php\?.*?illust_id=\d+).*?$"));
                mr.Add(Regex.Matches(html, @"(http(s{0,1}):\/\/www\.pixiv\.net\/(.*?\/){0,1}artworks\/\d+).*?$"));
                mr.Add(Regex.Matches(html, @"(.*?\.pximg\.net\/img-.*?\/\d+_p\d+\.((png)|(jpg)|(jpeg)|(gif)|(bmp)))$"));

                mr.Add(Regex.Matches(html, @"(http(s{0,1}):\/\/pixiv\.navirank\.com\/id\/\d+).*?$"));
                mr.Add(Regex.Matches(html, @"(http(s{0,1}):\/\/pixiv\.navirank\.com\/user\/\d+).*?$"));
                mr.Add(Regex.Matches(html, @"(http(s{0,1}):\/\/pixiv\.navirank\.com\/tag\/.*?\/)$"));

                mr.Add(Regex.Matches(html, @"^(\d+)(_((p)|(ugoira))*\d+)"));

                foreach (var mi in mr)
                {
                    if (mi.Count > 50)
                    {
                        ShowMessageBox("There are too many links, which may cause the program to crash and cancel the operation.", "WARNING");
                        continue;
                    }
                    foreach (Match m in mi)
                    {
                        var link = m.Groups[1].Value.Trim().Trim(trim_char);
                        long id;
                        if (long.TryParse(link, out id))
                        {
                            links.Add($"https://www.pixiv.net/artworks/{link}");
                            links.Add($"https://www.pixiv.net/member.php?id={link}");
                        }
                        else if (!string.IsNullOrEmpty(link) && !links.Contains(link)) links.Add(link);
                    }
                }
            }
            return (links);
        }

        public static ICommand Cmd_Search { get; } = new DelegateCommand<object>(obj => {
            if (obj is string)
            {
                var content = ParseLink((string)obj);
                var id = ParseID(content);

                if (!string.IsNullOrEmpty(content))
                {
                    foreach(Window win in Application.Current.Windows)
                    {
                        if (win.Title.Contains(content) || win.Title.Contains($": {id},") || win.Title.Contains($"/ {id} /"))
                        {
                            win.Activate();
                            return;
                        }
                    }

                    var viewer = new ContentWindow();
                    viewer.Title = $"Searching {content} ...";
                    viewer.Width = WIDTH_MIN;
                    viewer.Height = HEIGHT_DEF;
                    viewer.MinWidth = WIDTH_MIN;
                    viewer.MinHeight = HEIGHT_MIN;
                    viewer.MaxHeight = HEIGHT_MAX;

                    var page = new SearchResultPage();
                    page.CurrentWindow = viewer;
                    page.UpdateDetail(content);

                    viewer.Content = page;
                    viewer.Show();
                }
            }
        });

        public static ICommand Cmd_Drop { get; } = new DelegateCommand<object>(async obj => {
            if (obj is IEnumerable)
            {
                foreach (var link in (obj as List<string>))
                {
                    await Task.Run(new Action(() => {
                        Cmd_Search.Execute(link);
                    }));
                }
            }
        });

        #region SearchBox routines
        private static ObservableCollection<string> auto_suggest_list = new ObservableCollection<string>() {};
        public static ObservableCollection<string> AutoSuggestList
        {
            get { return (auto_suggest_list); }
        }

        public static IEnumerable<string> GetSuggestList(this string text)
        {
            List<string> result = new List<string>();

            if (!string.IsNullOrEmpty(text))
            {
                if (Regex.IsMatch(text, @"^\d+$", RegexOptions.IgnoreCase))
                {
                    result.Add($"IllustID: {text}");
                    result.Add($"UserID: {text}");
                }
                result.Add($"User: {text}");
                result.Add($"Fuzzy Tag: {text}");
                result.Add($"Fuzzy: {text}");
                result.Add($"Tag: {text}");
                result.Add($"Caption: {text}");
            }

            return (result);
        }

        public static void SearchBox_TextChanged(object sender, RoutedEventArgs e)
        {
            if (sender is ComboBox)
            {
                var SearchBox = sender as ComboBox;
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
        }

        public static void SearchBox_DropDownOpened(object sender, EventArgs e)
        {
            if (sender is ComboBox)
            {
                var SearchBox = sender as ComboBox;

                var textBox = Keyboard.FocusedElement as TextBox;
                if (textBox != null && textBox.Text.Length == 1 && textBox.SelectionLength == 1)
                {
                    textBox.SelectionLength = 0;
                    textBox.SelectionStart = 1;
                }
            }
        }

        public static void SearchBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox)
            {
                var SearchBox = sender as ComboBox;

                e.Handled = true;
                var items = e.AddedItems;
                if (items.Count > 0)
                {
                    var item = items[0];
                    if (item is string)
                    {
                        var query = (string)item;
                        Cmd_Search.Execute(query);
                    }
                }
            }
        }

        public static void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (sender is ComboBox)
            {
                var SearchBox = sender as ComboBox;

                if (e.Key == Key.Return)
                {
                    e.Handled = true;
                    Cmd_Search.Execute(SearchBox.Text);
                }
            }
        }
        #endregion

        public static void UpdateTheme()
        {
            foreach (Window win in Application.Current.Windows)
            {
                if (win.Content is IllustDetailPage)
                {
                    var page = win.Content as IllustDetailPage;
                    page.UpdateTheme();
                }
                else if (win.Title.Equals("DropBox", StringComparison.CurrentCultureIgnoreCase))
                {
                    if (win.Content is Image)
                    {
                        var effect = (win.Content as Image).Effect;
                        if (effect is ThresholdEffect) (effect as ThresholdEffect).BlankColor = Theme.AccentColor;
                    }
                }
            }
        }

        public static void Show(this ProgressRing progress, bool show)
        {
            if(progress is ProgressRing)
            {
                if (show)
                {
                    progress.Visibility = Visibility.Visible;
                    progress.IsEnabled = true;
                    progress.IsActive = true;
                }
                else
                {
                    progress.Visibility = Visibility.Hidden;
                    progress.IsEnabled = false;
                    progress.IsActive = false;
                }
            }
        }

        public static void Show(this ProgressRing progress)
        {
            progress.Show(true);
        }

        public static void Hide(this ProgressRing progress)
        {
            progress.Show(false);
        }

        public static void Show(this UIElement element, bool show, bool parent = false)
        {
            if (show)
                element.Visibility = Visibility.Visible;
            else
                element.Visibility = Visibility.Collapsed;

            if (parent && element.GetParentObject() is UIElement)
                (element.GetParentObject() as UIElement).Visibility = element.Visibility;
        }

        public static void Show(this UIElement element, bool parent = false)
        {
            element.Show(true, parent);
        }

        public static void Hide(this UIElement element, bool parent = false)
        {
            element.Show(false, parent);
        }

        internal static DownloadManagerPage _downManager = new DownloadManagerPage();

        public static void ShowDownloadManager(bool active = false)
        {
            if (_downManager is DownloadManagerPage)
            {

            }
            else
                _downManager = new DownloadManagerPage();
            _downManager.AutoStart = false;

            Window _dm = null;
            foreach (Window win in Application.Current.Windows)
            {
                if (win.Content is DownloadManagerPage)
                {
                    _dm = win;
                    break;
                }
            }

            if (_dm is Window)
            {
                _dm.Show();
                if (active)
                    _dm.Activate();
            }
            else
            {
                var viewer = new ContentWindow();
                viewer.Title = $"Download Manager";
                viewer.Width = WIDTH_MIN;
                viewer.Height = HEIGHT_MIN;
                viewer.MinWidth = WIDTH_MIN;
                viewer.MinHeight = HEIGHT_MIN;
                viewer.Content = _downManager;
                viewer.Tag = _downManager;
                _downManager.window = viewer;
                viewer.Show();
            }
        }

        private static async Task<Pixeez.Tokens> RefreshToken()
        {
            Pixeez.Tokens result = null;
            try
            {
                var authResult = await Pixeez.Auth.AuthorizeAsync(setting.User, setting.Pass, setting.RefreshToken, setting.Proxy, setting.UsingProxy);
                setting.AccessToken = authResult.Authorize.AccessToken;
                setting.RefreshToken = authResult.Authorize.RefreshToken;
                setting.ExpTime = authResult.Key.KeyExpTime.ToLocalTime();
                setting.ExpiresIn = authResult.Authorize.ExpiresIn.Value;
                setting.Update = Convert.ToInt64(DateTime.Now.ToFileTime() / 10000000);
                setting.MyInfo = authResult.Authorize.User;
                setting.Save();
                result = authResult.Tokens;
            }
            catch (Exception ex)
            {
                if (!string.IsNullOrEmpty(setting.User) && !string.IsNullOrEmpty(setting.Pass))
                {
                    try
                    {
                        var authResult = await Pixeez.Auth.AuthorizeAsync(setting.User, setting.Pass, setting.Proxy, setting.UsingProxy);
                        setting.AccessToken = authResult.Authorize.AccessToken;
                        setting.RefreshToken = authResult.Authorize.RefreshToken;
                        setting.ExpTime = authResult.Key.KeyExpTime.ToLocalTime();
                        setting.ExpiresIn = authResult.Authorize.ExpiresIn.Value;
                        setting.Update = Convert.ToInt64(DateTime.Now.ToFileTime() / 10000000);
                        setting.MyInfo = authResult.Authorize.User;
                        setting.Save();
                        result = authResult.Tokens;
                    }
                    catch (Exception exx)
                    {
                        var ret = exx.Message;
                        var tokens = await ShowLogin();
                    }
                }
                var rt = ex.Message;
            }
            return (result);
        }

        public static async Task<Pixeez.Tokens> ShowLogin(bool force=false)
        {
            Pixeez.Tokens result = null;
            foreach (Window win in Application.Current.Windows)
            {
                if (win is PixivLoginDialog) return(result);
            }
            try
            {
                if(!force && setting.ExpTime > DateTime.Now && !string.IsNullOrEmpty(setting.AccessToken))
                {
                    result = Pixeez.Auth.AuthorizeWithAccessToken(setting.AccessToken, setting.Proxy, setting.UsingProxy);
                }
                else
                {
                    if (!string.IsNullOrEmpty(setting.User) && !string.IsNullOrEmpty(setting.Pass) && !string.IsNullOrEmpty(setting.RefreshToken))
                    {
                        try
                        {
                            result = await RefreshToken();
                        }
                        catch (Exception)
                        {
                            result = Pixeez.Auth.AuthorizeWithAccessToken(setting.AccessToken, setting.Proxy, setting.UsingProxy);
                        }
                    }
                    else
                    {
                        var dlgLogin = new PixivLoginDialog() { AccessToken=setting.AccessToken, RefreshToken=setting.RefreshToken };
                        var ret = dlgLogin.ShowDialog();
                        result = dlgLogin.Tokens;
                    }
                }
            }
            catch(Exception ex)
            {
                await ex.Message.ShowMessageBoxAsync("ERROR");
            }
            return (result);
        }

        public static string InsertLineBreak(this string text, int lineLength)
        {
            if (string.IsNullOrEmpty(text)) return (string.Empty);
            //return Regex.Replace(text, @"(.{" + lineLength + @"})", "$1" + Environment.NewLine);
            var t = Regex.Replace(text, @"[\n\r]", "", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            //t = Regex.Replace(t, @"<[^>]*>", "$1", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            t = Regex.Replace(t, @"(<br *?/>)", Environment.NewLine, RegexOptions.IgnoreCase | RegexOptions.Multiline);
            t = Regex.Replace(t, @"(<a .*?>(.*?)</a>)|(<strong>(.*?)</strong>)", "$2", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            t = Regex.Replace(t, @"<.*?>(.*?)</.*?>", "$1", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            return Regex.Replace(t, @"(.{" + lineLength + @"})", "$1" + Environment.NewLine, RegexOptions.IgnoreCase | RegexOptions.Multiline);
        }

        public static string HtmlEncode(this string text)
        {
            return (WebUtility.HtmlEncode(text));
        }

        public static string HtmlDecodeFix(this string text)
        {
            string result = text;

            var patten = new Regex(@"&(amp;){0,1}#(([0-9]{1,6})|(x([a-fA-F0-9]{1,5})));", RegexOptions.IgnoreCase);
            result = WebUtility.UrlDecode(WebUtility.HtmlDecode(result));
            foreach (Match match in patten.Matches(result))
            {
                var v = Convert.ToInt32(match.Groups[2].Value);
                if (v > 0xFFFF)
                    result = result.Replace(match.Value, char.ConvertFromUtf32(v));
            }

            return (result);
        }

        public static string SanityAge(this string sanity)
        {
            string age = "all-age";

            int san = 2;
            if (int.TryParse(sanity, out san))
            {
                switch (sanity)
                {
                    case "3":
                        age = "12+";
                        break;
                    case "4":
                        age = "15+";
                        break;
                    case "5":
                        age = "17+";
                        break;
                    case "6":
                        age = "18+";
                        break;
                    default:
                        age = "all";
                        break;
                }
            }
            else
            {
                age = sanity;
            }
            return (age);
        }

        // To return an array of strings instead:
        public static string[] Slice(this string text, int lineLength)
        {
            if (string.IsNullOrEmpty(text)) return (new string[] { });
            //return Regex.Matches(text, @"(.{" + lineLength + @"})").Cast<Match>().Select(m => m.Value).ToArray();
            var t = Regex.Replace(text, @"[\n\r]", "", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            t = Regex.Replace(t, @"(<br *?/>)", Environment.NewLine, RegexOptions.IgnoreCase | RegexOptions.Multiline);
            t = Regex.Replace(t, @"(<a .*?>(.*?)</a>)|(<strong>(.*?)</strong>)", "$2", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            t = Regex.Replace(t, @"<.*?>(.*?)</.*?>", "$1", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            return Regex.Matches(t, @"(.{" + lineLength + @"})", RegexOptions.IgnoreCase | RegexOptions.Multiline).Cast<Match>().Select(m => m.Value).ToArray();
        }

        public static async Task<MemoryStream> ToMemoryStream(this Pixeez.AsyncResponse response)
        {
            MemoryStream result = null;
            using (var stream = await response.GetResponseStreamAsync())
            {
                result = new MemoryStream();
                await stream.CopyToAsync(result);
            }
            return (result);
        }

        public async static Task<BitmapSource> ConvertBitmapDPI(this BitmapSource source, double dpiX = 96, double dpiY = 96)
        {
            if (dpiX == source.DpiX || dpiY == source.DpiY) return (source);

            int width = source.PixelWidth;
            int height = source.PixelHeight;

            var palette = source.Palette;
            int stride = width * ((source.Format.BitsPerPixel + 31) / 32 * 4);
            byte[] pixelData = new byte[stride * height];
            source.CopyPixels(pixelData, stride, 0);

            BitmapSource result = source;
            try
            {
                using (var ms = new MemoryStream())
                {
                    var nbmp = BitmapSource.Create(width, height, dpiX, dpiY, source.Format, palette, pixelData, stride);
                    PngBitmapEncoder pngEnc = new PngBitmapEncoder();
                    pngEnc.Frames.Add(BitmapFrame.Create(nbmp));
                    pngEnc.Save(ms);
                    var pngDec = new PngBitmapDecoder(ms, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                    result = pngDec.Frames[0];
                }
            }
            catch(Exception ex)
            {
                await ex.Message.ShowMessageBoxAsync("ERROR");
            }
            return result;
        }

        public async static Task<ImageSource> ToImageSource(this Stream stream)
        {
            //await imgStream.GetResponseStreamAsync();
            BitmapSource result = null;
            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.StreamSource = stream;
                bmp.EndInit();
                bmp.Freeze();

                result = bmp;
                //result = BitmapFrame.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
            }
            catch (Exception ex)
            {
                var ret = ex.Message;
                try
                {
                    result = BitmapFrame.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                }
                catch(Exception exx)
                {
                    var retx = exx.Message;
                }
            }
            if(result is ImageSource)
            {
                var dpi = new DPI();
                if (result.DpiX != dpi.X || result.DpiY != dpi.Y)
                    result = await ConvertBitmapDPI(result, dpi.X, dpi.Y);
            }
            return (result);
        }

        internal static bool IsDownloaded(this Pixeez.Objects.Work illust, bool is_meta_single_page = false)
        {
            if (illust is Pixeez.Objects.Work)
                return (illust.GetOriginalUrl().IsDownloaded(is_meta_single_page));
            else
                return (false);
        }

        internal static bool IsDownloaded(this Pixeez.Objects.Work illust, out string filepath, bool is_meta_single_page = false)
        {
            if (illust is Pixeez.Objects.Work)
                return (illust.GetOriginalUrl().IsDownloaded(out filepath, is_meta_single_page));
            else
            {
                filepath = string.Empty;
                return (false);
            }            
        }

        internal static bool IsDownloaded(this Pixeez.Objects.Work illust, int index = -1)
        {
            if (illust is Pixeez.Objects.Work)
                return (illust.GetOriginalUrl(index).IsDownloaded());
            else
                return (false);            
        }

        internal static bool IsDownloaded(this Pixeez.Objects.Work illust, out string filepath, int index = -1, bool is_meta_single_page = false)
        {
            if (illust is Pixeez.Objects.Work)
                return (illust.GetOriginalUrl(index).IsDownloaded(out filepath, is_meta_single_page));
            else
            {
                filepath = string.Empty;
                return (false);
            }
        }

        internal static bool IsPartDownloaded(this ImageItem item)
        {
            if (item.Illust is Pixeez.Objects.Work)
                return (item.Illust.GetOriginalUrl().IsPartDownloaded());
            else
                return (false);
        }

        internal static bool IsPartDownloaded(this ImageItem item, out string filepath)
        {
            if (item.Illust is Pixeez.Objects.Work)
                return (item.Illust.GetOriginalUrl().IsPartDownloaded(out filepath));
            else
            {
                filepath = string.Empty;
                return (false);
            }
        }

        internal static bool IsPartDownloaded(this Pixeez.Objects.Work illust)
        {
            if (illust is Pixeez.Objects.Work)
                return (illust.GetOriginalUrl().IsPartDownloaded());
            else
                return (false);
        }

        internal static bool IsPartDownloaded(this Pixeez.Objects.Work illust, out string filepath)
        {
            if (illust is Pixeez.Objects.Work)
                return (illust.GetOriginalUrl().IsPartDownloaded(out filepath));
            else
            {
                filepath = string.Empty;
                return (false);
            }
        }

        internal static bool IsDownloaded(this string url, bool is_meta_single_page = false)
        {
            bool result = false;
            var file = url.GetImageName(is_meta_single_page);
            foreach (var local in setting.LocalStorage)
            {
                if (string.IsNullOrEmpty(local)) continue;

                var folder = local.FolderMacroReplace(url.GetIllustId());
                var f = Path.Combine(folder, file);
                if (File.Exists(f))
                {
                    result = true;
                    break;
                }
            }
            return (result);
        }

        internal static bool IsDownloaded(this string url, out string filepath, bool is_meta_single_page = false)
        {
            bool result = false;
            filepath = string.Empty;

            var file = url.GetImageName(is_meta_single_page);
            foreach (var local in setting.LocalStorage)
            {
                if (string.IsNullOrEmpty(local)) continue;

                var folder = local.FolderMacroReplace(url.GetIllustId());
                var f = Path.Combine(folder, file);
                if (File.Exists(f))
                {
                    filepath = f;
                    result = true;
                    break;
                }
            }

            return (result);
        }

        internal static bool IsPartDownloaded(this string url)
        {
            bool result = false;
            var file = url.GetImageName(true);
            int[] range = Enumerable.Range(0, 250).ToArray();

            foreach (var local in setting.LocalStorage)
            {
                if (string.IsNullOrEmpty(local)) continue;

                var folder = local.FolderMacroReplace(url.GetIllustId());
                var f = Path.Combine(folder, file);
                if (File.Exists(f))
                {
                    result = true;
                    break;
                }

                var fn = Path.GetFileNameWithoutExtension(file);
                var fe = Path.GetExtension(file);
                foreach (var fc in range)
                {
                    var fp = Path.Combine(folder, $"{fn}_{fc}{fe}");
                    if (File.Exists(fp))
                    {
                        result = true;
                        break;
                    }
                }
                if (result) break;
            }
            return (result);
        }

        internal static bool IsPartDownloaded(this string url, out string filepath)
        {
            bool result = false;
            var file = url.GetImageName(true);
            int[] range = Enumerable.Range(0, 250).ToArray();

            filepath = string.Empty;
            foreach (var local in setting.LocalStorage)
            {
                if (string.IsNullOrEmpty(local)) continue;

                var folder = local.FolderMacroReplace(url.GetIllustId());
                var f = Path.Combine(folder, file);
                if (File.Exists(f))
                {
                    filepath = f;
                    result = true;
                    break;
                }

                var fn = Path.GetFileNameWithoutExtension(file);
                var fe = Path.GetExtension(file);
                foreach (var fc in range)
                {
                    var fp = Path.Combine(folder, $"{fn}_{fc}{fe}");
                    if (File.Exists(fp))
                    {
                        filepath = fp;
                        result = true;
                        break;
                    }
                }
                if (result) break;
            }
            return (result);
        }

        internal static bool IsFileReady(this string filename)
        {
            // If the file can be opened for exclusive access it means that the file
            // is no longer locked by another process.
            try
            {
                if (!File.Exists(filename)) return true;

                using (FileStream inputStream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.None))
                    return inputStream.Length > 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        internal static void WaitForFile(this string filename)
        {
            //This will lock the execution until the file is ready
            //TODO: Add some logic to make it async and cancelable
            while (!IsFileReady(filename)) { }
        }

        public static string GetLocalFile(this string url)
        {
            string result = url;
            if (!string.IsNullOrEmpty(url) && cache is CacheImage)
            {
                result = cache.GetCacheFile(url);
            }
            return (result);
        }

        public static async Task<ImageSource> LoadImage(this string file)
        {
            ImageSource result = null;
            if (!string.IsNullOrEmpty(file) && File.Exists(file))
            {
                using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    await Task.Run(async () =>
                    {
                        result = await stream.ToImageSource();
                    });
                }
            }
            return (result);
        }

        public static async Task<ImageSource> LoadImage(this string url, Pixeez.Tokens tokens)
        {
            ImageSource result = null;
            if (!string.IsNullOrEmpty(url) && cache is CacheImage)
            {
                result = await cache.GetImage(url, tokens);
            }
            return (result);
        }

        public static async Task<string> LoadImagePath(this string url, Pixeez.Tokens tokens)
        {
            string result = null;
            if (!string.IsNullOrEmpty(url) && cache is CacheImage)
            {
                result = await cache.GetImagePath(url, tokens);
            }
            return (result);
        }

        public static async Task<bool> SaveImage(this string url, Pixeez.Tokens tokens, string file, bool overwrite=true)
        {
            bool result = false;
            if (!string.IsNullOrEmpty(file))
            {
                try
                {
                    if (!overwrite && File.Exists(file) && new FileInfo(file).Length > 0)
                    {
                        return (true);
                    }
                    using (var response = await tokens.SendRequestAsync(Pixeez.MethodType.GET, url))
                    {
                        if (response != null && response.Source.StatusCode == HttpStatusCode.OK)
                        {
                            using (var ms = await response.ToMemoryStream())
                            {
                                var dir = Path.GetDirectoryName(file);
                                if (!Directory.Exists(dir))
                                {
                                    Directory.CreateDirectory(dir);
                                }
                                //using (var sms = new FileStream(file, FileMode.Create, FileAccess.Write, FileShare.Read))
                                //{
                                //    ms.Seek(0, SeekOrigin.Begin);
                                //    await sms.WriteAsync(ms.ToArray(), 0, (int)ms.Length);
                                //    result = true;
                                //}
                                //WaitForFile(file);
                                File.WriteAllBytes(file, ms.ToArray());
                                result = true;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    if(ex is IOException)
                    {

                    }
                    else
                    {
                        ex.Message.ShowMessageBox("ERROR");
                    }
                }
            }
            return (result);
        }

        public static async Task<string> SaveImage(this string url, Pixeez.Tokens tokens, bool is_meta_single_page=false, bool overwrite = true)
        {
            string result = string.Empty;
            //url = Regex.Replace(url, @"//.*?\.pixiv.net/", "//i.pximg.net/", RegexOptions.IgnoreCase);
            var file = url.GetImageName(is_meta_single_page);
            if (string.IsNullOrEmpty(setting.LastFolder))
            {
                SaveFileDialog dlgSave = new SaveFileDialog();
                dlgSave.FileName = file;
                if (dlgSave.ShowDialog() == true)
                {
                    file = dlgSave.FileName;
                    setting.LastFolder = Path.GetDirectoryName(file);
                }
                else file = string.Empty;
            }

            try
            {
                if (!string.IsNullOrEmpty(file))
                {
                    if (!overwrite && File.Exists(file) && new FileInfo(file).Length > 0)
                    {
                        return (file);
                    }

                    using (var response = await tokens.SendRequestAsync(Pixeez.MethodType.GET, url))
                    {
                        if (response != null && response.Source.StatusCode == HttpStatusCode.OK)
                        {
                            using (var ms = await response.ToMemoryStream())
                            {
                                file = Path.Combine(setting.LastFolder, Path.GetFileName(file));
                                //using(var sms = new FileStream(fn, FileMode.Create, FileAccess.Write, FileShare.Read))
                                //{
                                //    ms.Seek(0, SeekOrigin.Begin);
                                //    await sms.WriteAsync(ms.ToArray(), 0, (int)ms.Length);
                                //    result = fn;
                                //}
                                //WaitForFile(file);
                                File.WriteAllBytes(file, ms.ToArray());
                                result = file;
                            }
                        }
                        else result = null;
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex is IOException)
                {

                }
                else
                {
                    ex.Message.ShowMessageBox("ERROR");
                }
            }
            return (result);
        }

        public static async Task<string> SaveImage(this string url, Pixeez.Tokens tokens, DateTime dt, bool is_meta_single_page = false, bool overwrite = true)
        {
            var file = await url.SaveImage(tokens, is_meta_single_page, overwrite);
            if (!string.IsNullOrEmpty(file))
            {
                File.SetCreationTime(file, dt);
                File.SetLastWriteTime(file, dt);
                File.SetLastAccessTime(file, dt);
                $"{Path.GetFileName(file)} is saved!".ShowToast("Successed", file);

                if (Regex.IsMatch(file, @"_ugoira\d+\.", RegexOptions.IgnoreCase))
                {
                    var ugoira_url = url.Replace("img-original", "img-zip-ugoira");
                    //ugoira_url = Regex.Replace(ugoira_url, @"(_ugoira)(\d+)(\..*?)", "_ugoira1920x1080.zip", RegexOptions.IgnoreCase);
                    ugoira_url = Regex.Replace(ugoira_url, @"_ugoira\d+\..*?$", "_ugoira1920x1080.zip", RegexOptions.IgnoreCase);
                    var ugoira_file = await ugoira_url.SaveImage(tokens, dt, true, overwrite);
                    if (!string.IsNullOrEmpty(ugoira_file))
                    {
                        File.SetCreationTime(ugoira_file, dt);
                        File.SetLastWriteTime(ugoira_file, dt);
                        File.SetLastAccessTime(ugoira_file, dt);
                        $"{Path.GetFileName(ugoira_file)} is saved!".ShowToast("Successed", ugoira_file);
                    }
                    else
                    {
                        $"Save {Path.GetFileName(ugoira_url)} failed!".ShowToast("Failed", "");
                    }
                }
            }
            else
            {
                $"Save {Path.GetFileName(url)} failed!".ShowToast("Failed", "");
            }
            return (file);
        }

        public static async Task<List<string>> SaveImage(Dictionary<string, DateTime> files, Pixeez.Tokens tokens, bool is_meta_single_page = false)
        {
            List<string> result = new List<string>();

            foreach (var file in files)
            {
                var f = await file.Key.SaveImage(tokens, file.Value, is_meta_single_page);
                result.Add(f);
            }
            SystemSounds.Beep.Play();

            return (result);
        }

        public static void SaveImage(this string url, string thumb, DateTime dt, bool is_meta_single_page = false, bool overwrite = true)
        {
            ShowDownloadManager();
            if(_downManager is DownloadManagerPage)
            {
                _downManager.Add(url, thumb, dt, is_meta_single_page, overwrite);
            }
        }

        public static void SaveImages(Dictionary<Tuple<string, bool>, Tuple<string, DateTime>> files, bool overwrite = true)
        {
            foreach (var file in files)
            {
                var url = file.Key.Item1;
                var is_meta_single_page =  file.Key.Item2;
                var thumb = file.Value.Item1;
                var dt = file.Value.Item2;
                url.SaveImage(thumb, dt, is_meta_single_page, overwrite);
            }
            SystemSounds.Beep.Play();
        }

        public static async Task<ImageSource> GetImageFromURL(this string url)
        {
            ImageSource result = null;

            var uri = new Uri(url);
            var webRequest = WebRequest.CreateDefault(uri);
            var ext = Path.GetExtension(url).ToLower();
            switch (ext)
            {
                case ".jpeg":
                case ".jpg":
                    webRequest.ContentType = "image/jpeg";
                    break;
                case ".png":
                    webRequest.ContentType = "image/png";
                    break;
                case ".bmp":
                    webRequest.ContentType = "image/bmp";
                    break;
                case ".gif":
                    webRequest.ContentType = "image/gif";
                    break;
                case ".tiff":
                case ".tif":
                    webRequest.ContentType = "image/tiff";
                    break;
                default:
                    webRequest.ContentType = "application/octet-stream";
                    break;
            }

            var proxy = Setting.ProxyServer();
            var useproxy = Setting.UseProxy();
            HttpClientHandler handler = new HttpClientHandler()
            {
                Proxy = string.IsNullOrEmpty(Setting.ProxyServer()) ? null : new WebProxy(proxy, true, new string[] { "127.0.0.1", "localhost", "192.168.1" }),
                UseProxy = string.IsNullOrEmpty(proxy) || !useproxy ? false : true
            };
            using (HttpClient client = new HttpClient(handler))
            {
                HttpResponseMessage response = await client.GetAsync(url);
                byte[] content = await response.Content.ReadAsByteArrayAsync();
                //return "data:image/png;base64," + Convert.ToBase64String(content);
                BitmapImage image = new BitmapImage();
                image.BeginInit();
                image.StreamSource = new MemoryStream(content);
                image.EndInit();
                image.Freeze();
                result = image;
            }

            return (result);
        }

        public static async Task<ImageSource> ToImageSource(this string url, Pixeez.Tokens tokens)
        {
            ImageSource result = null;
            //url = Regex.Replace(url, @"//.*?\.pixiv.net/", "//i.pximg.net/", RegexOptions.IgnoreCase);
            using (var response = await tokens.SendRequestAsync(Pixeez.MethodType.GET, url))
            {
                if (response.Source.StatusCode == HttpStatusCode.OK)
                    result = await response.ToImageSource();
                else
                    result = null;
            }
            return (result);
        }

        public static async Task<ImageSource> ToImageSource(this Pixeez.AsyncResponse response)
        {
            ImageSource result = null;
            using (var stream = await response.GetResponseStreamAsync())
            {
                result = await stream.ToImageSource();
            }
            return (result);
        }

        public static MetroWindow GetActiveWindow()
        {
            MetroWindow window = Application.Current.Windows.OfType<MetroWindow>().SingleOrDefault(x => x.IsActive);
            if (window == null) window = Application.Current.MainWindow as MetroWindow;
            return (window);
        }

        public static Window GetActiveWindow(this System.Windows.Controls.Page page)
        {
            var window = Window.GetWindow(page);
            if (window == null) window = GetActiveWindow();
            return (window);
        }

        public static void ShowMessageBox(this string content, string title)
        {
            ShowMessageDialog(title, content);
        }

        public static async Task ShowMessageBoxAsync(this string content, string title)
        {
            await ShowMessageDialogAsync(title, content);
        }

        public static async void ShowMessageDialog(string title, string content)
        {
            MetroWindow window = GetActiveWindow();
            await window.ShowMessageAsync(title, content);
        }

        public static async Task ShowMessageDialogAsync(string title, string content)
        {
            MetroWindow window = GetActiveWindow();
            await window.ShowMessageAsync(title, content);
        }

        public static async void ShowProgressDialog(object sender, RoutedEventArgs e)
        {
            var mySettings = new MetroDialogSettings()
            {
                NegativeButtonText = "Close now",
                AnimateShow = false,
                AnimateHide = false
            };

            MetroWindow window = GetActiveWindow();

            var controller = await window.ShowProgressAsync("Please wait...", "We are baking some cupcakes!", settings: mySettings);
            controller.SetIndeterminate();

            //await Task.Delay(5000);

            controller.SetCancelable(true);

            double i = 0.0;
            while (i < 6.0)
            {
                double val = (i / 100.0) * 20.0;
                controller.SetProgress(val);
                controller.SetMessage("Baking cupcake: " + i + "...");

                if (controller.IsCanceled)
                    break; //canceled progressdialog auto closes.

                i += 1.0;

                //await Task.Delay(2000);
            }

            await controller.CloseAsync();

            if (controller.IsCanceled)
            {

                await window.ShowMessageAsync("No cupcakes!", "You stopped baking!");
            }
            else
            {
                await window.ShowMessageAsync("Cupcakes!", "Your cupcakes are finished! Enjoy!");
            }
        }

        public static void ShowToast(this string content, string title = "Pixiv", string imgsrc = "", object tag = null)
        {
            INotificationDialogService _dailogService = new NotificationDialogService();
            NotificationConfiguration cfgDefault = NotificationConfiguration.DefaultConfiguration;
            NotificationConfiguration cfg = new NotificationConfiguration(
                //new TimeSpan(0, 0, 30), 
                TimeSpan.FromSeconds(3),
                cfgDefault.Width+32, cfgDefault.Height,
                "ToastTemplate",
                //cfgDefault.TemplateName, 
                cfgDefault.NotificationFlowDirection
            );

            var newNotification = new MyNotification()
            {
                Title = title,
                ImgURL = imgsrc,
                Message = content,
                Tag = tag
            };

            _dailogService.ClearNotifications();
            _dailogService.ShowNotificationWindow(newNotification, cfg);
        }

        #region Illust Tile ListView routines
        public static bool IsSameIllust(this string id, int hash)
        {
            return (cache.IsSameIllust(hash, id));
        }

        public static bool IsSameIllust(this long id, int hash)
        {
            return (cache.IsSameIllust(hash, $"{id}"));
        }

        public static bool IsSameIllust(this long? id, int hash)
        {
            return (cache.IsSameIllust(hash, $"{id ?? -1}"));
        }

        public static bool IsSameIllust(this ImageItem item, int hash)
        {
            bool result = false;

            if (item.ItemType == ImageItemType.Work) {
                result = item.Illust.GetPreviewUrl(item.Index).GetImageId().IsSameIllust(hash) || item.Illust.GetOriginalUrl(item.Index).GetImageId().IsSameIllust(hash);
            }

            return (result);
        }

        public static bool IsSameIllust(this ImageItem item, long id)
        {
            bool result = false;

            try
            {
                if (long.Parse(item.ID) == id) result = true;
            }
            catch (Exception) { }

            return (result);
        }

        public static bool IsSameIllust(this ImageItem item, long? id)
        {
            bool result = false;

            try
            {
                if (long.Parse(item.ID) == (id ?? -1)) result = true;
            }
            catch (Exception) { }

            //long id_s = -1;
            //long.TryParse(item.ID, out id_s);
            //if (id_s == id.Value) result = true;
            //if(string.Equals(item.ID, id.ToString(), StringComparison.CurrentCultureIgnoreCase))
            //{
            //    result = true;
            //}

            return (result);
        }

        public static bool IsSameIllust(this ImageItem item, ImageItem item_now)
        {
            bool result = false;

            try
            {
                if (long.Parse(item.ID) == long.Parse(item_now.ID)) result = true;
            }
            catch (Exception) { }

            //long id_s = -1;
            //long.TryParse(item.ID, out id_s);
            //long id_t = -1;
            //long.TryParse(item_now.ID, out id_t);
            //if (id_s == id_t) result = true;

            return (result);
        }

        public static async Task<Pixeez.Objects.Work> RefreshIllust(this Pixeez.Objects.Work Illust, Pixeez.Tokens tokens)
        {
            var result = Illust;
            var illusts = await tokens.GetWorksAsync(Illust.Id.Value);
            foreach (var illust in illusts)
            {
                illust.Cache();
                Illust = illust;
                result = illust;
                break;
            }
            return (result);
        }

        public static async Task<Pixeez.Objects.UserBase> RefreshUser(this Pixeez.Objects.Work Illust, Pixeez.Tokens tokens)
        {
            var result = Illust.User;
            var users = await tokens.GetUsersAsync(Illust.User.Id.Value);
            foreach (var user in users)
            {
                if (user.Id.Value == Illust.User.Id.Value)
                {
                    user.Cache();
                    Illust.User.is_followed = user.is_followed;
                    result = user;
                    break;
                }
            }
            return (result);
        }

        public static async Task<Pixeez.Objects.UserBase> RefreshUser(this Pixeez.Objects.UserBase User, Pixeez.Tokens tokens)
        {
            var result = User;
            var users = await tokens.GetUsersAsync(User.Id.Value);
            foreach (var user in users)
            {
                if (user.Id.Value == User.Id.Value)
                {
                    User.is_followed = user.is_followed;
                    result = user;
                    user.Cache();
                    break;
                }
            }
            return (result);
        }

        public static bool IsLiked(this Pixeez.Objects.Work illust)
        {
            bool result = false;
            illust = cacheIllust.ContainsKey(illust.Id) ? cacheIllust[illust.Id] : illust;
            if (illust.User != null)
            {
                result = illust.IsBookMarked();// || (illust.IsLiked ?? false);
            }
            return (result);
        }

        public static bool IsLiked(this Pixeez.Objects.UserBase user)
        {
            bool result = false;
            user = cacheUser.ContainsKey(user.Id) ? cacheUser[user.Id] : user;
            if (user != null)
            {
                result = user.is_followed ?? (user as Pixeez.Objects.User).IsFollowing ?? false;
            }
            return (result);
        }

        public static bool IsLiked(this ImageItem item)
        {
            return (item.ItemType == ImageItemType.User ? item.Illust.User.IsLiked() : item.Illust.IsLiked());
        }

        public static void UpdateTiles(this ObservableCollection<ImageItem> collection, ImageItem item = null)
        {
            if (collection is ObservableCollection<ImageItem>)
            {
                if (item is ImageItem)
                {
                    int idx = collection.IndexOf(item);
                    if (idx >= 0 && idx < collection.Count())
                    {
                        collection.Remove(item);
                        collection.Insert(idx, item);
                    }
                }
                else
                {
                    CollectionViewSource.GetDefaultView(collection).Refresh();                    
                }
            }
        }

        public static void UpdateTiles(this ObservableCollection<ImageItem> collection, IEnumerable<ImageItem> items)
        {
            if (collection is ObservableCollection<ImageItem>)
            {
                if (items is IEnumerable<ImageItem>)
                {
                    var count = collection.Count();
                    foreach (ImageItem sub in items)
                    {
                        int idx = collection.IndexOf(sub);
                        if (idx >= 0 && idx < count)
                        {
                            collection.Remove(sub);
                            collection.Insert(idx, sub);
                        }
                    }
                }
                else
                {
                    CollectionViewSource.GetDefaultView(collection).Refresh();
                }
            }
        }

        public static bool UpdateTilesDaownloadStatus(this ImageListGrid gallery, bool fuzzy = true)
        {
            bool result = false;
            if (gallery.SelectedItems.Count <= 0 || gallery.SelectedIndex < 0) return(result);
            try
            {
                foreach (var item in gallery.SelectedItems)
                {
                    if (item.Illust == null) continue;
                    bool download = fuzzy ? item.Illust.IsPartDownloaded() : item.Illust.GetOriginalUrl(item.Index).IsDownloaded();
                    if (item.IsDownloaded != download)
                    {
                        item.IsDownloaded = download;
                        result |= download;
                    }
                    item.IsFavorited = item.IsLiked() && item.DisplayFavMark;
                }
            }
#if DEBUG
            catch(Exception e)
            {
                e.Message.ShowMessageBox("ERROR");
            }
#else
            catch (Exception) { }
#endif
            return (result);
        }

        public static async Task<bool> LikeIllust(this ImageItem item, bool pub = true)
        {
            bool result = false;

            if (item.ItemType == ImageItemType.Work || item.ItemType == ImageItemType.Works || item.ItemType == ImageItemType.Manga )
            {
                var tokens = await ShowLogin();
                if (tokens == null) return (result);

                var illust = item.Illust;
                try
                {
                    if (pub)
                    {
                        await tokens.AddMyFavoriteWorksAsync((long)illust.Id, illust.Tags);
                    }
                    else
                    {
                        await tokens.AddMyFavoriteWorksAsync((long)illust.Id, illust.Tags, "private");
                    }
                }
                catch (Exception) { }
                finally
                {
                    try
                    {
                        tokens = await ShowLogin();
                        illust = await illust.RefreshIllust(tokens);
                        if (illust != null)
                        {
                            result = illust.IsLiked();
                            item.Illust = illust;
                            item.IsFavorited = result;
                        }
                    }
                    catch (Exception) { }
                }
            }

            return (result);
        }

        public static async Task<bool> UnLikeIllust(this ImageItem item, bool pub = true)
        {
            bool result = false;

            if (item.ItemType == ImageItemType.Work || item.ItemType == ImageItemType.Works || item.ItemType == ImageItemType.Manga)
            {
                var tokens = await ShowLogin();
                if (tokens == null) return (result);

                var illust = item.Illust;
                var lastID = illust.Id;
                try
                {
                    await tokens.DeleteMyFavoriteWorksAsync((long)illust.Id);
                    await tokens.DeleteMyFavoriteWorksAsync((long)illust.Id, "private");
                }
                catch (Exception) { }
                finally
                {
                    try
                    {
                        tokens = await ShowLogin();
                        illust = await illust.RefreshIllust(tokens);
                        if (illust != null)
                        {
                            result = illust.IsLiked();
                            item.Illust = illust;
                            item.IsFavorited = result;
                        }
                    }
                    catch (Exception) { }
                }
            }

            return (result);
        }

        public static void LikeIllust(this ObservableCollection<ImageItem> collection, bool pub = true)
        {
            var opt = new ParallelOptions();
            opt.MaxDegreeOfParallelism = 5;
            var items = collection.GroupBy(i => i.ID).Select(g => g.First()).ToList();
            var ret = Parallel.ForEach(items, opt, (item, loopstate, elementIndex) =>
            {
                if (item is ImageItem && item.Illust is Pixeez.Objects.Work)
                {
                    item.Dispatcher.BeginInvoke((Action)(async () =>
                    {
                        try
                        {
                            var result = item.Illust.IsLiked() ? true : await item.LikeIllust(pub);
                            if (item.ItemType == ImageItemType.Work) item.IsFavorited = result;
                        }
                        catch (Exception){}
                    }));
                }
            });
        }

        public static void UnLikeIllust(this ObservableCollection<ImageItem> collection, bool pub = true)
        {
            var opt = new ParallelOptions();
            opt.MaxDegreeOfParallelism = 5;
            var items = collection.GroupBy(i => i.ID).Select(g => g.First()).ToList();
            var ret = Parallel.ForEach(items, opt, (item, loopstate, elementIndex) =>
            {
                if (item is ImageItem && item.Illust is Pixeez.Objects.Work)
                {
                    item.Dispatcher.BeginInvoke((Action)(async () =>
                    {
                        try
                        {
                            var result = item.IsLiked() ? await item.UnLikeIllust() : false;
                            if (item.ItemType == ImageItemType.Work) item.IsFavorited = result;
                        }
                        catch (Exception){}
                    }));
                }
            });
        }

        public static void LikeIllust(this IList<ImageItem> collection, bool pub = true)
        {
            LikeIllust(new ObservableCollection<ImageItem>(collection), pub);
        }

        public static void UnLikeIllust(this IList<ImageItem> collection)
        {
            UnLikeIllust(new ObservableCollection<ImageItem>(collection));
        }

        public static async Task<bool> LikeUser(this ImageItem item, bool pub = true)
        {
            bool result = false;

            if ((item.ItemType == ImageItemType.User || item.ItemType == ImageItemType.Work || item.ItemType == ImageItemType.Works || item.ItemType == ImageItemType.Manga) && item.User is Pixeez.Objects.UserBase)
            {
                try
                {
                    var user = item.User;
                    result = await user.Like(pub);
                    if (item.ItemType == ImageItemType.User)
                        item.IsFavorited = result;
                }
                catch (Exception) { }
            }

            return (result);
        }

        public static async Task<bool> UnLikeUser(this ImageItem item, bool pub = true)
        {
            bool result = false;

            if (item.ItemType == ImageItemType.User && item.User is Pixeez.Objects.UserBase)
            {
                try
                {
                    var user = item.User;
                    result = await user.UnLike();
                    if (item.ItemType == ImageItemType.User)
                        item.IsFavorited = result;
                }
                catch (Exception) { }
            }

            return (result);
        }

        public static void LikeUser(this ObservableCollection<ImageItem> collection, bool pub = true)
        {
            var opt = new ParallelOptions();
            opt.MaxDegreeOfParallelism = 5;
            //var items = collection.Distinct();
            var items = collection.GroupBy(i => i.UserID).Select(g => g.First()).ToList();
            var ret = Parallel.ForEach(items, opt, (item, loopstate, elementIndex) =>
            {
                if (item is ImageItem && item.User is Pixeez.Objects.UserBase)
                {
                    item.Dispatcher.BeginInvoke((Action)(async () =>
                    {
                        try
                        {
                            var result = item.User.IsLiked() ? true : await item.LikeUser(pub);
                            if (item.ItemType == ImageItemType.User) item.IsFavorited = result;
                        }
                        catch (Exception){}
                    }));
                }
            });
        }

        public static void UnLikeUser(this ObservableCollection<ImageItem> collection)
        {
            var opt = new ParallelOptions();
            opt.MaxDegreeOfParallelism = 5;
            var items = collection.GroupBy(i => i.UserID).Select(g => g.First()).ToList();
            var ret = Parallel.ForEach(items, opt, (item, loopstate, elementIndex) =>
            {
                if (item is ImageItem && item.User is Pixeez.Objects.UserBase)
                {
                    item.Dispatcher.BeginInvoke((Action)(async () =>
                    {
                        try
                        {
                            var result = item.User.IsLiked() ? await item.UnLikeUser() : false;
                            if (item.ItemType == ImageItemType.User) item.IsFavorited = result;
                        }
                        catch (Exception){}
                    }));
                }
            });
        }

        public static void LikeUser(this IList<ImageItem> collection, bool pub = true)
        {
            LikeUser(new ObservableCollection<ImageItem>(collection), pub);
        }

        public static void UnLikeUser(this IList<ImageItem> collection)
        {
            UnLikeUser(new ObservableCollection<ImageItem>(collection));
        }

        public static async Task<bool> Like(this Pixeez.Objects.UserBase user, bool pub = true)
        {
            return (await user.LikeUser(pub));
        }

        public static async Task<bool> UnLike(this Pixeez.Objects.UserBase user, bool pub = true)
        {
            return (await user.UnLikeUser());
        }

        public static async Task<bool> LikeUser(this Pixeez.Objects.UserBase user, bool pub = true)
        {
            bool result = false;

            var tokens = await ShowLogin();
            if (tokens == null) return (result);

            try
            {
                if (pub)
                {
                    await tokens.AddFavouriteUser((long)user.Id);
                }
                else
                {
                    await tokens.AddFavouriteUser((long)user.Id, "private");
                }
            }
            catch (Exception) { }
            finally
            {
                try
                {
                    tokens = await ShowLogin();
                    user = await user.RefreshUser(tokens);
                    if (user != null)
                    {
                        result = user.IsLiked();
                    }
                }
                catch (Exception) { }
            }
            return (result);
        }

        public static async Task<bool> UnLikeUser(this Pixeez.Objects.UserBase user)
        {
            bool result = false;

            var tokens = await ShowLogin();
            if (tokens == null) return (result);

            try
            {
                await tokens.DeleteFavouriteUser(user.Id.ToString());
                await tokens.DeleteFavouriteUser(user.Id.ToString(), "private");
            }
            catch (Exception) { }
            finally
            {
                try
                {
                    tokens = await ShowLogin();
                    user = await user.RefreshUser(tokens);
                    if (user != null)
                    {
                        result = user.IsLiked();
                    }
                }
                catch (Exception) { }
            }
            return (result);
        }

        public static void Cache(this Pixeez.Objects.UserBase user)
        {
            if(user is Pixeez.Objects.UserBase)
                cacheUser[user.Id] = user;
        }

        public static void Cache(this Pixeez.Objects.Work illust)
        {
            if (illust is Pixeez.Objects.Work)
                cacheIllust[illust.Id] = illust;
        }
        #endregion

        #region Drop Box routines
        private static void DropBox_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left )
            {
                if(sender is ContentWindow)
                {
                    var window = sender as ContentWindow;
                    window.DragMove();
                    setting.DropBoxPosition = new Point(window.Left, window.Top);
                    setting.Save();
                }
            }
            else if(e.ChangedButton == MouseButton.XButton1)
            {
                if (sender is ContentWindow)
                {
                    //var window = sender as ContentWindow;
                    //window.Hide();
                    e.Handled = true;
                }
            }
        }

        private static void DropBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed )
            {
                if (sender is ContentWindow)
                {
                    var window = sender as ContentWindow;
                    // In maximum window state case, window will return normal state and continue moving follow cursor
                    if (window.WindowState == WindowState.Maximized)
                    {
                        window.WindowState = WindowState.Normal;
                        // 3 or any where you want to set window location affter return from maximum state
                        //Application.Current.MainWindow.Top = 3;
                    }
                    window.DragMove();
                }
            }
        }

        private static void DropBox_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount >= 3)
            {
                if (sender is ContentWindow)
                {
                    var window = sender as ContentWindow;
                    window.Hide();
                }
            }
        }

        private static void DropBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is ContentWindow)
            {
                var window = sender as ContentWindow;
                window.Hide();
            }
        }

        public static bool ShowDropBox(bool show = true)
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

            if (box is ContentWindow)
            {
                //box.Activate();
            }
            else
            {
                //var box = new Window();
                box = new ContentWindow();
                box.MouseDown += DropBox_MouseDown;
                ///box.MouseMove += DropBox_MouseMove;
                //box.MouseDoubleClick += DropBox_MouseDoubleClick;
                box.MouseLeftButtonDown += DropBox_MouseLeftButtonDown;
                box.Width = 48;
                box.Height = 48;
                //box.Background = new SolidColorBrush(Color.FromArgb(160, 255, 255, 255));
                box.Background = new SolidColorBrush(Theme.AccentColor);
                //box.Background = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
                box.OverlayBrush = Theme.AccentBrush;
                //box.OverlayOpacity = 0.8;

                box.Opacity = 0.85;
                box.AllowsTransparency = true;
                //box.SaveWindowPosition = true;
                //box.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                box.AllowDrop = true;
                box.Topmost = true;
                box.ResizeMode = ResizeMode.NoResize;
                box.ShowInTaskbar = false;
                box.ShowIconOnTitleBar = false;
                box.ShowCloseButton = false;
                box.ShowMinButton = false;
                box.ShowMaxRestoreButton = false;
                box.ShowSystemMenuOnRightClick = false;
                box.ShowTitleBar = false;
                //box.WindowStyle = WindowStyle.None;
                box.Title = "DropBox";

                var img = new Image() { Source = box.Icon };
                img.Effect = new ThresholdEffect() { Threshold = 0.67, BlankColor = Theme.AccentColor };
                //img.Effect = new TranspranceEffect() { TransColor = Theme.AccentColor };
                //img.Effect = new TransparenceEffect() { TransColor = Color.FromRgb(0x00, 0x96, 0xfa) };
                //img.Effect = new ReplaceColorEffect() { Threshold = 0.5, SourceColor = Color.FromArgb(0xff, 0x00, 0x96, 0xfa), TargetColor = Theme.AccentColor };
                //img.Effect = new ReplaceColorEffect() { Threshold = 0.5, SourceColor = Color.FromRgb(0x00, 0x96, 0xfa), TargetColor = Colors.Transparent };
                //img.Effect = new ReplaceColorEffect() { Threshold = 0.5, SourceColor = Color.FromRgb(0x00, 0x96, 0xfa), TargetColor = Theme.AccentColor };
                //img.Effect = new ExcludeReplaceColorEffect() { Threshold = 0.05, ExcludeColor = Colors.White, TargetColor = Theme.AccentColor };
                box.Content = img;
                if (setting.DropBoxPosition != null)
                {
                    double x= setting.DropBoxPosition.X;
                    double y =setting.DropBoxPosition.Y;
                    if (x == 0 && y == 0)
                        box.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    else
                    {
                        box.Left = x;
                        box.Top = y;
                    }                        
                }
            }
            if (box.IsVisible)
            {
                box.Hide();
            }
            else
            {
                box.Show();
                box.Activate();
            }

            return (box.IsVisible);
        }        
        #endregion
    }

    public class MyNotification : Notification
    {
        //public string ImgURL { get; set; }
        //public string Message { get; set; }
        //public string Title { get; set; }
        public object Tag { get; set; }
    }

    public static class ExtensionMethods
    {
        // MakePackUri is a utility method for computing a pack uri
        // for the given resource. 
        public static Uri MakePackUri(this string relativeFile)
        {
            Assembly a = typeof(ThresholdEffect).Assembly;

            // Extract the short name.
            string assemblyShortName = a.ToString().Split(',')[0];
            string uriString = $"pack://application:,,,/{assemblyShortName};component/{relativeFile}";

            return new Uri(uriString);
        }

        public static string GetImageName(this string url, bool is_meta_single_page)
        {
            string result = string.Empty;
            if (!string.IsNullOrEmpty(url))
            {
                result = Path.GetFileName(url).Replace("_p", "_");
                if (is_meta_single_page) result = result.Replace("_0.", ".");
            }
            return (result);
        }

        public static string GetIllustId(this string url)
        {
            string result = string.Empty;
            if (!string.IsNullOrEmpty(url))
            {
                var m = Regex.Match(Path.GetFileName(url), @"(\d+)(_p\d+.*?)", RegexOptions.IgnoreCase);
                if (m.Groups.Count > 0)
                {
                    result = m.Groups[1].Value;
                }
            }
            return (result);
        }

        public static string GetImageId(this string url)
        {
            string result = string.Empty;
            if (!string.IsNullOrEmpty(url))
            {
                result = Path.GetFileNameWithoutExtension(url);
            }
            return (result);
        }

        public static string FolderMacroReplace(this string text)
        {
            var result = text;
            result = MacroReplace(result, @"%id%", text.GetIllustId());
            return (result);
        }

        public static string FolderMacroReplace(this string text, string target)
        {
            var result = text;
            result = MacroReplace(result, @"%id%", target);
            return (result);
        }

        public static string MacroReplace(this string text, string macro, string target)
        {
            return(Regex.Replace(text, macro, target, RegexOptions.IgnoreCase));
        }

        public static DependencyObject GetVisualChildFromTreePath(this DependencyObject dpo, int[] path)
        {
            if (path.Length == 0) return dpo;
            if (VisualTreeHelper.GetChildrenCount(dpo) == 0) return (dpo);
            List<int> newPath = new List<int>(path);
            newPath.RemoveAt(0);
            return VisualTreeHelper.GetChild(dpo, path[0]).GetVisualChildFromTreePath(newPath.ToArray());
        }

        public static childItem FindVisualChild<childItem>(DependencyObject obj) where childItem : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child != null && child is childItem)
                    return (childItem)child;
                else
                {
                    childItem childOfChild = FindVisualChild<childItem>(child);
                    if (childOfChild != null)
                        return childOfChild;
                }
            }
            return null;
        }

        public static childItem FindVisualChild<childItem>(this DependencyObject parent, DependencyObject obj) where childItem : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                if (child != null && child is childItem && child == obj)
                    return (childItem)child;
                else
                {
                    childItem childOfChild = child.FindVisualChild<childItem>(obj);
                    if (childOfChild != null)
                        return childOfChild;
                }
            }
            return null;
        }

        public static List<T> GetVisualChildren<T>(this DependencyObject obj) where T : DependencyObject
        {
            List<T> childList = new List<T>();
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child != null && child is T)
                    childList.Add(child as T);

                var childOfChilds = child.GetVisualChildren<T>();
                if (childOfChilds != null)
                {
                    childList.AddRange(childOfChilds);
                }
            }

            if (childList.Count > 0)
                return childList;

            return null;
        }

        public static Tuple<double, double> AspectRatio(this ImageSource image)
        {
            double bestDelta = double.MaxValue;
            double i = 1;
            int j = 1;
            double bestI = 0;
            int bestJ = 0;

            var ratio = image.Width / image.Height;

            for (int iterations = 0; iterations < 100; iterations++)
            {
                double delta = i / j - ratio;

                // Optionally, quit here if delta is "close enough" to zero
                if (delta < 0) i += 0.1;
                else if (delta == 0)
                {
                    i = 1;
                    j = 1;
                }
                else j++;

                double newDelta = Math.Abs( i / j - ratio);
                if (newDelta < bestDelta)
                {
                    bestDelta = newDelta;
                    bestI = i;
                    bestJ = j;
                }
            }
            return (new Tuple<double, double>(bestI, bestJ));
        }
    }

}
