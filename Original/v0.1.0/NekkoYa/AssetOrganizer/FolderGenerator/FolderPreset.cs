using System.Collections.Generic;
using UnityEngine;

namespace NekkoYa.AssetOrganizer.Editor
{
    internal sealed class FolderPreset : ScriptableObject
    {
        [SerializeField] private List<FolderNode> roots = new List<FolderNode>();

        public List<FolderNode> Roots
        {
            get
            {
                if (roots == null)
                    roots = new List<FolderNode>();
                return roots;
            }
            set => roots = value ?? new List<FolderNode>();
        }
    }
}
