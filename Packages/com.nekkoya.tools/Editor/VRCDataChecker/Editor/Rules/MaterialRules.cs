using L10n = NekkoYa.Editor.NekkoYaLocalization;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace NekkoYa.VRCDataChecker
{
    public static class MaterialRules
    {
        public static void CollectTextureUses(CheckContext context, Material material, string path)
        {
            // Saved entries include inactive feature textures and properties left by an old shader.
            using (var serialized = new SerializedObject(material))
            {
                var textures = serialized.FindProperty("m_SavedProperties.m_TexEnvs");
                if (textures == null) return;
                for (int i = 0; i < textures.arraySize; i++)
                {
                var entry = textures.GetArrayElementAtIndex(i);
                string property = entry.FindPropertyRelative("first").stringValue;
                var texture = entry.FindPropertyRelative("second.m_Texture").objectReferenceValue as Texture;
                if (texture == null) continue;
                string texturePath = AssetDatabase.GetAssetPath(texture);
                if (string.IsNullOrEmpty(texturePath)) continue;
                string reason = material.shader != null && material.shader.name != "Hidden/InternalErrorShader" && !material.HasProperty(property)
                    ? L10n.Text("現在のShaderにない、以前の設定です。") : LilToonIntegration.InactiveReason(material, property);
                string label = TextureLabel(property);
                var use = new TextureUse { materialPath = path, materialName = material.name, material = material, property = property,
                    label = label, inactiveReason = reason, usedByRenderer = context.rendererMaterials.Contains(path) };
                if (!context.textureReferences.TryGetValue(texturePath, out var references)) context.textureReferences[texturePath] = references = new List<TextureUse>();
                references.RemoveAll(u => u.material == material && u.property == property);
                references.Add(use);
                if (context.textureUses.TryGetValue(texturePath, out var previousUses)) previousUses.RemoveAll(u => u.material == material && u.property == property);
                if (reason != null) continue;
                if (!context.textureUses.TryGetValue(texturePath, out var uses)) context.textureUses[texturePath] = uses = new List<TextureUse>();
                uses.RemoveAll(u => u.material == material && u.property == property);
                uses.Add(use);
                }
            }
        }
        public static string TextureLabel(string property)
        {
            switch (property)
            {
                case "_MainTex": case "_BaseMap": return L10n.Text("メインカラーの画像");
                case "_BumpMap": return L10n.Text("ノーマルマップ");
                case "_Bump2ndMap": return L10n.Text("ノーマルマップ2nd");
                case "_MatCapTex": return L10n.Text("マットキャップの画像");
                case "_MatCap2ndTex": return L10n.Text("マットキャップ2ndの画像");
                case "_EmissionMap": return L10n.Text("発光の画像");
                case "_Emission2ndMap": return L10n.Text("発光2ndの画像");
                case "_Main2ndTex": return L10n.Text("メインカラー2ndの画像");
                case "_Main3rdTex": return L10n.Text("メインカラー3rdの画像");
                default: return property;
            }
        }
        public static void CollectProjectTextureUses(CheckContext context, Action<string, float> progress)
        {
            var paths = AssetDatabase.FindAssets("t:Material", new[] { "Assets" }).Concat(AssetDatabase.FindAssets("t:Model", new[] { "Assets" }))
                .Select(AssetDatabase.GUIDToAssetPath).Concat(context.dependencyGraph.recursive.Where(p => p.EndsWith(".mat", StringComparison.OrdinalIgnoreCase)))
                .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            bool complete = true;
            for (int i = 0; i < paths.Length; i++)
            {
                string path = paths[i];
                progress(L10n.Text("Material使用先の確認（Assets全体）: ") + path, (float)i / Math.Max(1, paths.Length));
                CheckRunner.Guard(context, path, () =>
                {
                    try { foreach (var material in context.Load(path).OfType<Material>()) CollectTextureUses(context, material, path); }
                    catch { complete = false; throw; }
                });
            }
            context.materialUsageComplete = complete;
        }
        public static void Evaluate(CheckContext context, Material material, string path, bool ignored)
        {
            CollectTextureUses(context, material, path);
            var shader = material.shader;
            if (shader == null || shader.name == "Hidden/InternalErrorShader")
            {
                context.Add("MAT_SHADER_001", CheckSeverity.Error, CheckCategory.Material, path, L10n.Text("ShaderがMissing、またはUnityのエラーShaderです。"), L10n.Text("必要なShaderを導入し、MaterialのShaderを確認してください。"));
                return;
            }
            if (ignored) return;
            if (material.renderQueue >= 3000) context.Add("MAT_TRANSPARENT_001", CheckSeverity.Info, CheckCategory.Material, path, L10n.Text("Transparent系Render Queue: ") + material.renderQueue);
            if (material.shaderKeywords.Length > context.settings.thresholds.materialKeywordWarningCount)
                context.Add("MAT_KEYWORD_001", CheckSeverity.Warning, CheckCategory.Material, path, L10n.Text("Shader Keyword数: ") + material.shaderKeywords.Length);
            if (material.passCount > context.settings.thresholds.shaderPassWarningCount)
                context.Add("MAT_PASS_001", CheckSeverity.Warning, CheckCategory.Material, path, L10n.Text("Shader Pass数: ") + material.passCount);
            if (context.Mobile && !material.enableInstancing)
                context.Add("MAT_INSTANCING_001", CheckSeverity.Warning, CheckCategory.Material, path, "GPU Instancingが無効です。", L10n.Text("Shaderと描画方式が対応している場合のみ有効化を検討してください。SkinnedMeshRendererには通常適用されません。"));
        }
    }
}
