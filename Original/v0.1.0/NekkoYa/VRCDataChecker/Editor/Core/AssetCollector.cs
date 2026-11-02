using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Nekkoya.VRCDataChecker
{
    public static class AssetCollector
    {
        public static string Normalize(string path) => (path ?? "").Replace('\\', '/').TrimEnd('/');
        public static bool IsInside(string path, string root) => Normalize(path).StartsWith(Normalize(root) + "/", StringComparison.OrdinalIgnoreCase);
        public static string Validate(string path)
        {
            path = Normalize(path);
            if (string.IsNullOrEmpty(path)) return "検査フォルダを指定してください。";
            if (path != "Assets" && !path.StartsWith("Assets/", StringComparison.Ordinal)) return "Assets以下のフォルダを指定してください。Packagesは指定できません。";
            return AssetDatabase.IsValidFolder(path) ? null : "指定されたAssetはフォルダではありません。";
        }
        public static string[] Enumerate(string root) => AssetDatabase.FindAssets(string.Empty, new[] { Normalize(root) })
            .Select(AssetDatabase.GUIDToAssetPath).Select(Normalize).Where(p => !AssetDatabase.IsValidFolder(p) && IsInside(p, root))
            .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(p => p, StringComparer.Ordinal).ToArray();
        public static long FileSize(string path)
        {
            if (!path.StartsWith("Assets/", StringComparison.Ordinal)) return 0;
            var absolute = Path.GetFullPath(Path.Combine(Application.dataPath, path.Substring(7)));
            var dataRoot = Path.GetFullPath(Application.dataPath).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            return absolute.StartsWith(dataRoot, StringComparison.OrdinalIgnoreCase) && File.Exists(absolute) ? new FileInfo(absolute).Length : 0;
        }
        public static Dictionary<string, AssetInfo> Collect(CheckContext context, Action<string, float> progress)
        {
            var paths = Enumerate(context.rootFolder);
            context.internalAssetPaths = paths;
            var assets = new Dictionary<string, AssetInfo>(StringComparer.OrdinalIgnoreCase);
            context.assets = assets;
            for (int i = 0; i < paths.Length; i++)
            {
                string path = paths[i];
                progress("Asset列挙: " + path, (float)i / Math.Max(1, paths.Length));
                CheckRunner.Guard(context, path, () => assets[path] = new AssetInfo { path = path,
                    guid = AssetDatabase.AssetPathToGUID(path), mainType = AssetDatabase.GetMainAssetTypeAtPath(path),
                    fileSizeBytes = FileSize(path), isInsideRoot = true,
                    isIgnored = CheckerSettings.Matches(path.Substring(context.rootFolder.Length + 1), context.settings.exclusions) });
            }
            return assets;
        }
        public static void BuildDependencies(CheckContext context, Dictionary<string, AssetInfo> assets, Action<string, float> progress)
        {
            // Traverse external nodes too so a transitive dependency has its actual direct referrer.
            var queue = new Queue<string>(context.internalAssetPaths);
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int count = 0;
            while (queue.Count > 0)
            {
                var path = queue.Dequeue();
                if (!visited.Add(path)) continue;
                progress("依存関係解析: " + path, (float)count / Math.Max(1, count + queue.Count + 1));
                count++;
                CheckRunner.Guard(context, path, () =>
                {
                    var dependencies = AssetDatabase.GetDependencies(path, false).Select(Normalize)
                        .Where(p => !string.Equals(p, path, StringComparison.OrdinalIgnoreCase)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                    context.dependencyGraph.direct[path] = dependencies;
                    if (!assets.TryGetValue(path, out var info))
                        assets[path] = info = new AssetInfo { path = path, guid = AssetDatabase.AssetPathToGUID(path), mainType = AssetDatabase.GetMainAssetTypeAtPath(path) };
                    info.directDependencies = dependencies;
                    foreach (var dependency in dependencies)
                    {
                        context.dependencyGraph.recursive.Add(dependency);
                        if (dependency.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase) || dependency.StartsWith("Packages/", StringComparison.OrdinalIgnoreCase)) queue.Enqueue(dependency);
                    }
                });
            }
            foreach (var asset in assets.Values) asset.referencedBy = context.dependencyGraph.ReferencedBy(asset.path);
        }
    }
}
