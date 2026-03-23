using Godot;

namespace Unary.Core
{
	[Tool]
	[GlobalClass, Icon("res://addons/unary.core.editor/icons/Brush.svg")]
	public partial class EditorBrush : Node3D
	{
		public override void _Ready()
		{
#if TOOLS
			if (!Engine.IsEditorHint())
#endif
			{
				QueueFree();
			}
		}
	}
}
