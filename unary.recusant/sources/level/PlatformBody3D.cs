using Godot;
using Unary.Core;

namespace Unary.Recusant
{
    [Tool]
    [GlobalClass]
    public partial class PlatformBody3D : AnimatableBody3D, IPhysicsProcess
    {
        [Export]
        public float DamagePerTick = 1.0f;

        private int _blockerFrames = 0;

        public bool ShouldMove
        {
            get
            {
                return _blockerFrames == 0;
            }
        }

        private SlotHandle _handle;

        public override void _Ready()
        {
            if (Engine.Singleton.IsEditorHint())
            {
                return;
            }

            _handle = Updater.Singleton.PhysicsProcess.Subscribe(this);
        }

        public override void _ExitTree()
        {
            if (Engine.Singleton.IsEditorHint())
            {
                return;
            }

            Updater.Singleton.PhysicsProcess.Unsubscribe(_handle);
        }

        public void ForbidMove()
        {
            _blockerFrames = 1;
        }

        public void PhysicsProcess(float delta)
        {
            _blockerFrames--;

            if (_blockerFrames < 0)
            {
                _blockerFrames = 0;
            }
        }
    }
}
