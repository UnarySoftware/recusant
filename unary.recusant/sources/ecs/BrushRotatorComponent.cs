using Godot;
using Unary.Core;

namespace Unary.Recusant
{
    [Tool]
    [GlobalClass]
    public partial class BrushRotatorComponent : Component
    {
        private BrushEntity brush;

        public override void Initialize()
        {
            brush = Entity.GetComponent<BrushEntityComponent>().GetBrushEntity();
        }

        private double _timer = 0.0f;

        public override void _PhysicsProcess(double delta)
        {
            if (Engine.Singleton.IsEditorHint() || brush == null)
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
