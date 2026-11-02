using System;
using System.Collections.Generic;

namespace NekkoYa.AssetOrganizer.Editor
{
    internal static class AssetNameValidator
    {
        private static readonly char[] InvalidCharacters = { '/', '\\', ':', '*', '?', '"', '<', '>', '|' };

        public static string Validate(string name, string label)
        {
            if (string.IsNullOrWhiteSpace(name))
                return L(label + " is empty.", label + "が空です。");
            if (name.IndexOfAny(InvalidCharacters) >= 0)
                return L(label + " contains an invalid character: / \\ : * ? \" < > |",
                    label + "に使用できない文字が含まれています: / \\ : * ? \" < > |");
            if (name == "." || name == "..")
                return L(label + " cannot be '.' or '..'.", label + "に「.」または「..」は使用できません。");
            if (name.EndsWith(".", StringComparison.Ordinal) || name.EndsWith(" ", StringComparison.Ordinal))
                return L(label + " cannot end with a period or space.", label + "の末尾にピリオドや空白は使用できません。");

            foreach (char character in name)
            {
                if (char.IsControl(character))
                    return L(label + " contains a control character.", label + "に制御文字が含まれています。");
            }

            return null;
        }

        public static void ValidateSiblingNames<T>(IReadOnlyList<T> items, Func<T, string> getName,
            string location, List<string> errors)
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < items.Count; i++)
            {
                string name = getName(items[i]);
                string error = Validate(name,
                    L("Folder name at " + location, location + "のフォルダー名"));
                if (error != null)
                    errors.Add(error);
                else if (!names.Add(name))
                    errors.Add(L("Duplicate folder '" + name + "' at " + location + ".",
                        location + "に同名フォルダー「" + name + "」が複数あります。"));
            }
        }

        private static string L(string english, string japanese)
        {
            return OrganizerLocalization.Text(english, japanese);
        }
    }
}
