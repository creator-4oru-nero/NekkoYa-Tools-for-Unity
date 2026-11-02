using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Nekkoya.VRCDataChecker
{
    [Serializable]
    public sealed class CheckerThresholds
    {
        public int generalTextureWarningSize = 4096, maskTextureWarningSize = 2048, matcapTextureWarningSize = 2048;
        public int rampTextureWarningSize = 512, iconTextureWarningSize = 1024;
        public int materialKeywordWarningCount = 32, shaderPassWarningCount = 4;
        public int particleMaxCountWarning = 1000, longPathWarningLength = 180;
        public float avatarBoundsWarningMeters = 5f;
    }
    [Serializable] public sealed class DependencyPattern { public string pattern; public DependencyKind kind; }

    [FilePath("ProjectSettings/NekkoyaVRCDataChecker.asset", FilePathAttribute.Location.ProjectFolder)]
    public sealed class CheckerSettings : ScriptableSingleton<CheckerSettings>
    {
        public CheckerThresholds thresholds = new CheckerThresholds();
        public string exclusions = "Documentation\nDocs\nPreview\nScreenshots\nSource\nPSD\n*.md\n*.txt\nLICENSE*\nREADME*";
        public string ignoredReferenceProperties = "";
        public string keepPatterns = "*.psd\n*.blend";
        public bool checkUnused = true;
        public string recommendedAndroidFormats = "ASTC";
        public List<DependencyPattern> dependencyPatterns = new List<DependencyPattern>();
        public void SaveSettings() => Save(true);
        public static bool Matches(string path, string patterns)
        {
            foreach (var raw in (patterns ?? "").Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var pattern = raw.Trim();
                if (pattern.Length == 0) continue;
                var regex = "^" + Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$";
                if (Regex.IsMatch(path, regex, RegexOptions.IgnoreCase)) return true;
                foreach (var part in path.Replace('\\', '/').Split('/'))
                    if (Regex.IsMatch(part, regex, RegexOptions.IgnoreCase)) return true;
            }
            return false;
        }
    }
}
