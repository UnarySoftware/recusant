#if TOOLS

using Godot;

namespace Unary.Core.Editor
{
    [Tool]
    public partial class PluginLazyResourcePicker : EditorResourcePicker
    {
        public override void _SetCreateOptions(GodotObject menuNode)
        {
            return;
        }
    }
}

#endif
