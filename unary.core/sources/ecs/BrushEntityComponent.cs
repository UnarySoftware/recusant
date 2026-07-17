using Godot;
using System.Collections.Generic;

namespace Unary.Core
{
    [Tool]
    [GlobalClass, Icon("res://addons/unary.core.editor/icons/Component.svg")]
    public partial class BrushEntityComponent : Component
    {
        public string BrushName = string.Empty;

        private readonly List<BrushEntity> _brushes = [];
        private bool _initialized = false;

        private void TryInitialize()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;

            // Move the group logic handling to 
            var nodes = GetTree().GetNodesInGroup(BrushName);

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
