using L10n = NekkoYa.Editor.NekkoYaLocalization;
using System;
using System.Linq;
using System.Text;

namespace NekkoYa.VRCDataChecker
{
    public static class ReportExporter
    {
        private static string Escape(string text) => (text ?? "").Replace("\\", "\\\\").Replace("`", "\\`").Replace("*", "\\*").Replace("_", "\\_").Replace("[", "\\[").Replace("]", "\\]").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\r", "").Replace("\n", "\n  ");
        public static string Markdown(CheckContext context)
        {
            var output = new StringBuilder("# VRC Data Checker Report\n\n");
            output.AppendLine("- Folder: " + Escape(context.rootFolder));
            output.AppendLine("- Mode: " + context.targetMode);
            output.AppendLine("- Platform: " + context.platforms);
            output.AppendLine("- Checked: " + context.checkedAt.ToString("yyyy-MM-dd HH:mm:ss"));
            output.AppendLine("- Status: " + (context.cancelled ? L10n.Text("Cancelled — 部分的な結果です") : context.results.Any(r => r.ruleId == "CHECK_INTERNAL_001") ? L10n.Text("Incomplete — 検査例外あり") : "Completed"));
            output.AppendLine("- Assets: " + (context.internalAssetPaths?.Count ?? 0));
            output.AppendLine("- Source bytes (excluding .meta): " + (context.assets?.Values.Where(a => a.isInsideRoot).Sum(a => a.fileSizeBytes) ?? 0));
            output.AppendLine("- Texture memory estimate (checked internal textures): " + context.textureMemoryBytes + " bytes");
            foreach (CheckSeverity severity in Enum.GetValues(typeof(CheckSeverity))) output.AppendLine("- " + severity + ": " + context.results.Count(r => r.severity == severity && r.category != CheckCategory.UnusedAsset));
            output.AppendLine("- Unused candidates: " + context.results.Count(r => r.category == CheckCategory.UnusedAsset));
            output.AppendLine(L10n.Text("\n検査対象Assetは変更していません。推奨値はVRChatの絶対制限ではありません。未使用候補は動的読み込みを判定できません。"));
            output.AppendLine(L10n.Text("\n## Scope\n\nPhase 1: 依存関係、Serialized参照、Texture、Model／Mesh、Material、Prefab、未使用候補。MA詳細、Animation／Animator、VRC Parameters／Performance Rankの検査は未実装です。"));
            foreach (CheckSeverity severity in Enum.GetValues(typeof(CheckSeverity)))
            {
                output.AppendLine("\n## " + severity + "\n");
                foreach (var result in context.results.Where(r => r.severity == severity && r.category != CheckCategory.UnusedAsset).OrderBy(r => r.category).ThenBy(r => r.assetPath))
                {
                    output.AppendLine("### " + Escape(result.ruleId) + " — " + Escape(result.title));
                    output.AppendLine("- Category: " + result.category);
                    output.AppendLine("- Asset: " + Escape(result.assetPath));
                    if (!string.IsNullOrEmpty(result.objectPath)) output.AppendLine("- Object: " + Escape(result.objectPath));
                    if (!string.IsNullOrEmpty(result.propertyPath)) output.AppendLine("- Property: " + Escape(result.propertyPath));
                    output.AppendLine("- Message: " + Escape(result.message));
                    output.AppendLine("- Recommendation: " + Escape(result.recommendation));
                    foreach (var use in result.textureReferences)
                        output.AppendLine("- Material slot: " + Escape(use.materialPath + " / " + use.materialName + " / " + use.label + " (" + use.property + ") / " + (use.inactiveReason ?? L10n.Text("有効または状態判定不可"))));
                    if (result.referencedBy.Count > 0) output.AppendLine("- Referenced by: " + string.Join("; ", result.referencedBy.Select(Escape)));
                    output.AppendLine();
                }
            }
            output.AppendLine(L10n.Text("\n## Unused Candidates / 未使用候補\n"));
            foreach (var result in context.results.Where(r => r.category == CheckCategory.UnusedAsset).OrderBy(r => r.assetPath))
            {
                output.AppendLine("- " + Escape(result.assetPath));
                output.AppendLine("  - " + Escape(result.message));
                output.AppendLine("  - " + Escape(result.recommendation));
                foreach (var use in result.textureReferences)
                    output.AppendLine("  - Material slot: " + Escape(use.materialPath + " / " + use.property + " / " + (use.inactiveReason ?? L10n.Text("有効または状態判定不可"))));
            }
            output.AppendLine("\n## External Dependencies\n");
            foreach (var path in context.dependencyGraph.recursive.Where(p => !AssetCollector.IsInside(p, context.rootFolder)).OrderBy(p => p))
            {
                output.AppendLine("- " + Escape(path));
                foreach (var source in context.dependencyGraph.ReferencedBy(path)) output.AppendLine("  - Referenced by: " + Escape(source));
            }
            output.AppendLine("\n## Excluded Assets\n");
            if (context.assets != null) foreach (var asset in context.assets.Values.Where(a => a.isInsideRoot && a.isIgnored).OrderBy(a => a.path)) output.AppendLine("- " + Escape(asset.path));
            output.AppendLine(L10n.Text("\n除外Assetも依存関係・参照切れ検査は対象です。Scene内部のComponent走査は行いません。Missing GUIDはUnityのSerializedObjectで識別できる範囲に限ります。"));
            return output.ToString();
        }
    }
}
