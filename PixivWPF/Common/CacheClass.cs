﻿using Newtonsoft.Json;
using PixivWPF.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Media;

namespace PixivWPF.Common
{
    class CacheImage
    {
        private Setting setting = Setting.Load();

        private Dictionary<string, string> _caches = new Dictionary<string, string>();
        private string _CacheFolder = string.Empty;
        private string _CacheDB = string.Empty;

        public CacheImage()
        {
            _CacheFolder = Path.Combine(setting.APPPATH, "cache");
            _CacheDB = Path.Combine(setting.APPPATH, "cache.json");

            Load();
        }

        public void Load()
        {
            if (File.Exists(_CacheDB))
            {
                var text = File.ReadAllText(_CacheDB);
                if (text.Length > 20)
                    _caches = JsonConvert.DeserializeObject<Dictionary<string, string>>(text);
            }
        }
        
        public void Save()
        {
            var text = JsonConvert.SerializeObject(_caches, Formatting.Indented);
            File.WriteAllText(_CacheDB, text, new UTF8Encoding(true));
        }

        public async Task<ImageSource> GetImage(string url)
        {
            var tokens = await CommonHelper.ShowLogin();
            if (tokens == null) return null;

            return (await GetImage(url));
        }

        public async Task<ImageSource> GetImage(string url, Pixeez.Tokens tokens)
        {
            ImageSource result = null;
            var trimchars = new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
            var file = Regex.Replace(url, @"http(s)*://.*?\.((pixiv\..*?)|(pximg\..*?))/", $"", RegexOptions.IgnoreCase);
            file = file.Replace("/", "\\").TrimStart(trimchars);
            file = Path.Combine(_CacheFolder, file);

            if (_caches.ContainsKey(url))
            {
                var fcache = _caches[url].TrimStart(trimchars);
                file = Path.Combine(_CacheFolder, fcache);
                if (File.Exists(file))
                {
                    result = await file.LoadImage();
                }
                else
                {
                    tokens = await CommonHelper.ShowLogin();
                    if (tokens == null) return null;

                    var success = await url.ToImageFile(tokens, file, false);
                    if (success)
                    {
                        result = await file.LoadImage();
                    }
                }
            }
            else if (File.Exists(file))
            {
                result = await file.LoadImage();
                if (result is ImageSource)
                {
                    _caches[url] = file.Replace(_CacheFolder, "");
                    Save();
                }
            }
            else
            {
                tokens = await CommonHelper.ShowLogin();
                if (tokens == null) return null;

                var success = await url.ToImageFile(tokens, file, false);
                if (success)
                {
                    result = await file.LoadImage();
                    _caches[url] = file.Replace(_CacheFolder, "");
                    Save();
                }
            }
            return (result);
        }

    }
}
