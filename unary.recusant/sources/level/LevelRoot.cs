using Godot;
using Unary.Core;

namespace Unary.Recusant
{
    [Tool]
    [GlobalClass]
    public partial class LevelRoot : Node3D
    {
        public static StringName LevelRootGroup { get; } = new(nameof(LevelRoot));

#if TOOLS
        [ExportToolButton("Build Navigation")]
        public Callable BuildNavigationCall => Callable.From(OnBuildNavigation);

        private void OnBuildNavigation()
        {
            CallDeferred(MethodName.BuildNavigation);
        }

        public override void _Ready()
        {
            if (Engine.Singleton.IsEditorHint())
            {
                CallDeferred(MethodName.InitializeNodes);
            }
        }
#endif

        private bool Initialized = false;

        [Export(PropertyHint.Range, "15.0,50.0,1.0")]
        public float BoundsSize = 15.0f;

        // Used to visualize path from start to finish on a map
        // Not saved, only used for the editor visualization
        public Vector3[] Points;

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
        // Stores NavBrush.AiNavType
        public int[] PolyTypes;

        [Export]
        // Stores NavBrush.AiNavFlags
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
            property.MakeReadOnly(PropertyName.VertexDistance, PropertyName.Bounds, PropertyName.BoundsCount, PropertyName.BoundsPolys);
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
    }
}
