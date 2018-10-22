﻿using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using Notifications.Wpf;
using PixivWPF.Pages;
using Prism.Commands;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
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
        private static Setting setting = Setting.Load();
        private static CacheImage cache = new CacheImage();

        public static ICommand Cmd_OpenIllust { get; } = new DelegateCommand<object>(obj => {
            if (obj is ImageListGrid)
            {
                var list = obj as ImageListGrid;
                foreach (var item in list.SelectedItems)
                {
                    if (list.Name.Equals("RelativeIllusts", StringComparison.CurrentCultureIgnoreCase) ||
                       list.Name.Equals("FavoriteIllusts", StringComparison.CurrentCultureIgnoreCase))
                    {
                        if (item.Illust == null && item.Tag is Pixeez.Objects.User)
                            Cmd_OpenIllust.Execute(item.Tag as Pixeez.Objects.User);
                        else
                            Cmd_OpenIllust.Execute(item);
                    }
                    else
                    {
                        var page = new IllustImageViewerPage();
                        page.UpdateDetail(item);
                        var viewer = new ContentWindow();
                        viewer.Content = page;
                        viewer.Title = $"ID: {item.ID}, {item.Subject}";
                        viewer.Width = 720;
                        viewer.Height = 900;
                        viewer.Show();
                    }
                }
            }
            else if (obj is ImageItem)
            {
                var item = obj as ImageItem;
                switch (item.ItemType)
                {
                    case ImageItemType.Work:
                        Cmd_OpenIllust.Execute(item.Tag as Pixeez.Objects.Work);
                        break;
                    case ImageItemType.Page:
                    case ImageItemType.Pages:
                        var page = new IllustImageViewerPage();
                        page.UpdateDetail(item);
                        var viewer = new ContentWindow();
                        viewer.Content = page;
                        viewer.Title = $"ID: {item.ID}, {item.Subject}";
                        viewer.Width = 720;
                        viewer.Height = 900;
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
                var viewer = new ContentWindow();
                var page = new IllustDetailPage();
                var illust = obj as Pixeez.Objects.Work;

                var url = illust.GetThumbnailUrl();
                var tooltip = string.IsNullOrEmpty(illust.Caption) ? string.Empty : string.Join("", illust.Caption.InsertLineBreak(48).Take(256));
                var item = new ImageItem()
                {
                    Thumb = url,
                    Index = -1,
                    Count = (int)illust.PageCount,
                    BadgeValue = illust.PageCount.Value.ToString(),
                    BadgeVisibility = illust.PageCount > 1 ? Visibility.Visible : Visibility.Collapsed,
                    DisplayBadge = illust.PageCount > 1 ? true : false,
                    ID = illust.Id.ToString(),
                    UserID = illust.User.Id.ToString(),
                    Subject = illust.Title,
                    DisplayTitle = true,
                    Caption = illust.Caption,
                    ToolTip = tooltip,
                    Illust = illust,
                    Tag = illust
                };
                page.UpdateDetail(item);

                viewer.Title = $"ID: {illust.Id}, {illust.Title}";
                viewer.Width = 720;
                viewer.Height = 900;
                viewer.Content = page;
                viewer.Show();
            }
            else if (obj is Pixeez.Objects.User)
            {
                var viewer = new ContentWindow();
                var page = new IllustDetailPage();

                var user = obj as Pixeez.Objects.User;
                page.UpdateDetail(user);
                viewer.Title = $"User: {user.Name} / {user.Id} / {user.Account}";

                viewer.Width = 720;
                viewer.Height = 800;
                viewer.Content = page;
                viewer.Show();
            }
        });

        public static ICommand Cmd_Search { get; } = new DelegateCommand<object>(obj => {
            if (obj is string)
            {
                var content = (string)obj;

                if (Regex.IsMatch(content, @"(.*?illust_id=)(\d+)(.*)", RegexOptions.IgnoreCase))
                    content = Regex.Replace(content, @"(.*?illust_id=)(\d+)(.*)", "IllustID: $2", RegexOptions.IgnoreCase).Trim();
                else if (Regex.IsMatch(content, @"^(.*?\?id=)(\d+)(.*)$", RegexOptions.IgnoreCase))
                    content = Regex.Replace(content, @"^(.*?\?id=)(\d+)(.*)$", "UserID: $2", RegexOptions.IgnoreCase).Trim();
                else if (Regex.IsMatch(content, @"^(.*?tag_full&word=)(.*)$", RegexOptions.IgnoreCase))
                {
                    content = Regex.Replace(content, @"^(.*?tag_full&word=)(.*)$", "Tag: $2", RegexOptions.IgnoreCase).Trim();
                    content = Uri.UnescapeDataString(content);
                }
                else if (!Regex.IsMatch(content, @"((UserID)|(IllustID)|(Tag)|(Caption)|(Fuzzy)|(Fuzzy Tag)):", RegexOptions.IgnoreCase))
                {
                    content = $"Caption: {content}";
                }

                if (!string.IsNullOrEmpty(content))
                {
                    var viewer = new ContentWindow();
                    viewer.Title = $"Searching {content} ...";
                    viewer.Width = 720;
                    viewer.Height = 850;

                    var page = new SearchResultPage();
                    page.CurrentWindow = viewer;
                    page.UpdateDetail(content);

                    viewer.Content = page;
                    viewer.Show();
                }
            }
        });

        internal static DownloadManagerPage _downManager = new DownloadManagerPage();

        public static void ShowDownloadManager()
        {
            if (_downManager is Pages.DownloadManagerPage)
            {

            }
            else
                _downManager = new Pages.DownloadManagerPage();
            _downManager.AutoStart = true;

            Window _dm = null;
            foreach (Window win in Application.Current.Windows)
            {
                if (win.Content is Pages.DownloadManagerPage)
                {
                    _dm = win;
                    break;
                }
            }

            if (_dm is Window)
            {
                _dm.Show();
            }
            else
            {
                var viewer = new ContentWindow();
                viewer.Title = $"Download Manager";
                viewer.Width = 720;
                viewer.Height = 520;
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

        public static BitmapSource ConvertBitmapDPI(this BitmapSource source, double dpiX = 96, double dpiY = 96)
        {
            if (dpiX == source.DpiX || dpiY == source.DpiY) return (source);

            int width = source.PixelWidth;
            int height = source.PixelHeight;

            var palette = source.Palette;
            int stride = width * ((source.Format.BitsPerPixel + 31) / 32 * 4);
            byte[] pixelData = new byte[stride * height];
            source.CopyPixels(pixelData, stride, 0);

            BitmapSource result = null;
            using (var ms = new MemoryStream())
            {
                var nbmp = BitmapSource.Create(width, height, dpiX, dpiY, source.Format, palette, pixelData, stride);
                PngBitmapEncoder pngEnc = new PngBitmapEncoder();
                pngEnc.Frames.Add(BitmapFrame.Create(nbmp));
                pngEnc.Save(ms);
                var pngDec = new PngBitmapDecoder(ms, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                result = pngDec.Frames[0];
            }
            return result;
        }

        public static ImageSource ToImageSource(this Stream stream)
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
                    result = ConvertBitmapDPI(result, dpi.X, dpi.Y);
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

        public static async Task<ImageSource> LoadImage(this string file)
        {
            ImageSource result = null;
            if (!string.IsNullOrEmpty(file) && File.Exists(file))
            {
                using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    await Task.Run(() =>
                    {
                        result = stream.ToImageSource();
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

        public static async Task<bool> ToImageFile(this string url, Pixeez.Tokens tokens, string file, bool overwrite=true)
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

        public static async Task<string> ToImageFile(this string url, Pixeez.Tokens tokens, bool is_meta_single_page=false, bool overwrite = true)
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

        public static async Task<string> ToImageFile(this string url, Pixeez.Tokens tokens, DateTime dt, bool is_meta_single_page = false, bool overwrite = true)
        {
            var file = await url.ToImageFile(tokens, is_meta_single_page, overwrite);
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
                    var ugoira_file = await ugoira_url.ToImageFile(tokens, dt, true, overwrite);
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

        public static async Task<List<string>> ToImageFiles(Dictionary<string, DateTime> files, Pixeez.Tokens tokens, bool is_meta_single_page = false)
        {
            List<string> result = new List<string>();

            foreach (var file in files)
            {
                var f = await file.Key.ToImageFile(tokens, file.Value, is_meta_single_page);
                result.Add(f);
            }
            SystemSounds.Beep.Play();

            return (result);
        }

        public static void ToImageFile(this string url, string thumb, DateTime dt, bool is_meta_single_page = false, bool overwrite = true)
        {
            ShowDownloadManager();
            if(_downManager is Pages.DownloadManagerPage)
            {
                _downManager.Add(url, thumb, dt, is_meta_single_page, overwrite);
            }
        }

        public static void ToImageFiles(Dictionary<Tuple<string, bool>, Tuple<string, DateTime>> files, bool overwrite = true)
        {
            foreach (var file in files)
            {
                var url = file.Key.Item1;
                var is_meta_single_page =  file.Key.Item2;
                var thumb = file.Value.Item1;
                var dt = file.Value.Item2;
                url.ToImageFile(thumb, dt, is_meta_single_page, overwrite);
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
                    result = (ImageSource)await response.ToImageSource();
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
                result = (ImageSource)stream.ToImageSource();
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

        public static void ShowToast(this string content, string title = "Pixiv", string imgsrc = "")
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

            var newNotification = new Notification()
            {
                Title = title,
                ImgURL = imgsrc,
                Message = content
            };

            //_dailogService.ClearNotifications();
            _dailogService.ShowNotificationWindow(newNotification, cfg);
        }

        public static void ShowToastAsync(this string content, string title = "Pixiv", string imgsrc = "")
        {
            new Task(() =>
            {
                ShowToast(content, title, imgsrc);
            });
        }

        public static void ShowToasts(this string content, string title = "Pixiv", string file="")
        {
            var toastManager = new NotificationManager();

            var toastContent = new NotificationContent
            {
                Title = title,
                Message = content,
                Type = NotificationType.Success
            };

            toastManager.Show(toastContent, "", 
                TimeSpan.FromSeconds(30), 
                onClick:() =>
                {
                    System.Diagnostics.Process.Start(file);
                }
            );
        }

    }

    public static class ExtensionMethods
    {
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
            int i = 1;
            int j = 1;
            int bestI = 0;
            int bestJ = 0;

            var ratio = image.Width / image.Height;

            for (int iterations = 0; iterations < 100; iterations++)
            {
                double delta = (double) i / (double) j - ratio;

                // Optionally, quit here if delta is "close enough" to zero
                if (delta < 0) i++;
                else j++;

                double newDelta = Math.Abs((double) i / (double) j - ratio);
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
