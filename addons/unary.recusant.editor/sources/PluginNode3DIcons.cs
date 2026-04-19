#if TOOLS

using System;
using Godot;
using Unary.Core;
using Unary.Core.Editor;

namespace Unary.Recusant.Editor
{
    [Tool]
    public partial class PluginNode3DIcons : IPluginSystem
    {
        private EditorSelection _editorSelection;

        bool ISystem.Initialize()
        {
            //_editorSelection.SelectionChanged += OnSelectionChanged;
            return true;
        }

        void ISystem.Deinitialize()
        {
            //_editorSelection.SelectionChanged -= OnSelectionChanged;
        }

        private void OnSelectionChanged()
        {
            
        }
    }
}

#endif
