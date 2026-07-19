// Copyright (c) 2023 func-godot
// C# port of func_godot (https://github.com/func-godot/func_godot_plugin),
// used under the MIT License. See addons/unary.core.editor/sources/func_godot/LICENSE.md.

using Godot;

namespace FuncGodot
{
    /// <summary>
    /// FGD SolidClass entity definition. Builds a <see cref="MeshInstance3D"/> from the entity's brushes, plus
    /// <see cref="CollisionShape3D"/> nodes when <see cref="FuncGodotFGDEntityClass.NodeClass"/> inherits
    /// <see cref="CollisionObject3D"/>.
    /// </summary>
    [Tool]
    [GlobalClass]
    public partial class FuncGodotFGDSolidClass : FuncGodotFGDEntityClass
    {
        public FuncGodotFGDSolidClass()
        {
            Prefix = "@SolidClass";
        }

        public enum SpawnTypes
        {
            /// Geometry is built relative to the FuncGodotMap node's position.
            Worldspawn = 0,

            /// Geometry is merged into worldspawn and this entity is dropped, mimicking func_group.
            MergeWorldspawn = 1,

            /// Built as its own node, positioned by OriginType.
            Entity = 2,
        }

        public enum OriginTypes
        {
            /// Average of all brush vertices.
            Averaged = 0,

            /// The origin class property, in global coordinates.
            Absolute = 1,

            /// The origin class property, as an offset from the bounding box center.
            Relative = 2,

            /// Center of the bounds of all brushes textured with the origin texture. Falls back to BoundsCenter.
            Brush = 3,

            BoundsCenter = 4,

            /// Lowest bounding box corner. Standard Quake and Half-Life brush entity behavior.
            BoundsMins = 5,

            BoundsMaxs = 6,
        }

        public enum CollisionShapeTypes
        {
            /// No collision. For decorative geometry like vines, wires, or grass.
            None,

            /// One convex shape per brush. Required for non-StaticBody3D nodes such as Area3D.
            Convex,

            /// A single concave shape per surface type.
            Concave,
        }

        [Export]
        public SpawnTypes SpawnType = SpawnTypes.Entity;

        /// How this entity finds its center. Only used when SpawnType is Entity.
        [Export]
        public OriginTypes OriginType = OriginTypes.Brush;

        [ExportGroup("Geometry")]

        /// <summary>
        /// Snapping epsilon applied to this entity's generated vertices. Raising it can close seams between
        /// polygons; zero disables snapping. Overridden per entity by the map settings'
        /// <see cref="FuncGodotMapSettings.VertexMergeDistanceProperty"/> class property when a brush sets it.
        /// </summary>
        [Export]
        public float VertexMergeDistance = 0.0f;

        /// <summary>
        /// Culls this entity's interior faces, meaning faces whose vertices match or sit flush within a larger
        /// face. Costs build time proportional to the entity's brush count. Overridden per entity by the map
        /// settings' <see cref="FuncGodotMapSettings.CullInteriorFacesProperty"/> class property when a brush sets it.
        /// </summary>
        [Export]
        public bool CullInteriorFaces = false;

        [ExportGroup("Visual Build")]

        [Export]
        public bool BuildVisuals = true;

        /// Setting this to Static unwraps the mesh's UV2 during build.
        [Export]
        public GeometryInstance3D.GIModeEnum GlobalIlluminationMode = GeometryInstance3D.GIModeEnum.Static;

        [Export]
        public GeometryInstance3D.ShadowCastingSetting ShadowCastingSetting = GeometryInstance3D.ShadowCastingSetting.On;

        [Export]
        public bool BuildOcclusion = false;

        /// The mesh is only visible to cameras whose cull mask includes one of these layers.
        [Export(PropertyHint.Layers3DRender)]
        public uint RenderLayers = 1;

        [ExportGroup("Collision Build")]

        [Export]
        public CollisionShapeTypes CollisionShapeType = CollisionShapeTypes.Convex;

        [Export(PropertyHint.Layers3DPhysics)]
        public uint CollisionLayer = 1;

        [Export(PropertyHint.Layers3DPhysics)]
        public uint CollisionMask = 1;

        /// Higher priority means less penetration into this entity when a collision is resolved.
        [Export]
        public float CollisionPriority = 1.0f;

        /// Not used by Godot Physics. See Shape3D.
        [Export]
        public float CollisionShapeMargin = 0.04f;

        /// <summary>
        /// The following flags add a <c>func_godot_mesh_data</c> dictionary to the generated node's metadata.
        /// The arrays it holds are parallel: element N of each describes the same mesh face.
        /// </summary>
        [ExportGroup("Mesh Metadata")]

        /// <summary>
        /// Adds a texture lookup table: an array of StringName called <c>texture_names</c>, and a
        /// PackedInt32Array called <c>textures</c> indexing into it, one entry per face.
        /// </summary>
        [Export]
        public bool AddTexturesMetadata = false;

        /// Adds a PackedVector3Array called <c>vertices</c>, three vertices per face.
        [Export]
        public bool AddVertexMetadata = false;

        /// Adds a PackedVector3Array called <c>positions</c>, the center of each face, local to the node.
        [Export]
        public bool AddFacePositionMetadata = false;

        /// Adds a PackedVector3Array called <c>normals</c>, one per face.
        [Export]
        public bool AddFaceNormalMetadata = false;

        /// <summary>
        /// Adds a dictionary called <c>collision_shape_to_face_indices_map</c>, keyed by the name of each child
        /// CollisionShape3D, holding the face indices that shape covers. For example
        /// <c>{ "entity_1_brush_0_collision_shape" : [0, 1, 3] }</c>.
        /// </summary>
        [Export]
        public bool AddCollisionShapeToFaceIndicesMetadata = false;

        [ExportGroup("Scripting")]

        /// Script to attach to the node generated on map build.
        [Export]
        public Script ScriptClass;
    }
}
