﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Media;
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

using PixivWPF.Common;
using MahApps.Metro.IconPacks;
using TheArtOfDev.HtmlRenderer.Core.Entities;
using TheArtOfDev.HtmlRenderer.WPF;
using Prism.Commands;
using System.IO;

namespace PixivWPF.Pages
{
    /// <summary>
    /// IllustDetailPage.xaml 的交互逻辑
    /// </summary>
    public partial class IllustDetailPage : Page
    {
        internal object DataObject = null;
        internal Task lastTask = null;
        internal CancellationTokenSource cancelTokenSource;
        internal CancellationToken cancelToken;

        internal Task update_preview_task = null;
        internal CancellationTokenSource cancelUpdatePreviewTokenSource;
        internal CancellationToken cancelUpdatePreviewToken;

        public ICommand MouseDoubleClickCommand { get; } = new DelegateCommand<object>(obj => {
            MessageBox.Show("");
        });

        public void UpdateTheme()
        {
            //var fonts = string.Join(",", IllustTitle.FontFamily.FamilyNames.Values);

            var style = new StringBuilder();
            //style.AppendLine($"*{{font-family:FontAwesome, \"Segoe UI Emoji\", \"Segoe MDL2 Assets\", Monaco, Consolas, \"Courier New\", \"Segoe UI\", monospace, 思源黑体, 思源宋体, 微软雅黑, 宋体, 黑体, 楷体, \"WenQuanYi Microhei\", \"WenQuanYi Microhei Mono\", \"Microsoft YaHei\", Tahoma, Arial, Helvetica, sans-serif;}}");
            style.AppendLine($"*{{font-family:FontAwesome, \"Segoe UI Emoji\", \"Segoe MDL2 Assets\", Consolas, \"Courier New\", \"Segoe UI\", 思源黑体, 思源宋体, 微软雅黑, 宋体, 黑体, 楷体, Tahoma, Arial, Helvetica, sans-serif |important;}}");
            style.AppendLine($"body{{font-family:FontAwesome, \"Segoe UI Emoji\", \"Segoe MDL2 Assets\", Consolas, \"Courier New\", \"Segoe UI\", 思源黑体, 思源宋体, 微软雅黑, 宋体, 黑体, 楷体, Tahoma, Arial, Helvetica, sans-serif |important;}}");
            style.AppendLine($".tag{{background-color:{Common.Theme.AccentColor.ToHtml(false)}|important;color:{Common.Theme.TextColor.ToHtml(false)}|important;margin:4px;text-decoration:none;}}");
            style.AppendLine($".desc{{color:{Common.Theme.TextColor.ToHtml(false)} !important;text-decoration:none !important;}}");
            style.AppendLine($"a{{color:{Common.Theme.TextColor.ToHtml(false)}|important;text-decoration:none !important;}}");
            style.AppendLine($"img{{width:auto!important;;height:auto!important;;max-width:100%!important;;max-height:100% !important;}}");

            var BaseStyleSheet = string.Join("\n", style);
            IllustTags.BaseStylesheet = BaseStyleSheet;
            IllustDesc.BaseStylesheet = BaseStyleSheet;
            //IllustDesc.FontFamily = IllustTitle.FontFamily;

            var tags = IllustTags.Text;
            var desc = IllustDesc.Text;

            IllustTags.Text = string.Empty;
            IllustDesc.Text = string.Empty;

            IllustTags.Text = tags;
            IllustDesc.Text = desc;

            IllustDesc.AvoidImagesLateLoading = true;
        }

        public async void UpdateDetailIllust(ImageItem item)
        {
            try
            {
                IllustDetailWait.Show();

                var tokens = await CommonHelper.ShowLogin();
                DataObject = item;

                PreviewViewer.Show(true);
                PreviewBox.Show();
                PreviewBox.ToolTip = item.ToolTip;

                var dpi = new DPI();
                Preview.Source = new WriteableBitmap(300, 300, dpi.X, dpi.Y, PixelFormats.Bgra32, BitmapPalettes.WebPalette);
                PreviewWait.Show();

                string stat_viewed = "????";
                string stat_favorited = "????";
                var stat_tip = new List<string>();
                if (item.Illust is Pixeez.Objects.IllustWork)
                {
                    var illust = item.Illust as Pixeez.Objects.IllustWork;
                    stat_viewed = $"{illust.total_view}";
                    stat_favorited = $"{illust.total_bookmarks}";
                    stat_tip.Add($"Viewed    : {illust.total_view}");
                    stat_tip.Add($"Favorited : {illust.total_bookmarks}");
                }
                if (item.Illust.Stats != null)
                {
                    stat_viewed = $"{item.Illust.Stats.ViewsCount}";
                    stat_favorited = $"{item.Illust.Stats.FavoritedCount.Public} / {item.Illust.Stats.FavoritedCount.Private}";
                    stat_tip.Add($"Scores    : {item.Illust.Stats.Score}");
                    stat_tip.Add($"Viewed    : {item.Illust.Stats.ViewsCount}");
                    stat_tip.Add($"Scored    : {item.Illust.Stats.ScoredCount}");
                    stat_tip.Add($"Comments  : {item.Illust.Stats.CommentedCount}");
                    stat_tip.Add($"Favorited : {item.Illust.Stats.FavoritedCount.Public} / {item.Illust.Stats.FavoritedCount.Private}");
                }
                stat_tip.Add($"Size      : {item.Illust.Width}x{item.Illust.Height}");

                if (item.IsDownloaded)
                {
                    IllustDownloaded.Show();
                    string fp = string.Empty;
                    //item.Illust.GetOriginalUrl(item.Index).IsDownloaded(out fp, item.Illust.PageCount <= 1);
                    item.Illust.GetOriginalUrl().IsPartDownloaded(out fp);
                    IllustDownloaded.Tag = fp;
                    ToolTipService.SetToolTip(IllustDownloaded, fp);
                }
                else
                {
                    IllustDownloaded.Hide();
                    IllustDownloaded.Tag = null;
                    ToolTipService.SetToolTip(IllustDownloaded, null);
                }                

                IllustSize.Text = $"{item.Illust.Width}x{item.Illust.Height}";
                IllustViewed.Text = stat_viewed;
                IllustFavorited.Text = stat_favorited;

                IllustStatInfo.Show();
                IllustStatInfo.ToolTip = string.Join("\r", stat_tip).Trim();

                IllustAuthor.Text = item.Illust.User.Name;
                IllustAuthorAvator.Source = new WriteableBitmap(64, 64, dpi.X, dpi.Y, PixelFormats.Bgra32, BitmapPalettes.WebPalette);
                IllustAuthorAvatorWait.Show();

                IllustTitle.Text = $"{item.Illust.Title}";

                IllustDate.Text = item.Illust.GetDateTime().ToString("yyyy-MM-dd HH:mm:ss");
                IllustDateInfo.ToolTip = IllustDate.Text;
                IllustDateInfo.Show();

                ActionCopyIllustDate.Header = item.Illust.GetDateTime().ToString("yyyy-MM-dd HH:mm:sszzz");

                FollowAuthor.Show();
                if (item.Illust.User.is_followed == true ||
                    (item.Illust.User is Pixeez.Objects.User && (item.Illust.User as Pixeez.Objects.User).IsFollowing == true))
                {
                    FollowAuthor.Tag = PackIconModernKind.Check;// "Check";
                    ActionFollowAuthorRemove.IsEnabled = true;
                }
                else
                {
                    FollowAuthor.Tag = PackIconModernKind.Add;// "Add";
                    ActionFollowAuthorRemove.IsEnabled = false;
                }

                BookmarkIllust.Show();
                if (item.Illust.IsBookMarked())
                {
                    BookmarkIllust.Tag = PackIconModernKind.Heart;// "Heart";
                    ActionBookmarkIllustRemove.IsEnabled = true;
                }
                else
                {
                    BookmarkIllust.Tag = PackIconModernKind.HeartOutline;// "HeartOutline";
                    ActionBookmarkIllustRemove.IsEnabled = false;
                }

                IllustActions.Show();

                if (item.Illust.Tags.Count > 0)
                {
                    var html = new StringBuilder();
                    foreach (var tag in item.Illust.Tags)
                    {
                        //html.AppendLine($"<a href=\"https://www.pixiv.net/search.php?s_mode=s_tag_full&word={Uri.EscapeDataString(tag)}\" class=\"tag\" data-tag=\"{tag}\">{tag}</a>");
                        html.AppendLine($"<a href=\"https://www.pixiv.net/search.php?s_mode=s_tag&word={Uri.EscapeDataString(tag)}\" class=\"tag\" data-tag=\"{tag}\">{tag}</a>");
                        //html.AppendLine($"<button class=\"tag\" data-tag=\"{tag}\">{tag}</button>");
                    }
                    IllustTags.Foreground = Theme.TextBrush;
                    IllustTags.Text = string.Join(";", html);
                    IllustTagExpander.Header = "Tags";
                    IllustTagExpander.Show();
                }
                else
                {
                    IllustTagExpander.Hide();
                }

                if (!string.IsNullOrEmpty(item.Illust.Caption) && item.Illust.Caption.Length > 0)
                {
                    IllustDesc.Text = $"<div class=\"desc\">{item.Illust.Caption.HtmlDecodeFix()}</div>".Replace("\r\n", "<br/>");
                    IllustDescExpander.Show();
                }
                else
                {
                    IllustDescExpander.Hide();
                }

                SubIllusts.Items.Clear();
                //SubIllusts.Refresh();
                SubIllustsExpander.IsExpanded = false;
                PreviewBadge.Badge = item.Illust.PageCount;
                if (item.Illust is Pixeez.Objects.IllustWork && item.Illust.PageCount > 1)
                {
                    PreviewBadge.Show();
                    SubIllustsExpander.Show();
                    SubIllustsNavPanel.Show();
                    SubIllustsExpander.IsExpanded = true;
                }
                //else if (item.Illust is Pixeez.Objects.NormalWork && item.Illust.Metadata != null && item.Illust.PageCount > 1)
                else if (item.Illust is Pixeez.Objects.NormalWork && item.Illust.PageCount > 1)
                {
                    PreviewBadge.Show();
                    SubIllustsExpander.Show();
                    SubIllustsNavPanel.Show();
                    SubIllustsExpander.IsExpanded = true;
                }
                else
                {
                    PreviewBadge.Hide();
                    SubIllustsExpander.Hide();
                    SubIllustsNavPanel.Hide();
                }

                RelativeIllustsExpander.Header = "Related Illusts";
                RelativeIllustsExpander.IsExpanded = false;
                RelativeIllustsExpander.Show();
                RelativeNextPage.Hide();

                FavoriteIllustsExpander.Header = "Author Favorite";
                FavoriteIllustsExpander.IsExpanded = false;
                FavoriteIllustsExpander.Show();
                FavoriteNextPage.Hide();

                CommentsExpander.IsExpanded = false;
                CommentsExpander.Show();
                CommentsNavigator.Hide();

                if (cancelToken.IsCancellationRequested)
                {
                    cancelToken.ThrowIfCancellationRequested();
                    return;
                }
                IllustAuthorAvator.Source = await item.Illust.User.GetAvatarUrl().LoadImage(tokens);
                if (IllustAuthorAvator.Source != null)
                {
                    IllustAuthorAvatorWait.Hide();
                }

                var img = await item.Illust.GetPreviewUrl().LoadImage(tokens);
                if (cancelToken.IsCancellationRequested)
                {
                    cancelToken.ThrowIfCancellationRequested();
                    return;
                }
                if (img == null || img.Width < 350)
                {
                    if (cancelToken.IsCancellationRequested)
                    {
                        cancelToken.ThrowIfCancellationRequested();
                        return;
                    }
                    var large = await item.Illust.GetOriginalUrl().LoadImage(tokens);
                    if (cancelToken.IsCancellationRequested)
                    {
                        cancelToken.ThrowIfCancellationRequested();
                        return;
                    }
                    if (large != null) Preview.Source = large;
                }
                else
                    Preview.Source = img;

                if (Preview.Source != null) 
                {
                    Preview.Show();
                    PreviewWait.Hide();
                }

                IllustDetailWait.Hide();
            }
            catch (Exception ex)
            {
                ex.Message.ShowMessageBox("ERROR");
            }
            finally
            {
                IllustDetailWait.Hide();
            }
        }

        public async void UpdateDetail(ImageItem item)
        {
            try
            {
                if (lastTask is Task)
                {
                    cancelToken.ThrowIfCancellationRequested();
                    lastTask.Wait();
                }

                if (lastTask == null || (lastTask is Task && (lastTask.IsCanceled || lastTask.IsCompleted || lastTask.IsFaulted)))
                {
                    lastTask = new Task(() =>
                    {
                        UpdateDetailIllust(item);
                    }, cancelTokenSource.Token, TaskCreationOptions.None);
                    lastTask.RunSynchronously();
                    //lastTask.Start();
                    await lastTask;
                }
            }
            catch (Exception ex)
            {
                ex.Message.ShowMessageBox("ERROR");
            }
            finally
            {
                IllustDetailWait.Hide();
            }
        }

        public async void UpdateDetail(Pixeez.Objects.UserBase user)
        {
            try
            {
                IllustDetailWait.Show();

                var tokens = await CommonHelper.ShowLogin();

                var UserInfo = await tokens.GetUserInfoAsync(user.Id.Value.ToString());
                var nuser = UserInfo.user;
                var nprof = UserInfo.profile;
                var nworks = UserInfo.workspace;

                DataObject = user;

                PreviewViewer.Hide(true);
                PreviewViewer.Height = 0;
                PreviewBox.Hide();
                PreviewBox.Height = 0;
                Preview.Source = null;
                //if (nprof.background_image_url is string)
                //    Preview.Source = await ((string)nprof.background_image_url).LoadImage(tokens);
                //else
                //    Preview.Source = await nuser.GetPreviewUrl().LoadImage(tokens);

                IllustSizeIcon.Kind = PackIconModernKind.Image;
                IllustSize.Text = $"{nprof.total_illusts + nprof.total_manga}";
                IllustViewedIcon.Kind = PackIconModernKind.Check;
                IllustViewed.Text = $"{nprof.total_follow_users}";
                IllustFavorited.Text = $"{nprof.total_illust_bookmarks_public}";

                IllustStatInfo.Show();

                IllustTitle.Text = string.Empty;
                IllustAuthor.Text = nuser.Name;
                IllustAuthorAvator.Source = await nuser.GetAvatarUrl().LoadImage(tokens);
                if (IllustAuthorAvator.Source != null)
                {
                    IllustAuthorAvatorWait.Hide();
                }

                FollowAuthor.Show();
                if (nuser.is_followed.Value)
                {
                    FollowAuthor.Tag = PackIconModernKind.Check;// "Check";
                    ActionFollowAuthorRemove.IsEnabled = true;
                }
                else
                {
                    FollowAuthor.Tag = PackIconModernKind.Add;// "Add";
                    ActionFollowAuthorRemove.IsEnabled = false;
                }

                BookmarkIllust.Hide();
                IllustActions.Hide();

                if (nuser != null && nprof != null && nworks != null)
                {
                    StringBuilder desc = new StringBuilder();
                    desc.AppendLine($"Account:<br/> {nuser.Account} / {nuser.Id} / {nuser.Name} / {nuser.Email}");
                    desc.AppendLine($"<br/>Stat:<br/> {nprof.total_illust_bookmarks_public} Bookmarked / {nprof.total_follower} Following / {nprof.total_follow_users} Follower /<br/> {nprof.total_illusts} Illust / {nprof.total_manga} Manga / {nprof.total_novels} Novels /<br/> {nprof.total_mypixiv_users} MyPixiv User");
                    desc.AppendLine($"<hr/>");

                    desc.AppendLine($"<br/>Profile:<br/> {nprof.gender} / {nprof.birth} / {nprof.region} / {nprof.job}");
                    desc.AppendLine($"<br/>Contacts:<br/>twitter: <a href=\"{nprof.twitter_url}\">@{nprof.twitter_account}</a> / web: {nprof.webpage}");
                    desc.AppendLine($"<hr/>");

                    desc.AppendLine($"<br/>Workspace Device_:<br/> {nworks.pc} / {nworks.monitor} / {nworks.tablet} / {nworks.mouse} / {nworks.printer} / {nworks.scanner} / {nworks.tool}");
                    desc.AppendLine($"<br/>Workspace Environment:<br/> {nworks.desk} / {nworks.chair} / {nworks.desktop} / {nworks.music} / {nworks.comment}");

                    if (!string.IsNullOrEmpty(nworks.workspace_image_url))
                    {
                        desc.AppendLine($"<hr/>");
                        desc.AppendLine($"<br/>Workspace Images:<br/> <img src=\"{nworks.workspace_image_url}\"/>");
                    }

                    IllustTags.Foreground = Common.Theme.TextBrush;
                    IllustTags.Text = string.Join(";", desc);
                    IllustTagExpander.Header = "User Infomation";
                    IllustTagExpander.IsExpanded = false;
                    IllustTagExpander.Show();
                }
                else
                {
                    IllustTagExpander.Hide();
                }

                CommentsExpander.Hide();
                CommentsNavigator.Hide();

                if (!string.IsNullOrEmpty(nuser.comment) && nuser.comment.Length > 0)
                {
                    var comment = nuser.comment.HtmlEncode();
                    IllustDesc.Text = $"<div class=\"desc\">{comment.HtmlDecodeFix()}</div>".Replace("\r\n", "<br/>").Replace("\r", "<br/>").Replace("\n", "<br/>");
                    IllustDescExpander.Show();
                }
                else
                {
                    IllustDescExpander.Hide();
                }

                SubIllusts.Items.Clear();
                //SubIllusts.Refresh();
                SubIllustsExpander.IsExpanded = false;
                SubIllustsExpander.Hide();
                SubIllustsNavPanel.Hide();
                PreviewBadge.Hide();

                RelativeIllustsExpander.Header = "Illusts";
                RelativeIllustsExpander.Show();
                RelativeNextPage.Hide();
                RelativeIllustsExpander.IsExpanded = false;

                FavoriteIllustsExpander.Header = "Favorite";
                FavoriteIllustsExpander.Show();
                FavoriteNextPage.Hide();
                FavoriteIllustsExpander.IsExpanded = false;

                IllustDetailWait.Hide();
            }
            catch (Exception ex)
            {
                ex.Message.ShowMessageBox("ERROR");
            }
            finally
            {
                IllustDetailWait.Hide();
            }
        }

        internal async void ShowIllustPages(Pixeez.Tokens tokens, ImageItem item, int start = 0, int count = 30)
        {
            try
            {
                IllustDetailWait.Show();

                //SubIllustsPanel.Visibility = Visibility.Collapsed;
                SubIllusts.Items.Clear();
                if (item.Illust is Pixeez.Objects.IllustWork)
                {
                    //System.Threading.Thread.Sleep(250);
                    var subset = item.Illust as Pixeez.Objects.IllustWork;
                    if (subset.meta_pages.Count() > 1)
                    {
                        btnSubIllustPrevPages.Tag = start - count;
                        var total = subset.meta_pages.Count();
                        for (var i = start; i < total; i++)
                        {
                            if (i < 0) continue;

                            var pages = subset.meta_pages[i];
                            pages.AddTo(SubIllusts.Items, item.Illust, i, item.NextURL);

                            if (i - start >= count - 1) break;
                            btnSubIllustNextPages.Tag = i + 2;
                        }

                        if ((int)btnSubIllustPrevPages.Tag < 0)
                            btnSubIllustPrevPages.Visibility = Visibility.Collapsed;
                        else
                            btnSubIllustPrevPages.Visibility = Visibility.Visible;

                        if ((int)btnSubIllustNextPages.Tag > total - 1)
                            btnSubIllustNextPages.Visibility = Visibility.Collapsed;
                        else
                            btnSubIllustNextPages.Visibility = Visibility.Visible;

                        //SubIllustsPanel.InvalidateVisual();
                        SubIllusts.UpdateImageTiles(tokens);
                        var nullimages = SubIllusts.Items.Where(img => img.Source == null);
                        if (nullimages.Count() > 0)
                        {
                            //System.Threading.Thread.Sleep(250);
                            SubIllusts.UpdateImageTiles(tokens);
                        }
                    }
                }
                else if (item.Illust is Pixeez.Objects.NormalWork)
                {
                    //System.Threading.Thread.Sleep(250);
                    var subset = item.Illust as Pixeez.Objects.NormalWork;
                    if(subset.Metadata == null)
                    {
                        var illusts = await tokens.GetWorksAsync(item.Illust.Id.Value);
                        foreach (var illust in illusts)
                        {
                            item.Illust = illust;
                            subset = illust;
                            break;
                        }
                    }

                    if (subset.Metadata != null && subset.Metadata.Pages != null && subset.Metadata.Pages.Count() > 1)
                    {
                        btnSubIllustPrevPages.Tag = start - count;
                        var total = subset.Metadata.Pages.Count();
                        for (var i = start; i < total; i++)
                        {
                            if (i < 0) continue;

                            var pages = subset.Metadata.Pages[i];
                            pages.AddTo(SubIllusts.Items, item.Illust, i, item.NextURL);

                            if (i - start >= count - 1) break;
                            btnSubIllustNextPages.Tag = i + 2;
                        }

                        if ((int)btnSubIllustPrevPages.Tag < 0)
                            btnSubIllustPrevPages.Visibility = Visibility.Collapsed;
                        else
                            btnSubIllustPrevPages.Visibility = Visibility.Visible;

                        if ((int)btnSubIllustNextPages.Tag >= total - 1)
                            btnSubIllustNextPages.Visibility = Visibility.Collapsed;
                        else
                            btnSubIllustNextPages.Visibility = Visibility.Visible;

                        //SubIllustsPanel.InvalidateVisual();
                        SubIllusts.UpdateImageTiles(tokens);
                        var nullimages = SubIllusts.Items.Where(img => img.Source == null);
                        if (nullimages.Count() > 0)
                        {
                            //System.Threading.Thread.Sleep(250);
                            SubIllusts.UpdateImageTiles(tokens);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ex.Message.ShowMessageBox("ERROR");
            }
            finally
            {
                IllustDetailWait.Hide();
            }
        }

        internal async void ShowRelativeInline(Pixeez.Tokens tokens, ImageItem item, string next_url="")
        {
            try
            {
                IllustDetailWait.Show();
                RelativeIllusts.Items.Clear();

                var lastUrl = next_url;
                var relatives = string.IsNullOrEmpty(next_url) ? await tokens.GetRelatedWorks(item.Illust.Id.Value) : await tokens.AccessNewApiAsync<Pixeez.Objects.RecommendedRootobject>(next_url);
                next_url = relatives.next_url ?? string.Empty;

                if (relatives.illusts is Array)
                {
                    if (next_url.Equals(lastUrl, StringComparison.CurrentCultureIgnoreCase))
                        RelativeNextPage.Visibility = Visibility.Collapsed;
                    else RelativeNextPage.Visibility = Visibility.Visible;

                    RelativeIllustsExpander.Tag = next_url;
                    foreach (var illust in relatives.illusts)
                    {
                        illust.AddTo(RelativeIllusts.Items, relatives.next_url);
                    }
                    RelativeIllusts.UpdateImageTiles(tokens);
                }
            }
            catch (Exception ex)
            {
                ex.Message.ShowMessageBox("ERROR");
            }
            finally
            {
                IllustDetailWait.Hide();
            }
        }

        internal async void ShowUserWorksInline(Pixeez.Tokens tokens, Pixeez.Objects.UserBase user, string next_url = "")
        {
            try
            {
                IllustDetailWait.Show();
                RelativeIllusts.Items.Clear();

                var lastUrl = next_url;
                var relatives = string.IsNullOrEmpty(next_url) ? await tokens.GetUserWorksAsync(user.Id.Value) : await tokens.AccessNewApiAsync<Pixeez.Objects.RecommendedRootobject>(next_url);
                next_url = relatives.next_url ?? string.Empty;

                if (relatives.illusts is Array)
                {
                    if (next_url.Equals(lastUrl, StringComparison.CurrentCultureIgnoreCase))
                        RelativeNextPage.Visibility = Visibility.Collapsed;
                    else RelativeNextPage.Visibility = Visibility.Visible;

                    RelativeIllustsExpander.Tag = next_url;
                    foreach (var illust in relatives.illusts)
                    {
                        illust.AddTo(RelativeIllusts.Items, relatives.next_url);
                    }
                    RelativeIllusts.UpdateImageTiles(tokens);
                }
            }
            catch (Exception ex)
            {
                ex.Message.ShowMessageBox("ERROR");
            }
            finally
            {
                IllustDetailWait.Hide();
            }
        }

        private string last_restrict = string.Empty;
        internal async void ShowFavoriteInline(Pixeez.Tokens tokens, Pixeez.Objects.UserBase user, string next_url = "")
        {
            try
            {
                IllustDetailWait.Show();
                FavoriteIllusts.Items.Clear();

                Setting setting = Setting.Load();

                var lastUrl = next_url;
                //var restrict = setting.MyInfo is Pixeez.Objects.UserBase &&  setting.MyInfo.Id.Value == user.Id.Value ? "public" : "private";
                //var restrict = Keyboard.Modifiers == ModifierKeys.Alt || Keyboard.Modifiers == ModifierKeys.Control || Keyboard.Modifiers == ModifierKeys.Shift ? "private" : "public";
                var restrict = Keyboard.Modifiers != ModifierKeys.None ? "private" : "public";

                if (!last_restrict.Equals(restrict, StringComparison.CurrentCultureIgnoreCase)) next_url = string.Empty;
                FavoriteIllustsExpander.Header = $"Favorite ({System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(restrict)})";

                var favorites = string.IsNullOrEmpty(next_url) ? await tokens.GetUserFavoriteWorksAsync(user.Id.Value, restrict) : await tokens.AccessNewApiAsync<Pixeez.Objects.RecommendedRootobject>(next_url);
                next_url = favorites.next_url ?? string.Empty;
                last_restrict = restrict;

                if (favorites.illusts is Array)
                {
                    if (next_url.Equals(lastUrl, StringComparison.CurrentCultureIgnoreCase))
                        FavoriteNextPage.Visibility = Visibility.Collapsed;
                    else FavoriteNextPage.Visibility = Visibility.Visible;

                    FavoriteIllustsExpander.Tag = next_url;
                    foreach (var illust in favorites.illusts)
                    {
                        illust.AddTo(FavoriteIllusts.Items, favorites.next_url);
                    }
                    FavoriteIllusts.UpdateImageTiles(tokens);
                }
            }
            catch (Exception ex)
            {
                ex.Message.ShowMessageBox("ERROR");
            }
            finally
            {
                IllustDetailWait.Hide();
            }
        }

        public IllustDetailPage()
        {
            InitializeComponent();

            RelativeIllusts.Columns = 5;

            IllustDetailWait.Visibility = Visibility.Collapsed;

            cancelTokenSource = new CancellationTokenSource();
            cancelToken = cancelTokenSource.Token;

            cancelUpdatePreviewTokenSource = new CancellationTokenSource();
            cancelUpdatePreviewToken = cancelUpdatePreviewTokenSource.Token;

            UpdateTheme();
        }

        private async void IllustTags_LinkClicked(object sender, RoutedEvenArgs<HtmlLinkClickedEventArgs> args)
        {
            var tokens = await CommonHelper.ShowLogin();
            if (tokens == null) return;

            if (args.Data is HtmlLinkClickedEventArgs)
            {
                try
                {
                    var link = args.Data as HtmlLinkClickedEventArgs;

                    if (link.Attributes.ContainsKey("data-tag"))
                    {
                        args.Handled = true;
                        link.Handled = true;

                        var tag  = link.Attributes["data-tag"];
                        if (Keyboard.Modifiers == ModifierKeys.Control)
                            CommonHelper.Cmd_Search.Execute($"Tag:{tag}");
                        else
                            CommonHelper.Cmd_Search.Execute($"Fuzzy Tag:{tag}");

                        //if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                        //    CommonHelper.Cmd_Search.Execute($"Tag:{tag}");
                        //else
                        //    CommonHelper.Cmd_Search.Execute($"Fuzzy Tag:{tag}");
                    }
                }
                catch (Exception e)
                {
                    e.Message.ShowMessageBox("ERROR");
                }
            }
        }

        private async void IllustTags_ImageLoad(object sender, RoutedEvenArgs<HtmlImageLoadEventArgs> args)
        {
            if (args.Data is HtmlImageLoadEventArgs)
            {
                try
                {
                    var img = args.Data as HtmlImageLoadEventArgs;

                    if (string.IsNullOrEmpty(img.Src)) return;

                    var tokens = await CommonHelper.ShowLogin();
                    if (tokens == null) return;

                    var src = await img.Src.LoadImagePath(tokens);
                    img.Callback(src);
                    img.Handled = true;
                    args.Handled = true;
                }
                catch (Exception e)
                {
                    e.Message.ShowMessageBox("ERROR");
                }
            }
        }

        private async void IllustDesc_LinkClicked(object sender, RoutedEvenArgs<HtmlLinkClickedEventArgs> args)
        {
            if (args.Data is HtmlLinkClickedEventArgs)
            {
                try
                {
                    var link = args.Data as HtmlLinkClickedEventArgs;

                    if (link.Attributes.ContainsKey("href"))
                    {
                        args.Handled = true;
                        link.Handled = true;

                        var href = link.Attributes["href"];
                        if (href.StartsWith("pixiv://illusts/"))
                        {
                            var illust_id = Regex.Replace(href, @"pixiv://illusts/(\d+)", "$1", RegexOptions.IgnoreCase);
                            if (!string.IsNullOrEmpty(illust_id))
                            {
                                var tokens = await CommonHelper.ShowLogin();
                                if (tokens == null) return;

                                var illusts = await tokens.GetWorksAsync(Convert.ToInt32(illust_id));
                                foreach (var illust in illusts)
                                {
                                    if (illust is Pixeez.Objects.Work)
                                    {
                                        CommonHelper.Cmd_OpenIllust.Execute(illust);
                                    }
                                }
                            }
                        }
                        else if (href.StartsWith("pixiv://users/"))
                        {
                            var user_id = Regex.Replace(href, @"pixiv://users/(\d+)", "$1", RegexOptions.IgnoreCase);
                            if (!string.IsNullOrEmpty(user_id))
                            {
                                var tokens = await CommonHelper.ShowLogin();
                                if (tokens == null) return;

                                var users = await tokens.GetUsersAsync(Convert.ToInt32(user_id));
                                foreach (var user in users)
                                {
                                    if (user is Pixeez.Objects.User)
                                    {
                                        CommonHelper.Cmd_OpenIllust.Execute(user);
                                    }
                                }
                            }
                        }
                        else
                        {
                            args.Handled = false;
                            link.Handled = false;
                        }
                    }
                }
                catch (Exception e)
                {
                    e.Message.ShowMessageBox("ERROR");
                }
            }
        }

        private async void IllustDesc_ImageLoad(object sender, RoutedEvenArgs<HtmlImageLoadEventArgs> args)
        {
            if (args.Data is HtmlImageLoadEventArgs)
            {
                try
                {
                    var img = args.Data as HtmlImageLoadEventArgs;

                    if (string.IsNullOrEmpty(img.Src)) return;

                    var tokens = await CommonHelper.ShowLogin();
                    if (tokens == null) return;

                    var src = await img.Src.LoadImagePath(tokens);
                    img.Callback(src);
                    img.Handled = true;
                    args.Handled = true;
                }
                catch (Exception e)
                {
                    e.Message.ShowMessageBox("ERROR");
                }
            }
        }

        private void IllustDownloaded_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (IllustDownloaded.Tag is string)
            {
                var fp = IllustDownloaded.Tag as string;
                if (!string.IsNullOrEmpty(fp) && File.Exists(fp))
                {
                    System.Diagnostics.Process.Start(fp);
                }
            }
        }

        private void Preview_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount >= 2)
            {
                if (SubIllusts.Items.Count() <= 0)
                {
                    if (DataObject is ImageItem)
                    {
                        var item = DataObject as ImageItem;
                        item.ItemType = ImageItemType.Pages;
                        CommonHelper.Cmd_OpenIllust.Execute(item);
                    }
                }
                else
                {
                    if (SubIllusts.SelectedItems == null || SubIllusts.SelectedItems.Count <= 0)
                        SubIllusts.SelectedIndex = 0;
                    CommonHelper.Cmd_OpenIllust.Execute(SubIllusts);
                }
                e.Handled = true;
            }
        }

        #region Illust Actions
        private void ActionIllustInfo_Click(object sender, RoutedEventArgs e)
        {
            if (sender == ActionCopyIllustTitle)
            {
                Clipboard.SetText(IllustTitle.Text);
            }
            else if (sender == ActionCopyIllustAuthor)
            {
                Clipboard.SetText(IllustAuthor.Text);
            }
            else if (sender == ActionCopyAuthorID)
            {
                if (DataObject is ImageItem)
                {
                    var item = DataObject as ImageItem;
                    Clipboard.SetText(item.UserID);
                }
            }
            else if (sender == ActionCopyIllustID || sender == PreviewCopyIllustID)
            {
                if (DataObject is ImageItem)
                {
                    var item = DataObject as ImageItem;
                    Clipboard.SetText(item.ID);
                }
            }
            else if (sender == ActionCopyIllustDate)
            {
                Clipboard.SetText(ActionCopyIllustDate.Header.ToString());
            }
            else if (sender == ActionIllustWebPage)
            {
                if (DataObject is ImageItem)
                {
                    var item = DataObject as ImageItem;
                    if (item.Illust is Pixeez.Objects.Work)
                    {
                        var href = $"https://www.pixiv.net/member_illust.php?mode=medium&illust_id={item.ID}";
                        try
                        {
                            System.Diagnostics.Process.Start(href);
                        }
                        catch (Exception ex)
                        {
                            ex.Message.ShowMessageBox("ERROR");
                        }
                    }
                }
            }
            else if (sender == ActionIllustNewWindow)
            {
                if (DataObject is ImageItem)
                {
                    var item = DataObject as ImageItem;
                    if (item.Illust is Pixeez.Objects.Work)
                    {
                        CommonHelper.Cmd_OpenIllust.Execute(item.Illust);
                    }
                }
            }
            else if (sender == ActionIllustWebLink)
            {
                if (DataObject is ImageItem)
                {
                    var item = DataObject as ImageItem;
                    if (item.Illust is Pixeez.Objects.Work)
                    {
                        var href = $"https://www.pixiv.net/member_illust.php?mode=medium&illust_id={item.ID}";
                        Clipboard.SetText(href);
                    }
                }
            }
        }

        private async void ActionIllustAuthourInfo_Click(object sender, RoutedEventArgs e)
        {
            var tokens = await CommonHelper.ShowLogin();
            if (tokens == null) return;

            if(sender == ActionIllustAuthorInfo || sender == btnAuthorInto)
            {
                if (DataObject is ImageItem)
                {
                    CommonHelper.Cmd_OpenIllust.Execute((DataObject as ImageItem).Illust.User);
                }
            }
            else if(sender == ActionIllustAuthorFollowing)
            {
                if (DataObject is Pixeez.Objects.UserBase)
                {
                }
            }
            else if (sender == ActionIllustAuthorFollowed)
            {

            }
            else if (sender == ActionIllustAuthorFavorite)
            {

            }

        }

        private void ActionShowIllustPages_Click(object sender, RoutedEventArgs e)
        {
            SubIllustsExpander.IsExpanded = !SubIllustsExpander.IsExpanded;
        }

        private void ActionShowRelative_Click(object sender, RoutedEventArgs e)
        {
            if (!RelativeIllustsExpander.IsExpanded) RelativeIllustsExpander.IsExpanded = true;
        }

        private void ActionShowFavorite_Click(object sender, RoutedEventArgs e)
        {
            if (!FavoriteIllustsExpander.IsExpanded) FavoriteIllustsExpander.IsExpanded = true;
        }
        #endregion

        #region Following User / Bookmark Illust routines
        private void ActionIllust_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;

            if (sender == BookmarkIllust)
                BookmarkIllust.ContextMenu.IsOpen = true;
            else if (sender == FollowAuthor)
                FollowAuthor.ContextMenu.IsOpen = true;
            else if (sender == IllustActions)
            {
                if (Window.GetWindow(this) is ContentWindow)
                    ActionIllustNewWindow.Visibility = Visibility.Collapsed;
                else
                    ActionIllustNewWindow.Visibility = Visibility.Visible;
                IllustActions.ContextMenu.IsOpen = true;
            }
        }

        private async void ActionBookmarkIllust_Click(object sender, RoutedEventArgs e)
        {
            var tokens = await CommonHelper.ShowLogin();
            if (tokens == null) return;

            if (DataObject is ImageItem)
            {
                var item = DataObject as ImageItem;
                var illust = item.Illust;
                var lastID = illust.Id;
                try
                {
                    if (sender == ActionBookmarkIllustPublic)
                    {
                        await tokens.AddMyFavoriteWorksAsync((long)illust.Id, illust.Tags);
                    }
                    else if (sender == ActionBookmarkIllustPrivate)
                    {
                        await tokens.AddMyFavoriteWorksAsync((long)illust.Id, illust.Tags, "private");
                    }
                    else if (sender == ActionBookmarkIllustRemove)
                    {
                        await tokens.DeleteMyFavoriteWorksAsync((long)illust.Id);
                        await tokens.DeleteMyFavoriteWorksAsync((long)illust.Id, "private");
                    }
                    //await item.RefreshIllust(tokens);
                }
                catch (Exception) { }
                finally
                {
                    try
                    {
                        var currentItem = DataObject as ImageItem;
                        var currentIllust = currentItem.Illust;
                        if (lastID == currentIllust.Id)
                        {
                            tokens = await CommonHelper.ShowLogin();
                            await item.RefreshIllustAsync(tokens);
                            currentItem = DataObject as ImageItem;
                            currentIllust = currentItem.Illust;
                            if (lastID == currentIllust.Id)
                            {
                                if (item.Illust.IsBookMarked())
                                {
                                    BookmarkIllust.Tag = PackIconModernKind.Heart;
                                    ActionBookmarkIllustRemove.IsEnabled = true;
                                }
                                else
                                {
                                    BookmarkIllust.Tag = PackIconModernKind.HeartOutline;
                                    ActionBookmarkIllustRemove.IsEnabled = false;
                                }
                            }
                        }
                    }
                    catch (Exception) { }
                }
            }
        }

        private async void ActionFollowAuthor_Click(object sender, RoutedEventArgs e)
        {
            var tokens = await CommonHelper.ShowLogin();
            if (tokens == null) return;

            if (DataObject is ImageItem)
            {
                var item = DataObject as ImageItem;
                var illust = item.Illust;
                var lastID = illust.Id;
                try
                {
                    if (sender == ActionFollowAuthorPublic)
                    {
                        await tokens.AddFavouriteUser((long)illust.User.Id);
                    }
                    else if (sender == ActionFollowAuthorPrivate)
                    {
                        await tokens.AddFavouriteUser((long)illust.User.Id, "private");
                    }
                    else if (sender == ActionFollowAuthorRemove)
                    {
                        await tokens.DeleteFavouriteUser(illust.User.Id.ToString());
                        await tokens.DeleteFavouriteUser(illust.User.Id.ToString(), "private");
                    }
                    //await item.RefreshUserInfo(tokens);
                }
                catch (Exception) { }
                finally
                {
                    try
                    {
                        var currentItem = DataObject as ImageItem;
                        var currentIllust = currentItem.Illust;
                        if (lastID == currentIllust.Id)
                        {
                            tokens = await CommonHelper.ShowLogin();
                            await item.RefreshUserInfoAsync(tokens);
                            currentItem = DataObject as ImageItem;
                            currentIllust = currentItem.Illust;
                            if (lastID == currentIllust.Id)
                            {
                                if (item.Illust.User != null && item.Illust.User.is_followed != null && item.Illust.User.is_followed.Value)
                                {
                                    FollowAuthor.Tag = PackIconModernKind.Check;
                                    ActionFollowAuthorRemove.IsEnabled = true;
                                }
                                else
                                {
                                    FollowAuthor.Tag = PackIconModernKind.Add;
                                    ActionFollowAuthorRemove.IsEnabled = false;
                                }
                            }
                        }
                    }
                    catch (Exception) { }
                }
            }
            else if (DataObject is Pixeez.Objects.UserBase)
            {
                var user = DataObject as Pixeez.Objects.UserBase;
                try
                {
                    if (sender == ActionFollowAuthorPublic)
                        await tokens.AddFavouriteUser((long)user.Id);
                    else if (sender == ActionFollowAuthorPrivate)
                        await tokens.AddFavouriteUser((long)user.Id, "private");
                    else if (sender == ActionFollowAuthorRemove)
                    {
                        await tokens.DeleteFavouriteUser(user.Id.ToString());
                        await tokens.DeleteFavouriteUser(user.Id.ToString(), "private");
                    }
                }
                catch (Exception) { }
                finally
                {
                    try
                    {
                        //Thread.Sleep(250);
                        tokens = await CommonHelper.ShowLogin();
                        var users = await tokens.GetUsersAsync(user.Id.Value);
                        foreach (var u in users)
                        {                            
                            user.is_followed = u.IsFollowing;
                            DataObject = u;
                            break;
                        }
                        if (user.is_followed.Value)
                        {
                            FollowAuthor.Tag = PackIconModernKind.Check;
                            ActionFollowAuthorRemove.IsEnabled = true;
                        }
                        else
                        {
                            FollowAuthor.Tag = PackIconModernKind.Add;
                            ActionFollowAuthorRemove.IsEnabled = false;
                        }
                    }
                    catch (Exception) { }
                }
            }
        }
        #endregion

        #region Illust Multi-Pages related routines
        private async void SubIllustsExpander_Expanded(object sender, RoutedEventArgs e)
        {
            if (SubIllusts.Items.Count() <= 1)
            {
                var tokens = await CommonHelper.ShowLogin();
                if (tokens == null) return;

                if (DataObject is ImageItem)
                {
                    var item = DataObject as ImageItem;
                    ShowIllustPages(tokens, item);
                }
            }
        }

        private void SubIllustsExpander_Collapsed(object sender, RoutedEventArgs e)
        {
            //IllustDetailWait.Hide();
        }

        private void ActionOpenIllust_Click(object sender, RoutedEventArgs e)
        {
            if (SubIllusts.Items.Count() <= 0)
            {
                if (DataObject is ImageItem)
                {
                    var item = DataObject as ImageItem;
                    item.ItemType = ImageItemType.Pages;
                    CommonHelper.Cmd_OpenIllust.Execute(item);
                }
            }
            else
            {
                if (SubIllusts.SelectedItems == null || SubIllusts.SelectedItems.Count <= 0)
                    SubIllusts.SelectedIndex = 0;
                CommonHelper.Cmd_OpenIllust.Execute(SubIllusts);
            }
        }

        private async void ActionSaveIllust_Click(object sender, RoutedEventArgs e)
        {
            var tokens = await CommonHelper.ShowLogin();
            if (tokens == null) return;

            if (sender == PreviewSave)
            {
                var item = DataObject as ImageItem;
                CommonHelper.Cmd_SaveIllust.Execute(item);
            }
            else if (SubIllusts.SelectedItems != null && SubIllusts.SelectedItems.Count > 0)
            {
                foreach (var item in SubIllusts.SelectedItems)
                {
                    CommonHelper.Cmd_SaveIllust.Execute(item);
                }
            }
            else if (SubIllusts.SelectedItem is ImageItem)
            {
                var item = SubIllusts.SelectedItem;
                CommonHelper.Cmd_SaveIllust.Execute(item);
            }
            else if (DataObject is ImageItem)
            {
                var item = DataObject as ImageItem;
                CommonHelper.Cmd_SaveIllust.Execute(item);
            }

        }

        private async void ActionSaveAllIllust_Click(object sender, RoutedEventArgs e)
        {
            var tokens = await CommonHelper.ShowLogin();
            if (tokens == null) return;

            IllustsSaveProgress.Visibility = Visibility.Visible;
            IllustsSaveProgress.Value = 0;
            IProgress<int> progress = new Progress<int>(i => { IllustsSaveProgress.Value = i; });

            Pixeez.Objects.Work illust = null;
            if (DataObject is Pixeez.Objects.Work)
                illust = DataObject as Pixeez.Objects.Work;
            else if (DataObject is ImageItem)
                illust = (DataObject as ImageItem).Illust;

            if (illust != null)
            {
                var dt = illust.GetDateTime();

                if (illust is Pixeez.Objects.IllustWork)
                {
                    var illustset = illust as Pixeez.Objects.IllustWork;
                    var is_meta_single_page = illust.PageCount==1 ? true : false;
                    var idx=1;
                    var total = illustset.meta_pages.Count();
                    foreach (var pages in illustset.meta_pages)
                    {
                        var url = pages.GetOriginalUrl();
                        tokens = await CommonHelper.ShowLogin();
                        //await url.ToImageFile(tokens, dt, is_meta_single_page);
                        url.SaveImage(pages.GetThumbnailUrl(), dt, is_meta_single_page);

                        idx++;
                        progress.Report((int)((double)idx / total * 100));
                    }
                }
                else if (illust is Pixeez.Objects.NormalWork)
                {
                    var url = illust.GetOriginalUrl();
                    var illustset = illust as Pixeez.Objects.NormalWork;
                    var is_meta_single_page = illust.PageCount==1 ? true : false;
                    tokens = await CommonHelper.ShowLogin();
                    //await url.ToImageFile(tokens, dt, is_meta_single_page);
                    if (is_meta_single_page)
                    {
                        url.SaveImage(illust.GetThumbnailUrl(), dt, is_meta_single_page);
                    }
                    else
                    {
                        tokens = await CommonHelper.ShowLogin();
                        var illusts = await tokens.GetWorksAsync(illust.Id.Value);
                        foreach(var w in illusts)
                        {
                            if(w.Metadata != null && w.Metadata.Pages != null)
                            {
                                foreach (var p in w.Metadata.Pages)
                                {
                                    var u = p.GetOriginalUrl();
                                    u.SaveImage(p.GetThumbnailUrl(), dt, is_meta_single_page);
                                }
                            }
                            
                        }
                    }
                    
                }
                IllustsSaveProgress.Value = 100;
                IllustsSaveProgress.Visibility = Visibility.Collapsed;
                SystemSounds.Beep.Play();
            }
        }

        long lastSelectionChanged = 0;
        ImageItem lastSelectionItem = null;
        private void SubIllusts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
#if DEBUG
            Console.WriteLine($"{DateTime.Now.ToFileTime() - lastSelectionChanged}, {sender}, {e.Handled}, {e.RoutedEvent}, {e.OriginalSource}, {e.Source}");
#endif
            var idx = SubIllusts.SelectedIndex;
            if (SubIllusts.SelectedItem is ImageItem && SubIllusts.SelectedItems.Count == 1)
            {
                if (DateTime.Now.ToFileTime() - lastSelectionChanged < 500000)
                {
                    SubIllusts.SelectedItem = lastSelectionItem;
                    return;
                }
                //IllustDetailViewer
                e.Handled = true;

                if (update_preview_task is Task)
                {
                    cancelUpdatePreviewToken.ThrowIfCancellationRequested();
                    update_preview_task.Wait();
                }

                update_preview_task = new Task(async () =>
                {
                    PreviewWait.Show();

                    var item = SubIllusts.SelectedItem as ImageItem;
                    lastSelectionItem = item;
                    lastSelectionChanged = DateTime.Now.ToFileTime();

                    string fp = string.Empty;
                    if (item.Illust.GetOriginalUrl(item.Index).IsDownloaded(out fp))
                    {
                        IllustDownloaded.Visibility = Visibility.Visible;
                        IllustDownloaded.Tag = fp;
                        ToolTipService.SetToolTip(IllustDownloaded, fp);
                    }
                    else
                    {
                        IllustDownloaded.Visibility = Visibility.Collapsed;
                        IllustDownloaded.Tag = null;
                        ToolTipService.SetToolTip(IllustDownloaded, null);
                    }

                    var tokens = await CommonHelper.ShowLogin();
                    var img = await item.Illust.GetPreviewUrl(item.Index).LoadImage(tokens);
                    if (img == null || img.Width < 350)
                    {
                        var large = await item.Illust.GetOriginalUrl().LoadImage(tokens);
                        if (large != null) img = large;
                    }
                    if (img != null) Preview.Source = img;

                    PreviewWait.Hide();
                }, cancelUpdatePreviewTokenSource.Token, TaskCreationOptions.None);
                update_preview_task.RunSynchronously();
                SubIllusts.SelectedIndex = idx;
                //await update_preview_task;
                //Thread.Sleep(100);
            }
        }

        private void SubIllusts_MouseWheel(object sender, MouseWheelEventArgs e)
        {

        }

        private void SubIllusts_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            CommonHelper.Cmd_OpenIllust.Execute(SubIllusts);
        }

        private void SubIllusts_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                CommonHelper.Cmd_OpenIllust.Execute(SubIllusts);
            }
        }

        private async void SubIllustPagesNav_Click(object sender, RoutedEventArgs e)
        {
            if (sender == btnSubIllustPrevPages || sender == btnSubIllustNextPages)
            {
                var btn = sender as Button;
                if (btn.Tag is int)
                {
                    var start = (int)btn.Tag;
                    if (start < 0) return;

                    var tokens = await CommonHelper.ShowLogin();
                    if (tokens == null) return;

                    if (DataObject is ImageItem)
                    {
                        var item = DataObject as ImageItem;
                        if (start < item.Count)
                            ShowIllustPages(tokens, item, start);
                    }
                }
            }
        }

        private async void ActionPrevSubIllustPage_Click(object sender, RoutedEventArgs e)
        {
            var btn = btnSubIllustPrevPages;
            if (btn.Tag is int)
            {
                var start = (int)btn.Tag;
                if (start < 0) return;

                var tokens = await CommonHelper.ShowLogin();
                if (tokens == null) return;

                if (DataObject is ImageItem)
                {
                    var item = DataObject as ImageItem;
                    if (start < item.Count)
                        ShowIllustPages(tokens, item, start);
                }
            }
        }

        private async void ActionNextSubIllustPage_Click(object sender, RoutedEventArgs e)
        {
            var btn = btnSubIllustNextPages;
            if (btn.Tag is int)
            {
                var start = (int)btn.Tag;
                if (start < 0) return;

                var tokens = await CommonHelper.ShowLogin();
                if (tokens == null) return;

                if (DataObject is ImageItem)
                {
                    var item = DataObject as ImageItem;
                    if (start < item.Count)
                        ShowIllustPages(tokens, item, start);
                }
            }
        }
        #endregion

        #region Relative Panel related routines
        private async void RelativeIllustsExpander_Expanded(object sender, RoutedEventArgs e)
        {
            var tokens = await CommonHelper.ShowLogin();
            if (tokens == null) return;

            if (DataObject is ImageItem)
            {
                var item = DataObject as ImageItem;
                ShowRelativeInline(tokens, item);
            }
            else if(DataObject is Pixeez.Objects.UserBase)
            {
                var user = DataObject as Pixeez.Objects.UserBase;
                ShowUserWorksInline(tokens, user);
            }
            //RelativeNextPage.Visibility = Visibility.Visible;
        }

        private void RelativeIllustsExpander_Collapsed(object sender, RoutedEventArgs e)
        {
            IllustDetailWait.Hide();
        }

        private void ActionOpenRelative_Click(object sender, RoutedEventArgs e)
        {
            CommonHelper.Cmd_OpenIllust.Execute(RelativeIllusts);
        }

        private void ActionCopyRelativeIllustID_Click(object sender, RoutedEventArgs e)
        {
            CommonHelper.Cmd_CopyIllustIDs.Execute(RelativeIllusts);
        }

        private void ActionSaveRelative_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ActionSaveAllRelative_Click(object sender, RoutedEventArgs e)
        {

        }

        private void RelativeIllusts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RelativeIllusts.SelectedItems.Count <= 0 || RelativeIllusts.SelectedIndex < 0) return;
            var targets = new List<ImageItem>();
            foreach (var item in RelativeIllusts.SelectedItems)
            {
                if (item.Illust == null) continue;
                bool download = item.Illust.GetOriginalUrl().IsPartDownloaded();
                if (item.IsDownloaded != download)
                {
                    item.IsDownloaded = download;
                    targets.Add(item);
                }
            }
            if (targets.Count > 0)
            {
                var idx = RelativeIllusts.SelectedIndex;
                var items = RelativeIllusts.SelectedItems;
                RelativeIllusts.Items.UpdateTiles(targets);
                RelativeIllusts.SelectedItems.Clear();
                foreach (var item in items)
                    RelativeIllusts.SelectedItems.Add(item);
                RelativeIllusts.SelectedIndex = idx;
            }
            e.Handled = false;
        }

        private void RelativeIllusts_MouseWheel(object sender, MouseWheelEventArgs e)
        {

        }

        private void RelativeIllusts_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            CommonHelper.Cmd_OpenIllust.Execute(RelativeIllusts);
        }

        private void RelativeIllusts_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                CommonHelper.Cmd_OpenIllust.Execute(RelativeIllusts);
            }
        }

        private void RelativePrevPage_Click(object sender, RoutedEventArgs e)
        {

        }

        private async void RelativeNextPage_Click(object sender, RoutedEventArgs e)
        {
            var tokens = await CommonHelper.ShowLogin();
            if (tokens == null) return;

            var next_url = string.Empty;
            if (RelativeIllustsExpander.Tag is string)
            {
                next_url = RelativeIllustsExpander.Tag as string;
                if (string.IsNullOrEmpty(next_url))
                    RelativeNextPage.Visibility = Visibility.Collapsed;
                else
                    RelativeNextPage.Visibility = Visibility.Visible;
            }

            if (DataObject is ImageItem)
            {
                var item = DataObject as ImageItem;
                ShowRelativeInline(tokens, item, next_url);
            }
            else if (DataObject is Pixeez.Objects.UserBase)
            {
                var user = DataObject as Pixeez.Objects.UserBase;
                ShowUserWorksInline(tokens, user, next_url);
            }
        }
        #endregion

        #region Autoor Favorite routines
        private async void FavoriteIllustsExpander_Expanded(object sender, RoutedEventArgs e)
        {
            var tokens = await CommonHelper.ShowLogin();
            if (tokens == null) return;

            if (DataObject is ImageItem)
            {
                var item = DataObject as ImageItem;
                var user = item.Illust.User;
                ShowFavoriteInline(tokens, user);
            }
            else if (DataObject is Pixeez.Objects.UserBase)
            {
                var user = DataObject as Pixeez.Objects.UserBase;
                ShowFavoriteInline(tokens, user);
            }
            //FavoriteNextPage.Visibility = Visibility.Visible;
        }

        private void FavoriteIllustsExpander_Collapsed(object sender, RoutedEventArgs e)
        {
            FavoriteIllustsExpander.Header = "Favorite";
            IllustDetailWait.Hide();
        }

        private void ActionOpenFavorite_Click(object sender, RoutedEventArgs e)
        {
            CommonHelper.Cmd_OpenIllust.Execute(FavoriteIllusts);
        }

        private void ActionCopyFavoriteIllustID_Click(object sender, RoutedEventArgs e)
        {
            CommonHelper.Cmd_CopyIllustIDs.Execute(FavoriteIllusts);
        }

        private void FavriteIllusts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FavoriteIllusts.SelectedItems.Count <= 0 || FavoriteIllusts.SelectedIndex < 0) return;
            var targets = new List<ImageItem>();
            foreach (var item in FavoriteIllusts.SelectedItems)
            {
                if (item.Illust == null) continue;
                bool download = item.Illust.GetOriginalUrl().IsPartDownloaded();
                if (item.IsDownloaded != download)
                {
                    item.IsDownloaded = download;
                    targets.Add(item);
                }
            }
            if (targets.Count > 0)
            {
                var idx = FavoriteIllusts.SelectedIndex;
                var items = FavoriteIllusts.SelectedItems;
                FavoriteIllusts.Items.UpdateTiles(targets);
                FavoriteIllusts.SelectedItems.Clear();
                foreach (var item in items)
                    FavoriteIllusts.SelectedItems.Add(item);
                FavoriteIllusts.SelectedIndex = idx;
            }
            e.Handled = false;
        }

        private void FavriteIllusts_MouseWheel(object sender, MouseWheelEventArgs e)
        {

        }

        private void FavriteIllusts_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            CommonHelper.Cmd_OpenIllust.Execute(FavoriteIllusts);
        }

        private void FavriteIllusts_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                CommonHelper.Cmd_OpenIllust.Execute(FavoriteIllusts);
            }
        }

        private void FavoritePrevPage_Click(object sender, RoutedEventArgs e)
        {

        }

        private async void FavoriteNextPage_Click(object sender, RoutedEventArgs e)
        {
            var tokens = await CommonHelper.ShowLogin();
            if (tokens == null) return;

            var next_url = string.Empty;
            if (FavoriteIllustsExpander.Tag is string)
            {
                next_url = RelativeIllustsExpander.Tag as string;
                if (string.IsNullOrEmpty(next_url))
                    FavoriteNextPage.Visibility = Visibility.Collapsed;
                else
                    FavoriteNextPage.Visibility = Visibility.Visible;
            }

            if (DataObject is ImageItem)
            {
                var item = DataObject as ImageItem;
                var user = item.Illust.User;
                ShowFavoriteInline(tokens, user, next_url);
            }
            else if (DataObject is Pixeez.Objects.UserBase)
            {
                var user = DataObject as Pixeez.Objects.UserBase;
                ShowFavoriteInline(tokens, user, next_url);
            }
        }
        #endregion

        #region Illust Comments related routines
        private async void CommentsExpander_Expanded(object sender, RoutedEventArgs e)
        {
            var tokens = await CommonHelper.ShowLogin();
            if (tokens == null) return;

            if (DataObject is ImageItem)
            {
                IllustDetailWait.Show();

                var item = DataObject as ImageItem;
                //ShowCommentsInline(tokens, item);
                //CommentsViewer.Language = System.Windows.Markup.XmlLanguage.GetLanguage("zh");
                CommentsViewer.NavigateToString("about:blank");

                    //.Document = string.Empty;
                var result = await tokens.GetIllustComments(item.ID, "0", true);
                foreach(var comment in result.comments)
                {
                    //comment.
                }
                //CommentsViewer.NavigateToString(comm

                IllustDetailWait.Hide();
            }
        }

        private void CommentsExpander_Collapsed(object sender, RoutedEventArgs e)
        {
            IllustDetailWait.Hide();
        }

        private void CommentsPrevPage_Click(object sender, RoutedEventArgs e)
        {

        }

        private void CommentsNextPage_Click(object sender, RoutedEventArgs e)
        {

        }

        #endregion

        #region Common ImageListGrid Context Menu
        private void ActionMenu_Opened(object sender, RoutedEventArgs e)
        {
            if (sender is ContextMenu)
            {
                var host = (sender as ContextMenu).PlacementTarget;
                if (host == SubIllustsExpander)
                {
                    foreach(MenuItem item in (sender as ContextMenu).Items)
                    {
                        if(item.Uid.Equals("ActionPrevPage", StringComparison.CurrentCultureIgnoreCase)){
                            item.Visibility = Visibility.Visible;
                        }
                    }
                }
                else if (host == RelativeIllustsExpander)
                {
                    foreach (MenuItem item in (sender as ContextMenu).Items)
                    {
                        if (item.Uid.Equals("ActionPrevPage", StringComparison.CurrentCultureIgnoreCase))
                        {
                            item.Visibility = Visibility.Collapsed;
                        }
                        else if (item.Uid.Equals("ActionNextPage", StringComparison.CurrentCultureIgnoreCase))
                        {
                            var next_url = RelativeIllustsExpander.Tag as string;
                            if (string.IsNullOrEmpty(next_url))
                                item.Visibility = Visibility.Collapsed;
                            else
                                item.Visibility = Visibility.Visible;
                        }
                    }
                }
                else if (host == FavoriteIllustsExpander)
                {
                    foreach (MenuItem item in (sender as ContextMenu).Items)
                    {
                        if (item.Uid.Equals("ActionPrevPage", StringComparison.CurrentCultureIgnoreCase))
                        {
                            item.Visibility = Visibility.Collapsed;
                        }
                        else if (item.Uid.Equals("ActionNextPage", StringComparison.CurrentCultureIgnoreCase))
                        {
                            var next_url = FavoriteIllustsExpander.Tag as string;
                            if (string.IsNullOrEmpty(next_url))
                                item.Visibility = Visibility.Collapsed;
                            else
                                item.Visibility = Visibility.Visible;
                        }
                    }
                }
                else if (host == CommentsExpander)
                {
                    foreach (MenuItem item in (sender as ContextMenu).Items)
                    {
                        if (item.Uid.Equals("ActionPrevPage", StringComparison.CurrentCultureIgnoreCase))
                        {
                            item.Visibility = Visibility.Visible;
                        }
                    }
                }
            }
        }

        private void ActionCopyIllustID_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem && (sender as MenuItem).Parent is ContextMenu)
            {
                var host = ((sender as MenuItem).Parent as ContextMenu).PlacementTarget;
                if (host == SubIllustsExpander)
                {
                    if (DataObject is ImageItem)
                    {
                        var item = DataObject as ImageItem;
                        Clipboard.SetText(item.UserID);
                    }
                }
                else if (host == RelativeIllustsExpander)
                {
                    CommonHelper.Cmd_CopyIllustIDs.Execute(RelativeIllusts);
                }
                else if (host == FavoriteIllustsExpander)
                {
                    CommonHelper.Cmd_CopyIllustIDs.Execute(FavoriteIllusts);
                }
                else if (host == CommentsExpander)
                {

                }
            }
        }

        private void ActionOpenSelectedIllust_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem && (sender as MenuItem).Parent is ContextMenu)
            {
                var host = ((sender as MenuItem).Parent as ContextMenu).PlacementTarget;
                if (host == SubIllustsExpander)
                {
                    CommonHelper.Cmd_OpenIllust.Execute(SubIllusts);
                }
                else if (host == RelativeIllustsExpander)
                {
                    CommonHelper.Cmd_OpenIllust.Execute(RelativeIllusts);
                }
                else if (host == FavoriteIllustsExpander)
                {
                    CommonHelper.Cmd_OpenIllust.Execute(FavoriteIllusts);
                }
                else if (host == CommentsExpander)
                {

                }
            }
        }

        private void ActionPrevPage_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem && (sender as MenuItem).Parent is ContextMenu)
            {
                var host = ((sender as MenuItem).Parent as ContextMenu).PlacementTarget;
                if (host == SubIllustsExpander)
                {
                    ActionPrevSubIllustPage_Click(sender, e);
                }
                else if (host == RelativeIllustsExpander)
                {

                }
                else if (host == FavoriteIllustsExpander)
                {

                }
                else if (host == CommentsExpander)
                {

                }
            }
        }

        private void ActionNextPage_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem && (sender as MenuItem).Parent is ContextMenu)
            {
                var host = ((sender as MenuItem).Parent as ContextMenu).PlacementTarget;
                if (host == SubIllustsExpander)
                {
                    ActionNextSubIllustPage_Click(sender, e);
                }
                else if (host == RelativeIllustsExpander)
                {
                    RelativeNextPage_Click(RelativeIllusts, e);
                }
                else if (host == FavoriteIllustsExpander)
                {
                    FavoriteNextPage_Click(FavoriteIllusts, e);
                }
                else if (host == CommentsExpander)
                {

                }
            }
        }
        #endregion
    }

}
