using UnityEngine;

namespace NekkoYa.Tests
{
    // SDKを導入していない検証環境でも、参照移動とUndoの基礎動作を検証する。
    // 本物のVRC SDKの型名は使わず、SDK検出には影響させない。
    public sealed class PhysBoneTestComponent : MonoBehaviour
    {
        public Transform rootTransform;
        public PhysBoneTestComponent linkedSource;
        public float pull = 0.25f;
    }
}
