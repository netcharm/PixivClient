﻿using MahApps.Metro;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace PixivWPF.Common
{
    public static class Theme
    {
        private static List<string> accents = new List<string>() {
                //"BaseDark","BaseLight",
                "Amber","Blue","Brown","Cobalt","Crimson","Cyan", "Emerald","Green",
                "Indigo","Lime","Magenta","Mauve","Olive","Orange", "Pink",
                "Purple","Red","Sienna","Steel","Taupe","Teal","Violet","Yellow"
        };
        private static Setting setting = Setting.Load();
        public static IList<string> Accents
        {
            get { return accents; }
        }

        public static void Toggle()
        {
            Tuple<AppTheme, Accent> appStyle = ThemeManager.DetectAppStyle(Application.Current);
            var appTheme = appStyle.Item1;
            var appAccent = appStyle.Item2;

            var target = ThemeManager.GetInverseAppTheme(appTheme);
            ThemeManager.ChangeAppStyle(Application.Current, appAccent, target);
            setting.Theme = target.Name;
            setting.Save();
        }

        public static string CurrentAccent
        {
            get
            {
                Tuple<AppTheme, Accent> appStyle = ThemeManager.DetectAppStyle(Application.Current);
                AppTheme appTheme = appStyle.Item1;
                Accent appAccent = appStyle.Item2;
                return appAccent.Name;
            }
            set
            {
                Tuple<AppTheme, Accent> appStyle = ThemeManager.DetectAppStyle(Application.Current);
                AppTheme appTheme = appStyle.Item1;
                Accent appAccent = appStyle.Item2;
                ThemeManager.ChangeAppStyle(Application.Current, ThemeManager.GetAccent(value), appTheme);
                setting.Accent = value;
                setting.Save();
            }
        }

        public static string CurrentTheme
        {
            get
            {
                Tuple<AppTheme, Accent> appStyle = ThemeManager.DetectAppStyle(Application.Current);
                AppTheme appTheme = appStyle.Item1;
                Accent appAccent = appStyle.Item2;
                return appTheme.Name;
            }
            set
            {
                Tuple<AppTheme, Accent> appStyle = ThemeManager.DetectAppStyle(Application.Current);
                AppTheme appTheme = appStyle.Item1;
                Accent appAccent = appStyle.Item2;
                ThemeManager.ChangeAppStyle(Application.Current, appAccent, ThemeManager.GetAppTheme(value));
                setting.Theme = value;
                setting.Save();
            }
        }

        public static Color WhiteColor
        {
            get
            {
                Tuple<AppTheme, Accent> appStyle = ThemeManager.DetectAppStyle(Application.Current);
                AppTheme appTheme = appStyle.Item1;
                Accent appAccent = appStyle.Item2;
                return (appTheme.Resources["WhiteColor"] as Brush).ToColor();
            }
        }

        public static Brush WhiteBrush
        {
            get
            {
                Tuple<AppTheme, Accent> appStyle = ThemeManager.DetectAppStyle(Application.Current);
                AppTheme appTheme = appStyle.Item1;
                Accent appAccent = appStyle.Item2;
                return (appTheme.Resources["WhiteBrush"] as Brush);
            }
        }

        public static Color BlackColor
        {
            get
            {
                Tuple<AppTheme, Accent> appStyle = ThemeManager.DetectAppStyle(Application.Current);
                AppTheme appTheme = appStyle.Item1;
                Accent appAccent = appStyle.Item2;
                return (appTheme.Resources["BlackColor"] as Brush).ToColor();
            }
        }

        public static Brush BlackBrush
        {
            get
            {
                Tuple<AppTheme, Accent> appStyle = ThemeManager.DetectAppStyle(Application.Current);
                AppTheme appTheme = appStyle.Item1;
                Accent appAccent = appStyle.Item2;
                return (appTheme.Resources["BlackBrush"] as Brush);
            }
        }

        public static Color AccentColor
        {
            get
            {
                Tuple<AppTheme, Accent> appStyle = ThemeManager.DetectAppStyle(Application.Current);
                AppTheme appTheme = appStyle.Item1;
                Accent appAccent = appStyle.Item2;
                return (Color)appAccent.Resources["AccentColor"];
            }
        }

        public static Brush AccentBrush
        {
            get
            {
                Tuple<AppTheme, Accent> appStyle = ThemeManager.DetectAppStyle(Application.Current);
                AppTheme appTheme = appStyle.Item1;
                Accent appAccent = appStyle.Item2;
                return appAccent.Resources["AccentColorBrush"] as Brush;
            }
        }

        public static Color TextColor
        {
            get
            {
                Tuple<AppTheme, Accent> appStyle = ThemeManager.DetectAppStyle(Application.Current);
                AppTheme appTheme = appStyle.Item1;
                Accent appAccent = appStyle.Item2;
                return (appTheme.Resources["TextBrush"] as Brush).ToColor();
            }
        }

        public static Brush TextBrush
        {
            get
            {
                Tuple<AppTheme, Accent> appStyle = ThemeManager.DetectAppStyle(Application.Current);
                AppTheme appTheme = appStyle.Item1;
                Accent appAccent = appStyle.Item2;
                return (appTheme.Resources["TextBrush"] as Brush);
            }
        }

        public static Color LabelTextColor
        {
            get
            {
                Tuple<AppTheme, Accent> appStyle = ThemeManager.DetectAppStyle(Application.Current);
                AppTheme appTheme = appStyle.Item1;
                Accent appAccent = appStyle.Item2;
                return (appTheme.Resources["LabelTextBrush"] as Brush).ToColor();
            }
        }

        public static Brush LabelTextBrush
        {
            get
            {
                Tuple<AppTheme, Accent> appStyle = ThemeManager.DetectAppStyle(Application.Current);
                AppTheme appTheme = appStyle.Item1;
                Accent appAccent = appStyle.Item2;
                return (appTheme.Resources["LabelTextBrush"] as Brush);
            }
        }

        public static Color IdealForegroundColor
        {
            get
            {
                Tuple<AppTheme, Accent> appStyle = ThemeManager.DetectAppStyle(Application.Current);
                AppTheme appTheme = appStyle.Item1;
                Accent appAccent = appStyle.Item2;
                return (appTheme.Resources["IdealForegroundColorBrush"] as Brush).ToColor();
            }
        }

        public static Brush IdealForegroundBrush
        {
            get
            {
                Tuple<AppTheme, Accent> appStyle = ThemeManager.DetectAppStyle(Application.Current);
                AppTheme appTheme = appStyle.Item1;
                Accent appAccent = appStyle.Item2;
                return (appTheme.Resources["IdealForegroundColorBrush"] as Brush);
            }
        }

        public static Brush IdealForegroundDisableBrush
        {
            get
            {
                Tuple<AppTheme, Accent> appStyle = ThemeManager.DetectAppStyle(Application.Current);
                AppTheme appTheme = appStyle.Item1;
                Accent appAccent = appStyle.Item2;
                return (appTheme.Resources["DarkIdealForegroundDisableBrush"] as Brush);
            }
        }

        public static Brush GrayBrush
        {
            get
            {
                Tuple<AppTheme, Accent> appStyle = ThemeManager.DetectAppStyle(Application.Current);
                AppTheme appTheme = appStyle.Item1;
                Accent appAccent = appStyle.Item2;
                return (appTheme.Resources["GrayBrush6"] as Brush);
            }
        }

        public static Brush GrayBrushs(int index)
        {
            Tuple<AppTheme, Accent> appStyle = ThemeManager.DetectAppStyle(Application.Current);
            AppTheme appTheme = appStyle.Item1;
            Accent appAccent = appStyle.Item2;
            if (index < 1) index = 1;
            else if (index > 10) index = 10;
            return (appTheme.Resources[$"GrayBrush{index}"] as Brush);
        }

        public static Color ToColor(this Brush b, bool prefixsharp = true)
        {
            if (b is SolidColorBrush)
            {
                return (b as SolidColorBrush).Color;
            }
            else
            {
                var hc = b.ToString();//.Replace("#", "");
                var c = System.Drawing.ColorTranslator.FromHtml(hc);
                var rc = Color.FromArgb(c.A, c.R, c.G, c.B);
                return (rc);
            }
        }

        public static string ToHtml(this Brush b, bool prefixsharp = true)
        {
            if (prefixsharp)
                return (b.ToString());
            else
                return (b.ToString().Replace("#", ""));
        }

        public static string ToHtml(this Color c, bool alpha = true, bool prefixsharp = true)
        {
            string result = string.Empty;

            if (alpha)
                result = string.Format("{0:X2}{1:X2}{2:X2}{3:X2}", c.A, c.R, c.G, c.B);
            else
                result = string.Format("{0:X2}{1:X2}{2:X2}", c.R, c.G, c.B);

            if (prefixsharp)
                result = $"#{result}";

            return (result);
        }
    }

}
