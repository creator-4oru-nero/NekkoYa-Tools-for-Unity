using System;
using System.Linq;
using NekkoYa.PhysBoneOrganizer.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NekkoYa.Tests
{
    public sealed class PhysBoneOrganizerTests
    {
        private Scene previousScene;
        private Scene testScene;
        private Transform avatar;
        private Transform armature;
        private Transform bone;
        private PhysBoneTestComponent source;

        [SetUp]
        public void SetUp()
        {
            previousScene = SceneManager.GetActiveScene();
            testScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(testScene);
            avatar = new GameObject("Avatar").transform;
            armature = Child(avatar, "Armature");
            bone = Child(armature, "Bone");
            source = bone.gameObject.AddComponent<PhysBoneTestComponent>();
        }

        [TearDown]
        public void TearDown()
        {
            if (previousScene.IsValid() && previousScene.isLoaded) SceneManager.SetActiveScene(previousScene);
            if (testScene.IsValid() && testScene.isLoaded) EditorSceneManager.CloseScene(testScene, true);
        }

        private static Transform Child(Transform parent, string name)
        {
            var child = new GameObject(name).transform;
            child.SetParent(parent, false);
            return child;
        }

        private PhysBoneOrganizerOperations.Plan Plan() => PhysBoneOrganizerOperations.BuildPlan(avatar, armature, typeof(PhysBoneTestComponent));

        [Test]
        public void DetectsJapaneseAndEnglishArmaturesWithoutMatchingUnrelatedNames()
        {
            var japanese = Child(avatar, "アーマチュア.001");
            Child(avatar, "ArmatureBackup");
            CollectionAssert.AreEquivalent(new[] { armature, japanese }, PhysBoneOrganizerOperations.DetectArmatures(avatar));
        }

        [Test]
        public void MissingSdkAndUnrelatedRootsAreRejected()
        {
            Assert.That(PhysBoneOrganizerOperations.Validate(avatar, armature, null), Is.Not.Null);
            var unrelated = new GameObject("Unrelated").transform;
            Assert.That(PhysBoneOrganizerOperations.Validate(avatar, unrelated, typeof(PhysBoneTestComponent)), Is.Not.Null);
            Assert.That(PhysBoneOrganizerOperations.Collect(armature, null), Is.Empty);
        }

        [Test]
        public void PlanningDoesNotCreateObjectsOrModifySources()
        {
            int children = avatar.childCount;
            string before = EditorJsonUtility.ToJson(source);
            var plan = Plan();
            Assert.That(plan.Sources.Length, Is.EqualTo(1));
            Assert.That(avatar.childCount, Is.EqualTo(children));
            Assert.That(EditorJsonUtility.ToJson(source), Is.EqualTo(before));
            Assert.That(bone.GetComponent<PhysBoneTestComponent>(), Is.SameAs(source));
        }

        [Test]
        public void MovesSettingsDefaultsRootAndUndoRestoresComponentsAndReferences()
        {
            source.pull = 0.73f;
            var observer = Child(avatar, "Observer").gameObject.AddComponent<PhysBoneTestComponent>();
            observer.linkedSource = source;
            var result = PhysBoneOrganizerOperations.Execute(Plan());
            var moved = result.GetComponentInChildren<PhysBoneTestComponent>(true);
            Assert.That(bone.GetComponent<PhysBoneTestComponent>(), Is.Null);
            Assert.That(moved.rootTransform, Is.EqualTo(bone));
            Assert.That(moved.pull, Is.EqualTo(0.73f));
            Assert.That(observer.linkedSource, Is.EqualTo(moved));
            Assert.That(bone.parent, Is.EqualTo(armature));
            Assert.That(result.transform.parent, Is.EqualTo(avatar));
            Undo.PerformUndo();
            var restored = bone.GetComponent<PhysBoneTestComponent>();
            Assert.That(restored, Is.Not.Null);
            Assert.That(restored.rootTransform, Is.Null);
            Assert.That(restored.pull, Is.EqualTo(0.73f));
            Assert.That(observer.linkedSource, Is.EqualTo(restored));
            Assert.That(avatar.Find("PB"), Is.Null);
            Undo.PerformRedo();
            var redone = avatar.Find("PB").GetComponentInChildren<PhysBoneTestComponent>(true);
            Assert.That(redone.gameObject.activeInHierarchy, Is.True);
            Assert.That(redone.pull, Is.EqualTo(0.73f));
            Assert.That(observer.linkedSource, Is.EqualTo(redone));
        }

        [Test]
        public void PreservesExplicitRootInactiveStateAndExistingPb()
        {
            var existing = Child(avatar, "PB");
            source.rootTransform = armature;
            source.enabled = false;
            bone.gameObject.SetActive(false);
            var result = PhysBoneOrganizerOperations.Execute(Plan());
            var moved = result.GetComponentInChildren<PhysBoneTestComponent>(true);
            Assert.That(result.name, Is.EqualTo("PB (1)"));
            Assert.That(existing, Is.Not.Null);
            Assert.That(moved.rootTransform, Is.EqualTo(armature));
            Assert.That(moved.enabled, Is.False);
            Assert.That(moved.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void RemapsMutualSourceReferencesAndGivesDuplicateBonesUniqueHolders()
        {
            var other = Child(armature, "Bone").gameObject.AddComponent<PhysBoneTestComponent>();
            source.linkedSource = other;
            other.linkedSource = source;
            var result = PhysBoneOrganizerOperations.Execute(Plan());
            var moved = result.GetComponentsInChildren<PhysBoneTestComponent>(true);
            Assert.That(moved.Length, Is.EqualTo(2));
            Assert.That(moved[0].linkedSource, Is.EqualTo(moved[1]));
            Assert.That(moved[1].linkedSource, Is.EqualTo(moved[0]));
            Assert.That(moved.Select(item => item.gameObject.name).Distinct().Count(), Is.EqualTo(2));
        }

        [Test]
        public void RejectsChangedPlanBeforeCreatingContainer()
        {
            var plan = Plan();
            source.pull += 0.1f;
            Assert.Throws<InvalidOperationException>(() => PhysBoneOrganizerOperations.Execute(plan));
            Assert.That(avatar.Find("PB"), Is.Null);
            Assert.That(bone.GetComponent<PhysBoneTestComponent>(), Is.EqualTo(source));
        }

        [Test]
        public void ContainerUsesAvatarSceneWhenAnotherSceneIsActive()
        {
            var otherScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(otherScene);
                var result = PhysBoneOrganizerOperations.Execute(Plan());
                Assert.That(result.scene, Is.EqualTo(testScene));
                Assert.That(result.transform.parent, Is.EqualTo(avatar));
                Assert.That(otherScene.GetRootGameObjects(), Is.Empty);
            }
            finally
            {
                SceneManager.SetActiveScene(testScene);
                EditorSceneManager.CloseScene(otherScene, true);
            }
        }

        [Test]
        public void NewlyAddedReferenceRequiresReviewAgain()
        {
            var plan = Plan();
            var observer = Child(avatar, "New Observer").gameObject.AddComponent<PhysBoneTestComponent>();
            observer.linkedSource = source;
            Assert.Throws<InvalidOperationException>(() => PhysBoneOrganizerOperations.Execute(plan));
            Assert.That(avatar.Find("PB"), Is.Null);
            Assert.That(observer.linkedSource, Is.EqualTo(source));
        }

        [Test]
        public void ReadOnlyReferenceIsRejectedBeforeChangingSources()
        {
            var observer = Child(avatar, "Observer").gameObject.AddComponent<PhysBoneTestComponent>();
            observer.linkedSource = source;
            observer.hideFlags = HideFlags.NotEditable;
            try
            {
                Assert.Throws<InvalidOperationException>(() => Plan());
                Assert.That(avatar.Find("PB"), Is.Null);
                Assert.That(bone.GetComponent<PhysBoneTestComponent>(), Is.EqualTo(source));
                Assert.That(observer.linkedSource, Is.EqualTo(source));
            }
            finally { observer.hideFlags = HideFlags.None; }
        }
    }
}
