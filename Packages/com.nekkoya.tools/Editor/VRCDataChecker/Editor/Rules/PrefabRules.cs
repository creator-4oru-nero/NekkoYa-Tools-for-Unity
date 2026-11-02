using L10n = NekkoYa.Editor.NekkoYaLocalization;
using System;
using System.Linq;
using UnityEngine;

namespace NekkoYa.VRCDataChecker
{
    public static class PrefabRules
    {
        public static void Transform(CheckContext context, Transform transform, string path, string objectPath)
        {
            var scale = transform.localScale;
            if (scale.x < 0 || scale.y < 0 || scale.z < 0) context.Add("PREFAB_NEGSCALE_001", CheckSeverity.Warning, CheckCategory.Prefab, path, L10n.Text("負のScaleです。"), objectPath: objectPath);
            if (!Mathf.Approximately(scale.x, scale.y) || !Mathf.Approximately(scale.y, scale.z)) context.Add("PREFAB_NONUNIFORM_001", CheckSeverity.Info, CheckCategory.Prefab, path, L10n.Text("非一様Scaleです。"), objectPath: objectPath);
            if (transform.CompareTag("EditorOnly")) context.Add("PREFAB_EDITORONLY_001", CheckSeverity.Warning, CheckCategory.Prefab, path, L10n.Text("EditorOnlyタグが付いています。"), L10n.Text("配布物の実行時に消えてよいObjectか確認してください。"), objectPath);
            if (transform.gameObject.layer != 0) context.Add("PREFAB_LAYER_001", CheckSeverity.Info, CheckCategory.Prefab, path, "Layer: " + transform.gameObject.layer + " (" + LayerMask.LayerToName(transform.gameObject.layer) + ")", objectPath: objectPath);
            if (transform.Cast<Transform>().GroupBy(t => t.name).Any(g => g.Count() > 1)) context.Add("PREFAB_NAME_001", CheckSeverity.Info, CheckCategory.Prefab, path, L10n.Text("同一階層に同名GameObjectがあります。Animationパスが曖昧になる可能性があります。"), objectPath: objectPath);
        }
        public static void Component(CheckContext context, Component component, string path, string objectPath)
        {
            if (component is Renderer renderer)
            {
                if (renderer.sharedMaterials.Any(m => m == null)) context.Add("PREFAB_MATERIAL_001", CheckSeverity.Error, CheckCategory.Prefab, path, L10n.Text("RendererのMaterial Slotが空です。"), L10n.Text("Materialを設定してください。"), objectPath);
                Mesh mesh = null;
                if (renderer is SkinnedMeshRenderer skinned) mesh = skinned.sharedMesh;
                else if (renderer is MeshRenderer) mesh = renderer.GetComponent<MeshFilter>()?.sharedMesh;
                if (renderer is SkinnedMeshRenderer || renderer is MeshRenderer)
                {
                    if (mesh == null) context.Add("PREFAB_MESH_001", CheckSeverity.Error, CheckCategory.Prefab, path, L10n.Text("RendererのMeshが空です。"), L10n.Text("Meshを設定してください。"), objectPath);
                    else ModelRules.InspectUsage(context, mesh, renderer, renderer is SkinnedMeshRenderer, false, path, objectPath);
                }
                if (renderer is ParticleSystemRenderer particles && particles.renderMode == ParticleSystemRenderMode.Mesh)
                {
                    var meshes = new Mesh[particles.meshCount];
                    particles.GetMeshes(meshes);
                    if (meshes.Length == 0 || meshes.Any(m => m == null)) context.Add("PARTICLE_MESH_001", CheckSeverity.Error, CheckCategory.Particle, path, L10n.Text("Mesh ParticleのMeshが空です。"), objectPath: objectPath);
                    foreach (var particleMesh in meshes) ModelRules.InspectUsage(context, particleMesh, particles, false, true, path, objectPath);
                }
            }
            if (component is MeshFilter filter && filter.sharedMesh == null) context.Add("PREFAB_MESH_001", CheckSeverity.Error, CheckCategory.Prefab, path, L10n.Text("MeshFilterのMeshが空です。"), objectPath: objectPath);
            if (!context.IsAvatar) return;
            string type = component.GetType().FullName ?? component.GetType().Name;
            string id = null;
            CheckSeverity severity = CheckSeverity.Warning;
            if (type.IndexOf("DynamicBone", StringComparison.OrdinalIgnoreCase) >= 0) id = "AVATAR_DYNAMICBONE_001";
            else if (type.StartsWith("UnityEngine.Animations.", StringComparison.Ordinal) && type.EndsWith("Constraint", StringComparison.Ordinal)) id = "AVATAR_CONSTRAINT_001";
            else if (component is Cloth) id = "AVATAR_CLOTH_001";
            else if (component is Light) id = "AVATAR_LIGHT_001";
            else if (component is Camera) id = "AVATAR_CAMERA_001";
            else if (component is AudioSource) id = "AVATAR_AUDIO_001";
            else if (component is Rigidbody || component is Collider || component is Joint) { id = "AVATAR_PHYSICS_001"; severity = CheckSeverity.Info; }
            if (id != null)
            {
                if (context.Mobile) severity = CheckSeverity.Warning;
                context.Add(id, severity, CheckCategory.VRC, path, L10n.Text("Avatarで使用に注意が必要なComponent: ") + type + (context.Mobile ? L10n.Text("（Mobile対象）") : ""),
                    L10n.Text("対象プラットフォームのSDK対応状況と意図した動作を確認してください。"), objectPath);
            }
        }
    }
}
