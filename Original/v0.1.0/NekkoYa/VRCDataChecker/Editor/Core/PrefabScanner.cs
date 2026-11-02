using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Nekkoya.VRCDataChecker
{
    public static class PrefabScanner
    {
        public static string ObjectPath(Transform target, Transform root)
        {
            string relative = AnimationUtility.CalculateTransformPath(target, root);
            return root.name + (relative.Length == 0 ? "" : "/" + relative);
        }
        public static void Scan(CheckContext context, string path, Action<string, float> progress)
        {
            GameObject root = null;
            try
            {
                root = PrefabUtility.LoadPrefabContents(path);
                bool ignored = context.assets.TryGetValue(path, out var info) && info.isIgnored;
                var transforms = root.GetComponentsInChildren<Transform>(true);
                if (!ignored && root.transform.localScale != Vector3.one)
                    context.Add("PREFAB_SCALE_001", CheckSeverity.Warning, CheckCategory.Prefab, path, "Root Scaleが1,1,1ではありません。", objectPath: root.name);
                for (int i = 0; i < transforms.Length; i++)
                {
                    var transform = transforms[i];
                    string objectPath = ObjectPath(transform, root.transform);
                    progress("Prefab内部: " + path + " / " + objectPath, (float)i / Math.Max(1, transforms.Length));
                    CheckRunner.Guard(context, path, () =>
                    {
                        if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) > 0)
                            context.Add("PREFAB_SCRIPT_001", CheckSeverity.Error, CheckCategory.Prefab, path, "Missing Scriptがあります。", "必要なスクリプトを導入するか、不要なComponentを削除してください。", objectPath);
                        if (!ignored) PrefabRules.Transform(context, transform, path, objectPath);
                        foreach (var component in transform.GetComponents<Component>())
                        {
                            if (component == null) continue;
                            CheckRunner.Guard(context, path, () => SerializedReferenceScanner.Scan(context, component, path, objectPath, root));
                            if (!ignored) CheckRunner.Guard(context, path, () => PrefabRules.Component(context, component, path, objectPath));
                            if (component is Renderer renderer)
                            {
                                CheckRunner.Guard(context, path, () =>
                                {
                                    foreach (var material in renderer.sharedMaterials.Where(m => m != null))
                                    {
                                        string materialPath = AssetDatabase.GetAssetPath(material);
                                        context.rendererMaterials.Add(materialPath);
                                        // Include external materials that use an internal texture, but do not inspect their settings.
                                        CheckRunner.Guard(context, materialPath, () => MaterialRules.CollectTextureUses(context, material, materialPath));
                                    }
                                });
                            }
                        }
                    });
                }
            }
            finally { if (root != null) PrefabUtility.UnloadPrefabContents(root); }
        }
    }
}
