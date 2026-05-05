using Godot;

namespace Unary.Recusant
{
    [Tool]
    [GlobalClass]
    public partial class PlayerCamera3D : Camera3D
    {
        public enum PostProcessSlot
        {
            GeneralBlur = 0,
            Placeholder1 = 1,
            Placeholder2 = 2,
            Placeholder3 = 3,
            Max
        };

        public float Offset = -0.001f;

        [Export]
        public MeshInstance3D PostProcessMesh;

        public override void _Ready()
        {
#if TOOLS
            if (Engine.Singleton.IsEditorHint())
            {
                return;
            }
#endif
            CameraManager.Singleton.Add(this);
        }

        public override void _EnterTree()
        {
#if TOOLS
            if (Engine.Singleton.IsEditorHint())
            {
                return;
            }
#endif
            CameraManager.Singleton.Remove(this);
        }
    }
}
