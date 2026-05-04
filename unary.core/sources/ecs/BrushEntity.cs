using Godot;
using Unary.Core.Editor;

namespace Unary.Core
{
    [Tool]
    [GlobalClass, Icon("res://addons/unary.core.editor/icons/Brush.svg")]
    public partial class BrushEntity : Node3D
    {
        [Export]
        public Entity Entity;

#if TOOLS
        public override void _Ready()
        {
            CallDeferred(MethodName.SetEntity);
        }

        private void SetEntity()
        {
            if (!IsInsideTree() || Entity != null)
            {
                return;
            }

            var groups = GetGroups();

            if (groups.Count == 0)
            {
                return;
            }

            if (groups.Count > 1)
            {
                PluginLogger.Warning(this, "BrushEntity has more than 1 node group");
                return;
            }

            var group = groups[0];

            var nodes = GetTree().GetNodesInGroup(group);

            foreach (var node in nodes)
            {
                if (node is BrushEntityComponent component)
                {
                    Entity = component.Entity;
                }
            }
        }

#endif

    }
}
