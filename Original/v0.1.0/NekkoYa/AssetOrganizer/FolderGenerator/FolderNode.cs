using System;
using System.Collections.Generic;
using UnityEngine;

namespace NekkoYa.AssetOrganizer.Editor
{
    [Serializable]
    internal sealed class FolderNode
    {
        [SerializeField] private string name = "New Folder";
        [SerializeField] private List<FolderNode> children = new List<FolderNode>();
        [SerializeField] private bool expanded = true;

        public string Name
        {
            get => name ?? string.Empty;
            set => name = value ?? string.Empty;
        }

        public List<FolderNode> Children
        {
            get
            {
                if (children == null)
                    children = new List<FolderNode>();
                return children;
            }
        }

        public bool Expanded
        {
            get => expanded;
            set => expanded = value;
        }

        public FolderNode Clone()
        {
            var clone = new FolderNode { Name = Name, Expanded = Expanded };
            foreach (FolderNode child in Children)
                clone.Children.Add(child.Clone());
            return clone;
        }
    }
}
