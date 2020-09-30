﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Speech.Synthesis;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace PixivWPF.Common
{
    public class SpeechTTS
    {
        #region Speech Synthesis routines
        public static List<InstalledVoice> InstalledVoices { get; private set; } = null;

        private static Dictionary<CultureInfo, List<string>> nametable = new Dictionary<CultureInfo, List<string>>() {
            { CultureInfo.GetCultureInfo("zh-CN"), new List<string>() { "huihui", "yaoyao", "lili", "kangkang" } },
            { CultureInfo.GetCultureInfo("zh-TW"), new List<string>() { "hanhan", "yating", "zhiwei" } },
            { CultureInfo.GetCultureInfo("ja-JP"), new List<string>() { "haruka", "ayumi", "sayaka", "ichiro" } },
            { CultureInfo.GetCultureInfo("ko-KR"), new List<string>() { "heami" } },
            { CultureInfo.GetCultureInfo("en-US"), new List<string>() { "david", "zira", "mark", "eva" } }
        };

        private SpeechSynthesizer synth = null;
        private string voice_default = string.Empty;
        //private bool SPEECH_AUTO = false;
        private bool SPEECH_SLOW = false;
        private string SPEECH_TEXT = string.Empty;
        private CultureInfo SPEECH_CULTURE = null;

        public static Dictionary<string, string> GetNames()
        {
            var result = nametable.Select(n => new KeyValuePair<string, string>(n.Key.IetfLanguageTag, string.Join(", ", n.Value)));
            return (result.ToDictionary(kv => kv.Key, kv => kv.Value));
        }

        public static Dictionary<CultureInfo, List<string>> SetNames(Dictionary<string, string> names)
        {
            var result = names.Select(n => new KeyValuePair<CultureInfo, List<string>>(CultureInfo.GetCultureInfo(n.Key.Trim()), n.Value.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).Select(v => v.Trim()).ToList()));
            return (result.ToDictionary(kv => kv.Key, kv => kv.Value));
        }

        public static void SetCustomNames(Dictionary<string, string> names)
        {
            nametable = SetNames(names);
        }

        private void Synth_StateChanged(object sender, StateChangedEventArgs e)
        {
            if (synth == null) return;

            if (synth.State == SynthesizerState.Paused)
            {

            }
            else if (synth.State == SynthesizerState.Speaking)
            {

            }
            else if (synth.State == SynthesizerState.Ready)
            {

            }
        }

        private void Synth_SpeakStarted(object sender, SpeakStartedEventArgs e)
        {

        }

        private void Synth_SpeakProgress(object sender, SpeakProgressEventArgs e)
        {
            //throw new NotImplementedException();
        }

        private async void Synth_SpeakCompleted(object sender, SpeakCompletedEventArgs e)
        {
            if (IsCompleted is Action) await IsCompleted.InvokeAsync();
        }
        #endregion

        public CultureInfo Culture { get; set; } = CultureInfo.CurrentCulture;
        public Action IsCompleted { get; set; } = null;

        private CultureInfo DetectCulture(string text)
        {
            CultureInfo result = CultureInfo.CurrentCulture;

            //
            // 中文：[\u4e00-\u9fcc, \u3400-\u4db5, \u20000-\u2a6d6, \u2a700-\u2b734, \u2b740-\u2b81d, \uf900-\ufad9, \u2f800-\u2fa1d]
            // 繁体标点: [\u3000-\u3003, \u3008-\u300F, \u3010-\u3011, \u3014-\u3015, \u301C-\u301E]
            // BIG-5: [\ue000-\uf848]
            // 日文：[\u0800-\u4e00] [\u3041-\u31ff]
            // 韩文：[\uac00-\ud7ff]
            //
            //var m_jp = Regex.Matches(text, @"([\u0800-\u4e00])", RegexOptions.Multiline | RegexOptions.IgnoreCase);
            //var m_zh = Regex.Matches(text, @"([\u4e00-\u9fbb])", RegexOptions.Multiline | RegexOptions.IgnoreCase);
            var GBK = Encoding.GetEncoding("GBK");
            var BIG5 = Encoding.GetEncoding("BIG5");
            var JAP = Encoding.GetEncoding("Shift-JIS");
            var UTF8 = Encoding.UTF8;

            if (Regex.Matches(text, @"[\u3041-\u31ff]", RegexOptions.Multiline).Count > 0)
            {
                result = CultureInfo.GetCultureInfoByIetfLanguageTag("ja-JP");
            }
            else if (Regex.Matches(text, @"[\uac00-\ud7ff]", RegexOptions.Multiline).Count > 0)
            {
                result = CultureInfo.GetCultureInfoByIetfLanguageTag("ko-KR");
            }
            else if (Regex.Matches(text, @"[\u3400-\u4dbf\u4e00-\u9fbb]", RegexOptions.Multiline).Count > 0)
            {
                result = CultureInfo.GetCultureInfoByIetfLanguageTag("zh-CN");
            }
            //else if (GBK.GetString(GBK.GetBytes(text)).Equals(text))
            //{
            //    result = CultureInfo.GetCultureInfoByIetfLanguageTag("zh-CN");
            //}
            else if (Regex.Matches(text, @"[\u3000-\u3003\u3008-\u300F\u3010-\u3011\u3014-\u3015\u301C-\u301E\ua140-\ua3bf\ua440-\uc67e\uc940-\uf9d5\ue000-\uf848]", RegexOptions.Multiline).Count > 0)
            {
                result = CultureInfo.GetCultureInfoByIetfLanguageTag("zh-TW");
            }
            else
            {
                result = CultureInfo.GetCultureInfoByIetfLanguageTag("en-US");
            }

            return (result);
        }

        private InstalledVoice GetVoice(CultureInfo culture)
        {
            InstalledVoice result = null;
            if (culture is CultureInfo)
            {
                foreach (InstalledVoice voice in synth.GetInstalledVoices())
                {
                    VoiceInfo info = voice.VoiceInfo;
                    var vl = info.Culture.IetfLanguageTag;
                    if (vl.Equals(culture.IetfLanguageTag, StringComparison.CurrentCultureIgnoreCase))
                    {
                        result = voice;
                        break;
                    }
                }
            }
            return (result);
        }

        private string GetVoiceName(CultureInfo culture)
        {
            string result = string.Empty;
            if (culture is CultureInfo)
            {
                foreach (InstalledVoice voice in synth.GetInstalledVoices())
                {
                    VoiceInfo info = voice.VoiceInfo;
                    var vl = info.Culture.IetfLanguageTag;
                    if (vl.Equals(culture.IetfLanguageTag, StringComparison.CurrentCultureIgnoreCase))
                    {
                        result = voice.VoiceInfo.Name;
                        break;
                    }
                }
            }
            return (result);
        }

        private string GetCustomVoiceName(CultureInfo culture)
        {
            string result = string.Empty;
            var nvs = GetVoiceNames();
            if (nvs.ContainsKey(culture))
            {
                //string[] ns = new string[] {"huihui", "yaoyao", "lili", "yating", "hanhan", "haruka", "ayumi", "heami", "david", "zira"};
                foreach (var n in nametable[culture])
                {
                    var found = false;
                    foreach (var nl in nvs[culture])
                    {
                        var nll = nl.ToLower();
                        if (nll.Contains(n))
                        {
                            result = nl;
                            found = true;
                            break;
                        }
                    }
                    if (found) break;
                }
            }
            if (string.IsNullOrEmpty(result)) result = GetVoiceName(culture);
            return (result);
        }

        private Dictionary<CultureInfo, List<string>> GetVoiceNames()
        {
            Dictionary<CultureInfo, List<string>> result = new Dictionary<CultureInfo, List<string>>();
            foreach (InstalledVoice voice in synth.GetInstalledVoices())
            {
                VoiceInfo info = voice.VoiceInfo;
                if (result.ContainsKey(info.Culture))
                {
                    result[info.Culture].Add(info.Name);
                    result[info.Culture].Sort();
                }
                else
                    result[info.Culture] = new List<string>() { info.Name };
            }
            return (result);
        }

        public void Play(string text, CultureInfo locale = null, bool async = true)
        {
            if (!(synth is SpeechSynthesizer)) return;

            var voices = synth.GetInstalledVoices();
            if (voices.Count <= 0) return;

            if (synth.State == SynthesizerState.Paused)
            {
                synth.Resume();
                return;
            }

            try
            {
                synth.SpeakAsyncCancelAll();
                synth.Resume();

                synth.SelectVoice(voice_default);

                if (!(locale is CultureInfo)) locale = DetectCulture(text);

                var voice = GetCustomVoiceName(locale);
                if(!string.IsNullOrEmpty(voice)) synth.SelectVoice(voice);

                //synth.Volume = 100;  // 0...100
                //synth.Rate = 0;     // -10...10
                if (text.Equals(SPEECH_TEXT, StringComparison.CurrentCultureIgnoreCase) && 
                    SPEECH_CULTURE.IetfLanguageTag.Equals(locale.IetfLanguageTag, StringComparison.CurrentCultureIgnoreCase))
                    SPEECH_SLOW = !SPEECH_SLOW;
                else
                    SPEECH_SLOW = false;

                if (SPEECH_SLOW) synth.Rate = -5;
                else synth.Rate = 0;

                synth.SpeakAsyncCancelAll();
                synth.Resume();

                if (async)
                    synth.SpeakAsync(text);  // Asynchronous
                else
                    synth.Speak(text);       // Synchronous

                SPEECH_TEXT = text;
                SPEECH_CULTURE = locale;
            }
#if DEBUG
            catch (Exception ex)
            {
                ex.Message.DEBUG();
            }
#else
            catch (Exception ){}
#endif            
        }

        public void Play(PromptBuilder prompt, CultureInfo locale = null, bool async = true)
        {
            if (!(synth is SpeechSynthesizer)) return;

            if (!(prompt is PromptBuilder) || prompt.IsEmpty) return;

            var voices = synth.GetInstalledVoices();
            if (voices.Count <= 0) return;

            if (synth.State == SynthesizerState.Paused)
            {
                synth.Resume();
                return;
            }

            try
            {
                synth.SpeakAsyncCancelAll();
                synth.Resume();

                synth.SelectVoice(voice_default);

                if (!(locale is CultureInfo)) locale = prompt.Culture;

                var voice = GetCustomVoiceName(locale);
                if (!string.IsNullOrEmpty(voice)) synth.SelectVoice(voice);

                //synth.Volume = 100;  // 0...100
                //synth.Rate = 0;     // -10...10
                var prompt_xml = prompt.ToXml();
                if (prompt_xml.Equals(SPEECH_TEXT, StringComparison.CurrentCultureIgnoreCase) &&
                    SPEECH_CULTURE.IetfLanguageTag.Equals(locale.IetfLanguageTag, StringComparison.CurrentCultureIgnoreCase))
                    SPEECH_SLOW = !SPEECH_SLOW;
                else
                    SPEECH_SLOW = false;

                if (SPEECH_SLOW) synth.Rate = -5;
                else synth.Rate = 0;

                synth.SpeakAsyncCancelAll();
                synth.Resume();

                if (async)
                    synth.SpeakAsync(prompt);  // Asynchronous
                else
                    synth.Speak(prompt);       // Synchronous

                SPEECH_TEXT = prompt_xml;
                SPEECH_CULTURE = prompt.Culture;
            }
#if DEBUG
            catch (Exception ex)
            {
                ex.Message.DEBUG();
            }
#else
            catch (Exception) { }
#endif            
        }

        public void Play(IEnumerable<string> contents, CultureInfo locale = null)
        {
            var prompt = new PromptBuilder();
            prompt.ClearContent();
            foreach (var text in contents)
            {
                var culture = locale == null ? DetectCulture(text) : locale;
                prompt.StartParagraph(culture);
                prompt.StartSentence(culture);
                prompt.StartVoice(GetCustomVoiceName(culture));
                prompt.AppendText(text);
                prompt.EndVoice();
                prompt.EndSentence();
                prompt.EndParagraph();
            }
            Play(prompt);
        }

        public void Pause()
        {
            if (synth is SpeechSynthesizer &&
                synth.State == SynthesizerState.Speaking)
            {
                synth.Pause();
            }                
        }

        public void Resume()
        {
            if (synth is SpeechSynthesizer && 
                synth.State == SynthesizerState.Paused){
                    synth.Resume();
            }
        }

        public void Stop()
        {
            if (synth is SpeechSynthesizer)
            {
                synth.SpeakAsyncCancelAll();
                synth.Resume();
            }
        }

        public SpeechTTS()
        {
            try
            {
                #region Synthesis
                synth = new SpeechSynthesizer();
                synth.SpeakStarted += Synth_SpeakStarted;
                synth.SpeakProgress += Synth_SpeakProgress;
                synth.StateChanged += Synth_StateChanged;
                synth.SpeakCompleted += Synth_SpeakCompleted;
                #endregion

                voice_default = synth.Voice.Name;
                InstalledVoices = synth.GetInstalledVoices().ToList();
            }
            catch (Exception) { synth = null; }
        }
    }

}
