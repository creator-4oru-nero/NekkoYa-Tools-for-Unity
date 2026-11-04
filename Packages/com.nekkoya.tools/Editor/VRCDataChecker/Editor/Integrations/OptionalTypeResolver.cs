using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NekkoYa.VRCDataChecker
{
    public static class OptionalTypeResolver
    {
        private static readonly Dictionary<string, Type> Cache = new Dictionary<string, Type>();
        public static Type Find(string fullName)
        {
            if (Cache.TryGetValue(fullName, out var type)) return type;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(fullName, false);
                if (type != null) break;
            }
            Cache[fullName] = type;
            return type;
        }
        public static bool IsInstalled(string fullName) => Find(fullName) != null;
    }

    public static class ShaderDependencyClassifier
    {
        public static DependencyKind Classify(string path, CheckContext context)
        {
            if (AssetCollector.IsInside(path, context.rootFolder)) return DependencyKind.Internal;
            if (path.IndexOf("unity_builtin_extra", StringComparison.OrdinalIgnoreCase) >= 0 || path.IndexOf("unity default resources", StringComparison.OrdinalIgnoreCase) >= 0) return DependencyKind.UnityBuiltIn;
            foreach (var entry in context.settings.dependencyPatterns)
                if (!string.IsNullOrWhiteSpace(entry.pattern) && CheckerSettings.Matches(path, entry.pattern)) return entry.kind;
            string value = path.ToLowerInvariant();
            if (value.Contains("com.vrchat") || value.Contains("/vrcsdk/")) return DependencyKind.VRCSDK;
            if (value.Contains("modular-avatar") || value.Contains("modularavatar")) return DependencyKind.ModularAvatar;
            if (value.Contains("vrcfury")) return DependencyKind.VRCFury;
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
            value += " " + (shader != null ? shader.name.ToLowerInvariant() : "");
            if (value.Contains("liltoon")) return DependencyKind.LilToon;
            if (value.Contains("poiyomi")) return DependencyKind.Poiyomi;
            if (value.StartsWith("packages/")) return DependencyKind.Package;
            return string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(path)) ? DependencyKind.Missing : DependencyKind.OtherExternal;
        }
    }
}
