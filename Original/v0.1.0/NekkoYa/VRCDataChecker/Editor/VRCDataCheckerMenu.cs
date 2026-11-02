using UnityEditor;

namespace Nekkoya.VRCDataChecker
{
    public static class VRCDataCheckerMenu
    {
        [MenuItem("Tools/NekkoYa/VRC配布データ検査")]
        public static void Open() => VRCDataCheckerWindow.Open();
        [MenuItem("Assets/NekkoYa/VRC配布データ検査", false, 2000)]
        public static void OpenSelected() => VRCDataCheckerWindow.Open(Selection.activeObject as DefaultAsset);
        [MenuItem("Assets/NekkoYa/VRC配布データ検査", true)]
        private static bool Validate() => AssetCollector.Validate(AssetDatabase.GetAssetPath(Selection.activeObject)) == null;
    }
}
