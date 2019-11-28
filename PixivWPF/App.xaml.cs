﻿using PixivWPF.Common;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace PixivWPF
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            var setting = Setting.Load();
            if (!string.IsNullOrEmpty(setting.Theme))
                Theme.CurrentTheme = setting.Theme;
            if (!string.IsNullOrEmpty(setting.Accent))
                Theme.CurrentAccent = setting.Accent;            
        }
    }
}
