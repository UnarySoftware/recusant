// Copyright (c) 2023 func-godot
// C# port of func_godot (https://github.com/func-godot/func_godot_plugin),
// used under the MIT License. See addons/unary.core.editor/sources/func_godot/LICENSE.md.

using Godot;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Unary.Core;

namespace FuncGodot
{
    /// <summary>
    /// The single func_godot configuration resource. Since Valve 220 is the only supported map format and
    /// TrenchBroom the only supported editor, there is exactly one game: the TrenchBroom game config, the
    /// entity definitions exported as its FGD, and the settings every <see cref="FuncGodotMap"/> builds
    /// with all live here. Load the shared instance with <see cref="Load"/> rather than constructing new
    /// ones.
    /// </summary>
    [Tool]
    [GlobalClass]
    public partial class FuncGodotConfig : Resource
    {
        public const string ConfigPath = "res://func_godot.tres";

        [ExportToolButton("Export GameConfig")]
        public Callable ExportFileButton => Callable.From(ExportFile);

        [ExportToolButton("Export FGD")]
        public Callable ExportFgdButton => Callable.From(() => DoExportFile());

        /// <summary>
        /// Name of the game in TrenchBroom's game list, and the name of the FGD written beside its config.
        /// </summary>
        [Export]
        public string GameName = "Recusant";

        /// Icon for TrenchBroom's game list.
        [Export]
        public Texture2D Icon;

        public const string InitialMap = "initial_valve.map";

        [ExportGroup("Entities")]

        /// <summary>
        /// Folder inside each mod holding that mod's entity definitions, one <see cref="FuncGodotFGDEntityClass"/>
        /// resource per file. Every mod owns the entities it ships, so there is no central list to keep in sync -
        /// see <see cref="GetFgdClasses"/>.
        /// </summary>
        public const string ModFgdFolder = "fgd";

        /// <summary>
        /// Scale expression modifying the default display scale of entities in TrenchBroom. See the
        /// TrenchBroom manual's entity configuration documentation.
        /// </summary>
        [Export]
        public string EntityScale = "32";

        /// Whether TrenchBroom instantiates default entity properties when creating a new entity.
        [Export]
        public bool SetDefaultProperties = false;

        /// Whether <see cref="FuncGodotFGDModelPointClass"/> entities export display models on FGD export.
        [Export]
        public bool GenerateModelPointClassModels = true;

        /// Folder that <see cref="FuncGodotFGDModelPointClass"/> display models are exported into.
        [Export]
        public string ModelPointClassSavePath = string.Empty;

        [ExportGroup("Build Settings")]

        /// Derived from <see cref="InverseScaleFactor"/>. Brush coordinates are multiplied by this.
        public float ScaleFactor { get; private set; } = 1.0f / DefaultInverseScaleFactor;

        /// <summary>
        /// Inverse scale factor a <see cref="FuncGodotFGDModelPointClass"/> falls back to when its own scale
        /// expression is empty, and the value <see cref="InverseScaleFactor"/> starts at.
        /// </summary>
        public const float DefaultInverseScaleFactor = 39.37f;

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
        } = DefaultInverseScaleFactor;

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

        /// Node groups to add every generated node to.
        [Export]
        public Godot.Collections.Array<string> EntityNodeGroups = [];

        [ExportSubgroup("Entity Property Names")]

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

        /// Top level textures folder, relative to the game path. Called materials in recent TrenchBroom.
        public const string TexturesRootFolder = ".trenchbroom";

        /// Textures matching these patterns are hidden from TrenchBroom.
        [Export]
        public Godot.Collections.Array<string> TextureExclusionPatterns =
        [
            "*_albedo", "*_ao", "*_emission", "*_height",
            "*_metallic", "*_normal", "*_orm", "*_roughness", "*_sss",
        ];

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

        /// <summary>
        /// Saves generated materials to disk so they can be reused across maps. Saved materials no longer
        /// follow <see cref="DefaultMaterial"/>.
        /// </summary>
        [Export]
        public bool SaveGeneratedMaterials = true;

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

        [ExportGroup("Tags")]

        [Export]
        public Godot.Collections.Array<TrenchBroomTag> BrushTags = [];

        [Export]
        public Godot.Collections.Array<TrenchBroomTag> BrushFaceTags = [];

        [ExportGroup("Face Attributes")]

        /// Texture scale on new brushes, and the value a UV scale reset returns to.
        [Export]
        public Vector2 DefaultUvScale = Vector2.One;

        [ExportGroup("Paths")]

        /// <summary>
        /// Where the TrenchBroom game configuration is written on Windows, FGD included. Relative paths are
        /// taken as relative to the project root - see <see cref="ResolvePath"/>.
        /// </summary>
        public const string GameConfigFolderWindows = "../windows/trenchbroom/games/Recusant";

        /// The Linux counterpart of <see cref="GameConfigFolderWindows"/>.
        public const string GameConfigFolderLinux = "../linux/trenchbroom/games/Recusant";

        /// <summary>
        /// The folder TrenchBroom is pointed at as its game path. Display model paths in the FGD are written
        /// relative to it, so it must match TrenchBroom's own setting.
        /// </summary>
        public const string MapEditorGamePath = ".";

        /// Loads the shared config resource, or null when it is missing.
        public static FuncGodotConfig Load()
        {
            return ResourceLoader.Exists(ConfigPath) ? ResourceLoader.Load(ConfigPath) as FuncGodotConfig : null;
        }

        /// <summary>
        /// Turns a configured folder into an absolute OS path. Relative values resolve against the project
        /// root rather than the editor process's working directory, so a committed path means the same thing
        /// no matter how the editor was launched. Absolute paths and empty values are returned unchanged.
        /// </summary>
        public static string ResolvePath(string path)
        {
            if (string.IsNullOrEmpty(path) || path.IsAbsolutePath())
            {
                return path ?? string.Empty;
            }

            return ProjectSettings.GlobalizePath("res://").PathJoin(path).SimplifyPath();
        }

        /// <summary>
        /// The TrenchBroom game config folder for the platform the editor is running on, resolved to an
        /// absolute path. Empty, with an error pushed, on a platform that has no folder configured.
        /// </summary>
        public string GetGameConfigFolder()
        {
            string platform = OS.GetName();

            string folder = platform switch
            {
                "Windows" => GameConfigFolderWindows,
                "Linux" => GameConfigFolderLinux,
                _ => null,
            };

            if (folder == null)
            {
                GD.PushError($"No TrenchBroom game config folder configured for platform '{platform}'");
                return string.Empty;
            }

            return ResolvePath(folder);
        }

        #region GAME CONFIG EXPORT

        /// <summary>
        /// Writes the icon, GameConfig.cfg, and FGD into the TrenchBroom game config folder.
        /// </summary>
        public void ExportFile()
        {
            string configFolder = GetGameConfigFolder();

            if (string.IsNullOrEmpty(configFolder))
            {
                GD.PushError("Skipping export: No TrenchBroom Game folder");
                return;
            }

            if (!DirAccess.DirExistsAbsolute(configFolder)
                && DirAccess.MakeDirRecursiveAbsolute(configFolder) != Error.Ok)
            {
                GD.PushError("Skipping export: Failed to create directory");
                return;
            }

            if (Icon != null)
            {
                string iconPath = configFolder.PathJoin("icon.png");
                GD.Print("Exporting icon to ", iconPath);

                Image image = Icon.GetImage();
                image.Resize(32, 32, Image.Interpolation.Lanczos);
                image.SavePng(iconPath);
            }

            string configPath = configFolder.PathJoin("GameConfig.cfg");
            GD.Print("Exporting TrenchBroom Game Config to ", configPath);

            using (FileAccess file = FileAccess.Open(configPath, FileAccess.ModeFlags.Write))
            {
                if (file == null)
                {
                    GD.PushError("Skipping export: Failed to open ", configPath);
                    return;
                }

                file.StoreString(BuildConfigText());
            }

            // The FGD ships next to the config, so TrenchBroom finds it by relative name.
            if (DoExportFile(configFolder) != Error.Ok)
            {
                GD.PushError("Could not export FGD.");
                return;
            }

            GD.Print("TrenchBroom Game Config export complete\n");
        }

        private static string GetMatchKey(TrenchBroomTag.TagMatchTypes matchType)
        {
            return matchType switch
            {
                TrenchBroomTag.TagMatchTypes.Texture => "material",
                TrenchBroomTag.TagMatchTypes.Classname => "classname",
                _ => "ERROR",
            };
        }

        /// Renders a tag array into the .cfg's JSON.
        private string BuildTagsText(Godot.Collections.Array<TrenchBroomTag> tags)
        {
            StringBuilder result = new();

            for (int i = 0; i < tags.Count; i++)
            {
                TrenchBroomTag tag = tags[i];

                if (tag == null)
                {
                    continue;
                }

                StringBuilder attributes = new();

                for (int j = 0; j < tag.TagAttributes.Count; j++)
                {
                    if (j > 0)
                    {
                        attributes.Append(", ");
                    }

                    attributes.Append('"').Append(tag.TagAttributes[j]).Append('"');
                }

                result.Append("{\n");
                result.Append($"\t\t\t\t\"name\": \"{tag.TagName}\",\n");
                result.Append($"\t\t\t\t\"attribs\": [ {attributes} ],\n");
                result.Append($"\t\t\t\t\"match\": \"{GetMatchKey(tag.TagMatchType)}\",\n");
                result.Append($"\t\t\t\t\"pattern\": \"{tag.TagPattern}\"");

                if (!string.IsNullOrEmpty(tag.TextureName))
                {
                    result.Append(",\n");
                    result.Append($"\t\t\t\t\"material\": \"{tag.TextureName}\"");
                }

                result.Append("\n\t\t\t}");

                if (i < tags.Count - 1)
                {
                    result.Append(',');
                }
            }

            return result.ToString();
        }

        private string BuildConfigText()
        {
            string initialMap = string.IsNullOrEmpty(InitialMap)
                ? string.Empty
                : $", \"initialmap\": \"{InitialMap}\"";

            // Only the Valve 220 format is supported.
            string mapFormats = $"{{ \"format\": \"Valve\"{initialMap} }}";

            StringBuilder exclusionBuilder = new();

            for (int i = 0; i < TextureExclusionPatterns.Count; i++)
            {
                if (i > 0)
                {
                    exclusionBuilder.Append(", ");
                }

                exclusionBuilder.Append('"').Append(TextureExclusionPatterns[i]).Append('"');
            }

            string exclusions = exclusionBuilder.ToString();
            string fgdName = $"\"{GameName}.fgd\"";
            string brushTags = BuildTagsText(BrushTags);
            string brushFaceTags = BuildTagsText(BrushFaceTags);

            string uvScale = string.Format(
                CultureInfo.InvariantCulture,
                "\"scale\": [{0}, {1}]",
                DefaultUvScale.X,
                DefaultUvScale.Y);

            return $$"""
            {
            	"version": 9,
            	"name": "{{GameName}}",
            	"icon": "icon.png",
            	"fileformats": [
            		{{mapFormats}}
            	],
            	"filesystem": {
            		"searchpath": ".",
            		"packageformat": { "extension": ".zip", "format": "zip" }
            	},
            	"materials": {
            		"root": "{{TexturesRootFolder}}",
            		"extensions": [".png"],
            		"excludes": [ {{exclusions}} ]
            	},
            	"entities": {
            		"definitions": [ {{fgdName}} ],
            		"defaultcolor": "0.6 0.6 0.6 1.0",
            		"scale": {{EntityScale}},
            		"setDefaultProperties": {{(SetDefaultProperties ? "true" : "false")}}
            	},
            	"tags": {
            		"brush": [
            			{{brushTags}}
            		],
            		"brushface": [
            			{{brushFaceTags}}
            		]
            	},
            	"faceattribs": {
            		"defaults": {
            			{{uvScale}}
            		},
            		"contentflags": [],
            		"surfaceflags": []
            	}
            }
            """;
        }

        #endregion

        #region FGD EXPORT

        /// <summary>
        /// Writes the FGD into <paramref name="fgdOutputFolder"/>. Exporting the FGD on its own defaults to
        /// the TrenchBroom game config folder, which is where <see cref="ExportFile"/> also puts it, so both
        /// routes land in the same place.
        /// </summary>
        public Error DoExportFile(string fgdOutputFolder = "")
        {
            if (string.IsNullOrEmpty(fgdOutputFolder))
            {
                fgdOutputFolder = GetGameConfigFolder();
            }

            // No-op when ExportFile already handed us an absolute folder.
            fgdOutputFolder = ResolvePath(fgdOutputFolder);

            if (string.IsNullOrEmpty(fgdOutputFolder))
            {
                GD.PushError("Skipping export: No FGD output folder");
                return Error.DoesNotExist;
            }

            if (string.IsNullOrEmpty(GameName))
            {
                GD.PushError("Skipping export: Empty game name");
                return Error.InvalidParameter;
            }

            if (!DirAccess.DirExistsAbsolute(fgdOutputFolder)
                && DirAccess.MakeDirRecursiveAbsolute(fgdOutputFolder) != Error.Ok)
            {
                GD.PushError("Skipping export: Failed to create directory");
                return Error.CantCreate;
            }

            string content = BuildClassText();

            if (string.IsNullOrEmpty(content))
            {
                return Error.InvalidData;
            }

            string fgdPath = fgdOutputFolder.PathJoin(GameName + ".fgd");

            using FileAccess file = FileAccess.Open(fgdPath, FileAccess.ModeFlags.Write);

            if (file == null)
            {
                GD.PushError("Failed to open file for writing: ", fgdPath);
                return Error.FileCantOpen;
            }

            GD.Print("Exporting FGD to ", fgdPath);
            file.StoreString(content);

            return Error.Ok;
        }

        /// Builds the full FGD text.
        public string BuildClassText()
        {
            StringBuilder result = new();

            List<FuncGodotFGDEntityClass> entities = GetFgdClasses();

            // Keyed by classname, holding the file that claimed it. Classnames have to be unique across every
            // mod, since TrenchBroom and the map build both match entities on nothing else.
            Dictionary<string, string> classnames = [];
            bool failure = false;

            for (int index = 0; index < entities.Count; index++)
            {
                FuncGodotFGDEntityClass entity = entities[index];

                if (entity.FuncGodotInternal)
                {
                    continue;
                }

                if (entity is FuncGodotFGDModelPointClass modelPointClass)
                {
                    modelPointClass.ModelGenerationEnabled = GenerateModelPointClassModels;
                }

                if (string.IsNullOrEmpty(entity.Classname))
                {
                    GD.PushError($"FGD class cannot be exported with empty classname ({entity.ResourcePath})");
                    failure = true;
                    continue;
                }

                if (classnames.TryGetValue(entity.Classname, out string existing))
                {
                    GD.PushError($"Duplicate class name '{entity.Classname}' in {existing} and {entity.ResourcePath}");
                    failure = true;
                    continue;
                }

                classnames[entity.Classname] = entity.ResourcePath;

                result.Append(entity.BuildDefText());

                if (index < entities.Count - 1)
                {
                    result.Append('\n');
                }
            }

            return failure ? string.Empty : result.ToString();
        }

        /// <summary>
        /// The mods to gather entity definitions from. The editor and the running game each have their own mod
        /// manager, and only one of the two exists at a time: Bootstrap, and with it <see cref="ModLoader"/>,
        /// is absent while a scene is merely open for editing.
        /// </summary>
        private static List<ModManifest> GetMods()
        {
#if TOOLS
            if (Engine.Singleton.IsEditorHint())
            {
                // Every mod in the project, not just the ones the mod selector has enabled: TrenchBroom is
                // pointed at the whole project, and PluginTrenchbroom exposes every mod's materials the same way.
                // Scanned rather than read off PluginMods, whose list lives in plain managed state that a .NET
                // domain reload throws away without ever re-running the plugin's _EnterTree.
                return ModManifest.ScanProject();
            }
#endif

            return ModLoader.Singleton?.EnabledMods ?? [];
        }

        /// <summary>
        /// Mod ids with dependencies first. Order matters downstream: an entity may inherit a
        /// <see cref="FuncGodotFGDBaseClass"/> from a mod it depends on, and the FGD format requires a base
        /// class to be declared before anything inheriting it.
        /// </summary>
        private static List<string> GetModIds()
        {
            List<ModManifest> mods = GetMods();
            HashSet<string> present = [.. mods.Select(mod => mod.ModId)];

            List<TopoSortItem<string>> items = [];

            foreach (ModManifest mod in mods)
            {
                HashSet<string> dependencies = [];

                foreach (ModManifestDependency dependency in mod.Dependencies)
                {
                    // A dependency on a mod that is not in the project cannot order anything.
                    if (present.Contains(dependency.ModId))
                    {
                        dependencies.Add(dependency.ModId);
                    }
                }

                items.Add(new TopoSortItem<string>(mod.ModId, [.. dependencies]));
            }

            try
            {
                return [.. items.TopoSort(item => item.Target, item => item.Dependencies).Select(item => item.Target)];
            }
            catch (ArgumentException)
            {
                GD.PushError("Cyclic mod dependency: falling back to unordered FGD scanning");
                return [.. present];
            }
        }

        /// <summary>
        /// Every entity definition the project's mods ship, loaded from each mod's <see cref="ModFgdFolder"/>.
        /// Base classes come first so they are declared before the entities inheriting them, and mods are
        /// visited in dependency order so that holds across mod boundaries too.
        /// </summary>
        public List<FuncGodotFGDEntityClass> GetFgdClasses()
        {
            List<FuncGodotFGDEntityClass> baseClasses = [];
            List<FuncGodotFGDEntityClass> entityClasses = [];

            foreach (string modId in GetModIds())
            {
                string folder = $"res://{modId}".PathJoin(ModFgdFolder);

                if (!DirAccess.DirExistsAbsolute(folder))
                {
                    continue;
                }

                string[] files = DirAccess.GetFilesAt(folder);

                // Sorted so the exported FGD comes out identical between runs and machines.
                Array.Sort(files, StringComparer.Ordinal);

                foreach (string file in files)
                {
                    // An exported project hands out remapped names for resources converted at export time.
                    string name = file.EndsWith(".remap") ? file.GetBaseName() : file;

                    if (name.GetExtension() is not ("tres" or "res"))
                    {
                        continue;
                    }

                    string path = folder.PathJoin(name);

                    if (ResourceLoader.Load(path) is not FuncGodotFGDEntityClass definition)
                    {
                        GD.PushError($"Skipping {path}: not a FuncGodotFGDEntityClass");
                        continue;
                    }

                    if (definition is FuncGodotFGDBaseClass)
                    {
                        baseClasses.Add(definition);
                    }
                    else
                    {
                        entityClasses.Add(definition);
                    }
                }
            }

            baseClasses.AddRange(entityClasses);
            return baseClasses;
        }

        /// <summary>
        /// The buildable entity definitions, keyed by classname, with every inherited meta property flattened
        /// onto a copy of each definition. Class properties and descriptions are compiled on demand from each
        /// definition's NodeType (see <see cref="FuncGodotFGDEntityClass.RetrieveAllClassProperties"/>). This is
        /// what the parser matches map entities against. Mods are visited in dependency order, so a mod reusing
        /// a classname from a mod it depends on replaces that definition for the build - the FGD export reports
        /// the collision as an error, since TrenchBroom can only be given one definition per classname.
        /// </summary>
        public Dictionary<string, FuncGodotFGDEntityClass> GetEntityDefinitions()
        {
            Dictionary<string, FuncGodotFGDEntityClass> result = [];

            foreach (FuncGodotFGDEntityClass entity in GetFgdClasses())
            {
                if (string.IsNullOrWhiteSpace(entity.Classname))
                {
                    GD.PushError($"Skipping {entity.ResourcePath}: Empty classname");
                    continue;
                }

                if (entity is not FuncGodotFGDPointClass && entity is not FuncGodotFGDSolidClass)
                {
                    continue;
                }

                FuncGodotFGDEntityClass definition = (FuncGodotFGDEntityClass)entity.Duplicate();

                Godot.Collections.Dictionary<string, Variant> metaProperties = [];

                // Base classes first, so the entity's own values win on collision. Class properties and their
                // descriptions are compiled on demand from each definition's NodeType, so only the authored meta
                // properties need flattening here.
                foreach (FuncGodotFGDEntityClass baseClass in GenerateBaseClassList(definition))
                {
                    Merge(metaProperties, baseClass.MetaProperties);
                }

                Merge(metaProperties, definition.MetaProperties);

                definition.MetaProperties = metaProperties;

                result[entity.Classname] = definition;
            }

            return result;
        }

        private static void Merge(
            Godot.Collections.Dictionary<string, Variant> target,
            Godot.Collections.Dictionary<string, Variant> source)
        {
            foreach (string key in source.Keys)
            {
                target[key] = source[key];
            }
        }

        /// Walks the base class hierarchy depth first, reporting any cycle or duplicate.
        private static List<FuncGodotFGDEntityClass> GenerateBaseClassList(
            FuncGodotFGDEntityClass definition,
            List<string> visited = null)
        {
            visited ??= [];
            visited.Add(definition.Classname);

            List<FuncGodotFGDEntityClass> baseClasses = [];

            foreach (FuncGodotFGDBaseClass baseClass in definition.BaseClasses)
            {
                if (baseClass == null)
                {
                    continue;
                }

                if (visited.Contains(baseClass.Classname))
                {
                    GD.PushError($"Entity '{definition.Classname}' contains cycle/duplicate to Entity '{baseClass.Classname}'");
                    continue;
                }

                baseClasses.Add(baseClass);
                baseClasses.AddRange(GenerateBaseClassList(baseClass, visited));
            }

            return baseClasses;
        }

        #endregion
    }
}
