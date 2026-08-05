using Godot;
using System.Linq;
using Unary.Core;

namespace Unary.Recusant.Levels.Test
{
    [Tool]
    [GlobalClass]
    public partial class PlatformRotator : Component, IFgdOwner
    {
        [Export]
        public string BrushName = "Rotator";
        private BrushDynamic brush;
        private PlatformBody3D platform;

        public override void Initialize()
        {
            brush = FgdManager.Singleton.OwnByNameSingle<BrushDynamic>(this, BrushName);
            platform = (PlatformBody3D)brush.GetChild(0);
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

            var Position = platform.Position;

            _timer += delta;
            Position.Y = (float)(Mathf.Sin(_timer) + 2.0);
            platform.Position = Position;
        }

        public void OnDestroy(BaseFgd fgd)
        {
            Entity.QueueFree();
        }
    }
}
