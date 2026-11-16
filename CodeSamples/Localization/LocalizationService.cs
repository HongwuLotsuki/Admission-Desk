using System;
using System.Globalization;
using Project22.ResumeSystem;
using UnityEngine;

namespace Project22.UI
{
    public static class LocalizationService
    {
        public const string Chinese = "zh-Hans";
        public const string English = "en";
        private static LocalizationCatalog chinese, english, active;
        private static bool initialized;
        public static event Action LanguageChanged;
        public static string CurrentLanguage { get { EnsureInitialized(); return active.languageCode; } }
        public static bool IsChinese => CurrentLanguage == Chinese;

        public static void EnsureInitialized()
        {
            if (initialized) return;
            chinese = Resources.Load<LocalizationCatalog>("Localization/ChineseSimplified");
            english = Resources.Load<LocalizationCatalog>("Localization/English");
            if (chinese == null || english == null) { Debug.LogError("Localization catalogs are missing. Run Project 22 > Create Localization Catalogs."); return; }
            string saved = Project22Settings.GetString("Project22.Language", "");
            if (saved != Chinese && saved != English)
                saved = English;
            active = saved == Chinese ? chinese : english;
            initialized = true;
        }

        public static void SetLanguage(string language)
        {
            EnsureInitialized();
            if (active == null || (language != Chinese && language != English)) return;
            var next = language == Chinese ? chinese : english;
            if (active == next) return;
            active = next;
            Project22Settings.SetString("Project22.Language", language);
            LanguageChanged?.Invoke();
        }

        public static string Text(string id, string fallback = null)
        {
            EnsureInitialized();
            if (active != null && active.TryGet(id, out var text)) return text;
            return fallback ?? "[" + id + "]";
        }

        public static string TextInLanguage(string id, string language, string fallback = null)
        {
            EnsureInitialized();
            var catalog = language == Chinese ? chinese : language == English ? english : null;
            if (catalog != null && catalog.TryGet(id, out var text)) return text;
            return fallback ?? "[" + id + "]";
        }

        public static string Format(string id, string fallback, params object[] args)
        {
            try { return string.Format(CultureInfo.InvariantCulture, Text(id, fallback), args); }
            catch (FormatException e) { Debug.LogError("Invalid localization format for " + id + ": " + e.Message); return fallback ?? id; }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() { chinese = english = active = null; initialized = false; LanguageChanged = null; }
    }
}
