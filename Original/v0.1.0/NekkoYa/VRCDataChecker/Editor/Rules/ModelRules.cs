using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Nekkoya.VRCDataChecker
{
    public static class ModelRules
    {
        public static void Evaluate(CheckContext context, string path)
        {
            if (AssetImporter.GetAtPath(path) is ModelImporter importer && !Mathf.Approximately(importer.globalScale, 1f))
                context.Add("MESH_SCALE_001", CheckSeverity.Info, CheckCategory.Model, path, "Model Import Scale: " + importer.globalScale);
            foreach (var mesh in context.Load(path).OfType<Mesh>())
                CheckRunner.Guard(context, path, () =>
                {
                    long triangles = 0;
                    for (int i = 0; i < mesh.subMeshCount; i++) if (mesh.GetTopology(i) == MeshTopology.Triangles) triangles += (long)mesh.GetIndexCount(i) / 3;
                    context.Add("MESH_STATS_001", CheckSeverity.Info, CheckCategory.Mesh, path,
                        mesh.name + ": Vertices=" + mesh.vertexCount + ", Triangles=" + triangles + ", SubMeshes=" + mesh.subMeshCount + ", BlendShapes=" + mesh.blendShapeCount + ", Bounds=" + mesh.bounds);
                    var names = Enumerable.Range(0, mesh.blendShapeCount).Select(mesh.GetBlendShapeName);
                    if (names.GroupBy(n => n).Any(g => g.Count() > 1)) context.Add("MESH_BLENDSHAPE_001", CheckSeverity.Warning, CheckCategory.Mesh, path, mesh.name + ": 重複BlendShape名があります。");
                });
        }
        public static void InspectUsage(CheckContext context, Mesh mesh, Renderer renderer, bool skinned, bool particle, string prefabPath, string objectPath)
        {
            if (mesh == null) return;
            string meshPath = AssetDatabase.GetAssetPath(mesh);
            // External meshes are dependencies, not normal inspection targets.
            if (!AssetCollector.IsInside(meshPath, context.rootFolder)) return;
            if (context.assets.TryGetValue(meshPath, out var info) && info.isIgnored) return;
            var importer = AssetImporter.GetAtPath(meshPath) as ModelImporter;
            bool readable = importer != null ? importer.isReadable : mesh.isReadable;
            if (!readable && (skinned || particle))
            {
                var severity = context.IsAvatar ? CheckSeverity.Error : particle ? CheckSeverity.Warning : CheckSeverity.Info;
                context.Add(particle ? "MESH_READABLE_002" : "MESH_READABLE_001", severity, CheckCategory.Mesh, meshPath,
                    (particle ? "Particle Mesh" : "Skinned Mesh") + "として使用されていますがRead/WriteがOFFです。参照元: " + prefabPath + " / " + objectPath,
                    "対象用途で必要なRead/Write設定を確認してください。", objectPath);
            }
            if (mesh.subMeshCount != renderer.sharedMaterials.Length)
                context.Add("MESH_MATERIAL_001", CheckSeverity.Warning, CheckCategory.Mesh, prefabPath, "SubMesh数とMaterial Slot数が不一致です。", "意図的な複数Pass描画か確認してください。", objectPath);
            if (!mesh.HasVertexAttribute(VertexAttribute.Normal)) context.Add("MESH_NORMAL_001", CheckSeverity.Warning, CheckCategory.Mesh, meshPath, mesh.name + ": Normalがありません。");
            bool normalMap = renderer.sharedMaterials.Where(m => m != null && m.shader != null).Any(m => m.GetTexturePropertyNames().Any(p =>
                (p.IndexOf("normal", StringComparison.OrdinalIgnoreCase) >= 0 || p.IndexOf("bump", StringComparison.OrdinalIgnoreCase) >= 0) && m.GetTexture(p) != null));
            if (normalMap && !mesh.HasVertexAttribute(VertexAttribute.Tangent)) context.Add("MESH_TANGENT_001", CheckSeverity.Warning, CheckCategory.Mesh, meshPath, mesh.name + ": Normal Map用途が推定されますがTangentがありません。");
            if (mesh.bounds.size.magnitude > context.settings.thresholds.avatarBoundsWarningMeters)
                context.Add("MESH_BOUNDS_001", CheckSeverity.Warning, CheckCategory.Mesh, meshPath, "MeshのローカルBoundsが推奨値を超えています: " + mesh.bounds.size);
            if (skinned)
            {
                if (!readable)
                    context.Add("MESH_DATA_001", CheckSeverity.Info, CheckCategory.Mesh, meshPath, "Read/WriteがOFFのため頂点ごとのWeight検査をスキップしました。");
                else
                {
                    var weights = mesh.GetAllBoneWeights();
                    var counts = mesh.GetBonesPerVertex();
                    try
                    {
                        int index = 0, zero = 0;
                        foreach (var count in counts)
                        {
                            float total = 0;
                            for (int j = 0; j < count; j++) total += weights[index++].weight;
                            if (total <= 0) zero++;
                        }
                        if (counts.Length == 0 && mesh.vertexCount > 0) zero = mesh.vertexCount;
                        if (zero > 0) context.Add("MESH_WEIGHT_001", CheckSeverity.Warning, CheckCategory.Mesh, meshPath, mesh.name + ": Weight 0頂点が " + zero + "個あります。");
                    }
                    finally { weights.Dispose(); counts.Dispose(); }
                }
            }
        }
    }
}
