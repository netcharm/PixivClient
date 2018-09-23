﻿using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PixivWPF.Common
{
    public enum PixivPage
    {
        None,
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
        RankingWeek,
        RankingWeekOriginal,
        RankingWeekRookie,
        RankingWeekR18,
        RankingWeekR18G,
        RankingMonth,
        RankingYear
    }

    public static class CommonHelper
    {
        private static Setting setting = Setting.Load();

        private static async void RefreshToken()
        {
            if (!string.IsNullOrEmpty(setting.User) && !string.IsNullOrEmpty(setting.Pass) && !string.IsNullOrEmpty(setting.AccessToken))
            {
                try
                {
                    var authResult = await Pixeez.Auth.AuthorizeAsync(setting.User, setting.Pass, setting.AccessToken, setting.Proxy, setting.UsingProxy);
                    setting.AccessToken = authResult.Authorize.AccessToken;
                    setting.Update = Convert.ToInt64(DateTime.Now.ToFileTime() / 10000000);
                    setting.Save();
                }
                catch (Exception ex)
                {
                    var authResult = await Pixeez.Auth.AuthorizeAsync(setting.User, setting.Pass, setting.Proxy, setting.UsingProxy);
                    setting.AccessToken = authResult.Authorize.AccessToken;
                    setting.Update = Convert.ToInt64(DateTime.Now.ToFileTime() / 10000000);
                    setting.Save();
                    var rt = ex.Message;
                }
                //var authResult = await Pixeez.Auth.AuthorizeAsync(setting.User, setting.Pass, "", setting.Proxy, setting.UsingProxy);
                //setting.AccessToken = authResult.Authorize.AccessToken;
                //setting.Update = Convert.ToInt64(DateTime.Now.ToFileTime() / 10000000);
                //setting.Save();
            }
        }

        public static async Task<Pixeez.Tokens> ShowLogin()
        {
            Pixeez.Tokens result = null;
            var accesstoken = setting.AccessToken;
            try
            {
                if (Convert.ToInt64(DateTime.Now.ToFileTime() / 10000000) - setting.Update < 3600)
                {
                    result = Pixeez.Auth.AuthorizeWithAccessToken(setting.AccessToken, setting.Proxy, setting.UsingProxy);
                    RefreshToken();
                }
                else
                {
                    if (!string.IsNullOrEmpty(setting.User) && !string.IsNullOrEmpty(setting.Pass) && !string.IsNullOrEmpty(setting.AccessToken))
                    {
                        try
                        {
                            //RefreshToken();
                            var authResult = await Pixeez.Auth.AuthorizeAsync(setting.User, setting.Pass, setting.AccessToken, setting.Proxy, setting.UsingProxy);
                            //var authResult = await Pixeez.Auth.AuthorizeAsync(setting.User, setting.Pass, "", setting.Proxy, setting.UsingProxy);
                            setting.AccessToken = authResult.Authorize.AccessToken;
                            setting.Update = Convert.ToInt64(DateTime.Now.ToFileTime() / 10000000);
                            result = authResult.Tokens;
                        }
                        catch (Exception)
                        {
                            result = Pixeez.Auth.AuthorizeWithAccessToken(setting.AccessToken, setting.Proxy, setting.UsingProxy);
                        }
                    }
                    else
                    {
                        var dlgLogin = new PixivLoginDialog() { AccessToken=accesstoken };
                        var ret = dlgLogin.ShowDialog();
                        result = dlgLogin.Tokens;
                    }
                }
            }
            catch(Exception ex)
            {
                MetroWindow window = Application.Current.MainWindow as MetroWindow;
                await window.ShowMessageAsync("ERROR", ex.Message);
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

        public static async Task<string> ToImageFile(this string url, Pixeez.Tokens tokens, bool is_meta_single_page=false)
        {
            string result = string.Empty;
            //url = Regex.Replace(url, @"//.*?\.pixiv.net/", "//i.pximg.net/", RegexOptions.IgnoreCase);
            var fn = Path.GetFileName(url).Replace("_p", "_");
            if (is_meta_single_page)
                fn = fn.Replace("_0.", ".");

            //var fn = Path.GetFileName(url).Replace("_p0.", ".").Replace("_p", "_");
            if (string.IsNullOrEmpty(setting.LastFolder))
            {
                SaveFileDialog dlgSave = new SaveFileDialog();
                dlgSave.FileName = fn;
                if (dlgSave.ShowDialog() == true)
                {
                    fn = dlgSave.FileName;
                    setting.LastFolder = Path.GetDirectoryName(fn);
                }
                else fn = string.Empty;
            }

            if (!string.IsNullOrEmpty(fn))
            {
                using (var response = await tokens.SendRequestAsync(Pixeez.MethodType.GET, url))
                {
                    if (response.Source.StatusCode == HttpStatusCode.OK)
                    {
                        using (var ms = await response.ToMemoryStream())
                        {
                            fn = Path.Combine(setting.LastFolder, Path.GetFileName(fn));
                            File.WriteAllBytes($"{fn}", ms.ToArray());
                            result = fn;
                        }
                    }
                    else result = null;
                }
            }
            return (result);
        }

        public static async Task<string> ToImageFile(this string url, Pixeez.Tokens tokens, DateTime dt, bool is_meta_single_page = false)
        {
            var file = await url.ToImageFile(tokens, is_meta_single_page);
            if (!string.IsNullOrEmpty(file))
            {
                File.SetCreationTime(file, dt);
                File.SetLastWriteTime(file, dt);
                File.SetLastAccessTime(file, dt);
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

            //await webRequest.BeginGetResponse((ar) =>
            //{
            //    var response = webRequest.EndGetResponse(ar);
            //    var stream = response.GetResponseStream();
            //    if (stream.CanRead)
            //    {
            //        byte[] buffer = new byte[response.ContentLength];
            //        stream.BeginRead(buffer, 0, buffer.Length, (aResult) =>
            //        {
            //            stream.EndRead(aResult);
            //            //File.WriteAllBytes("c:\\test.jpg", buffer);
            //            BitmapImage image = new BitmapImage();
            //            image.BeginInit();
            //            image.StreamSource = new MemoryStream(buffer);
            //            image.EndInit();
            //            image.Freeze();
            //            result = image;
            //        }, null);
            //    }
            //}, null);

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

        public static ImageSource ToImageSource(this Stream stream)
        {
            //await imgStream.GetResponseStreamAsync();
            try
            {
                //BitmapImage result = new BitmapImage();
                //result.BeginInit();
                //result.CacheOption = BitmapCacheOption.OnLoad;
                //result.StreamSource = stream;
                //result.EndInit();
                //result.Freeze();

                BitmapSource result = BitmapFrame.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                return (result);
            }
            catch (Exception)
            {
                BitmapSource result = BitmapFrame.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                return (result);
            }
            //return (result);
        }

        public static async void ShowMessageDialog(string title, string content)
        {
            MetroWindow window = Application.Current.MainWindow as MetroWindow;
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

            MetroWindow window = Application.Current.MainWindow as MetroWindow;

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
    }




}
