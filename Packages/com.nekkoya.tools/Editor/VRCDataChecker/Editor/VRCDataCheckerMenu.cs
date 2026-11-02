using UnityEditor;

namespace NekkoYa.VRCDataChecker
{
    public static class VRCDataCheckerMenu
    {
        [MenuItem("NekkoYa/VRC Data Checker")]
        public static void Open() => VRCDataCheckerWindow.Open();
        [MenuItem("Assets/NekkoYa/VRC Data Checker", false, 2000)]
        public static void OpenSelected() => VRCDataCheckerWindow.Open(Selection.activeObject as DefaultAsset);
        [MenuItem("Assets/NekkoYa/VRC Data Checker", true)]
        private static bool Validate() => AssetCollector.Validate(AssetDatabase.GetAssetPath(Selection.activeObject)) == null;
    }
}
