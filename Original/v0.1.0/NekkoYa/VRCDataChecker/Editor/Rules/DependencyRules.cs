using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Nekkoya.VRCDataChecker
{
    public static class DependencyRules
    {
        public static void Evaluate(CheckContext context)
        {
            foreach (var path in context.dependencyGraph.recursive.OrderBy(p => p))
            {
                CheckRunner.Guard(context, path, () =>
                {
                    var kind = ShaderDependencyClassifier.Classify(path, context);
                    if (kind == DependencyKind.Internal || kind == DependencyKind.UnityBuiltIn) return;
                    bool package = IsPackagePath(path);
                    var severity = package ? CheckSeverity.Info : kind == DependencyKind.Missing ? CheckSeverity.Error : kind == DependencyKind.OtherExternal ? CheckSeverity.Warning : CheckSeverity.Info;
                    context.Add(package ? "DEP_EXTERNAL_001" : kind == DependencyKind.Missing ? "DEP_MISSING_001" : kind == DependencyKind.OtherExternal ? "DEP_EXTERNAL_002" : "DEP_EXTERNAL_001",
                        severity, CheckCategory.Dependency, path, "フォルダ外依存: " + kind,
                        "配布許諾と導入前提を確認し、必要な依存パッケージをREADMEに記載してください。");
                    var parent = AssetCollector.Normalize(Path.GetDirectoryName(context.rootFolder));
                    if (kind == DependencyKind.OtherExternal && parent != "Assets" && AssetCollector.IsInside(path, parent))
                        context.Add("DEP_PRODUCT_001", CheckSeverity.Warning, CheckCategory.Dependency, path, "兄弟フォルダへの参照です。別商品のデータを参照している可能性があります。");
                });
            }
            foreach (var pair in context.dependencyGraph.direct.Where(x => AssetCollector.IsInside(x.Key, context.rootFolder)))
            {
                if (IsEditorPath(pair.Key) || pair.Key.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) || pair.Key.EndsWith(".asmdef", StringComparison.OrdinalIgnoreCase)) continue;
                foreach (var dependency in pair.Value.Where(p => IsEditorPath(p) && !IsPackagePath(p)))
                    context.Add("DEP_EDITOR_001", CheckSeverity.Error, CheckCategory.Dependency, pair.Key, "Runtime AssetからEditorフォルダ内のAssetを参照しています: " + dependency,
                        "ビルド対象で必要な参照か確認してください。Editor専用かどうかは配置パスからの推定です。");
            }
        }
        private static bool IsEditorPath(string path) => path.Split('/').Any(p => string.Equals(p, "Editor", StringComparison.OrdinalIgnoreCase));
        public static bool IsPackagePath(string path) => AssetCollector.Normalize(path).StartsWith("Packages/", StringComparison.OrdinalIgnoreCase);
        public static void Unused(CheckContext context, Action<string, float> progress)
        {
            var reached = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var queue = new Queue<string>(context.internalAssetPaths.Where(p => p.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase) || p.EndsWith(".unity", StringComparison.OrdinalIgnoreCase) ||
                CheckerSettings.Matches(p, "README*\nLICENSE*\nDocumentation\nDocs\nSource\n*.cs\n*.asmdef\n*.asmref") || CheckerSettings.Matches(p, context.settings.keepPatterns)));
            while (queue.Count > 0)
            {
                string path = queue.Dequeue();
                if (!reached.Add(path)) continue;
                progress("未使用候補解析: " + path, 0.95f);
                if (context.dependencyGraph.direct.TryGetValue(path, out var dependencies)) foreach (var dependency in dependencies) queue.Enqueue(dependency);
            }
            foreach (var path in context.internalAssetPaths)
            {
                if (!context.assets.TryGetValue(path, out var info) || info.isIgnored || CheckerSettings.Matches(path, context.settings.keepPatterns)) continue;
                bool texture = info.mainType != null && typeof(UnityEngine.Texture).IsAssignableFrom(info.mainType);
                if (texture && context.materialUsageComplete && (!context.textureUses.TryGetValue(path, out var uses) || uses.Count == 0))
                {
                    bool retained = context.textureReferences.TryGetValue(path, out var references) && references.Count > 0;
                    context.Add("UNUSED_TEXTURE_001", CheckSeverity.Info, CheckCategory.UnusedAsset, path,
                        retained ? "Materialの無効な機能／古い設定にだけ画像が残っています。現在の有効なMaterial設定では使われていません。" : "調査したMaterialのどの画像欄にも設定されていません。",
                        "Assets全体のMaterial（Model内も含む）と検査対象の依存Materialを確認しました。UI・メニューアイコン・Animation・スクリプトから使う画像の可能性もあるため、削除してよいという判定ではありません。機能をAnimationでONにする場合も確認してください。");
                }
                else if (!reached.Contains(path) && !(texture && context.materialUsageComplete))
                    context.Add("UNUSED_ASSET_001", CheckSeverity.Info, CheckCategory.UnusedAsset, path, "未使用の可能性があります。Entry Pointからの参照を確認できませんでした。",
                        "スクリプトによる動的読み込みや素材配布用途は判定できません。必要なデータは保持対象に設定してください。");
            }
        }
    }
}
