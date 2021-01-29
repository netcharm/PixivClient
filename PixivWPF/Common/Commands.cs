﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

using MahApps.Metro.Controls;
using Newtonsoft.Json;
using Prism.Commands;
using PixivWPF.Pages;

namespace PixivWPF.Common
{
    #region ICommand Json Converter
    public class ICommandTypeConverter<T> : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            //assume we can convert to anything for now
            return (objectType == typeof(ICommand));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            //explicitly specify the concrete type we want to create
            return serializer.Deserialize<T>(reader);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            //use the default serialization - it works fine
            serializer.Serialize(writer, value);
        }
    }
    #endregion

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

    public static class Commands
    {
        private static Setting setting = Application.Current.LoadSetting();

        private static DownloadManagerPage _downManager_page = new DownloadManagerPage();

        private const int WIDTH_MIN = 720;
        private const int HEIGHT_MIN = 524;
        private const int HEIGHT_DEF = 900;
        private const int HEIGHT_MAX = 1024;
        private const int WIDTH_DEF = 1280;
        private const int WIDTH_PEDIA = 1024;
        private const int WIDTH_SEARCH = 710;

        private static bool IsPagesGallary(ImageListGrid gallery)
        {
            bool result = false;
            try
            {
                if (gallery.Name.Equals("SubIllusts", StringComparison.CurrentCultureIgnoreCase))
                    result = true;
            }
            catch (Exception ex) { ex.ERROR("SUBPAGES"); }
            return (result);
        }

        private static bool IsNormalGallary(ImageListGrid gallery)
        {
            bool result = false;
            try
            {
                if (gallery.Name.Equals("RelativeItems", StringComparison.CurrentCultureIgnoreCase) ||
                    gallery.Name.Equals("ResultItems", StringComparison.CurrentCultureIgnoreCase) ||
                    gallery.Name.Equals("FavoriteItems", StringComparison.CurrentCultureIgnoreCase) ||
                    gallery.Name.Equals("HistoryItems", StringComparison.CurrentCultureIgnoreCase))
                    result = true;
            }
            catch (Exception ex) { ex.ERROR("GALLARY"); }
            return (result);
        }

        public static void Invoke(this ICommand cmd, dynamic param)
        {
            Dispatcher.CurrentDispatcher.BeginInvoke(new Action(delegate
            {
                cmd.Execute(param);
            }));
        }

        public static async void InvokeAsync(this ICommand cmd, dynamic param, bool realtime = false)
        {
            await new Action(() =>
            {
                cmd.Execute(param);
            }).InvokeAsync(realtime);
        }

        public static ICommand Login { get; } = new DelegateCommand(() =>
        {
            var setting = Application.Current.LoadSetting();
            var dlgLogin = new PixivLoginDialog() { AccessToken = setting.AccessToken };
            var ret = dlgLogin.ShowDialog();
            if (ret ?? false) setting.AccessToken = dlgLogin.AccessToken;
        });

        public static ICommand CopyText { get; } = new DelegateCommand<dynamic>(obj =>
        {
            if (obj is string)
            {
                var text = obj as string;
                if (!string.IsNullOrEmpty(text))
                {
                    var data = new DataObject();
                    data.SetData(DataFormats.Text, text);
                    data.SetData(DataFormats.UnicodeText, text);
                    Clipboard.SetDataObject(data, true);
                }
            }
            else if (obj is HtmlTextData)
            {
                CopyHtml.Execute(obj);
            }
            else
            {
                if (obj != null) CopyText.Execute(obj.ToString());
            }
        });

        public static ICommand CopyHtml { get; } = new DelegateCommand<dynamic>(obj =>
        {
            try
            {
                if (obj is string)
                {
                    var text = obj as string;
                    if (!string.IsNullOrEmpty(text))
                    {
                        ClipboardHelper.CopyToClipboard(text, text);
                    }
                }
                else if (obj is HtmlTextData)
                {
                    var ht = obj as HtmlTextData;
                    ClipboardHelper.CopyToClipboard(ht.Html, ht.Text);
                }
                else
                {
                    CopyText.Execute(obj);
                }
            }
            catch (Exception ex) { ex.ERROR("COPY"); }
        });

        public static ICommand CopyArtworkIDs { get; } = new DelegateCommand<dynamic>(obj =>
        {
            var prefix = Keyboard.Modifiers == ModifierKeys.Control ? "id:" : string.Empty;
            if (obj is ImageListGrid)
            {
                var gallery = obj as ImageListGrid;
                if (IsNormalGallary(gallery))
                {
                    var ids = new  List<string>();
                    foreach (var item in gallery.GetSelected())
                    {
                        if (item.IsWork())
                        {
                            var id = $"{prefix}{item.ID}";
                            if (!ids.Contains(id)) ids.Add(id);
                        }
                    }
                    ids.Add("");
                    CopyText.Execute(string.Join(Environment.NewLine, ids));
                }
                else if (IsPagesGallary(gallery))
                {
                    var page = gallery.TryFindParent<IllustDetailPage>();
                    if (page is IllustDetailPage)
                    {
                        if (page.Contents is PixivItem && page.Contents.IsWork())
                            CopyText.Execute($"{prefix}{page.Contents.ID}");
                    }
                }
            }
            else if (obj is PixivItem)
            {
                var item = obj as PixivItem;
                if (item.IsWork())
                {
                    CopyText.Execute($"{prefix}{item.ID}");
                }
            }
            else if (obj is IEnumerable<string>)
            {
                var ids = new List<string>();
                foreach (var s in (obj as IEnumerable<string>))
                {
                    var id = $"{prefix}{s}";
                    if (!ids.Contains(id)) ids.Add(id);
                }
                CopyText.Execute(string.Join(Environment.NewLine, ids));
            }
            else if (obj is string)
            {
                var id = (obj as string).ParseLink().ParseID();
                if (!string.IsNullOrEmpty(id)) CopyText.Execute($"{prefix}{id}");
            }
        });

        public static ICommand CopyArtistIDs { get; } = new DelegateCommand<dynamic>(obj =>
        {
            var prefix = Keyboard.Modifiers == ModifierKeys.Control ? "uid:" : string.Empty;
            if (obj is ImageListGrid)
            {
                var gallery = obj as ImageListGrid;
                if (IsNormalGallary(gallery))
                {
                    var ids = new  List<string>();
                    foreach (var item in gallery.GetSelected())
                    {
                        var uid = $"{prefix}{item.UserID}";
                        if (!ids.Contains(uid)) ids.Add(uid);
                    }
                    ids.Add("");
                    CopyText.Execute(string.Join(Environment.NewLine, ids));
                }
                else if (IsPagesGallary(gallery))
                {
                    var page = gallery.TryFindParent<IllustDetailPage>();
                    if (page is IllustDetailPage)
                    {
                        if (page.Contents is PixivItem)
                            CopyText.Execute($"{prefix}{page.Contents.UserID}");
                    }
                }
            }
            else if (obj is PixivItem)
            {
                var item = obj as PixivItem;
                if (item.IsWork())
                {
                    CopyText.Execute($"{prefix}{item.UserID}");
                }
            }
            else if (obj is IEnumerable<string>)
            {
                var ids = new List<string>();
                foreach (var s in (obj as IEnumerable<string>))
                {
                    var uid = $"{prefix}{s}";
                    if (!ids.Contains(uid)) ids.Add(uid);
                }
                CopyText.Execute(string.Join(Environment.NewLine, ids));
            }
            else if (obj is string)
            {
                var id = (obj as string).ParseLink().ParseID();
                if (!string.IsNullOrEmpty(id)) CopyText.Execute($"{prefix}{id}");
            }
        });

        public static ICommand CopyArtworkWeblinks { get; } = new DelegateCommand<dynamic>(obj =>
        {
            if (obj is ImageListGrid)
            {
                var gallery = obj as ImageListGrid;
                if (IsNormalGallary(gallery))
                {
                    var ids = new  List<string>();
                    foreach (var item in gallery.GetSelected())
                    {
                        if (item.IsWork())
                        {
                            var id = $"{item.ID.ArtworkLink()}";
                            if (!ids.Contains(id)) ids.Add(id);
                        }
                    }
                    ids.Add("");
                    CopyText.Execute(string.Join(Environment.NewLine, ids));
                }
                else if (IsPagesGallary(gallery))
                {
                    var page = gallery.TryFindParent<IllustDetailPage>();
                    if (page is IllustDetailPage)
                    {
                        if (page.Contents is PixivItem && page.Contents.IsWork())
                            CopyText.Execute($"{page.Contents.ID.ArtworkLink()}");
                    }
                }
            }
            else if (obj is PixivItem)
            {
                var item = obj as PixivItem;
                if (item.IsWork())
                {
                    CopyText.Execute(item.ID.ArtworkLink());
                }
            }
            else if (obj is IEnumerable<string>)
            {
                var ids = new List<string>();
                foreach (var s in (obj as IEnumerable<string>))
                {
                    var id = s.ArtworkLink();
                    if (!ids.Contains(id)) ids.Add(id);
                }
                CopyText.Execute(string.Join(Environment.NewLine, ids));
            }
            else if (obj is string)
            {
                var id = (obj as string).ParseLink().ParseID();
                if (!string.IsNullOrEmpty(id)) CopyText.Execute($"{id.ArtworkLink()}");
            }
        });

        public static ICommand CopyArtistWeblinks { get; } = new DelegateCommand<dynamic>(obj =>
        {
            if (obj is ImageListGrid)
            {
                var gallery = obj as ImageListGrid;
                if (IsNormalGallary(gallery))
                {
                    var ids = new  List<string>();
                    foreach (var item in gallery.GetSelected())
                    {
                        var id = $"{item.UserID.ArtistLink()}";
                        if (!ids.Contains(id)) ids.Add(id);
                    }
                    ids.Add("");
                    CopyText.Execute(string.Join(Environment.NewLine, ids));
                }
                else if (IsPagesGallary(gallery))
                {
                    var page = gallery.TryFindParent<IllustDetailPage>();
                    if (page is IllustDetailPage)
                    {
                        if (page.Contents is PixivItem)
                            CopyText.Execute($"{page.Contents.UserID.ArtistLink()}");
                    }
                }
            }
            else if (obj is PixivItem)
            {
                var item = obj as PixivItem;
                CopyText.Execute(item.UserID.ArtistLink());
            }
            else if (obj is IEnumerable<string>)
            {
                var ids = new List<string>();
                foreach (var s in (obj as IEnumerable<string>))
                {
                    var id = s.ArtistLink();
                    if (!ids.Contains(id)) ids.Add(id);
                }
                CopyText.Execute(string.Join(Environment.NewLine, ids));
            }
            else if (obj is string)
            {
                var id = (obj as string).ParseLink().ParseID();
                if (!string.IsNullOrEmpty(id)) CopyText.Execute($"{id.ArtistLink()}");
            }
        });

        public static ICommand CopyDownloadInfo { get; } = new DelegateCommand<dynamic>(obj =>
        {
            if (obj is IEnumerable)
            {
                var items = obj as IList;
                if (items.Count <= 0) return;
                var sep = @"--------------------------------------------------------------------------------------------";
                var targets = new List<string>();
                targets.Add(sep);
                foreach (var item in items)
                {
                    if (item is DownloadInfo)
                    {
                        var info = (item as DownloadInfo).GetDownloadInfo();
                        if (info.Count() > 0)
                        {
                            targets.AddRange(info);
                            targets.Add(sep);
                        }
                    }
                }
                targets.Add("");
                CopyText.Execute(string.Join(Environment.NewLine, targets));
            }
            else if (obj is DownloadInfo)
            {
                CopyDownloadInfo.Execute(new List<DownloadInfo>() { obj as DownloadInfo });
            }
            else if (obj is ItemCollection)
            {
                CopyDownloadInfo.Execute(obj as IList);
            }
        });

        public static ICommand CopyPediaLink { get; } = new DelegateCommand<dynamic>(obj =>
        {
            if (obj is string)
            {
                var content = obj as string;
                var link = $"https://dic.pixiv.net/a/{content}";
                CopyText.Execute(content);
            }
            else if (obj is IEnumerable<string>)
            {
                var contents = obj as IEnumerable<string>;
                var links = new List<string>();
                foreach (var content in contents)
                {
                    links.Add($"https://dic.pixiv.net/a/{content}");
                }
                CopyText.Execute(string.Join(Environment.NewLine, links));
            }
        });

        public static ICommand CopyImage { get; } = new DelegateCommand<dynamic>(async obj =>
        {
            if (obj is string)
            {
                await new Action(() =>
                {
                    (obj as string).CopyImage();
                }).InvokeAsync(true);
            }
            else if (obj is CustomImageSource)
            {
                var img = obj as CustomImageSource;
                if (!string.IsNullOrEmpty(img.SourcePath) && File.Exists(img.SourcePath)) img.SourcePath.CopyImage();
            }
            else if (obj is Image)
            {
                var img = obj as Image;
                img.Source.CopyImage();
            }
            else if (obj is System.Windows.Media.ImageSource)
            {
                var img = obj as System.Windows.Media.ImageSource;
                img.CopyImage();
            }
            else if (obj is PixivItem)
            {
                var item = obj as PixivItem;
                if (item.IsWork())
                {
                    string fp = item.Illust.GetOriginalUrl(item.Index).GetImageCachePath();
                    if (!string.IsNullOrEmpty(fp))
                    {
                        await new Action(() =>
                        {
                            fp.CopyImage();
                        }).InvokeAsync(true);
                    }
                }
            }
            else if (obj is TilesPage)
            {
                (obj as TilesPage).CopyPreview();
            }
            else if (obj is IllustDetailPage)
            {
                (obj as IllustDetailPage).CopyPreview();
            }
            else if (obj is IllustImageViewerPage)
            {
                (obj as IllustImageViewerPage).CopyPreview();
            }
            else if (obj is Window)
            {
                var win = obj as Window;
                if (win.Content is Page) CopyImage.Execute(win.Content);
            }
        });

        public static ICommand OpenItem { get; } = new DelegateCommand<dynamic>(async obj =>
        {
            if (obj is PixivItem)
            {
                try
                {
                    var item = obj as PixivItem;
                    if (item.IsWork())
                    {
                        if (item.IsPage() || item.IsPages())
                            item.IsDownloaded = item.Illust.IsDownloadedAsync(item.Index);
                        else
                            item.IsDownloaded = item.Illust.IsPartDownloadedAsync();

                        OpenWork.Execute(item.Illust);
                    }
                    else if (item.IsUser())
                    {
                        OpenUser.Execute(item.User);
                    }
                }
                catch (Exception ex) { ex.ERROR("OPEN"); }
            }
            else if (obj is ImageListGrid)
            {
                var list = obj as ImageListGrid;
                foreach (var item in list.GetSelected())
                {
                    await new Action(() =>
                    {
                        OpenItem.Execute(item);
                    }).InvokeAsync();
                }
            }
        });

        public static ICommand OpenWork { get; } = new DelegateCommand<dynamic>(async obj =>
        {
            try
            {
                if (obj is Pixeez.Objects.Work)
                {
                    var illust = obj as Pixeez.Objects.Work;
                    var i = illust.Id.FindIllust();
                    if (i is Pixeez.Objects.Work) illust = i;

                    var title = $"ID: {illust.Id}, {illust.Title}";
                    if (await title.ActiveByTitle()) return;

                    await new Action(async () =>
                    {
                        var item = illust.WorkItem();
                        if (item is PixivItem)
                        {
                            var page = new IllustDetailPage() { FontFamily = setting.FontFamily, Contents = item };
                            var viewer = new ContentWindow()
                            {
                                Title = title,
                                Width = WIDTH_MIN,
                                Height = HEIGHT_DEF,
                                MinWidth = WIDTH_MIN,
                                MinHeight = HEIGHT_MIN,
                                FontFamily = setting.FontFamily,
                                Content = page
                            };
                            viewer.Show();
                            await Task.Delay(1);
                            Application.Current.DoEvents();
                        }
                    }).InvokeAsync();
                }
                else if (obj is PixivItem)
                {
                    var item = obj as PixivItem;
                    if (item.IsWork())
                        OpenWork.Execute(item.Illust);
                }
                else if (obj is ImageListGrid)
                {
                    var gallery = obj as ImageListGrid;
                    foreach (var item in gallery.GetSelected())
                    {
                        await new Action(() =>
                        {
                            OpenWork.Execute(item);
                        }).InvokeAsync();
                    }
                }
                else if (obj is IllustDetailPage)
                {
                    (obj as IllustDetailPage).OpenInNewWindow();
                }
                else if (obj is SearchResultPage)
                {
                    (obj as SearchResultPage).OpenWork();
                }
                else if (obj is HistoryPage)
                {
                    (obj as HistoryPage).OpenWork();
                }
                else if (obj is Window)
                {
                    var win = obj as Window;
                    if (win.Content is Page) OpenWork.Execute(win.Content);
                }
            }
            catch (Exception ex)
            {
                ex.Message.ShowMessageBox("ERROR[ILLUST]");
            }
        });

        public static ICommand OpenWorkPreview { get; } = new DelegateCommand<dynamic>(async obj =>
        {
            try
            {
                if (obj is PixivItem && (obj as PixivItem).IsWork())
                {
                    var item = obj as PixivItem;
                    if (item.IsPage() || item.IsPages())
                        item.IsDownloaded = item.Illust.IsDownloadedAsync(item.Index);
                    else
                        item.IsDownloaded = item.Illust.IsPartDownloadedAsync();

                    var suffix = item.Count > 1 ? $" - {item.Index}/{item.Count}" : string.Empty;
                    var title = $"Preview ID: {item.ID}, {item.Subject}";
                    if (await title.ActiveByTitle()) return;

                    await new Action(async () =>
                    {
                        var page = new IllustImageViewerPage() { FontFamily = setting.FontFamily, Contents = item };
                        var viewer = new ContentWindow()
                        {
                            Title = $"{title}",
                            Width = WIDTH_MIN,
                            Height = HEIGHT_DEF,
                            MinWidth = WIDTH_MIN,
                            MinHeight = HEIGHT_MIN,
                            FontFamily = setting.FontFamily,
                            Content = page
                        };
                        viewer.Show();
                        await Task.Delay(1);
                        Application.Current.DoEvents();
                    }).InvokeAsync();
                }
                else if (obj is ImageListGrid)
                {
                    var gallery = obj as ImageListGrid;
                    foreach (var item in gallery.GetSelected())
                    {
                        await new Action(() =>
                        {
                            OpenWorkPreview.Execute(item);
                        }).InvokeAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                ex.Message.ShowMessageBox("ERROR[PREVIEW]");
            }
        });

        public static ICommand OpenUser { get; } = new DelegateCommand<dynamic>(async obj =>
        {
            try
            {
                if (obj is Pixeez.Objects.UserBase)
                {
                    var user = obj as Pixeez.Objects.UserBase;
                    var u = user.Id.FindUser();
                    if (u is Pixeez.Objects.UserBase) user = u;

                    var title = $"User: {user.Name} / {user.Id} / {user.Account}";
                    if (await title.ActiveByTitle()) return;

                    await new Action(async () =>
                    {
                        var page = new IllustDetailPage() { FontFamily = setting.FontFamily, Contents = user.UserItem() };
                        var viewer = new ContentWindow()
                        {
                            Title = title,
                            Width = WIDTH_MIN,
                            Height = HEIGHT_DEF,
                            MinWidth = WIDTH_MIN,
                            MinHeight = HEIGHT_MIN,
                            FontFamily = setting.FontFamily,
                            Content = page
                        };
                        viewer.Show();
                        await Task.Delay(1);
                        Application.Current.DoEvents();
                    }).InvokeAsync();
                }
                else if (obj is PixivItem)
                {
                    var item = obj as PixivItem;
                    if (item.IsWork() || item.IsUser())
                        OpenUser.Execute(item.User);
                }
                else if (obj is ImageListGrid)
                {
                    await new Action(async () =>
                    {
                        var gallery = obj as ImageListGrid;
                        foreach (var item in gallery.GetSelected())
                        {
                            await new Action(() =>
                            {
                                OpenUser.Execute(item);
                            }).InvokeAsync();
                        }
                    }).InvokeAsync();
                }
                else if (obj is IllustDetailPage)
                {
                    (obj as IllustDetailPage).OpenUser();
                }
                else if (obj is SearchResultPage)
                {
                    (obj as SearchResultPage).OpenUser();
                }
                else if (obj is HistoryPage)
                {
                    (obj as HistoryPage).OpenUser();
                }
                else if (obj is Window)
                {
                    var win = obj as Window;
                    if (win.Content is Page) OpenUser.Execute(win.Content);
                }
            }
            catch (Exception ex)
            {
                ex.Message.ShowMessageBox("ERROR[USER]");
            }
        });

        public static ICommand OpenGallery { get; } = new DelegateCommand<dynamic>(obj =>
        {
            if (obj is ImageListGrid)
            {
                var gallery = obj as ImageListGrid;
                if (IsNormalGallary(gallery))
                {
                    OpenItem.Execute(gallery);
                }
                else if (IsPagesGallary(gallery))
                {
                    OpenWorkPreview.Execute(gallery);
                }
            }
        });

        public static ICommand OpenDownloaded { get; } = new DelegateCommand<dynamic>(async obj =>
        {
            try
            {
                if (obj is PixivItem)
                {
                    var item = obj as PixivItem;
                    if (item.IsWork())
                    {
                        var illust = item.Illust;

                        string fp = string.Empty;
                        if (item.Count > 1)
                        {
                            if (item.IsPage() || item.IsPages())
                                illust.IsDownloadedAsync(out fp, item.Index);
                            else
                                illust.IsPartDownloadedAsync(out fp);
                        }
                        else
                        {
                            illust.IsPartDownloadedAsync(out fp);
                        }
                        if (string.IsNullOrEmpty(fp))
                            OpenWorkPreview.Execute(item);
                        else
                            fp.OpenFileWithShell();
                    }
                }
                else if (obj is ImageListGrid)
                {
                    await new Action(async () =>
                    {
                        var gallery = obj as ImageListGrid;
                        foreach (var item in gallery.GetSelected())
                        {
                            await new Action(() =>
                            {
                                OpenDownloaded.Execute(item);
                            }).InvokeAsync();
                        }
                    }).InvokeAsync();
                }
                else if (obj is IList<PixivItem>)
                {
                    await new Action(async () =>
                    {
                        var gallery = obj as IList<PixivItem>;
                        foreach (var item in gallery)
                        {
                            await new Action(() =>
                            {
                                OpenDownloaded.Execute(item);
                            }).InvokeAsync();
                        }
                    }).InvokeAsync();
                }
                else if (obj is TilesPage)
                {
                    (obj as TilesPage).OpenIllust();
                }
                else if (obj is IllustDetailPage)
                {
                    (obj as IllustDetailPage).OpenIllust();
                }
                else if (obj is IllustImageViewerPage)
                {
                    (obj as IllustImageViewerPage).OpenIllust();
                }
                else if (obj is SearchResultPage)
                {
                    (obj as SearchResultPage).OpenIllust();
                }
                else if (obj is HistoryPage)
                {
                    (obj as HistoryPage).OpenIllust();
                }
                else if (obj is Window)
                {
                    var win = obj as Window;
                    if (win.Content is Page) OpenDownloaded.Execute(win.Content);
                }
            }
            catch (Exception ex)
            {
                ex.Message.ShowMessageBox("ERROR[DOWNLOADED]");
            }
        });

        public static ICommand OpenCachedImage { get; } = new DelegateCommand<dynamic>(async obj =>
        {
            try
            {
                if (obj is CustomImageSource)
                {
                    var img = obj as CustomImageSource;
                    if (!string.IsNullOrEmpty(img.SourcePath) && File.Exists(img.SourcePath)) ShellOpenFile.Execute(img.SourcePath);
                }
                else if (obj is PixivItem)
                {
                    var item = obj as PixivItem;
                    if (item.IsWork())
                    {
                        var illust = item.Illust;

                        if (item.Index >= 0)
                        {
                            string fp = string.Empty;
                            item.IsDownloaded = illust.IsDownloadedAsync(out fp, item.Index);
                            string fp_d = item.IsDownloaded ? fp : string.Empty;
                            string fp_o = illust.GetOriginalUrl(item.Index).GetImageCachePath();
                            string fp_p = illust.GetPreviewUrl(item.Index).GetImageCachePath();

                            if (File.Exists(fp_d)) ShellOpenFile.Execute(fp_d);
                            else if (File.Exists(fp_o)) ShellOpenFile.Execute(fp_o);
                            else if (File.Exists(fp_p)) ShellOpenFile.Execute(fp_p);
                        }
                        else
                        {
                            string fp = string.Empty;
                            item.IsDownloaded = illust.IsPartDownloadedAsync(out fp);
                            string fp_d = item.IsDownloaded ? fp : string.Empty;
                            string fp_o = illust.GetOriginalUrl().GetImageCachePath();
                            string fp_p = illust.GetPreviewUrl().GetImageCachePath();

                            if (File.Exists(fp_d)) ShellOpenFile.Execute(fp_d);
                            else if (File.Exists(fp_o)) ShellOpenFile.Execute(fp_o);
                            else if (File.Exists(fp_p)) ShellOpenFile.Execute(fp_p);
                        }
                    }
                }
                else if (obj is string)
                {
                    try
                    {
                        string s = obj as string;
                        Uri url = null;
                        if (!string.IsNullOrEmpty(s) && Uri.TryCreate(s, UriKind.RelativeOrAbsolute, out url)) ShellOpenFile.Execute(url);
                    }
                    catch (Exception ex) { ex.ERROR("CACHEDIMAGE"); }
                }
                else if (obj is ImageListGrid)
                {
                    await new Action(async () =>
                    {
                        var gallery = obj as ImageListGrid;
                        foreach (var item in gallery.GetSelected())
                        {
                            await new Action(() =>
                            {
                                OpenCachedImage.Execute(item);
                            }).InvokeAsync();
                        }
                    }).InvokeAsync();
                }
                else if (obj is IList<PixivItem>)
                {
                    await new Action(async () =>
                    {
                        var gallery = obj as IList<PixivItem>;
                        foreach (var item in gallery)
                        {
                            await new Action(() =>
                            {
                                OpenCachedImage.Execute(item);
                            }).InvokeAsync();
                        }
                    }).InvokeAsync();
                }
                else if (obj is TilesPage)
                {
                    (obj as TilesPage).OpenCachedImage();
                }
                else if (obj is IllustDetailPage)
                {
                    (obj as IllustDetailPage).OpenCachedImage();
                }
                else if (obj is IllustImageViewerPage)
                {
                    (obj as IllustImageViewerPage).OpenCachedImage();
                }
                else if (obj is SearchResultPage)
                {
                    (obj as SearchResultPage).OpenCachedImage();
                }
                else if (obj is HistoryPage)
                {
                    (obj as HistoryPage).OpenCachedImage();
                }
                else if (obj is Window)
                {
                    var win = obj as Window;
                    if (win.Content is Page) OpenCachedImage.Execute(win.Content);
                }
            }
            catch (Exception ex)
            {
                ex.Message.ShowMessageBox("ERROR[DOWNLOADED]");
            }
        });

        public static ICommand OpenHistory { get; } = new DelegateCommand(async () =>
        {
            try
            {
                var title = $"History";
                if (await title.ActiveByTitle()) return;

                await new Action(async () =>
                {
                    var page = new HistoryPage() { FontFamily = setting.FontFamily };
                    var viewer = new ContentWindow()
                    {
                        Title = title,
                        Width = WIDTH_SEARCH,
                        Height = HEIGHT_DEF,
                        MinWidth = WIDTH_SEARCH,
                        MinHeight = HEIGHT_MIN,
                        MaxWidth = WIDTH_MIN + 16,
                        FontFamily = setting.FontFamily,
                        Content = page
                    };
                    viewer.Show();
                    await Task.Delay(1);
                    Application.Current.DoEvents();
                }).InvokeAsync();
            }
            catch (Exception ex)
            {
                ex.Message.ShowMessageBox("ERROR[HISTORY]");
            }
        });

        public static ICommand AddToHistory { get; } = new DelegateCommand<dynamic>(async obj =>
        {
            if (obj is Pixeez.Objects.Work || obj is Pixeez.Objects.User || obj is Pixeez.Objects.UserBase)
            {
                try
                {
                    await new Action(() =>
                    {
                        var win = "History".GetWindowByTitle();
                        if (win is ContentWindow && win.Content is HistoryPage)
                            (win.Content as HistoryPage).AddToHistory(obj);
                        else
                        {
                            if (obj is Pixeez.Objects.Work) Application.Current.HistoryAdd(obj as Pixeez.Objects.Work);
                            else if (obj is Pixeez.Objects.User) Application.Current.HistoryAdd(obj as Pixeez.Objects.User);
                            else if (obj is Pixeez.Objects.UserBase) Application.Current.HistoryAdd(obj as Pixeez.Objects.UserBase);
                        }
                        Application.Current.DoEvents();
                    }).InvokeAsync();
                }
                catch (Exception ex)
                {
                    ex.Message.ShowMessageBox("ERROR[HISTORY]");
                }
            }
        });

        public static ICommand Open { get; } = new DelegateCommand<dynamic>(obj =>
        {
            if (obj is ImageListGrid)
            {
                OpenGallery.Execute(obj);
            }
            else if (obj is PixivItem)
            {
                OpenItem.Execute(obj);
            }
            else if (obj is Pixeez.Objects.Work)
            {
                OpenWork.Execute(obj);
            }
            else if (obj is Pixeez.Objects.UserBase)
            {
                OpenUser.Execute(obj);
            }
            else if (obj is TilesPage)
            {
                (obj as TilesPage).OpenIllust();
            }
            else if (obj is IllustDetailPage)
            {
                (obj as IllustDetailPage).OpenIllust();
            }
            else if (obj is IllustImageViewerPage)
            {
                (obj as IllustImageViewerPage).OpenIllust();
            }
            else if (obj is SearchResultPage)
            {
                (obj as SearchResultPage).OpenIllust();
            }
            else if (obj is HistoryPage)
            {
                (obj as HistoryPage).OpenIllust();
            }
            else if (obj is Window)
            {
                var win = obj as Window;
                if(win.Content is Page) Open.Execute(win.Content);
            }
            else if (obj is string)
            {
                OpenSearch.Execute(obj as string);
            }
        });

        public static ICommand AddDownloadItem { get; } = new DelegateCommand<dynamic>(async obj =>
        {
            await new Action(() =>
            {
                OpenDownloadManager.Execute(false);
                if (_downManager_page is DownloadManagerPage && obj is DownloadParams)
                {
                    var dp = obj as DownloadParams;
                    _downManager_page.Add(dp.Url, dp.ThumbUrl, dp.Timestamp, dp.IsSinglePage, dp.OverwriteExists);
                }
            }).InvokeAsync();
        });

        private static SemaphoreSlim CanOpenDownloadManager= new SemaphoreSlim(1, 1);
        public static ICommand OpenDownloadManager { get; } = new DelegateCommand<dynamic>(async obj =>
        {
            if (Mouse.RightButton == MouseButtonState.Pressed)
            {
                if (_downManager_page is DownloadManagerPage)
                {
                    await new Action(() =>
                    {
                        CopyDownloadInfo.Execute(_downManager_page.GetDownloadInfo());
                    }).InvokeAsync(true);
                }
            }
            else if (await CanOpenDownloadManager.WaitAsync(TimeSpan.FromSeconds(60)))
            {
                try
                {
                    if (obj is bool)
                    {
                        var active = (bool)obj;

                        var title = $"Download Manager";
                        if (active ? await title.ActiveByTitle() : await title.ShowByTitle()) return;

                        await new Action(() =>
                        {
                            if (!(_downManager_page is DownloadManagerPage))
                                _downManager_page = new DownloadManagerPage() { AutoStart = true };

                            setting = Application.Current.LoadSetting();
                            var viewer = new ContentWindow()
                            {
                                Title = title,
                                MinWidth = WIDTH_MIN + 80,
                                MinHeight = HEIGHT_MIN,
                                Width = setting.DownloadManagerPosition.Width <= WIDTH_MIN + 80 ? WIDTH_MIN + 80 : setting.DownloadManagerPosition.Width,
                                Height = setting.DownloadManagerPosition.Height <= HEIGHT_MIN ? HEIGHT_MIN : setting.DownloadManagerPosition.Height,
                                Left = setting.DownloadManagerPosition.Left >=0 ? setting.DownloadManagerPosition.Left : _downManager_page.Pos.X,
                                Top = setting.DownloadManagerPosition.Top >=0 ? setting.DownloadManagerPosition.Top : _downManager_page.Pos.Y,
                                FontFamily = setting.FontFamily,
                                Content = _downManager_page
                            };
                            _downManager_page.Window = viewer;
                            viewer.Show();
                        }).InvokeAsync(true);
                    }
                }
                catch (Exception ex) { ex.ERROR("DOWNLOADMANAGER"); }
                finally
                {
                    if (CanOpenDownloadManager is SemaphoreSlim && CanOpenDownloadManager.CurrentCount <= 0) CanOpenDownloadManager.Release();
                }
            }
        });

        public static ICommand OpenSearch { get; } = new DelegateCommand<dynamic>(async obj =>
        {
            if (obj is string && !string.IsNullOrEmpty((string)obj))
            {
                var content = CommonHelper.ParseLink((string)obj);
                if (!string.IsNullOrEmpty(content))
                {
                    if (content.StartsWith("IllustID:", StringComparison.CurrentCultureIgnoreCase))
                    {
                        var illust = content.ParseID().FindIllust();
                        if (illust is Pixeez.Objects.Work)
                        {
                            Open.Execute(illust);
                            return;
                        }
                    }
                    else if (content.StartsWith("UserID:", StringComparison.CurrentCultureIgnoreCase))
                    {
                        var user = content.ParseID().FindUser();
                        if (user is Pixeez.Objects.UserBase)
                        {
                            OpenUser.Execute(user);
                            return;
                        }
                    }

                    var title = $"Searching {content} ...";
                    if (await title.ActiveByTitle()) return;

                    await new Action(async () =>
                    {
                        var page = new SearchResultPage() { FontFamily = setting.FontFamily, Contents = content };
                        var viewer = new ContentWindow()
                        {
                            Title = title,
                            Width = WIDTH_SEARCH,
                            Height = HEIGHT_DEF,
                            MinWidth = WIDTH_SEARCH,
                            MinHeight = HEIGHT_MIN,
                            MaxHeight = HEIGHT_MAX,
                            MaxWidth = WIDTH_MIN + 16,
                            FontFamily = setting.FontFamily,
                            Content = page
                        };
                        viewer.Show();
                        await Task.Delay(1);
                        Application.Current.DoEvents();
                    }).InvokeAsync();
                }
            }
            else if (obj is IEnumerable<string>)
            {
                await new Action(async () =>
                {
                    foreach (var link in obj as IEnumerable<string>)
                    {
                        try
                        {
                            await new Action(() =>
                            {
                                OpenSearch.Execute(link);
                            }).InvokeAsync();
                        }
                        catch (Exception ex) { ex.ERROR("SEARCH"); }
                    }
                }).InvokeAsync();
            }
        });

        public static ICommand SaveIllust { get; } = new DelegateCommand<dynamic>(async obj =>
        {
            if (obj is PixivItem)
            {
                await new Action(() =>
                {
                    var item = obj as PixivItem;
                    if (item.IsWork())
                    {
                        var dt = item.Illust.GetDateTime();
                        var is_meta_single_page = item.Illust.PageCount == 1 ? true : false;
                        if (item.IsPage() || item.IsPages())
                        {
                            var url = item.Illust.GetOriginalUrl(item.Index);
                            if (!string.IsNullOrEmpty(url))
                            {
                                url.SaveImage(item.Illust.GetThumbnailUrl(item.Index), dt, is_meta_single_page);
                            }
                        }
                        else if (item.Illust is Pixeez.Objects.Work)
                        {
                            var url = item.Illust.GetOriginalUrl(item.Index);
                            if (!string.IsNullOrEmpty(url))
                            {
                                url.SaveImage(item.Illust.GetThumbnailUrl(item.Index), dt, is_meta_single_page);
                            }
                        }
                    }
                }).InvokeAsync();
            }
            else if (obj is ImageListGrid)
            {
                setting = Application.Current.LoadSetting();
                await new Action(async () =>
                {
                    var gallery = obj as ImageListGrid;
                    foreach (var item in gallery.GetSelected())
                    {
                        await new Action(() =>
                        {
                            SaveIllust.Execute(item);
                        }).InvokeAsync();
                    }
                }).InvokeAsync();
            }
            else if (obj is string)
            {
                var link = obj as string;
                var patten = @"(https?://.*?\.pximg\.net/img-original/img/\d{4}/\d{2}/\d{2}/\d{2}/\d{2}/\d{2}/)?(\d+)(_p?(\d+))?\..*?$";
                var id = Regex.Replace(link, patten, "$2", RegexOptions.IgnoreCase);
                var index = Regex.Replace(link, patten, "$4", RegexOptions.IgnoreCase);
                if (!string.IsNullOrEmpty(id))
                {
                    var illust = id.FindIllust();
                    if (!(illust is Pixeez.Objects.Work)) illust = await id.RefreshIllust();
                    if (illust is Pixeez.Objects.Work)
                    {
                        var item = illust.WorkItem();
                        int idx = item.Index;
                        int.TryParse(index, out idx);
                        item.Index = idx;
                        SaveIllust.Execute(item);
                    }
                }
            }
            else if (obj is TilesPage)
            {
                (obj as TilesPage).SaveIllust();
            }
            else if (obj is IllustDetailPage)
            {
                (obj as IllustDetailPage).SaveIllust();
            }
            else if (obj is IllustImageViewerPage)
            {
                (obj as IllustImageViewerPage).SaveIllust();
            }
            else if (obj is SearchResultPage)
            {
                (obj as SearchResultPage).SaveIllust();
            }
            else if (obj is HistoryPage)
            {
                (obj as HistoryPage).SaveIllust();
            }
            else if (obj is Window)
            {
                var win = obj as Window;
                if (win.Content is Page) SaveIllust.Execute(win.Content);
            }
        });

        public static ICommand SaveIllustAll { get; } = new DelegateCommand<dynamic>(async obj =>
        {
            if (obj is PixivItem)
            {
                await new Action(async () =>
                {
                    var item = obj as PixivItem;
                    if (item.IsWork())
                    {
                        var illust = item.Illust;
                        var dt = illust.GetDateTime();
                        var is_meta_single_page = illust.PageCount==1 ? true : false;

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
                                illust = await illust.RefreshIllust();
                                if (illust.Metadata != null && illust.Metadata.Pages != null)
                                {
                                    illust.Cache();
                                    foreach (var p in illust.Metadata.Pages)
                                    {
                                        var u = p.GetOriginalUrl();
                                        u.SaveImage(p.GetThumbnailUrl(), dt, is_meta_single_page);
                                    }
                                }
                            }
                        }
                    }
                }).InvokeAsync();
            }
            else if (obj is ImageListGrid)
            {
                setting = Application.Current.LoadSetting();
                await new Action(async () =>
                {
                    var gallery = obj as ImageListGrid;
                    foreach (var item in gallery.GetSelected())
                    {
                        await new Action(() =>
                        {
                            SaveIllustAll.Execute(item);
                        }).InvokeAsync();
                    }
                }).InvokeAsync();
            }
            else if (obj is string)
            {
                var link = obj as string;
                var patten = @"(https?://.*?\.pximg\.net/img-original/img/\d{4}/\d{2}/\d{2}/\d{2}/\d{2}/\d{2}/)?(\d+)(_p?(\d+))?\..*?$";
                var id = Regex.Replace(link, patten, "$2", RegexOptions.IgnoreCase);
                if (!string.IsNullOrEmpty(id))
                {
                    var illust = id.FindIllust();
                    if (!(illust is Pixeez.Objects.Work)) illust = await id.RefreshIllust();
                    if (illust is Pixeez.Objects.Work)
                    {
                        var item = illust.WorkItem();
                        SaveIllustAll.Execute(item);
                    }
                }
            }
            else if (obj is TilesPage)
            {
                (obj as TilesPage).SaveIllustAll();
            }
            else if (obj is IllustDetailPage)
            {
                (obj as IllustDetailPage).SaveIllustAll();
            }
            else if (obj is IllustImageViewerPage)
            {
                (obj as IllustImageViewerPage).SaveIllustAll();
            }
            else if (obj is SearchResultPage)
            {
                (obj as SearchResultPage).SaveIllustAll();
            }
            else if (obj is HistoryPage)
            {
                (obj as HistoryPage).SaveIllustAll();
            }
            else if (obj is Window)
            {
                var win = obj as Window;
                if (win.Content is Page) SaveIllustAll.Execute(win.Content);
            }
        });

        public static ICommand OpenDropBox { get; } = new DelegateCommand<dynamic>(async obj =>
        {
            if (obj is System.Windows.Controls.Primitives.ToggleButton)
            {
                var sender = obj as System.Windows.Controls.Primitives.ToggleButton;
                await new Action(() =>
                {
                    if (Keyboard.Modifiers == ModifierKeys.Control || Mouse.RightButton == MouseButtonState.Pressed)
                    {
                        IList<string> titles = Application.Current.OpenedWindowTitles();
                        if (titles.Count > 0) CopyText.Execute($"{string.Join(Environment.NewLine, titles)}{Environment.NewLine}");
                    }
                    else if (Keyboard.Modifiers == ModifierKeys.Shift)
                    {
                        SaveOpenedWindows.Execute(null);
                    }
                    else if (Keyboard.Modifiers == ModifierKeys.Alt)
                    {
                        LoadLastOpenedWindows.Execute(null);
                    }
                    else if (Keyboard.Modifiers == ModifierKeys.None)
                        CommonHelper.SetDropBoxState(true.ShowDropBox());
                }).InvokeAsync(true);
            }
        });

        public static ICommand OpenDragDrop { get; } = new DelegateCommand<IEnumerable<string>>(obj =>
        {
            if (obj is IEnumerable<string>)
            {
                OpenSearch.Execute(obj);
            }
        });

        public static ICommand SendToOtherInstance { get; } = new DelegateCommand<dynamic>(async obj =>
        {
            if (obj is string)
            {
                var content = obj as string;
                if (!string.IsNullOrEmpty(content))
                {
                    await new Action(async () =>
                    {
                        CommonHelper.SendToOtherInstance(content);
                        await Task.Delay(1);
                        Application.Current.DoEvents();
                    }).InvokeAsync();
                }
            }
            else if (obj is IEnumerable<string>)
            {
                var content = (obj as IEnumerable<string>).ToArray();
                if (content.Count() > 0)
                {
                    await new Action(async () =>
                    {
                        CommonHelper.SendToOtherInstance(content);
                        await Task.Delay(1);
                        Application.Current.DoEvents();
                    }).InvokeAsync();
                }
            }
            else if (obj is Pixeez.Objects.Work)
            {
                SendToOtherInstance.Execute($"id:{(obj as Pixeez.Objects.Work).Id}");
            }
            else if (obj is Pixeez.Objects.UserBase)
            {
                SendToOtherInstance.Execute($"uid:{(obj as Pixeez.Objects.UserBase).Id}");
            }
            else if (obj is PixivItem)
            {
                var item = obj as PixivItem;
                if (item.IsWork()) SendToOtherInstance.Execute(item.Illust);
                else if (item.IsUser()) SendToOtherInstance.Execute(item.User);
            }
            else if (obj is ImageListGrid)
            {
                var gallery = obj as ImageListGrid;
                if (IsNormalGallary(gallery))
                {
                    var ids = new  List<string>();
                    foreach (var item in gallery.GetSelected())
                    {
                        if (item.IsUser())
                        {
                            var uid = $"uid:{item.ID}";
                            if (!ids.Contains(uid)) ids.Add(uid);
                        }
                        else if (item.IsWork())
                        {
                            var id = $"id:{item.ID}";
                            if (!ids.Contains(id)) ids.Add(id);
                        }
                    }
                    SendToOtherInstance.Execute(ids);
                }
                else if (IsPagesGallary(gallery))
                {
                    var page = gallery.TryFindParent<IllustDetailPage>();
                    if (page is IllustDetailPage)
                    {
                        if (page.Contents is PixivItem && page.Contents.IsWork())
                            SendToOtherInstance.Execute(page.Contents);
                    }
                }
            }
        });

        public static ICommand ShellSendToOtherInstance { get; } = new DelegateCommand<dynamic>(async obj =>
        {
            if (obj is string)
            {
                var content = obj as string;
                if (!string.IsNullOrEmpty(content))
                {
                    await new Action(async () =>
                    {
                        CommonHelper.ShellSendToOtherInstance(content);
                        await Task.Delay(1);
                        Application.Current.DoEvents();
                    }).InvokeAsync();
                }
            }
            else if (obj is IEnumerable<string>)
            {
                var content = (obj as IEnumerable<string>).ToArray();
                if (content.Count() > 0)
                {
                    await new Action(async () =>
                    {
                        CommonHelper.ShellSendToOtherInstance(content);
                        await Task.Delay(1);
                        Application.Current.DoEvents();
                    }).InvokeAsync();
                }
            }
            else if (obj is Pixeez.Objects.Work)
            {
                ShellSendToOtherInstance.Execute($"id:{(obj as Pixeez.Objects.Work).Id}");
            }
            else if (obj is Pixeez.Objects.UserBase)
            {
                ShellSendToOtherInstance.Execute($"uid:{(obj as Pixeez.Objects.UserBase).Id}");
            }
            else if (obj is PixivItem)
            {
                var item = obj as PixivItem;
                if (item.IsUser()) ShellSendToOtherInstance.Execute(item.User);
                else if (item.IsWork()) ShellSendToOtherInstance.Execute(item.Illust);
            }
            else if (obj is ImageListGrid)
            {
                var gallery = obj as ImageListGrid;
                if (IsNormalGallary(gallery))
                {
                    var ids = new  List<string>();
                    foreach (var item in gallery.GetSelected())
                    {
                        if (item.IsUser()) ids.Add($"uid:{item.ID}");
                        else if (item.IsWork()) ids.Add($"id:{item.ID}");
                    }
                    ShellSendToOtherInstance.Execute(ids);
                }
                else if (IsPagesGallary(gallery))
                {
                    var page = gallery.TryFindParent<IllustDetailPage>();
                    if (page is IllustDetailPage)
                    {
                        if (page.Contents is PixivItem && page.Contents.IsWork())
                            ShellSendToOtherInstance.Execute(page.Contents);
                    }
                }
            }
        });

        public static ICommand OpenPedia { get; } = new DelegateCommand<dynamic>(async obj =>
        {
            if (obj is string)
            {
                var content = obj as string;
                await new Action(() =>
                {
                    OpenPediaWindow(content);
                }).InvokeAsync();
            }
            else if (obj is IEnumerable<string>)
            {
                await new Action(async () =>
                {
                    var texts = obj as IEnumerable<string>;
                    foreach (var text in texts)
                    {
                        await new Action(() =>
                        {
                            OpenPedia.Execute(text);
                        }).InvokeAsync();
                    }
                }).InvokeAsync();
            }
        });

        public static ICommand ShellOpenPixivPedia { get; } = new DelegateCommand<dynamic>(async obj =>
        {
            if (obj is string)
            {
                var content = obj as string;
                if (!string.IsNullOrEmpty(content))
                {
                    await new Action(() =>
                    {
                        content.ShellOpenPixivPedia();
                    }).InvokeAsync();
                }
            }
            else if (obj is IEnumerable<string>)
            {
                await new Action(async () =>
                {
                    var texts = obj as IEnumerable<string>;
                    foreach (var text in texts)
                    {
                        await new Action(() =>
                        {
                            ShellOpenPixivPedia.Execute(text);
                        }).InvokeAsync();
                    }
                }).InvokeAsync();
            }
        });

        public static ICommand ShellOpenFile { get; } = new DelegateCommand<dynamic>(async obj =>
        {
            if (obj is string)
            {
                var content = obj as string;
                if (!string.IsNullOrEmpty(content))
                {
                    await new Action(() =>
                    {
                        content.OpenFileWithShell();
                    }).InvokeAsync(true);
                }
            }
            else if (obj is Uri)
            {
                try
                {
                    var url = obj as Uri;
                    if ((url.IsFile || url.IsUnc) && File.Exists(url.LocalPath)) url.LocalPath.OpenFileWithShell();
                    else if (url.IsAbsoluteUri)
                    {
                        string fp_d = Uri.UnescapeDataString(url.AbsoluteUri).GetImageCachePath();
                        if (File.Exists(fp_d)) fp_d.OpenFileWithShell();
                    }
                }
                catch (Exception ex) { ex.ERROR("SHELL"); }
            }
            else if (obj is PixivItem)
            {
                var item = obj as PixivItem;
                string fp = item.Illust.GetOriginalUrl(item.Index).GetImageCachePath();
                if (!string.IsNullOrEmpty(fp))
                {
                    await new Action(() =>
                    {
                        fp.OpenFileWithShell();
                    }).InvokeAsync(true);
                }
            }
        });

        public static ICommand SaveTags { get; } = new DelegateCommand(() =>
        {
            Application.Current.SaveTags();
        });

        private static DateTime lastSaveOpenedWindows = setting.LastOpenedFile.GetFileTime();
        public static ICommand SaveOpenedWindows { get; } = new DelegateCommand<bool?>(async obj =>
        {
            var force = obj is bool ? (bool)obj : false;
            await new Action(() =>
            {
                try
                {
                    setting = Application.Current.LoadSetting();
                    var now = DateTime.Now;
                    //if (setting.LastOpenedFile.GetFileTime().DeltaSeconds(now) > setting.LastOpenedFileAutoSaveFrequency)
                    if (force || now.DeltaSeconds(lastSaveOpenedWindows) > setting.LastOpenedFileAutoSaveFrequency)
                    {
                        IList<string> titles = Application.Current.OpenedWindowTitles();
                        if (titles.Count > 0)
                        {
                            var links = JsonConvert.SerializeObject(titles, Formatting.Indented);
                            File.WriteAllText(setting.LastOpenedFile, links, new UTF8Encoding(true));
                            lastSaveOpenedWindows = now;
                        }
                    }
                }
                catch (Exception ex) { ex.ERROR("SAVEOPENED"); }
            }).InvokeAsync();
        });

        public static ICommand LoadLastOpenedWindows { get; } = new DelegateCommand(async () =>
        {
            await new Action(() =>
            {
                try
                {
                    setting = Application.Current.LoadSetting();
                    var opened = File.ReadAllText(setting.LastOpenedFile);
                    IList<string> titles = JsonConvert.DeserializeObject<IList<string>>(opened);
                    if (titles.Count > 0)
                    {
                        var links = string.Join(Environment.NewLine, titles).ParseLinks();
                        OpenSearch.Execute(links);
                    }
                }
                catch (Exception ex) { ex.ERROR("LOADLASTOPENED"); }
            }).InvokeAsync();
        });

        public static ICommand Speech { get; } = new DelegateCommand<dynamic>(obj =>
        {
            if (obj is string)
            {
                var content = obj as string;
                if (!string.IsNullOrEmpty(content))
                {
                    content.Play();
                }
            }
        });

        public static ICommand WriteLogs { get; } = new DelegateCommand<string>(obj =>
        {
            if (obj is string)
            {
                var content = obj as string;
                if (!string.IsNullOrEmpty(content))
                {
                    content.INFO();
                }
            }
        });

        public static ICommand OpenLogs { get; } = new DelegateCommand<string>(async obj =>
        {
            setting = Application.Current.LoadSetting();
            var logs = Application.Current.GetLogs();

            if (obj is string && !string.IsNullOrEmpty(obj as string))
            {
                var content = obj as string;
                foreach (var log in logs)
                {
                    if (log.ToLower().Contains(content.ToLower()))
                    {
                        await new Action(() =>
                        {
                            log.OpenFileWithShell(command: setting.ShellLogViewer);
                        }).InvokeAsync(true);
                        break;
                    }
                }
            }
            else
            {
                foreach (var log in logs)
                {                    
                    await new Action(() =>
                    {
                        log.OpenFileWithShell(command: setting.ShellLogViewer);
                    }).InvokeAsync(true);
                }
            }
        });

        #region tiles navigation
        public static ICommand RefreshPage { get; } = new DelegateCommand<dynamic>(obj =>
        {
            if (obj is TilesPage)
            {
                (obj as TilesPage).UpdateTiles();
            }
            else if (obj is IllustDetailPage)
            {
                var page = obj as IllustDetailPage;
                if (page.Contents is PixivItem) page.UpdateDetail(page.Contents);
            }
            else if (obj is IllustImageViewerPage)
            {
                var page = obj as IllustImageViewerPage;
                if (page.Contents is PixivItem) page.UpdateDetail(page.Contents);
            }
            else if (obj is SearchResultPage)
            {
                var page = obj as SearchResultPage;
                if (page.Contents is string) page.UpdateDetail(page.Contents);
            }
            else if (obj is HistoryPage)
            {
                var page = obj as HistoryPage;
                page.UpdateDetail();
            }
            else if (obj is Window)
            {
                var win = obj as Window;
                if (win.Content is Page) RefreshPage.Execute(win.Content);
            }
        });

        public static ICommand RefreshPageThumb { get; } = new DelegateCommand<dynamic>(obj =>
        {
            if (obj is TilesPage)
            {
                (obj as TilesPage).UpdateTilesThumb();
            }
            else if (obj is IllustDetailPage)
            {
                var page = obj as IllustDetailPage;
                page.UpdateThumb(true);
            }
            else if (obj is IllustImageViewerPage)
            {
                var overwrite = Keyboard.Modifiers == ModifierKeys.Alt ? true : false;
                var page = obj as IllustImageViewerPage;
                if (page.Contents is PixivItem) page.UpdateDetail(page.Contents, overwrite);
            }
            else if (obj is SearchResultPage)
            {
                var page = obj as SearchResultPage;
                page.UpdateThumb();
            }
            else if (obj is HistoryPage)
            {
                var page = obj as HistoryPage;
                page.UpdateThumb();
            }
            else if (obj is BrowerPage)
            {
                var page = obj as BrowerPage;
                page.UpdateDetail(page.Contents);
            }
            else if (obj is Window)
            {
                var win = obj as Window;
                if (win.Content is Page) RefreshPageThumb.Execute(win.Content);
            }
        });

        public static ICommand AppendTiles { get; } = new DelegateCommand<dynamic>(obj =>
        {
            if (obj is TilesPage)
            {
                (obj as TilesPage).AppendTiles();
            }
            else if (obj is Window)
            {
                var win = obj as Window;
                if (win.Content is Page) AppendTiles.Execute(win.Content);
            }
        });

        public static ICommand ScrollPageFirst { get; } = new DelegateCommand<dynamic>(obj =>
        {
            if (obj is TilesPage)
            {
                (obj as TilesPage).ScrollPageFirst();
            }
            else if (obj is SearchResultPage)
            {
                (obj as SearchResultPage).ScrollPageFirst();
            }
            else if (obj is HistoryPage)
            {
                (obj as HistoryPage).ScrollPageFirst();
            }
            else if (obj is Window)
            {
                var win = obj as Window;
                if (win.Content is Page) ScrollPageFirst.Execute(win.Content);
            }
        });

        public static ICommand ScrollPageLast { get; } = new DelegateCommand<dynamic>(obj =>
        {
            if (obj is TilesPage)
            {
                (obj as TilesPage).ScrollPageLast();
            }
            else if (obj is SearchResultPage)
            {
                (obj as SearchResultPage).ScrollPageLast();
            }
            else if (obj is HistoryPage)
            {
                (obj as HistoryPage).ScrollPageLast();
            }
            else if (obj is Window)
            {
                var win = obj as Window;
                if (win.Content is Page) ScrollPageLast.Execute(win.Content);
            }
        });

        public static ICommand ScrollPageUp { get; } = new DelegateCommand<dynamic>(obj =>
        {
            if (obj is TilesPage)
            {
                (obj as TilesPage).ScrollPageUp();
            }
            else if (obj is SearchResultPage)
            {
                (obj as SearchResultPage).ScrollPageUp();
            }
            else if (obj is HistoryPage)
            {
                (obj as HistoryPage).ScrollPageUp();
            }
            else if (obj is Window)
            {
                var win = obj as Window;
                if (win.Content is Page) ScrollPageUp.Execute(win.Content);
            }
        });

        public static ICommand ScrollPageDown { get; } = new DelegateCommand<dynamic>(obj =>
        {
            if (obj is TilesPage)
            {
                (obj as TilesPage).ScrollPageDown();
            }
            else if (obj is SearchResultPage)
            {
                (obj as SearchResultPage).ScrollPageDown();
            }
            else if (obj is HistoryPage)
            {
                (obj as HistoryPage).ScrollPageDown();
            }
            else if (obj is Window)
            {
                var win = obj as Window;
                if (win.Content is Page) ScrollPageDown.Execute(win.Content);
            }
        });

        public static ICommand PrevIllust { get; } = new DelegateCommand<dynamic>(obj =>
        {
            if (obj is TilesPage)
            {
                (obj as TilesPage).PrevIllust();
            }
            else if (obj is IllustDetailPage)
            {
                (obj as IllustDetailPage).PrevIllust();
            }
            else if (obj is IllustImageViewerPage)
            {
                (obj as IllustImageViewerPage).PrevIllust();
            }
            else if (obj is SearchResultPage)
            {
                (obj as SearchResultPage).PrevIllust();
            }
            else if (obj is HistoryPage)
            {
                (obj as HistoryPage).PrevIllust();
            }
            else if (obj is Window)
            {
                var win = obj as Window;
                if (win.Content is Page) PrevIllust.Execute(win.Content);
            }
        });

        public static ICommand NextIllust { get; } = new DelegateCommand<dynamic>(obj =>
        {
            if (obj is TilesPage)
            {
                (obj as TilesPage).NextIllust();
            }
            else if (obj is IllustDetailPage)
            {
                (obj as IllustDetailPage).NextIllust();
            }
            else if (obj is IllustImageViewerPage)
            {
                (obj as IllustImageViewerPage).NextIllust();
            }
            else if (obj is SearchResultPage)
            {
                (obj as SearchResultPage).NextIllust();
            }
            else if (obj is HistoryPage)
            {
                (obj as HistoryPage).NextIllust();
            }
            else if (obj is Window)
            {
                var win = obj as Window;
                if (win.Content is Page) NextIllust.Execute(win.Content);
            }
        });

        public static ICommand FirstIllust { get; } = new DelegateCommand<dynamic>(obj =>
        {
            if (obj is TilesPage)
            {
                (obj as TilesPage).FirstIllust();
            }
            else if (obj is IllustDetailPage)
            {
                (obj as IllustDetailPage).FirstIllust();
            }
            else if (obj is IllustImageViewerPage)
            {
                (obj as IllustImageViewerPage).FirstIllust();
            }
            else if (obj is SearchResultPage)
            {
                (obj as SearchResultPage).FirstIllust();
            }
            else if (obj is HistoryPage)
            {
                (obj as HistoryPage).FirstIllust();
            }
            else if (obj is Window)
            {
                var win = obj as Window;
                if (win.Content is Page) FirstIllust.Execute(win.Content);
            }
        });

        public static ICommand LastIllust { get; } = new DelegateCommand<dynamic>(obj =>
        {
            if (obj is TilesPage)
            {
                (obj as TilesPage).LastIllust();
            }
            else if (obj is IllustDetailPage)
            {
                (obj as IllustDetailPage).LastIllust();
            }
            else if (obj is IllustImageViewerPage)
            {
                (obj as IllustImageViewerPage).LastIllust();
            }
            else if (obj is SearchResultPage)
            {
                (obj as SearchResultPage).LastIllust();
            }
            else if (obj is HistoryPage)
            {
                (obj as HistoryPage).LastIllust();
            }
            else if (obj is Window)
            {
                var win = obj as Window;
                if (win.Content is Page) LastIllust.Execute(win.Content);
            }
        });

        public static ICommand PrevIllustPage { get; } = new DelegateCommand<dynamic>(obj =>
        {
            if (obj is TilesPage)
            {
                (obj as TilesPage).PrevIllustPage();
            }
            else if (obj is IllustDetailPage)
            {
                (obj as IllustDetailPage).PrevIllustPage();
            }
            else if (obj is IllustImageViewerPage)
            {
                (obj as IllustImageViewerPage).PrevIllustPage();
            }
            else if (obj is SearchResultPage)
            {
                (obj as SearchResultPage).PrevIllust();
            }
            else if (obj is HistoryPage)
            {
                (obj as HistoryPage).PrevIllust();
            }
            else if (obj is Window)
            {
                var win = obj as Window;
                if (win.Content is Page) PrevIllustPage.Execute(win.Content);
            }
        });

        public static ICommand NextIllustPage { get; } = new DelegateCommand<dynamic>(obj =>
        {
            if (obj is TilesPage)
            {
                (obj as TilesPage).NextIllustPage();
            }
            else if (obj is IllustDetailPage)
            {
                (obj as IllustDetailPage).NextIllustPage();
            }
            else if (obj is IllustImageViewerPage)
            {
                (obj as IllustImageViewerPage).NextIllustPage();
            }
            else if (obj is SearchResultPage)
            {
                (obj as SearchResultPage).NextIllust();
            }
            else if (obj is HistoryPage)
            {
                (obj as HistoryPage).NextIllust();
            }
            else if (obj is Window)
            {
                var win = obj as Window;
                if (win.Content is Page) NextIllustPage.Execute(win.Content);
            }
        });
        #endregion

        #region Like/Unlile Work/User relative
        public static ICommand LikeIllust { get; } = new DelegateCommand<dynamic>(async obj =>
        {
            try
            {
                setting = Application.Current.LoadSetting();
                var pub = setting.PrivateBookmarkPrefer ? false : true;

                if (obj is PixivItem)
                {
                    var item = obj as PixivItem;
                    await item.LikeIllust(pub);
                }
                else if (obj is ImageListGrid)
                {
                    var gallery = obj as ImageListGrid;
                    gallery.GetSelected().LikeIllust(pub);
                }
                else if (obj is IList<PixivItem>)
                {
                    var gallery = obj as IList<PixivItem>;
                    gallery.LikeIllust(pub);
                }
            }
            catch (Exception ex)
            {
                ex.Message.ShowMessageBox("ERROR[DOWNLOADED]");
            }
        });

        public static ICommand UnLikeIllust { get; } = new DelegateCommand<dynamic>(async obj =>
        {
            try
            {
                if (obj is PixivItem)
                {
                    var item = obj as PixivItem;
                    await item.UnLikeIllust();
                }
                else if (obj is ImageListGrid)
                {
                    var gallery = obj as ImageListGrid;
                    gallery.GetSelected().UnLikeIllust();
                }
                else if (obj is IList<PixivItem>)
                {
                    var gallery = obj as IList<PixivItem>;
                    gallery.UnLikeIllust();
                }
            }
            catch (Exception ex)
            {
                ex.Message.ShowMessageBox("ERROR[DOWNLOADED]");
            }
        });

        public static ICommand ChangeIllustLikeState { get; } = new DelegateCommand<dynamic>(async obj =>
        {
            try
            {
                setting = Application.Current.LoadSetting();
                var pub = setting.PrivateFavPrefer ? false : true;
                var toggle = setting.ToggleFavBookmarkState;

                if (obj is PixivItem)
                {
                    var item = obj as PixivItem;
                    if (toggle)
                    {
                        await item.ToggleLikeIllust(pub);
                    }
                    else
                    {
                        if (Keyboard.Modifiers == ModifierKeys.None)
                            await item.LikeIllust(pub);
                        else if (Keyboard.Modifiers == ModifierKeys.Shift)
                            await item.LikeIllust(!pub);
                        else if (Keyboard.Modifiers == ModifierKeys.Alt)
                            await item.UnLikeIllust();
                    }
                }
                else if (obj is ImageListGrid)
                {
                    var gallery = obj as ImageListGrid;
                    if (toggle)
                    {
                        gallery.GetSelected().ToggleLikeIllust(pub);
                    }
                    else
                    {
                        if (Keyboard.Modifiers == ModifierKeys.None)
                            gallery.GetSelected().LikeIllust(pub);
                        else if (Keyboard.Modifiers == ModifierKeys.Shift)
                            gallery.GetSelected().LikeIllust(!pub);
                        else if (Keyboard.Modifiers == ModifierKeys.Alt)
                            gallery.GetSelected().UnLikeIllust();
                    }
                }
                else if (obj is IList<PixivItem>)
                {
                    var gallery = obj as IList<PixivItem>;
                    if (toggle)
                    {
                        gallery.ToggleLikeIllust(pub);
                    }
                    else
                    {
                        if (Keyboard.Modifiers == ModifierKeys.None)
                            gallery.LikeIllust(pub);
                        else if (Keyboard.Modifiers == ModifierKeys.Shift)
                            gallery.LikeIllust(!pub);
                        else if (Keyboard.Modifiers == ModifierKeys.Alt)
                            gallery.UnLikeIllust();
                    }
                }
                else if (obj is TilesPage)
                {
                    ChangeIllustLikeState.Execute((obj as TilesPage).CurrentItem);
                }
                else if (obj is IllustDetailPage)
                {
                    (obj as IllustDetailPage).ChangeIllustLikeState();
                }
                else if (obj is IllustImageViewerPage)
                {
                    (obj as IllustImageViewerPage).ChangeIllustLikeState();
                }
                else if (obj is HistoryPage)
                {
                    (obj as IllustDetailPage).ChangeIllustLikeState();
                }
                else if (obj is SearchResultPage)
                {
                    (obj as SearchResultPage).ChangeIllustLikeState();
                }
                else if (obj is Window)
                {
                    var win = obj as Window;
                    if (win.Content is Page) ChangeIllustLikeState.Execute(win.Content);
                }
            }
            catch (Exception ex)
            {
                ex.Message.ShowMessageBox("ERROR[DOWNLOADED]");
            }
        });

        public static ICommand LikeUser { get; } = new DelegateCommand<dynamic>(async obj =>
        {
            try
            {
                setting = Application.Current.LoadSetting();
                var pub = setting.PrivateFavPrefer ? false : true;

                if (obj is PixivItem)
                {
                    var item = obj as PixivItem;
                    await item.LikeUser(pub);
                }
                else if (obj is ImageListGrid)
                {
                    var gallery = obj as ImageListGrid;
                    gallery.GetSelected().LikeUser(pub);
                }
                else if (obj is IList<PixivItem>)
                {
                    var gallery = obj as IList<PixivItem>;
                    gallery.LikeUser(pub);
                }
            }
            catch (Exception ex)
            {
                ex.Message.ShowMessageBox("ERROR[DOWNLOADED]");
            }
        });

        public static ICommand UnLikeUser { get; } = new DelegateCommand<dynamic>(async obj =>
        {
            try
            {
                if (obj is PixivItem)
                {
                    var item = obj as PixivItem;
                    await item.UnLikeUser();
                }
                else if (obj is ImageListGrid)
                {
                    var gallery = obj as ImageListGrid;
                    gallery.GetSelected().UnLikeUser();
                }
                else if (obj is IList<PixivItem>)
                {
                    var gallery = obj as IList<PixivItem>;
                    gallery.UnLikeUser();
                }
            }
            catch (Exception ex)
            {
                ex.Message.ShowMessageBox("ERROR[DOWNLOADED]");
            }
        });

        public static ICommand ChangeUserLikeState { get; } = new DelegateCommand<dynamic>(async obj =>
        {
            try
            {
                setting = Application.Current.LoadSetting();
                var pub = setting.PrivateFavPrefer ? false : true;
                var toggle = setting.ToggleFavBookmarkState;

                if (obj is PixivItem)
                {
                    var item = obj as PixivItem;
                    if (toggle)
                    {
                        await item.ToggleLikeUser(pub);
                    }
                    else
                    {
                        if (Keyboard.Modifiers == ModifierKeys.None)
                            await item.LikeUser(pub);
                        else if (Keyboard.Modifiers == ModifierKeys.Shift)
                            await item.LikeUser(!pub);
                        else if (Keyboard.Modifiers == ModifierKeys.Alt)
                            await item.UnLikeUser();
                    }
                }
                else if (obj is ImageListGrid)
                {
                    var gallery = obj as ImageListGrid;
                    if (toggle)
                    {
                        gallery.GetSelected().ToggleLikeUser(pub);
                    }
                    else
                    {
                        if (Keyboard.Modifiers == ModifierKeys.None)
                            gallery.GetSelected().LikeUser(pub);
                        else if (Keyboard.Modifiers == ModifierKeys.Shift)
                            gallery.GetSelected().LikeUser(!pub);
                        else if (Keyboard.Modifiers == ModifierKeys.Alt)
                            gallery.GetSelected().UnLikeUser();
                    }
                }
                else if (obj is IList<PixivItem>)
                {
                    var gallery = obj as IList<PixivItem>;
                    if (toggle)
                    {
                        gallery.ToggleLikeUser(pub);
                    }
                    else
                    {
                        if (Keyboard.Modifiers == ModifierKeys.None)
                            gallery.LikeUser(pub);
                        else if (Keyboard.Modifiers == ModifierKeys.Shift)
                            gallery.LikeUser(!pub);
                        else if (Keyboard.Modifiers == ModifierKeys.Alt)
                            gallery.UnLikeUser();
                    }
                }
                else if (obj is TilesPage)
                {
                    ChangeUserLikeState.Execute((obj as TilesPage).CurrentItem);
                }
                else if (obj is IllustDetailPage)
                {
                    (obj as IllustDetailPage).ChangeUserLikeState();
                }
                else if (obj is IllustImageViewerPage)
                {
                    (obj as IllustImageViewerPage).ChangeUserLikeState();
                }
                else if (obj is HistoryPage)
                {
                    (obj as IllustDetailPage).ChangeUserLikeState();
                }
                else if (obj is SearchResultPage)
                {
                    (obj as SearchResultPage).ChangeUserLikeState();
                }
                else if (obj is Window)
                {
                    var win = obj as Window;
                    if (win.Content is Page) ChangeUserLikeState.Execute(win.Content);
                }
            }
            catch (Exception ex)
            {
                ex.Message.ShowMessageBox("ERROR[DOWNLOADED]");
            }
        });
        #endregion

        #region PixivPedia relative
        private static async void OpenPediaWindow(string contents)
        {
            if (!string.IsNullOrEmpty(contents))
            {
                if (contents.ToLower().Contains("://dic.pixiv.net/a/"))
                    contents = Uri.UnescapeDataString(contents.Substring(contents.IndexOf("/a/") + 3));
                var title = $"PixivPedia: {contents} ...";
                if (await title.ActiveByTitle()) return;

                var page = new BrowerPage () { Contents = contents };
                var viewer = new ContentWindow()
                {
                    Title = title,
                    Width = WIDTH_PEDIA,
                    MinWidth = WIDTH_PEDIA,
                    Height = HEIGHT_DEF,
                    FontFamily = setting.FontFamily,
                    Content = page
                };
                viewer.Show();
                await Task.Delay(1);
                Application.Current.DoEvents();
            }
        }
        #endregion
    }
}
