using System.Collections.Generic;
using Godot;

namespace Unary.Core
{
    [Tool]
    [GlobalClass, Icon("res://addons/unary.core.editor/icons/Component.svg")]
    public partial class BrushEntityComponent : Component
    {
        private string _brushName = string.Empty;

        [Export]
        public string BrushName
        {
            get
            {
                return _brushName;
            }
            set
            {
                _brushName = value;
#if TOOLS
                if (Engine.Singleton.IsEditorHint())
                {
                    CallDeferred(MethodName.UpdateGroups);
                }
#endif
            }
        }

#if TOOLS
        public override void _Ready()
        {
            if (Engine.Singleton.IsEditorHint())
            {
                CallDeferred(MethodName.UpdateGroups);
            }
        }

        private const string prefixString = "vmf:";

        private void UpdateGroups()
        {
            var groups = GetGroups();

            foreach (var group in groups)
            {
                RemoveFromGroup(group);
            }

            if (string.IsNullOrEmpty(_brushName) || string.IsNullOrWhiteSpace(_brushName))
            {
                return;
            }

            AddToGroup(prefixString + _brushName, true);
        }
#endif

        private readonly List<BrushEntity> _brushes = [];
        private bool _initialized = false;

        private void TryInitialize()
        {
            if (_initialized)
            {
                return;
            }

            var nodes = GetTree().GetNodesInGroup(GetGroups()[0]);

            foreach (var node in nodes)
            {
                if (node is BrushEntity brushEntity)
                {
                    _brushes.Add(brushEntity);
                }
            }
        }

        public List<BrushEntity> GetBrushEntities()
        {
            TryInitialize();
            return _brushes;
        }

        public BrushEntity GetBrushEntity()
        {
            TryInitialize();
            if (_brushes.Count > 0)
            {
                return _brushes[0];
            }
            return null;
        }
    }
}
