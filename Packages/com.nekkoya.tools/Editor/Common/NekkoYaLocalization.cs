using System;
using UnityEditor;
using UnityEngine;

namespace NekkoYa.Editor
{
    public enum NekkoYaLanguage { JP, EN, KR }

    [InitializeOnLoad]
    public static class NekkoYaLocalization
    {
        private const string PreferenceKey = "NekkoYa.Language";
        public static event Action LanguageChanged;

        static NekkoYaLocalization()
        {
            EditorApplication.delayCall += RefreshMenus;
        }

        public static NekkoYaLanguage Language
        {
            get
            {
                int value = EditorPrefs.GetInt(PreferenceKey, 0);
                return value >= 0 && value <= 2 ? (NekkoYaLanguage)value : NekkoYaLanguage.JP;
            }
            set
            {
                if (!Enum.IsDefined(typeof(NekkoYaLanguage), value))
                    throw new ArgumentOutOfRangeException(nameof(value));
                if (value == Language) return;
                EditorPrefs.SetInt(PreferenceKey, (int)value);
                RefreshMenus();
                LanguageChanged?.Invoke();
                foreach (var window in Resources.FindObjectsOfTypeAll<EditorWindow>())
                    if (window.GetType().Namespace?.StartsWith("NekkoYa", StringComparison.Ordinal) == true)
                        window.Repaint();
            }
        }

        public static string Text(string japanese)
        {
            if (japanese == null || Language == NekkoYaLanguage.JP) return japanese;
            return NekkoYaTranslations.Entries.TryGetValue(japanese, out var translated)
                ? translated[Language == NekkoYaLanguage.EN ? 0 : 1] : japanese;
        }

        [MenuItem("NekkoYa/Language/JP", false, 10000)]
        private static void SelectJapanese() => Language = NekkoYaLanguage.JP;
        [MenuItem("NekkoYa/Language/EN", false, 10001)]
        private static void SelectEnglish() => Language = NekkoYaLanguage.EN;
        [MenuItem("NekkoYa/Language/KR", false, 10002)]
        private static void SelectKorean() => Language = NekkoYaLanguage.KR;

        [MenuItem("NekkoYa/Language/JP", true)]
        private static bool ValidateJapanese() { RefreshMenus(); return true; }
        [MenuItem("NekkoYa/Language/EN", true)]
        private static bool ValidateEnglish() { RefreshMenus(); return true; }
        [MenuItem("NekkoYa/Language/KR", true)]
        private static bool ValidateKorean() { RefreshMenus(); return true; }

        private static void RefreshMenus()
        {
            Menu.SetChecked("NekkoYa/Language/JP", Language == NekkoYaLanguage.JP);
            Menu.SetChecked("NekkoYa/Language/EN", Language == NekkoYaLanguage.EN);
            Menu.SetChecked("NekkoYa/Language/KR", Language == NekkoYaLanguage.KR);
        }
    }
}
