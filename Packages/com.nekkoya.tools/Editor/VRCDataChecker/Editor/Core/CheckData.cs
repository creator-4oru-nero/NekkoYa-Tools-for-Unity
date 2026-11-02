using L10n = NekkoYa.Editor.NekkoYaLocalization;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace NekkoYa.VRCDataChecker
{
    public enum CheckTargetMode { AvatarItem, WorldItem, CompleteAvatar, Generic }
    [Flags] public enum TargetPlatformMode { PC = 1, Mobile = 2 }
    public enum CheckSeverity { Error, Warning, Info }
    public enum CheckCategory { General, Dependency, Texture, Model, Mesh, Material, Shader, Prefab, Animation, Animator, ModularAvatar, VRC, Audio, Particle, Package, UnusedAsset }
    public enum DependencyKind { Internal, UnityBuiltIn, Package, VRCSDK, ModularAvatar, LilToon, Poiyomi, VRCFury, OtherExternal, Missing }

    [Serializable]
    public sealed class CheckResult
    {
        public string ruleId, title, message, recommendation, assetPath, objectPath, propertyPath;
        public CheckSeverity severity;
        public CheckCategory category;
        public List<string> referencedBy = new List<string>();
        public List<TextureUse> textureReferences = new List<TextureUse>();
    }

    public sealed class AssetInfo
    {
        public string guid, path;
        public Type mainType;
        public long fileSizeBytes;
        public bool isInsideRoot, isIgnored;
        public List<string> directDependencies = new List<string>();
        public List<string> referencedBy = new List<string>();
    }

    public sealed class DependencyGraph
    {
        public readonly Dictionary<string, List<string>> direct = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        public readonly HashSet<string> recursive = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public List<string> ReferencedBy(string path) => direct.Where(x => x.Value.Contains(path, StringComparer.OrdinalIgnoreCase)).Select(x => x.Key).OrderBy(x => x).ToList();
    }

    public sealed class TextureUse
    {
        public string materialPath, property, materialName, label, inactiveReason;
        public Material material;
        public bool usedByRenderer;
    }

    public sealed class CheckContext
    {
        public string rootFolder;
        public CheckTargetMode targetMode;
        public TargetPlatformMode platforms;
        public CheckerSettings settings;
        public IReadOnlyList<string> internalAssetPaths;
        public IReadOnlyDictionary<string, AssetInfo> assets;
        public DependencyGraph dependencyGraph = new DependencyGraph();
        public readonly Dictionary<string, List<TextureUse>> textureUses = new Dictionary<string, List<TextureUse>>(StringComparer.OrdinalIgnoreCase);
        public readonly Dictionary<string, List<TextureUse>> textureReferences = new Dictionary<string, List<TextureUse>>(StringComparer.OrdinalIgnoreCase);
        public bool materialUsageComplete;
        public readonly Dictionary<string, UnityEngine.Object[]> loaded = new Dictionary<string, UnityEngine.Object[]>(StringComparer.OrdinalIgnoreCase);
        public readonly HashSet<string> rendererMaterials = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public readonly List<CheckResult> results = new List<CheckResult>();
        public readonly DateTime checkedAt = DateTime.Now;
        public bool cancelled;
        public long textureMemoryBytes;
        public bool IsAvatar => targetMode == CheckTargetMode.AvatarItem || targetMode == CheckTargetMode.CompleteAvatar;
        public bool Mobile => (platforms & TargetPlatformMode.Mobile) != 0;
        public UnityEngine.Object[] Load(string path)
        {
            if (!loaded.TryGetValue(path, out var objects)) loaded[path] = objects = AssetDatabase.LoadAllAssetsAtPath(path);
            return objects;
        }
        public void Add(string id, CheckSeverity severity, CheckCategory category, string path, string message, string recommendation = L10n.Text("対象と用途を確認してください。"), string objectPath = null, string propertyPath = null)
        {
            results.Add(new CheckResult { ruleId = id, severity = severity, category = category, assetPath = path,
                title = System.IO.Path.GetFileName(path), message = message, recommendation = recommendation,
                objectPath = objectPath, propertyPath = propertyPath, referencedBy = dependencyGraph.ReferencedBy(path) });
        }
    }

    public interface ICheckRule
    {
        string RuleId { get; }
        CheckCategory Category { get; }
        bool IsApplicable(CheckContext context, AssetInfo asset);
        IEnumerable<CheckResult> Evaluate(CheckContext context, AssetInfo asset);
    }
    public interface IGlobalCheckRule
    {
        string RuleId { get; }
        CheckCategory Category { get; }
        IEnumerable<CheckResult> Evaluate(CheckContext context);
    }
}
