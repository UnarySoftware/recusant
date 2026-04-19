#if TOOLS

using System;
using Godot;
using Unary.Core;
using Unary.Core.Editor;

namespace Unary.Recusant.Editor
{
    [Tool]
    public partial class PluginNode3DPlacer : IPluginSystem
    {
        private EditorSelection _editorSelection;

        bool ISystem.Initialize()
        {
            _editorSelection = EditorInterface.Singleton.GetSelection();
            _editorSelection.SelectionChanged += OnSelectionChanged;
            return true;
        }

        void ISystem.Deinitialize()
        {
            _editorSelection.SelectionChanged -= OnSelectionChanged;
        }

        private void OnSelectionChanged()
        {
            var nodes = _editorSelection.GetTopSelectedNodes();
            var editedRoot = EditorInterface.Singleton.GetEditedSceneRoot();

            foreach (var node in nodes)
            {
                if (node == editedRoot)
                {
                    continue;
                }

                if (node is Node3D node3D)
                {
                    Type type = node3D.GetType();

                    this.Log(type.FullName);
                }
            }
        }
    }
}

#endif
