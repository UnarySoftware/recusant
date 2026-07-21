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

        [Export]
        [FgdProperty]
        public string TestStringProperty;
    }
}
