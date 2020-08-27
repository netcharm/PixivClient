﻿using MahApps.Metro.Controls;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace PixivWPF.Common
{
    [JsonObject(MemberSerialization.OptOut)]
    public class Setting
    {
        //private static string AppPath = Path.GetDirectoryName(Application.ResourceAssembly.CodeBase.ToString()).Replace("file:\\", "");
        private static string AppPath = Application.Current.Root();
        private static string config = Path.Combine(AppPath, "config.json");
        private static string tagsfile = Path.Combine(AppPath, "tags.json");

        private static Setting Cache = null;// Load(config);
        [JsonIgnore]
        public static Setting Instance
        {
            get { return (Cache); }
        }

        [JsonIgnore]
        private string username = string.Empty;
        [JsonIgnore]
        public string User
        {
            get
            {
                return (username);
            }
            set
            {
                username = value;
            }
        }

        [JsonIgnore]
        private string password = string.Empty;
        [JsonIgnore]
        public string Pass
        {
            get
            {
                return (password);
            }
            set
            {
                password = value;
            }
        }

        [JsonIgnore]
        private Pixeez.Objects.User myinfo = null;
        [JsonIgnore]
        public Pixeez.Objects.User MyInfo
        {
            get { return myinfo; }
            set { myinfo = value; }
        }

        private long update = 0;
        public long Update
        {
            get { return update; }
            set { update = value; }
        }

        private DateTime exptime=DateTime.Now;
        public DateTime ExpTime
        {
            get { return exptime; }
            set { exptime = value; }
        }

        private int expdurtime=3600;
        public int ExpiresIn
        {
            get { return expdurtime; }
            set { expdurtime = value; }
        }

        [JsonIgnore]
        private string lastfolder = string.Empty;
        [JsonIgnore]
        public string LastFolder
        {
            get { return lastfolder; }
            set
            {
                lastfolder = value;
                if (LocalStorage.Count(o => o.Folder.Equals(lastfolder)) <= 0)
                    LocalStorage.Add(new StorageType(lastfolder, true));
            }
        }

        [JsonIgnore]
        public string APP_PATH
        {
            get { return AppPath; }
        }

        private string accesstoken = string.Empty;
        public string AccessToken
        {
            get
            {
                return (accesstoken);
            }
            set
            {
                accesstoken = value;
            }
        }

        private string refreshtoken = string.Empty;
        public string RefreshToken
        {
            get
            {
                return (refreshtoken);
            }
            set
            {
                refreshtoken = value;
            }
        }

        private string proxy = string.Empty;
        public string Proxy
        {
            get
            {
                return (proxy);
            }
            set
            {
                proxy = value;
            }
        }

        private bool useproxy = false;
        public bool UsingProxy
        {
            get
            {
                return (useproxy);
            }
            set
            {
                useproxy = value;
            }
        }

        public string FontName { get; set; } = string.Empty;
        [JsonIgnore]
        private FontFamily fontfamily = SystemFonts.MessageFontFamily;
        [JsonIgnore]
        public FontFamily FontFamily { get { return (fontfamily); } }

        private string theme = string.Empty;
        public string Theme { get; set; }

        private string accent = string.Empty;
        public string Accent { get; set; }

        public bool PrivateFavPrefert { get; set; } = false;
        public bool PrivateBookmarkPrefert { get; set; } = false;

        public AutoExpandMode AutoExpand { get; set; } = AutoExpandMode.AUTO;

        public string ShellSearchBridgeApplication { get; set; } = "PixivWPFSearch.exe";
        public string ShellPixivPediaApplication { get; set; } = "nw.exe";
        public string ShellPixivPediaApplicationArgs { get; set; } = "--single-process --enable-node-worker --app-shell-host-window-size=1280x720";

        public DateTime ContentsTemplateTime { get; set; } = new DateTime(0);
        public string ContentsTemplateFile { get; } = "contents-template.html";
        public string ContentsTemplete { get; set; } = string.Empty;
        [JsonIgnore]
        public string CustomContentsTemplete { get; set; } = string.Empty;

        [JsonIgnore]
        public string SaveFolder { get; set; }

        public List<StorageType> LocalStorage { get; set; } = new List<StorageType>();

        public Point DropBoxPosition { get; set; } = new Point(0, 0);

        [JsonIgnore]
        public bool IsLoading { get; set; } = false;
        public void Save(string configfile = "")
        {
            try
            {
                if (IsLoading) return;

                if (string.IsNullOrEmpty(configfile)) configfile = config;

                if (Cache.LocalStorage.Count(o => o.Folder.Equals(Cache.SaveFolder)) < 0 && !string.IsNullOrEmpty(Cache.SaveFolder))
                {
                    Cache.LocalStorage.Add(new StorageType(Cache.SaveFolder, true));
                }

                if (Cache.LocalStorage.Count(o => o.Folder.Equals(Cache.LastFolder)) < 0 && !string.IsNullOrEmpty(Cache.LastFolder))
                {
                    Cache.LocalStorage.Add(new StorageType(Cache.LastFolder, true));
                }

                var text = JsonConvert.SerializeObject(Cache, Formatting.Indented);
                File.WriteAllText(configfile, text, new UTF8Encoding(true));

                UpdateContentsTemplete();

                SaveTags();
            }
            catch (Exception ex)
            {
                ex.Message.ShowMessageDialog("ERROR");
            }
        }

        private static void UpdateContentsTemplete()
        {
            if (File.Exists(Cache.ContentsTemplateFile))
            {
                Cache.CustomContentsTemplete = File.ReadAllText(Cache.ContentsTemplateFile);
                var ftc = File.GetCreationTime(Cache.ContentsTemplateFile);
                var ftw = File.GetLastWriteTime(Cache.ContentsTemplateFile);
                var fta = File.GetLastAccessTime(Cache.ContentsTemplateFile);
                if (ftw > Cache.ContentsTemplateTime || ftc > Cache.ContentsTemplateTime || fta > Cache.ContentsTemplateTime)
                {
                    Cache.ContentsTemplete = Cache.CustomContentsTemplete;
                    Cache.ContentsTemplateTime = DateTime.Now;
                    Cache.Save();
                }
            }
        }

        public static Setting Load(string configfile = "")
        {
            Setting result = new Setting();
            try
            {
                if (Cache is Setting) result = Cache;
                else
                {
                    if (string.IsNullOrEmpty(configfile)) configfile = config;
                    if (File.Exists(config))
                    {
                        try
                        {
                            var text = File.ReadAllText(configfile);
                            if (text.Length < 20)
                                Cache = new Setting();
                            else
                                Cache = JsonConvert.DeserializeObject<Setting>(text);

                            if (Cache.LocalStorage.Count <= 0 && !string.IsNullOrEmpty(Cache.SaveFolder))
                                Cache.LocalStorage.Add(new StorageType(Cache.SaveFolder, true));

                            Cache.LocalStorage.InitDownloadedWatcher();
#if DEBUG
                            #region Setup UI font
                            if (!string.IsNullOrEmpty(Cache.FontName))
                            {
                                try
                                {
                                    Cache.fontfamily = new FontFamily(Cache.FontName);
                                }
                                catch (Exception)
                                {
                                    Cache.fontfamily = SystemFonts.MessageFontFamily;
                                }
                            }
                            #endregion
#endif
                            #region Update Contents Template
                            UpdateContentsTemplete();
                            #endregion

                            result = Cache;
                        }
                        catch (Exception) { }
                    }
                }
                LoadTags();

            }
#if DEBUG
            catch (Exception ex) { ex.Message.ShowToast("ERROR"); }
#else
            catch (Exception) { }
#endif
            finally
            {
                //loading = false;
            }
            return (result);
        }

        public void SaveTags()
        {
            try
            {
                if (CommonHelper.TagsCache.Count > 0)
                {
                    var tags = JsonConvert.SerializeObject(CommonHelper.TagsCache, Formatting.Indented);
                    File.WriteAllText(tagsfile, tags, new UTF8Encoding(true));
                }
            }
            catch (Exception) { }
        }

        public static void LoadTags()
        {
            if (File.Exists(tagsfile))
            {
                try
                {
                    var tags = File.ReadAllText(tagsfile);
                    CommonHelper.TagsCache = JsonConvert.DeserializeObject<Dictionary<string, string>>(tags);
                }
                catch (Exception) { }
            }
        }

        public static string Token()
        {
            string result = null;
            if (Cache is Setting) result = Cache.AccessToken;
            return (result);
        }

        public static string ProxyServer()
        {
            string result = null;
            if (Cache is Setting) result = Cache.Proxy;
            return (result);
        }

        public static bool UseProxy()
        {
            bool result = false;
            if (Cache is Setting) result = Cache.UsingProxy;
            return (result);
        }

        public static bool Token(string token)
        {
            bool result = false;
            if (Cache is Setting)
            {
                Cache.AccessToken = token;
                result = true;
            }
            return (result);
        }

        public static string GetDeviceId()
        {
            string location = @"SOFTWARE\Microsoft\Cryptography";
            string name = "MachineGuid";

            using (RegistryKey localMachineX64View = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
            {
                using (RegistryKey rk = localMachineX64View.OpenSubKey(location))
                {
                    if (rk == null)
                        throw new KeyNotFoundException(string.Format("Key Not Found: {0}", location));

                    object machineGuid = rk.GetValue(name);
                    if (machineGuid == null)
                        throw new IndexOutOfRangeException(string.Format("Index Not Found: {0}", name));

                    return machineGuid.ToString().Replace("-", "");
                }
            }
        }
    }

}
