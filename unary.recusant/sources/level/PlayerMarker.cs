using Godot;

namespace Unary.Recusant
{
    [Tool]
    [GlobalClass]
    public partial class PlayerMarker : Node3D
    {
        public enum MarkerType
        {
            Start
        };

        [Export]
        public MarkerType Type
        {
            get
            {
                return field;
            }
            set
            {
                field = value;
#if TOOLS
                if (Engine.IsEditorHint())
                {
                    UpdateGizmos();
                }
#endif
            }
        } = MarkerType.Start;

        public static StringName PlayerMarkerGroup { get; } = new(nameof(PlayerMarker));

        private void InitializeGroup()
        {
            if (!IsInGroup(PlayerMarkerGroup))
            {
                AddToGroup(PlayerMarkerGroup, true);
            }
        }

        public override void _Ready()
        {
            if (Engine.Singleton.IsEditorHint())
            {
                CallDeferred(MethodName.InitializeGroup);
                return;
            }

            PlayerManager.Singleton.AddMarker(this);
        }

        public override void _ExitTree()
        {
            if (Engine.IsEditorHint())
            {
                return;
            }
            PlayerManager.Singleton.RemoveMarker(this);
        }

#if TOOLS
        public override void _Process(double delta)
        {
            if (!Engine.IsEditorHint())
            {
                return;
            }

            Vector3 rotation = RotationDegrees;
            rotation.X = 0.0f;
            rotation.Z = 0.0f;
            RotationDegrees = rotation;
        }
#endif

    }
}
