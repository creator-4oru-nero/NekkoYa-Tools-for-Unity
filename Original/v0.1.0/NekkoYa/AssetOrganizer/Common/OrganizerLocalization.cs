using UnityEditor;
using UnityEngine;

namespace NekkoYa.AssetOrganizer.Editor
{
    internal enum OrganizerLanguage
    {
        English,
        Japanese
    }

    internal static class OrganizerLocalization
    {
        private const string EditorPrefsKey = "NekkoYa.AssetOrganizer.Language";

        public static OrganizerLanguage Language
        {
            get
            {
                int defaultValue = Application.systemLanguage == SystemLanguage.Japanese
                    ? (int)OrganizerLanguage.Japanese
                    : (int)OrganizerLanguage.English;
                return (OrganizerLanguage)EditorPrefs.GetInt(EditorPrefsKey, defaultValue);
            }
            set => EditorPrefs.SetInt(EditorPrefsKey, (int)value);
        }

        public static string Text(string english, string japanese)
        {
            return Language == OrganizerLanguage.Japanese ? japanese : english;
        }
    }
}
