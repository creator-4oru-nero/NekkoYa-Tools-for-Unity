using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NekkoYa.VRCDataChecker.Tests
{
    public sealed class CheckerTests
    {
        private string root;
        private CheckerSettings settings;
        private string settingsSnapshot;
        private NekkoYa.Editor.NekkoYaLanguage previousLanguage;

        [SetUp]
        public void SetUp()
        {
            previousLanguage = NekkoYa.Editor.NekkoYaLocalization.Language;
            NekkoYa.Editor.NekkoYaLocalization.Language = NekkoYa.Editor.NekkoYaLanguage.JP;
            root = "Assets/__VRCDataCheckerTests_" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets", Path.GetFileName(root));
            settings = CheckerSettings.instance;
            settingsSnapshot = EditorJsonUtility.ToJson(settings);
            settings.exclusions = "";
            settings.keepPatterns = "";
            settings.checkUnused = true;
            settings.ignoredReferenceProperties = "";
            settings.thresholds = new CheckerThresholds();
            settings.dependencyPatterns = new List<DependencyPattern>();
        }
        [TearDown]
        public void TearDown()
        {
            NekkoYa.Editor.NekkoYaLocalization.Language = previousLanguage;
            if (settings != null && settingsSnapshot != null) EditorJsonUtility.FromJsonOverwrite(settingsSnapshot, settings);
            if (AssetDatabase.IsValidFolder(root)) AssetDatabase.DeleteAsset(root);
            EditorUtility.ClearProgressBar();
        }
        private CheckContext Run(CheckTargetMode mode = CheckTargetMode.AvatarItem) => CheckRunner.Run(root, mode, TargetPlatformMode.PC, settings, (_, __) => false, () => { });
        private Material Material(string name = "material")
        {
            var shader = Shader.Find("Standard");
            Assert.That(shader, Is.Not.Null, "Unity Standard Shader required for the fixture");
            var material = new Material(shader);
            AssetDatabase.CreateAsset(material, root + "/" + name + ".mat");
            return material;
        }
        private string Texture(bool sprite = false, bool readable = false)
        {
            string path = root + "/color.png";
            var texture = new Texture2D(16, 16);
            try { File.WriteAllBytes(path, texture.EncodeToPNG()); }
            finally { Object.DestroyImmediate(texture); }
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = sprite ? TextureImporterType.Sprite : TextureImporterType.Default;
            importer.mipmapEnabled = false;
            importer.isReadable = readable;
            importer.SaveAndReimport();
            return path;
        }
        private Mesh Mesh(bool readable = true)
        {
            var mesh = new Mesh { name = "FixtureMesh", vertices = new[] { Vector3.zero, Vector3.right, Vector3.up }, triangles = new[] { 0, 1, 2 }, normals = new[] { Vector3.forward, Vector3.forward, Vector3.forward } };
            AssetDatabase.CreateAsset(mesh, root + "/mesh.asset");
            if (!readable) { mesh.UploadMeshData(true); EditorUtility.SetDirty(mesh); AssetDatabase.SaveAssets(); }
            return mesh;
        }
        private string Prefab(Material material, bool inactive = false, bool skinned = false, Mesh mesh = null)
        {
            var rootObject = new GameObject("FixtureRoot");
            try
            {
                var child = new GameObject("Child");
                child.transform.SetParent(rootObject.transform, false);
                Renderer renderer;
                if (skinned) { var skin = child.AddComponent<SkinnedMeshRenderer>(); skin.sharedMesh = mesh; renderer = skin; }
                else { child.AddComponent<MeshFilter>().sharedMesh = mesh; renderer = child.AddComponent<MeshRenderer>(); }
                renderer.sharedMaterial = material;
                child.SetActive(!inactive);
                string path = root + "/item.prefab";
                PrefabUtility.SaveAsPrefabAsset(rootObject, path);
                return path;
            }
            finally { Object.DestroyImmediate(rootObject); }
        }
        private static bool Has(CheckContext context, string rule) => context.results.Any(r => r.ruleId == rule);

        [Test]
        public void CollectsNestedAssets()
        {
            AssetDatabase.CreateFolder(root, "Nested");
            File.WriteAllText(root + "/Nested/readme.txt", "fixture");
            AssetDatabase.ImportAsset(root + "/Nested/readme.txt");
            Assert.That(AssetCollector.Enumerate(root), Does.Contain(root + "/Nested/readme.txt"));
        }
        [TestCase("Assets/Hair/Models/a.fbx", "Assets/Hair", true)]
        [TestCase("Assets/HairExtra/a.fbx", "Assets/Hair", false)]
        [TestCase("Assets/Hair", "Assets/Hair", false)]
        [TestCase("Assets\\Hair\\a.mat", "Assets/Hair/", true)]
        public void RootBoundaryIsRespected(string path, string folder, bool expected) => Assert.That(AssetCollector.IsInside(path, folder), Is.EqualTo(expected));

        [Test]
        public void RecordsMaterialTextureDirectDependency()
        {
            string texture = Texture();
            var material = Material(); material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(texture);
            EditorUtility.SetDirty(material); AssetDatabase.SaveAssets();
            var result = Run();
            Assert.That(result.dependencyGraph.direct[root + "/material.mat"], Does.Contain(texture));
            Assert.That(result.dependencyGraph.ReferencedBy(texture), Does.Contain(root + "/material.mat"));
        }
        [Test]
        public void ExternalDependencyIncludesActualReferrer()
        {
            AssetDatabase.CreateFolder(root, "Product");
            string texture = Texture();
            var material = Material(); material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(texture);
            EditorUtility.SetDirty(material); AssetDatabase.SaveAssets();
            Assert.That(AssetDatabase.MoveAsset(root + "/material.mat", root + "/Product/material.mat"), Is.Empty);
            var result = CheckRunner.Run(root + "/Product", CheckTargetMode.Generic, TargetPlatformMode.PC, settings, (_, __) => false, () => { });
            var external = result.results.Single(r => r.ruleId == "DEP_EXTERNAL_002" && r.assetPath == texture);
            Assert.That(external.referencedBy, Does.Contain(root + "/Product/material.mat"));
        }
        [TestCase(false, true)]
        [TestCase(true, false)]
        public void MipmapRuleUsesRendererAndExcludesSprites(bool sprite, bool warning)
        {
            var material = Material(); material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(Texture(sprite));
            EditorUtility.SetDirty(material); AssetDatabase.SaveAssets();
            Prefab(material, mesh: Mesh());
            Assert.That(Has(Run(), "TEX_MIPMAP_001"), Is.EqualTo(warning));
        }
        [Test]
        public void ReadableTextureIsWarning()
        {
            Texture(readable: true);
            Assert.That(Run().results.Any(r => r.ruleId == "TEX_READABLE_001" && r.severity == CheckSeverity.Warning));
        }
        [Test]
        public void AvatarSkinnedUnreadableMeshIsError()
        {
            Prefab(Material(), skinned: true, mesh: Mesh(false));
            Assert.That(Run().results.Any(r => r.ruleId == "MESH_READABLE_001" && r.severity == CheckSeverity.Error));
        }
        [Test]
        public void WorldStaticUnreadableMeshIsNotError()
        {
            Prefab(Material(), mesh: Mesh(false));
            Assert.That(Run(CheckTargetMode.WorldItem).results.Any(r => r.ruleId == "MESH_READABLE_001" && r.severity == CheckSeverity.Error), Is.False);
        }
        [Test]
        public void InactiveRendererIsInspected()
        {
            Prefab(null, inactive: true);
            Assert.That(Has(Run(), "PREFAB_MATERIAL_001"));
            Assert.That(Has(Run(), "PREFAB_MESH_001"));
        }
        [Test]
        public void CancellationInsidePrefabUnloadsAndClearsProgress()
        {
            Prefab(Material());
            int previewCount = EditorSceneManager.previewSceneCount;
            bool cleared = false;
            var result = CheckRunner.Run(root, CheckTargetMode.AvatarItem, TargetPlatformMode.PC, settings,
                (label, _) => label.StartsWith("Prefab内部:"), () => cleared = true);
            Assert.That(result.cancelled);
            Assert.That(cleared);
            Assert.That(EditorSceneManager.previewSceneCount, Is.EqualTo(previewCount));
        }
        [Test]
        public void InspectionPreservesFilesSelectionAndSceneDirtyState()
        {
            Prefab(Material(), mesh: Mesh());
            AssetDatabase.SaveAssets();
            var hashes = Directory.GetFiles(root, "*", SearchOption.AllDirectories).ToDictionary(p => p, Hash);
            var selection = Selection.instanceIDs;
            var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            bool dirty = activeScene.isDirty;
            int previews = EditorSceneManager.previewSceneCount;
            Run();
            foreach (var entry in hashes) Assert.That(Hash(entry.Key), Is.EqualTo(entry.Value), entry.Key);
            Assert.That(Selection.instanceIDs, Is.EqualTo(selection));
            Assert.That(activeScene.isDirty, Is.EqualTo(dirty));
            Assert.That(EditorSceneManager.previewSceneCount, Is.EqualTo(previews));
        }
        private static string Hash(string path) { using (var hash = SHA256.Create()) return Convert.ToBase64String(hash.ComputeHash(File.ReadAllBytes(path))); }

        [Test]
        public void UnusedCandidateIsInformationalAndNotDefinitive()
        {
            Material();
            var result = Run().results.Single(r => r.ruleId == "UNUSED_ASSET_001");
            Assert.That(result.severity, Is.EqualTo(CheckSeverity.Info));
            Assert.That(result.message, Does.Contain("未使用の可能性"));
        }
        [Test]
        public void ExclusionsStillAppearInReport()
        {
            Texture(readable: true);
            settings.exclusions = "*.png";
            var result = Run();
            Assert.That(Has(result, "TEX_READABLE_001"), Is.False);
            Assert.That(ReportExporter.Markdown(result), Does.Contain("## Excluded Assets"));
            Assert.That(result.assets[root + "/color.png"].isIgnored);
        }
        [Test]
        public void UnknownOptionalTypeIsSafeAndCached()
        {
            Assert.That(OptionalTypeResolver.Find("Unknown.Package.Component"), Is.Null);
            Assert.That(OptionalTypeResolver.IsInstalled("Unknown.Package.Component"), Is.False);
        }
        [Test]
        public void EmptyObjectReferenceIsNotMissing()
        {
            var material = Material();
            using (var serialized = new SerializedObject(material))
            {
                var iterator = serialized.GetIterator();
                while (iterator.Next(true))
                    if (iterator.propertyType == SerializedPropertyType.ObjectReference && iterator.objectReferenceValue == null)
                        Assert.That(SerializedReferenceScanner.IsMissing(iterator), Is.False, iterator.propertyPath);
            }
        }
        [Test]
        public void MissingMaterialTextureIsDetectedAfterDeletingReferencedAsset()
        {
            string texture = Texture();
            var material = Material(); material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(texture);
            EditorUtility.SetDirty(material); AssetDatabase.SaveAssets();
            AssetDatabase.DeleteAsset(texture);
            AssetDatabase.ImportAsset(root + "/material.mat", ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            var result = Run();
            Assert.That(Has(result, "MAT_TEXTURE_001"));
            Assert.That(result.results.Count(r => r.ruleId == "REF_MISSING_001" && r.assetPath.EndsWith("material.mat")), Is.Zero);
        }

        [Test]
        public void MissingShaderIsAnError()
        {
            var material = Material();
            // Unity may substitute its internal error shader for a null assignment.
            using (var serialized = new SerializedObject(material))
            {
                serialized.FindProperty("m_Shader").objectReferenceValue = null;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            var context = new CheckContext { rootFolder = root, settings = settings };
            MaterialRules.Evaluate(context, material, root + "/material.mat", false);
            Assert.That(context.results.Any(r => r.ruleId == "MAT_SHADER_001" && r.severity == CheckSeverity.Error));
        }

        [Test]
        public void MissingScriptOnInactiveChildIsDetected()
        {
            // A self-contained broken-script fixture; no existing user asset is edited.
            string prefab = root + "/missing.prefab";
            File.WriteAllText(prefab, @"%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!1 &1000
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 4000}
  - component: {fileID: 114000}
  m_Layer: 0
  m_Name: MissingScriptFixture
  m_TagString: Untagged
  m_IsActive: 0
--- !u!4 &4000
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 1000}
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_Children: []
  m_Father: {fileID: 0}
--- !u!114 &114000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 1000}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 0123456789abcdef0123456789abcdef, type: 3}
  m_Name:
  m_EditorClassIdentifier:
");
            AssetDatabase.ImportAsset(prefab, ImportAssetOptions.ForceSynchronousImport);
            Assert.That(Has(Run(), "PREFAB_SCRIPT_001"));
        }

        [Test]
        public void DependencyCycleDoesNotLoop()
        {
            var context = new CheckContext { rootFolder = root, settings = settings,
                internalAssetPaths = new[] { root + "/entry.prefab", root + "/a.asset", root + "/b.asset" } };
            context.assets = context.internalAssetPaths.ToDictionary(p => p, p => new AssetInfo { path = p, isInsideRoot = true });
            context.dependencyGraph.direct[root + "/entry.prefab"] = new List<string> { root + "/a.asset" };
            context.dependencyGraph.direct[root + "/a.asset"] = new List<string> { root + "/b.asset" };
            context.dependencyGraph.direct[root + "/b.asset"] = new List<string> { root + "/a.asset" };
            int calls = 0;
            DependencyRules.Unused(context, (_, __) => { Assert.That(++calls, Is.LessThan(5)); });
            Assert.That(Has(context, "UNUSED_ASSET_001"), Is.False);
        }

        [TestCase(2048, 2048, 1024, false)]
        [TestCase(1024, 1024, 2048, true)]
        [TestCase(1024, 1024, 1024, false)]
        [TestCase(2048, 512, 1024, false)]
        [TestCase(1500, 1000, 2048, true)]
        public void MaxSizeOnlyWarnsWhenSettingExceedsSource(int width, int height, int maxSize, bool expected)
            => Assert.That(TextureRules.IsMaxSizeOverspecified(width, height, maxSize), Is.EqualTo(expected));

        [TestCase("Assets/NormalFolder/skin.png", false)]
        [TestCase("Assets/skin_bump.png", false)]
        [TestCase("Assets/skin_NormalMap.png", true)]
        [TestCase("Assets/skin_normal.png", true)]
        [TestCase("Assets/肌_ノーマルマップ.png", true)]
        public void NormalDetectionUsesOnlyTextureFilename(string path, bool expected)
            => Assert.That(TextureRules.IsNormalName(path), Is.EqualTo(expected));

        [Test]
        public void PackageEditorDependencyDoesNotBecomeError()
        {
            string packagePath = "Packages/com.fixture.example/Editor/Fixture.cs";
            var context = new CheckContext { rootFolder = root, settings = settings };
            context.dependencyGraph.recursive.Add(packagePath);
            context.dependencyGraph.direct[root + "/item.prefab"] = new List<string> { packagePath };
            DependencyRules.Evaluate(context);
            Assert.That(context.results.Any(r => r.severity != CheckSeverity.Info), Is.False);
            Assert.That(Has(context, "DEP_EXTERNAL_001"));
            context.results.Clear();
            context.dependencyGraph.direct[root + "/item.prefab"] = new List<string> { root + "/Editor/Fixture.cs" };
            DependencyRules.Evaluate(context);
            Assert.That(Has(context, "DEP_EDITOR_001"));
        }

        [Test]
        public void InitialSettingsMatchScreenshotAndRejectChanges()
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(Texture());
            importer.mipmapEnabled = true;
            importer.npotScale = TextureImporterNPOTScale.ToNearest;
            importer.anisoLevel = 1;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Bilinear;
            importer.sRGBTexture = true;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = false;
            importer.ignoreMipmapLimit = false;
            importer.mipmapLimitGroupName = "";
            importer.ignorePngGamma = false;
            importer.streamingMipmaps = false;
            importer.mipmapFilter = TextureImporterMipFilter.BoxFilter;
            importer.mipMapsPreserveCoverage = false;
            importer.borderMipmap = false;
            importer.fadeout = false;
            importer.swizzleR = TextureImporterSwizzle.R;
            importer.swizzleG = TextureImporterSwizzle.G;
            importer.swizzleB = TextureImporterSwizzle.B;
            importer.swizzleA = TextureImporterSwizzle.A;
            var defaults = importer.GetDefaultPlatformTextureSettings();
            defaults.maxTextureSize = 2048;
            defaults.format = TextureImporterFormat.Automatic;
            defaults.resizeAlgorithm = TextureResizeAlgorithm.Mitchell;
            defaults.textureCompression = TextureImporterCompression.Compressed;
            defaults.crunchedCompression = false;
            importer.SetPlatformTextureSettings(defaults);
            Assert.That(TextureRules.MatchesInitialSettings(importer));
            importer.sRGBTexture = false;
            Assert.That(TextureRules.MatchesInitialSettings(importer), Is.False);
            importer.sRGBTexture = true;
            var android = importer.GetPlatformTextureSettings("Android");
            android.overridden = true;
            importer.SetPlatformTextureSettings(android);
            Assert.That(TextureRules.MatchesInitialSettings(importer), Is.False);
        }

        [Test]
        public void LilToonFeatureSwitchesDoNotHideActiveOrUnknownProperties()
        {
            string shaderPath = root + "/lilFixture.shader";
            File.WriteAllText(shaderPath, "Shader \"Hidden/CheckerTest/lilToonFixture\" { Properties { _lilToonVersion (\"Version\", Float) = 0 _UseBumpMap (\"Normal\", Float) = 0 _BumpMap (\"Normal texture\", 2D) = \"bump\" {} _OtherTex (\"Other\", 2D) = \"white\" {} } SubShader { Pass {} } }");
            AssetDatabase.ImportAsset(shaderPath, ImportAssetOptions.ForceSynchronousImport);
            var material = new Material(AssetDatabase.LoadAssetAtPath<Shader>(shaderPath));
            try
            {
                Assert.That(LilToonIntegration.InactiveReason(material, "_BumpMap"), Is.Not.Null);
                material.SetFloat("_UseBumpMap", 1);
                Assert.That(LilToonIntegration.InactiveReason(material, "_BumpMap"), Is.Null);
                Assert.That(LilToonIntegration.InactiveReason(material, "_OtherTex"), Is.Null);
                Assert.That(LilToonIntegration.InactiveReason(material, "_OldShaderTexture"), Is.Not.Null);
                string texturePath = Texture();
                material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));
                var context = new CheckContext { rootFolder = root, settings = settings };
                MaterialRules.CollectTextureUses(context, material, root + "/fixture.mat");
                Assert.That(context.textureUses[texturePath].Count, Is.EqualTo(1));
                material.SetFloat("_UseBumpMap", 0);
                MaterialRules.CollectTextureUses(context, material, root + "/fixture.mat");
                Assert.That(context.textureUses[texturePath].Count, Is.Zero);
                Assert.That(context.textureReferences[texturePath].Single().property, Is.EqualTo("_BumpMap"));
                Assert.That(context.textureReferences[texturePath].Single().inactiveReason, Is.Not.Null);
            }
            finally { Object.DestroyImmediate(material); }
        }

        [Test]
        public void UnusedCandidatesHaveTheirOwnReportSection()
        {
            Material();
            string markdown = ReportExporter.Markdown(Run());
            Assert.That(markdown, Does.Contain("## Unused Candidates / 未使用候補"));
            int start = markdown.IndexOf("## Info", StringComparison.Ordinal);
            int end = markdown.IndexOf("## Unused Candidates", StringComparison.Ordinal);
            Assert.That(markdown.Substring(start, end - start), Does.Not.Contain("未使用の可能性"));
        }

        [TestCase(false, false, true)]
        [TestCase(true, false, true)]
        [TestCase(true, true, false)]
        public void TextureUnusedDistinguishesSavedAndActiveReferences(bool saved, bool active, bool expectedUnused)
        {
            string path = root + "/texture.png";
            var context = new CheckContext { rootFolder = root, settings = settings, materialUsageComplete = true,
                internalAssetPaths = new[] { path } };
            context.assets = new Dictionary<string, AssetInfo> { [path] = new AssetInfo { path = path, mainType = typeof(Texture2D), isInsideRoot = true } };
            if (saved) context.textureReferences[path] = new List<TextureUse> { new TextureUse { materialPath = "Assets/Other/material.mat", property = "_MatCapTex", inactiveReason = active ? null : "OFF" } };
            if (active) context.textureUses[path] = context.textureReferences[path];
            DependencyRules.Unused(context, (_, __) => { });
            Assert.That(Has(context, "UNUSED_TEXTURE_001"), Is.EqualTo(expectedUnused));
            Assert.That(Has(context, "UNUSED_ASSET_001"), Is.False, "A material outside the root still counts as texture use.");
            if (saved && !active) Assert.That(context.results.Single().message, Does.Contain("無効"));
        }
    }
}
