using Godot;
using Unary.Core;

namespace Unary.Recusant
{
    [Tool]
    [GlobalClass]
    public partial class BrushRotatorComponent : Component
    {
        private BrushEntity brush;
        private PlatformBody3D platform;

        public override void Initialize()
        {
            brush = Entity.GetComponent<BrushEntityComponent>().GetBrushEntity();
            platform = brush.GetNode<PlatformBody3D>("%PlatformBody3D");
        }

        private double _timer = 0.0f;

        public override void _PhysicsProcess(double delta)
        {
            if (Engine.Singleton.IsEditorHint() || brush == null)
            {
                return;
            }

            if (!platform.ShouldMove)
            {
                return;
            }

            brush.Rotate(Vector3.Up, (float)delta * 1.0f);

            var Position = brush.Position;

            _timer += delta;
            Position.Y = (float)(Mathf.Sin(_timer) + 1.0);
            brush.Position = Position;
        }
    }
}
