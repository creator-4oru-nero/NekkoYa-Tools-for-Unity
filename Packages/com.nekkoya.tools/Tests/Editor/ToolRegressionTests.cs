using System;
using System.Collections.Generic;
using NekkoYa.AssetOrganizer.Editor;
using NekkoYa.BoothDescriptionStudio;
using NekkoYa.Editor;
using NekkoYa.VRCDataChecker;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace NekkoYa.Tests
{
    public sealed class ToolRegressionTests
    {
        private bool hadPreference;
        private int preference;

        [SetUp]
        public void SetUp()
        {
            hadPreference = EditorPrefs.HasKey("NekkoYa.Language");
            preference = EditorPrefs.GetInt("NekkoYa.Language", 0);
        }

        [TearDown]
        public void TearDown()
        {
            NekkoYaLocalization.Language = NekkoYaLanguage.JP;
            if (hadPreference) EditorPrefs.SetInt("NekkoYa.Language", preference);
            else EditorPrefs.DeleteKey("NekkoYa.Language");
        }

        [Test]
        public void FreshInstallationUsesJapanese()
        {
            EditorPrefs.DeleteKey("NekkoYa.Language");
            Assert.That(NekkoYaLocalization.Language, Is.EqualTo(NekkoYaLanguage.JP));
        }

        [Test]
        public void LanguageIsPersistedAndNotifiesOpenWindows()
        {
            NekkoYaLocalization.Language = NekkoYaLanguage.JP;
            int changes = 0;
            Action changed = () => changes++;
            NekkoYaLocalization.LanguageChanged += changed;
            try
            {
                NekkoYaLocalization.Language = NekkoYaLanguage.KR;
                Assert.That(EditorPrefs.GetInt("NekkoYa.Language"), Is.EqualTo(2));
                Assert.That(NekkoYaLocalization.Text("保存"), Is.EqualTo("저장"));
                NekkoYaLocalization.Language = NekkoYaLanguage.KR;
                Assert.That(changes, Is.EqualTo(1));
            }
            finally { NekkoYaLocalization.LanguageChanged -= changed; }
        }

        [Test]
        public void UnknownTextAndNamesArePreserved()
        {
            NekkoYaLocalization.Language = NekkoYaLanguage.EN;
            Assert.That(NekkoYaLocalization.Text("ユーザー固有の名前"), Is.EqualTo("ユーザー固有の名前"));
            Assert.That(NekkoYaLocalization.Text(null), Is.Null);
            Assert.Throws<ArgumentOutOfRangeException>(() => NekkoYaLocalization.Language = (NekkoYaLanguage)99);
        }

        [TestCase(NekkoYaLanguage.JP)]
        [TestCase(NekkoYaLanguage.EN)]
        [TestCase(NekkoYaLanguage.KR)]
        public void NormalMapDetectionIsIndependentOfUiLanguage(NekkoYaLanguage language)
        {
            NekkoYaLocalization.Language = language;
            Assert.That(TextureRules.IsNormalName("Assets/肌ノーマル.png"), Is.True);
            Assert.That(TextureRules.IsNormalName("Assets/skin_normal.png"), Is.True);
            Assert.That(TextureRules.IsNormalName("Assets/skin_color.png"), Is.False);
        }

        [Test]
        public void ProductOutputAndTemplatesDoNotChangeWithUiLanguage()
        {
            NekkoYaLocalization.Language = NekkoYaLanguage.JP;
            var library = BoothDescriptionWindow.CreateLibrary();
            string snapshot = JsonUtility.ToJson(library);
            string expected = DescriptionFormat.Build(library.draft, out var errors);
            foreach (var language in new[] { NekkoYaLanguage.EN, NekkoYaLanguage.KR })
            {
                NekkoYaLocalization.Language = language;
                string actual = DescriptionFormat.Build(library.draft, out errors);
                Assert.That(actual, Is.EqualTo(expected));
                Assert.That(JsonUtility.ToJson(library), Is.EqualTo(snapshot));
            }
        }

        [Test]
        public void PrefabSummaryOutputRemainsCompatibleAcrossLanguages()
        {
            var entry = new PrefabEntry
            {
                name = "Custom Name",
                source = "総ポリゴン数：100\nマテリアル数：1\nテクスチャ数：1\n├Body：△100 / 〇1\n└1024×1024：1枚"
            };
            NekkoYaLocalization.Language = NekkoYaLanguage.JP;
            string expected = DescriptionFormat.PrefabText(entry, out var error);
            Assert.That(error, Is.Empty);
            foreach (var language in new[] { NekkoYaLanguage.EN, NekkoYaLanguage.KR })
            {
                NekkoYaLocalization.Language = language;
                Assert.That(DescriptionFormat.PrefabText(entry, out error), Is.EqualTo(expected));
                Assert.That(error, Is.Empty);
            }
        }

        [TestCase("../escape")]
        [TestCase("a/b")]
        [TestCase("..")]
        [TestCase("trailing.")]
        public void UnsafeAssetNamesAreRejected(string name)
        {
            Assert.That(AssetNameValidator.Validate(name, "Name"), Is.Not.Null);
        }

        [Test]
        public void FolderCreationRejectsPackagesAndPrefixLookalikes()
        {
            var roots = new List<FolderNode> { new FolderNode { Name = "Example" } };
            Assert.That(FolderUtility.Validate("Packages/com.nekkoya.tools", roots), Is.Not.Empty);
            Assert.That(FolderUtility.Validate("AssetsOther", roots), Is.Not.Empty);
            Assert.That(FolderUtility.Validate("Assets", new List<FolderNode>()), Is.Not.Empty);
        }

        [Test]
        public void RenameKeepsExtensionSeparateAndExpandsNameTokens()
        {
            Assert.That(RenameUtility.ExpandTemplate("{00}_{Name}", 7, "Body"), Is.EqualTo("07_Body"));
        }
    }
}
