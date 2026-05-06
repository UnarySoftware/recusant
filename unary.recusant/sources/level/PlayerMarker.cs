using Godot;
using Unary.Core;

namespace Unary.Recusant
{
    [Tool]
    [GlobalClass]
    [ScenePlaceable]
    public partial class PlayerMarker : Node3D, IGizmo
    {

#if TOOLS
        private static EditorSettingVariable<bool> _drawMarkers = new()
        {
            EditorDefault = true,
            Group = "Gizmos",
            Name = "PlayerMarkers",
            Description = "Draws Player Markers"
        };

        private void OnDrawChanged(EditorSettingVariableBase variable)
        {
            this.UpdateGizmo();
        }
#endif

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
                    this.UpdateGizmo();
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
#if TOOLS
            _drawMarkers.OnValueChanged += OnDrawChanged;

            if (Engine.Singleton.IsEditorHint())
            {
                CallDeferred(MethodName.InitializeGroup);
                return;
            }
#endif
            PlayerManager.Singleton.AddMarker(this);
            RuntimeGizmos.Singleton.Aquire(this);
        }

        public override void _ExitTree()
        {
#if TOOLS
            _drawMarkers.OnValueChanged -= OnDrawChanged;

            if (Engine.IsEditorHint())
            {
                return;
            }
#endif
            PlayerManager.Singleton.RemoveMarker(this);
            RuntimeGizmos.Singleton.Release(this);
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

        private static bool _initialized = false;
        private static readonly CylinderMesh _mesh = new()
        {
            TopRadius = PlayerConstants.PlayerShape.Cache.Radius,
            BottomRadius = PlayerConstants.PlayerShape.Cache.Radius,
            Height = PlayerConstants.PlayerShape.Cache.Height,
        };

        private static TriangleMesh _triangleMesh;

        private static Vector3 _position = new Vector3(0.0f, PlayerConstants.PlayerShape.Cache.Height / 2.0f, 0.0f);

        private static void TryInitializeGizmo()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;

            _triangleMesh ??= new();

            Vector3[] faces = _mesh.GetFaces();

            // This has to be done since Player has its origin at the bottom instead of middle
            for (int i = 0; i < faces.Length; i++)
            {
                faces[i] += _position;
            }

            _triangleMesh.CreateFromFaces(faces);
        }

        void IGizmo.DrawGizmo()
        {
            this.DrawBegin();
            TryInitializeGizmo();

            if (_drawMarkers.Value)
            {
                Color color;

                switch (Type)
                {
                    default:
                    case MarkerType.Start:
                        {
                            color = Colors.Green;
                            break;
                        }
                    case MarkerType.End:
                        {
                            color = Colors.Red;
                            break;
                        }
                }

                this.DrawMesh(_position, _mesh, color, true);
                this.DrawArrow(_position, _position + Vector3.Forward, color, false);
                this.AddCollision(_triangleMesh);
            }

            this.DrawEnd();
        }
    }
}
