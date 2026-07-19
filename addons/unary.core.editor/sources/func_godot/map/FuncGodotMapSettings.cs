// Copyright (c) 2023 func-godot
// C# port of func_godot (https://github.com/func-godot/func_godot_plugin),
// used under the MIT License. See addons/unary.core.editor/sources/func_godot/LICENSE.md.

using Godot;

namespace FuncGodot
{
    /// <summary>
    /// Reusable build configuration for <see cref="FuncGodotMap"/> nodes.
    /// </summary>
    [Tool]
    [GlobalClass]
    public partial class FuncGodotMapSettings : Resource
    {
        [ExportGroup("Build Settings")]

        /// Derived from <see cref="InverseScaleFactor"/>. Brush coordinates are multiplied by this.
        public float ScaleFactor { get; private set; } = 0.03125f;

        /// <summary>
        /// Ratio between map editor units and Godot units. Brush coordinates are divided by this. Entity
        /// properties are unaffected unless a script does so itself.
        /// </summary>
        [Export]
        public float InverseScaleFactor
        {
            get => field;
            set
            {
                if (value == 0.0f)
                {
                    GD.PushError("Error: Cannot set Inverse Scale Factor to Zero");
                    return;
                }

                field = value;
                ScaleFactor = 1.0f / value;
            }
        } = 32.0f;

        /// Translates map classnames into Godot nodes and packed scenes.
        [Export]
        public FuncGodotFGDFile EntityFgd;

        /// <summary>
        /// Organizes the SceneTree using TrenchBroom Layers and Groups, generated as Node3D nodes. Structural
        /// brushes are moved out of their groups and merged into worldspawn. Layers marked "omit from export"
        /// and everything inside them are skipped.
        /// </summary>
        [Export]
        public bool UseGroupsHierarchy = false;

        /// <summary>
        /// Texel size for UV2 unwrapping, before scaling. The real texel size is this divided by
        /// <see cref="InverseScaleFactor"/>; a ratio around 1/16 is a good starting point. Larger values mean
        /// coarser lightmaps.
        /// </summary>
        [Export]
        public float UvUnwrapTexelSize = 2.0f;

        [ExportGroup("Entity Settings")]

        /// Node groups to add every generated node to.
        [Export]
        public Godot.Collections.Array<string> EntityNodeGroups = [];

        [ExportSubgroup("Entity Property Names")]

        /// <summary>
        /// Class property used to name generated nodes, overridden per entity by
        /// <see cref="FuncGodotFGDEntityClass.NameProperty"/>. Names should be unique.
        /// </summary>
        [Export]
        public string EntityNameProperty = string.Empty;

        /// Class property that decides whether a solid entity's mesh is smooth shaded.
        [Export]
        public string EntitySmoothingProperty = "_phong";

        /// Class property holding the angle threshold above which vertices stop being smoothed together.
        [Export]
        public string EntitySmoothingAngleProperty = "_phong_angle";

        /// <summary>
        /// Class property holding the snapping epsilon for an entity's generated vertices. Raising it can
        /// close seams between polygons.
        /// </summary>
        [Export]
        public string VertexMergeDistanceProperty = "_vertex_merge_distance";

        /// <summary>
        /// Class property that culls a brush entity's interior faces, meaning faces whose vertices match or
        /// sit flush within a larger face. Costs build time proportional to the entity's brush count.
        /// </summary>
        [Export]
        public string CullInteriorFacesProperty = "_cull_interior_faces";

        [ExportGroup("Textures")]

        /// <summary>
        /// Searched for textures whose names match the ones assigned to brush faces in the map.
        /// </summary>
        [Export(PropertyHint.Dir)]
        public string BaseTextureDir = "res://textures";

        [Export]
        public Godot.Collections.Array<string> TextureFileExtensions = ["png", "jpg", "jpeg", "bmp", "tga", "webp"];

        [ExportSubgroup("Hint Textures")]

        /// <summary>
        /// Faces with this texture are dropped from the mesh but kept in the collision shape.
        /// </summary>
        [Export]
        public string ClipTexture
        {
            get => field;
            set => field = value.ToLower();
        } = "clip";

        /// <summary>
        /// Faces with this texture are dropped from the mesh, and from concave collision shapes.
        /// </summary>
        [Export]
        public string SkipTexture
        {
            get => field;
            set => field = value.ToLower();
        } = "skip";

        /// <summary>
        /// Faces with this texture are dropped from both mesh and collision. The bounds of the brushes they
        /// cover become the entity's origin when its OriginType is Brush.
        /// </summary>
        [Export]
        public string OriginTexture
        {
            get => field;
            set => field = value.ToLower();
        } = "origin";

        /// <summary>
        /// Faces with this texture are moved out of the visual mesh into a separate shadows-only
        /// MeshInstance3D, supporting an invisible shadow blocker. They contribute no collision.
        /// </summary>
        [Export]
        public string ShadowTexture
        {
            get => field;
            set => field = value.ToLower();
        } = "shadow";

        [ExportGroup("Materials")]

        /// <summary>
        /// Searched for material resources whose names match brush face textures. Falls back to
        /// <see cref="BaseTextureDir"/> when empty. Generated materials are saved here.
        /// </summary>
        [Export(PropertyHint.Dir)]
        public string BaseMaterialDir = string.Empty;

        [Export]
        public string MaterialFileExtension = "tres";

        /// Template used when a face's material has to be generated.
        [Export]
        public Material DefaultMaterial;

        /// Albedo sampler2D uniform, when <see cref="DefaultMaterial"/> is a ShaderMaterial.
        [Export]
        public string DefaultMaterialAlbedoUniform = string.Empty;

        /// <summary>
        /// Shader uniform name to texture suffix patterns, used only when <see cref="DefaultMaterial"/> is a
        /// ShaderMaterial. Each pattern takes the texture name as <c>{0}</c>, e.g. <c>"{0}_normal"</c>.
        /// </summary>
        [Export]
        public Godot.Collections.Dictionary<string, string> ShaderMaterialUniformMapPatterns = [];

        [ExportSubgroup("BaseMaterial3D Map Patterns")]

        [Export]
        public string AlbedoMapPattern = "{0}_albedo";

        [Export]
        public string NormalMapPattern = "{0}_normal";

        [Export]
        public string MetallicMapPattern = "{0}_metallic";

        [Export]
        public string RoughnessMapPattern = "{0}_roughness";

        [Export]
        public string EmissionMapPattern = "{0}_emission";

        [Export]
        public string AoMapPattern = "{0}_ao";

        [Export]
        public string HeightMapPattern = "{0}_height";

        [Export]
        public string OrmMapPattern = "{0}_orm";

        [ExportSubgroup("")]

        /// <summary>
        /// Saves generated materials to disk so they can be reused across maps. Saved materials no longer
        /// follow <see cref="DefaultMaterial"/>.
        /// </summary>
        [Export]
        public bool SaveGeneratedMaterials = true;
    }
}
