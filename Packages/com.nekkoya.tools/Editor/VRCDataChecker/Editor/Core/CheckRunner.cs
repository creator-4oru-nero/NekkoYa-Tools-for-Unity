using L10n = NekkoYa.Editor.NekkoYaLocalization;
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace NekkoYa.VRCDataChecker
{
    public static class CheckRunner
    {
        public static CheckContext Run(string folder, CheckTargetMode mode, TargetPlatformMode platforms, CheckerSettings settings,
            Func<string, float, bool> cancel = null, Action clearProgress = null)
        {
            folder = AssetCollector.Normalize(folder);
            var error = AssetCollector.Validate(folder);
            if (error != null) throw new ArgumentException(error);
            if (platforms == 0) throw new ArgumentException(L10n.Text("対象プラットフォームを1つ以上選んでください。"));
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) throw new InvalidOperationException(L10n.Text("コンパイル／Import終了後に検査してください。"));
            var context = new CheckContext { rootFolder = folder, targetMode = mode, platforms = platforms, settings = settings };
            var selection = Selection.instanceIDs;
            var active = Selection.activeInstanceID;
            cancel = cancel ?? ((label, amount) => EditorUtility.DisplayCancelableProgressBar("VRC Data Checker", label, amount));
            clearProgress = clearProgress ?? EditorUtility.ClearProgressBar;
            Action<string, float> progress = (label, amount) => { if (cancel(label, amount)) throw new OperationCanceledException(); };
            try
            {
                var assets = AssetCollector.Collect(context, progress);
                AssetCollector.BuildDependencies(context, assets, progress);
                DependencyRules.Evaluate(context);
                // Prefabs establish actual renderer/mesh usage before texture rules run.
                var prefabs = context.internalAssetPaths.Where(p => p.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase)).ToArray();
                for (int i = 0; i < prefabs.Length; i++)
                {
                    progress(L10n.Text("Prefab検査: ") + prefabs[i], (float)i / Math.Max(1, prefabs.Length));
                    var path = prefabs[i];
                    Guard(context, path, () => PrefabScanner.Scan(context, path, progress));
                }
                var paths = context.internalAssetPaths;
                MaterialRules.CollectProjectTextureUses(context, progress);
                for (int i = 0; i < paths.Count; i++)
                {
                    string path = paths[i];
                    progress(L10n.Text("Material・参照検査: ") + path, (float)i / Math.Max(1, paths.Count));
                    Guard(context, path, () =>
                    {
                        if (path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase)) return;
                        foreach (var obj in context.Load(path))
                        {
                            if (obj == null || obj is GameObject || obj is Component) continue;
                            Guard(context, path, () => SerializedReferenceScanner.Scan(context, obj, path));
                            if (obj is Material material)
                                Guard(context, path, () => MaterialRules.Evaluate(context, material, path, assets.TryGetValue(path, out var a) && a.isIgnored));
                        }
                    });
                }
                for (int i = 0; i < paths.Count; i++)
                {
                    string path = paths[i];
                    progress(L10n.Text("Texture・Model検査: ") + path, (float)i / Math.Max(1, paths.Count));
                    if (!assets.TryGetValue(path, out var info) || info.isIgnored) continue;
                    Guard(context, path, () => TextureRules.Evaluate(context, path));
                    Guard(context, path, () => ModelRules.Evaluate(context, path));
                }
                if (settings.checkUnused)
                {
                    progress(L10n.Text("未使用候補解析"), 0.95f);
                    DependencyRules.Unused(context, progress);
                }
                progress(L10n.Text("完了"), 1f);
            }
            catch (OperationCanceledException) { context.cancelled = true; }
            catch (Exception ex) { InternalError(context, folder, ex); }
            finally
            {
                try { clearProgress(); }
                finally
                {
                    if (!Selection.instanceIDs.SequenceEqual(selection)) Selection.instanceIDs = selection;
                    if (Selection.activeInstanceID != active) Selection.activeInstanceID = active;
                }
            }
            // Earlier findings may have been recorded before every graph node was known.
            foreach (var result in context.results)
            {
                result.referencedBy = context.dependencyGraph.ReferencedBy(result.assetPath);
                if (context.textureReferences.TryGetValue(result.assetPath, out var references))
                {
                    result.textureReferences = references.ToList();
                    if (result.ruleId == "DEP_EXTERNAL_002" && references.Count > 0 && references.All(u => u.inactiveReason != null))
                    {
                        result.message = L10n.Text("無効な機能／古いMaterial設定に、フォルダ外の画像への参照が残っています。");
                        result.recommendation = L10n.Text("下のMaterial使用先を確認してください。表示には使っていなくても、保存された参照は配布時の依存に含まれます。Animationで使わないことを確認して、不要な欄の画像を外してください。");
                    }
                }
            }
            return context;
        }
        public static void Guard(CheckContext context, string path, Action action)
        {
            try { action(); }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) { InternalError(context, path, ex); }
        }
        private static void InternalError(CheckContext context, string path, Exception exception)
        {
            context.Add("CHECK_INTERNAL_001", CheckSeverity.Info, CheckCategory.General, path,
                L10n.Text("このAssetの検査中に例外が発生しました。検査は不完全です。他のAssetの検査は続行します。 ") + exception.Message,
                L10n.Text("Consoleの例外と対象Assetを確認してください。"));
            Debug.LogException(new Exception("[NekkoYa][VRCDataChecker] " + path, exception));
        }
    }
}
