using UnityEditor;
using UnityEngine;

namespace Nekkoya.VRCDataChecker
{
    public static class SerializedReferenceScanner
    {
        public static bool IsMissing(SerializedProperty property) => property.propertyType == SerializedPropertyType.ObjectReference &&
            property.objectReferenceValue == null && property.objectReferenceInstanceIDValue != 0;

        public static void Scan(CheckContext context, Object obj, string path, string objectPath = null, GameObject prefabRoot = null)
        {
            using (var serialized = new SerializedObject(obj))
            {
                var property = serialized.GetIterator();
                while (property.Next(true))
                {
                    if (property.propertyType != SerializedPropertyType.ObjectReference ||
                        CheckerSettings.Matches(property.propertyPath, context.settings.ignoredReferenceProperties)) continue;
                    if (IsMissing(property))
                    {
                        if (obj is Material material)
                        {
                            string textureProperty = LilToonIntegration.TexturePropertyName(serialized, property.propertyPath);
                            if (textureProperty != null)
                            {
                                string inactive = LilToonIntegration.InactiveReason(material, textureProperty);
                                context.Add(inactive == null ? "MAT_TEXTURE_001" : "MAT_TEXTURE_INACTIVE_001",
                                    inactive == null ? CheckSeverity.Error : CheckSeverity.Info, CheckCategory.Material, path,
                                    inactive == null ? "Texture参照がMissingです: " + textureProperty : "lilToonの未使用設定にMissing参照が残っています: " + textureProperty + "。" + inactive,
                                    inactive == null ? "使用するTextureを再設定してください。" : "現在の設定では使用しません。Animationなどで機能をONにする場合は参照を確認してください。",
                                    objectPath, property.propertyPath);
                                continue;
                            }
                        }
                        context.Add("REF_MISSING_001", CheckSeverity.Error, CheckCategory.Dependency, path,
                            obj.GetType().FullName + ": Missing参照があります。", "参照先を再設定するか、不要な参照を明示的に削除してください。", objectPath, property.propertyPath);
                        if (obj is Animator && property.propertyPath == "m_Controller")
                            context.Add("PREFAB_ANIMATOR_001", CheckSeverity.Error, CheckCategory.Prefab, path, "Animator ControllerがMissingです。", "Controllerを再設定してください。", objectPath, property.propertyPath);
                    }
                    var reference = property.objectReferenceValue;
                    if (reference == null || prefabRoot == null || EditorUtility.IsPersistent(reference)) continue;
                    var go = reference as GameObject;
                    if (reference is Component component) go = component.gameObject;
                    if (go != null && go.scene.IsValid() && go.scene != prefabRoot.scene)
                        context.Add("REF_SCENE_001", CheckSeverity.Error, CheckCategory.Dependency, path, "Prefabから別SceneのObjectを参照しています。", "Prefab内または永続Assetへの参照に変更してください。", objectPath, property.propertyPath);
                }
            }
        }
    }
}
