using Godot;
using Unary.Core;

namespace Unary.Recusant
{
    [Tool]
    [GlobalClass]
    [Icon("res://addons/unary.core.editor/icons/Brush.svg")]
    public partial class BrushDynamic : BaseFgd, IFgdCollisionBody
    {
        [Export]
        public bool SelfDelete
        {
            get => false;
            set { if (value) QueueFree(); }
        }

        public static StringName BodyName { get; } = new("body");

        public CollisionObject3D CreateCollisionBody()
        {
            return new PlatformBody3D
            {
                Name = BodyName
            };
        }
    }
}
