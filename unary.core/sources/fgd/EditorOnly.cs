using Godot;

namespace Unary.Core
{
    [Tool]
    [GlobalClass]
    [Icon("res://addons/unary.core.editor/icons/Brush.svg")]
    public partial class EditorOnly : BaseFgd
    {
        [Export]
        [FgdProperty]
        public bool EditorPreserve = false;

        public override void _Ready()
        {
#if TOOLS
            if (Engine.Singleton.IsEditorHint())
            {
                return;
            }
#endif

            QueueFree();
        }

#if TOOLS
        public override void AppliedProperties()
        {
            if (EditorPreserve && Engine.Singleton.IsEditorHint())
            {
                return;
            }

            QueueFree();
        }
#endif
    }
}
