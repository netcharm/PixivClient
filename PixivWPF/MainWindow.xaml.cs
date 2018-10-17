﻿using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using PixivWPF.Common;
using System;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;

namespace PixivWPF
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public Frame MainContent = null;

        private Pages.TilesPage pagetiles = null;
        private Pages.NavPage pagenav = null;

        private ObservableCollection<string> auto_suggest_list = new ObservableCollection<string>() {"a", "b" };
        public ObservableCollection<string> AutoSuggestList
        {
            get { return (auto_suggest_list); }
        }

        public void UpdateTheme()
        {
            if (pagenav is Pages.NavPage) pagenav.CheckPage();
            if (pagetiles is Pages.TilesPage) pagetiles.UpdateTheme();
            foreach(Window win in Application.Current.Windows)
            {
                if(win.Content is Pages.IllustDetailPage)
                {
                    var page = win.Content as Pages.IllustDetailPage;
                    page.UpdateTheme();
                }
            }
            
        }

        public MainWindow()
        {
            InitializeComponent();
            
            SearchBox.ItemsSource = AutoSuggestList;

            MainContent = ContentFrame;

            //ContentFrame.Content = new Pages.PageLogin() { Tag = ContentFrame };
            pagetiles = new Pages.TilesPage() { Tag = ContentFrame };
            pagenav = new Pages.NavPage() { Tag = pagetiles, NavFlyout = NavFlyout };

            ContentFrame.Content = pagetiles;
            NavFrame.Content = pagenav;

            NavFlyout.Content = pagenav;
            NavFlyout.Theme = FlyoutTheme.Adapt;
            NavFlyout.Theme = FlyoutTheme.Accent;
            NavFlyout.Opacity = 0.95;

            ContentFrame.NavigationUIVisibility = NavigationUIVisibility.Hidden;
            NavFrame.NavigationUIVisibility = NavigationUIVisibility.Hidden;

            NavPageTitle.Text = pagetiles.TargetPage.ToString();

            CommandToggleTheme.ItemsSource = Common.Theme.Accents;
            CommandToggleTheme.SelectedIndex = Common.Theme.Accents.IndexOf(Common.Theme.CurrentAccent);
        }

#if DEBUG
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            foreach (Window win in Application.Current.Windows)
            {
                if (win == this) continue;
                win.Close();
            }
        }
#else
        private async void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;

            var opt = new MetroDialogSettings();
            opt.AffirmativeButtonText = "Yes";
            opt.NegativeButtonText = "No";
            opt.DefaultButtonFocus = MessageDialogResult.Affirmative;
            opt.DialogMessageFontSize = 24;
            opt.DialogResultOnCancel = MessageDialogResult.Canceled;

            var ret = await this.ShowMessageAsync("Confirm", "Continue Exit?", MessageDialogStyle.AffirmativeAndNegative, opt);
            if (ret == MessageDialogResult.Affirmative)
            {
                Application.Current.Shutdown();
            }
        }
#endif
        private void CommandToggleTheme_Click(object sender, RoutedEventArgs e)
        {
            Common.Theme.Toggle();
            this.UpdateTheme();
        }

        private void CommandToggleTheme_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(CommandToggleTheme.SelectedIndex>=0 && CommandToggleTheme.SelectedIndex< CommandToggleTheme.Items.Count)
            {
                Common.Theme.CurrentAccent = Common.Theme.Accents[CommandToggleTheme.SelectedIndex];
                if (pagenav is Pages.NavPage) pagenav.CheckPage();
            }
        }

        private void CommandLogin_Click(object sender, RoutedEventArgs e)
        {
            var accesstoken = Setting.Token();
            var dlgLogin = new PixivLoginDialog() { AccessToken = accesstoken };
            var ret = dlgLogin.ShowDialog();
            accesstoken = dlgLogin.AccessToken;
            Setting.Token(accesstoken);
        }

        private void CommandNav_Click(object sender, RoutedEventArgs e)
        {
            NavFlyout.IsOpen = !NavFlyout.IsOpen;
        }

        private void CommandNavRefresh_Click(object sender, RoutedEventArgs e)
        {
            NavPageTitle.Text = pagetiles.TargetPage.ToString();
            pagetiles.ShowImages(pagetiles.TargetPage, false);
        }

        private void CommandNavPrev_Click(object sender, RoutedEventArgs e)
        {
        }

        private void CommandNavNext_Click(object sender, RoutedEventArgs e)
        {
            NavPageTitle.Text = pagetiles.TargetPage.ToString();
            pagetiles.ShowImages(pagetiles.TargetPage, true);
        }

        private void NavFlyout_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if(e.NewValue is PixivPage)
            {
                NavPageTitle.Text = e.NewValue.ToString();
            }
        }

        private void CommandSearch_Click(object sender, RoutedEventArgs e)
        {
            CommonHelper.Cmd_Search.Execute(SearchBox.Text);
        }

        private void SearchBox_TextChanged(object sender, RoutedEventArgs e)
        {
            if (SearchBox.Text.Length > 0)
            {
                var content = SearchBox.Text;
                auto_suggest_list.Clear();

                content = Regex.Replace(content, @"(.*?illust_id=)(\d+)(.*)", "IllustID: $2", RegexOptions.IgnoreCase).Trim();
                content = Regex.Replace(content, @"^.*?\?id=(\d+)$", "UserID: $1", RegexOptions.IgnoreCase).Trim();
                content = Regex.Replace(content, @"((UserID)|(IllustID)|(Tag)|(Caption)|(Fuzzy)|(Fuzzy Tag)):", "", RegexOptions.IgnoreCase).Trim();

                if (Regex.IsMatch(content, @"^\d+$", RegexOptions.IgnoreCase))
                {
                    auto_suggest_list.Add($"UserID: {content}");
                    auto_suggest_list.Add($"IllustID: {content}");
                }
                auto_suggest_list.Add($"Fuzzy: {content}");
                auto_suggest_list.Add($"Tag: {content}");
                auto_suggest_list.Add($"Fuzzy Tag: {content}");
                auto_suggest_list.Add($"Caption: {content}");
                SearchBox.Items.Refresh();
                SearchBox.IsDropDownOpen = true;
                e.Handled = true;
            }
        }

        private void SearchBox_DropDownOpened(object sender, EventArgs e)
        {
            var textBox = Keyboard.FocusedElement as TextBox;
            if (textBox != null && textBox.Text.Length == 1 && textBox.SelectionLength == 1)
            {
                textBox.SelectionLength = 0;
                textBox.SelectionStart = 1;
            }
        }

        private void SearchBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            e.Handled = true;
            var items = e.AddedItems;
            if (items.Count > 0)
            {
                var item = items[0];
                if (item is string)
                {
                    var query = (string)item;
                    CommonHelper.Cmd_Search.Execute(query);
                }
            }
        }

        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                e.Handled = true;
                CommonHelper.Cmd_Search.Execute(SearchBox.Text);
            }
        }

        private void CommandDownloadManager_Click(object sender, RoutedEventArgs e)
        {
            CommonHelper.ShowDownloadManager();
        }
    }


}
