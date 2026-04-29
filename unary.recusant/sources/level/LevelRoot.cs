using Godot;
using System.Collections.Generic;
using Unary.Core;

namespace Unary.Recusant
{
    [Tool]
    [GlobalClass]
    [SceneAncor]
    public partial class LevelRoot : Node3D, IGizmo
    {
        public static StringName LevelRootGroup { get; } = new(nameof(LevelRoot));

#if TOOLS
        [ExportToolButton("Build Navigation")]
        public Callable BuildNavigationCall => Callable.From(OnBuildNavigation);

        [ExportToolButton("Reset Navigation")]
        public Callable ResetNavigationCall => Callable.From(OnResetNavigation);

        private void OnBuildNavigation()
        {
            CallDeferred(MethodName.BuildNavigation);
        }

        private void OnResetNavigation()
        {
            CallDeferred(MethodName.ResetNavigation);
        }

        private void OnGizmoChange(EditorSettingVariableBase variable)
        {
            this.UpdateGizmo();
        }

        public override void _Ready()
        {
            _drawVisualPaths.OnValueChanged += OnGizmoChange;
            _drawBounds.OnValueChanged += OnGizmoChange;

            if (Engine.Singleton.IsEditorHint())
            {
                CallDeferred(MethodName.InitializeNodes);
            }
        }

        public override void _ExitTree()
        {
            _drawVisualPaths.OnValueChanged -= OnGizmoChange;
            _drawBounds.OnValueChanged -= OnGizmoChange;
        }

        [Export(PropertyHint.Range, "0.01f,1.0f,0.000001f")]
        public float PathMargin = 0.01f;

        public struct VisualPath
        {
            public Vector3[] Points { get; set; }
            public Vector3 RealStart { get; set; }
            public Vector3 ResolvedStart { get; set; }
        }

        private static EditorSettingVariable<bool> _drawVisualPaths = new()
        {
            EditorDefault = true,
            CustomGroup = "Gizmos",
            CustomName = "LevelRoot Paths",
        };

        private static EditorSettingVariable<bool> _drawBounds = new()
        {
            EditorDefault = true,
            CustomGroup = "Gizmos",
            CustomName = "LevelRoot Bounds",
        };

        private DebugData<List<VisualPath>> VisualPaths
        {
            get { field ??= new(this, GetLevelName, nameof(VisualPaths)); return field; }
        }

        private DebugData<Vector3[]> FromStartToFinish
        {
            get { field ??= new(this, GetLevelName, nameof(FromStartToFinish)); return field; }
        }

#endif

        private bool Initialized = false;

        [Export(PropertyHint.Range, "15.0,50.0,1.0")]
        public float BoundsSize = 15.0f;

        [Export]
        private string LevelName = nameof(LevelRoot);

        public string GetLevelName()
        {
            return LevelName;
        }

        // Vertex related info

        // Flow distance to the end of the map of each vertex
        [Export]
        public float[] VertexDistance;

        // Poly related info

        // Polys, 3 vertex entry per poly
        // These are sorted by flow, so we never use NavMesh GetPolygon() method ever at runtime
        [Export]
        public int[] Polys;

        [Export]
        // Stores NavBrush.Flag for each poly
        public int[] PolyFlags;

        // Bound related info

        // Declaration of boundaries that are used to BVH all triangles in the level
        [Export]
        public Vector3[] Bounds;

        // How many polys does a single bound stores inside?
        [Export]
        public int[] BoundsCount;

        // References a poly that gets aquired from a Poly array
        [Export]
        public int[] BoundsPolys;

        public override void _ValidateProperty(Godot.Collections.Dictionary property)
        {
            property.MakeReadOnly(
                PropertyName.VertexDistance,
                PropertyName.Polys,
                PropertyName.PolyFlags,
                PropertyName.Bounds,
                PropertyName.BoundsCount,
                PropertyName.BoundsPolys);
            base._ValidateProperty(property);
        }

        public NavigationRegion3D NavigationRegion { get; private set; }
        public NavigationMesh NavigationMesh { get; private set; }

        private void Initialize()
        {
            NavigationRegion = GetNodeOrNull<NavigationRegion3D>("Navigation");

            if (NavigationRegion != null)
            {
                NavigationMesh = NavigationRegion.NavigationMesh;
            }

            LevelManager.Singleton.OnLoaded.Publish(new()
            {
                Root = LevelManager.Singleton.Root,
                Definition = LevelManager.Singleton.Definition,
            });

            RuntimeGizmos.Singleton.Aquire(this);

        }

        private void Deinitialize()
        {
            RuntimeGizmos.Singleton.Release(this);
        }

        public override void _Process(double delta)
        {
            if (Engine.Singleton.IsEditorHint())
            {
                return;
            }

            if (!Initialized)
            {
                Initialized = true;
                Initialize();
            }
        }

        void IGizmo.DrawGizmo()
        {
            this.DrawBegin();

            if (_drawVisualPaths.Value)
            {
                Vector3 pointBox = new(0.2f, 0.2f, 0.2f);

                if (VisualPaths.Value != null && VisualPaths.Value.Count > 0)
                {
                    foreach (var entry in VisualPaths.Value)
                    {
                        if (entry.Points != null && entry.Points.Length > 0)
                        {
                            this.DrawPath(entry.Points, Colors.Green);
                        }

                        this.DrawBoxWireframe(entry.RealStart, pointBox, Colors.Green);
                        this.DrawBoxWireframe(entry.ResolvedStart, pointBox, Colors.Red);
                    }
                }

                if (FromStartToFinish.Value != null && FromStartToFinish.Value.Length > 0)
                {
                    this.DrawBoxWireframe(FromStartToFinish.Value[0], pointBox, Colors.Yellow);
                    this.DrawBoxWireframe(FromStartToFinish.Value[^1], pointBox, Colors.Yellow);
                    this.DrawPath(FromStartToFinish.Value, Colors.Yellow);
                }
            }

            if (_drawBounds.Value)
            {
                Vector3 size = new(BoundsSize / 2.0f, BoundsSize / 2.0f, BoundsSize / 2.0f);

                if (Bounds != null)
                {
                    foreach (var bound in Bounds)
                    {
                        this.DrawBoxWireframe(bound, size, Colors.White);
                    }
                }
            }

            this.DrawEnd();
        }
    }
}
