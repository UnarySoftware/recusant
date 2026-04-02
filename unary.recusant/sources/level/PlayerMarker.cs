using Godot;

namespace Unary.Recusant
{
    [Tool]
    [GlobalClass]
    public partial class PlayerMarker : Node3D
    {
        public enum MarkerType
        {
            Start,
            End
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

        public override void _Ready()
        {
            if (Engine.IsEditorHint())
            {
                return;
            }

            PlayerManager.Singleton.AddMarker(this);

            if (RuntimeGizmos.Singleton.Test)
            {
                AddGizmo();
            }

            RuntimeGizmos.Singleton.OnTestChanged += OnGizmoChange;
        }

        private void AddGizmo()
        {
            _gizmo = RuntimeGizmos.Singleton.Aquire();
            _gizmo.SetBox(new(PlayerConstants.PlayerRadius * 2.0f, PlayerConstants.PlayerHeight, PlayerConstants.PlayerRadius * 2.0f), new Color(1.0f, 0.0f, 0.0f, 1.0f));
        }

        private void RemoveGizmo()
        {
            RuntimeGizmos.Singleton.Release(_gizmo);
        }

        private void OnGizmoChange(bool value)
        {
            if (value)
            {
                AddGizmo();
            }
            else
            {
                RemoveGizmo();
            }
        }

        private RuntimeGizmo _gizmo;

        public override void _ExitTree()
        {
            if (Engine.IsEditorHint())
            {
                return;
            }

            RuntimeGizmos.Singleton.OnTestChanged -= OnGizmoChange;

            PlayerManager.Singleton.RemoveMarker(this);
        }

#if TOOLS
        public override void _Process(double delta)
        {
            if (_gizmo != null)
            {
                Vector3 position = Position;
                position.Y += PlayerConstants.PlayerHeight / 2.0f;
                _gizmo?.SetPositionRotation(position, Rotation);
            }

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
