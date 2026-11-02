using System;
using UnityEngine;

namespace NekkoYa.AssetOrganizer.Editor
{
    [Serializable]
    internal sealed class RenameItem
    {
        [SerializeField] private UnityEngine.Object asset;
        [SerializeField] private string customName = string.Empty;

        public UnityEngine.Object Asset
        {
            get => asset;
            set => asset = value;
        }

        public string CustomName
        {
            get => customName ?? string.Empty;
            set => customName = value ?? string.Empty;
        }
    }
}
