// Copyright (c) 2023 func-godot
// C# port of func_godot (https://github.com/func-godot/func_godot_plugin),
// used under the MIT License. See addons/unary.core.editor/sources/func_godot/LICENSE.md.

using Godot;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace FuncGodot
{
    /// <summary>
    /// The single func_godot configuration resource. Since Valve 220 is the only supported map format and
    /// TrenchBroom the only supported editor, project defaults, the TrenchBroom game config, and this
    /// machine's folder paths all live here. Load the shared instance with <see cref="Load"/> rather than
    /// constructing new ones.
    /// <para>
    /// The folder paths under "Local Paths" are per-machine: they are stored as JSON in <c>user://</c> and
    /// are deliberately never serialized into the .tres, so one developer's paths stay out of version
    /// control. Use the export/reload buttons to persist them.
    /// </para>
    /// </summary>
    [Tool]
    [GlobalClass]
    public partial class FuncGodotConfig : Resource
    {
        public const string ConfigPath = "res://addons/unary.core.editor/func_godot_config.tres";

        /// Per-machine folder paths, stored outside the resource.
        private enum LocalPath
        {
            /// Where an exported FGD is written, unless the game config export overrides it.
            FgdOutputFolder,

            /// Where the TrenchBroom game configuration is written.
            TrenchBroomGameConfigFolder,

            /// The mapping folder holding all map editor assets, usually the project folder or a subfolder.
            MapEditorGamePath,
        }

        private const string LocalPathsFile = "user://func_godot_config.json";

        [ExportToolButton("Export GameConfig")]
        public Callable ExportFileButton => Callable.From(ExportFile);

        /// Name of the game in TrenchBroom's game list.
        [Export]
        public string GameName = "FuncGodot";

        /// Icon for TrenchBroom's game list.
        [Export]
        public Texture2D Icon;

        /// The map TrenchBroom creates when starting a new Valve format map. Optional.
        [Export]
        public string InitialMap = "initial_valve.map";

        [ExportGroup("Project")]

        /// Map settings assigned to a newly created <see cref="FuncGodotMap"/> node.
        [Export]
        public FuncGodotMapSettings DefaultMapSettings;

        /// <summary>
        /// Inverse scale factor a <see cref="FuncGodotFGDModelPointClass"/> falls back to when its own scale
        /// expression is empty.
        /// </summary>
        [Export]
        public float DefaultInverseScaleFactor = 32.0f;

        /// Folder that <see cref="FuncGodotFGDModelPointClass"/> display models are exported into.
        [Export]
        public string ModelPointClassSavePath = string.Empty;

        [ExportGroup("Textures")]

        /// Top level textures folder, relative to the game path. Called materials in recent TrenchBroom.
        [Export]
        public string TexturesRootFolder = "textures";

        /// Textures matching these patterns are hidden from TrenchBroom.
        [Export]
        public Godot.Collections.Array<string> TextureExclusionPatterns =
        [
            "*_albedo", "*_ao", "*_emission", "*_height",
            "*_metallic", "*_normal", "*_orm", "*_roughness", "*_sss",
        ];

        [ExportGroup("Entities")]

        /// <summary>
        /// The FGD to ship with this game. When several FGD files are in play this must be the master file
        /// that lists the others in <see cref="FuncGodotFGDFile.BaseFgdFiles"/>.
        /// </summary>
        [Export]
        public FuncGodotFGDFile FgdFile;

        /// <summary>
        /// Scale expression modifying the default display scale of entities in TrenchBroom. See the
        /// TrenchBroom manual's entity configuration documentation.
        /// </summary>
        [Export]
        public string EntityScale = "32";

        /// Whether TrenchBroom instantiates default entity properties when creating a new entity.
        [Export]
        public bool SetDefaultProperties = false;

        /// Whether FuncGodotFGDModelPointClass resources export display models with this game config.
        [Export]
        public bool GenerateModelPointClassModels = true;

        [ExportGroup("Tags")]

        [Export]
        public Godot.Collections.Array<TrenchBroomTag> BrushTags = [];

        [Export]
        public Godot.Collections.Array<TrenchBroomTag> BrushFaceTags = [];

        [ExportGroup("Face Attributes")]

        /// Texture scale on new brushes, and the value a UV scale reset returns to.
        [Export]
        public Vector2 DefaultUvScale = Vector2.One;

        [ExportGroup("Local Paths")]

        [Export(PropertyHint.GlobalDir)]
        public string FgdOutputFolder
        {
            get => GetLocalPath(LocalPath.FgdOutputFolder);
            set => SetLocalPath(LocalPath.FgdOutputFolder, value);
        }

        [Export(PropertyHint.GlobalDir)]
        public string TrenchBroomGameConfigFolder
        {
            get => GetLocalPath(LocalPath.TrenchBroomGameConfigFolder);
            set => SetLocalPath(LocalPath.TrenchBroomGameConfigFolder, value);
        }

        [Export(PropertyHint.GlobalDir)]
        public string MapEditorGamePath
        {
            get => GetLocalPath(LocalPath.MapEditorGamePath);
            set => SetLocalPath(LocalPath.MapEditorGamePath, value);
        }

        [ExportToolButton("Export local paths", Icon = "Save")]
        public Callable ExportLocalPathsButton => Callable.From(ExportLocalPaths);

        [ExportToolButton("Reload local paths", Icon = "Reload")]
        public Callable ReloadLocalPathsButton => Callable.From(ReloadLocalPaths);

        private readonly Dictionary<LocalPath, string> _localPaths = [];
        private bool _localPathsLoaded = false;

        /// Loads the shared config resource, or null when it is missing.
        public static FuncGodotConfig Load()
        {
            return ResourceLoader.Exists(ConfigPath) ? ResourceLoader.Load(ConfigPath) as FuncGodotConfig : null;
        }

        /// <summary>
        /// Keeps the per-machine paths out of the saved resource: they stay editable in the inspector but
        /// carry no Storage usage, so Godot never writes them into the .tres.
        /// </summary>
        public override void _ValidateProperty(Godot.Collections.Dictionary property)
        {
            string name = property["name"].AsString();

            if (name is nameof(FgdOutputFolder)
                or nameof(TrenchBroomGameConfigFolder)
                or nameof(MapEditorGamePath))
            {
                property["usage"] = (int)PropertyUsageFlags.Editor;
            }
        }

        private string GetLocalPath(LocalPath path)
        {
            if (!_localPathsLoaded)
            {
                ReloadLocalPaths();
            }

            return _localPaths.GetValueOrDefault(path, string.Empty);
        }

        private void SetLocalPath(LocalPath path, string value)
        {
            _localPaths[path] = value;
        }

        /// Reloads the per-machine paths from this machine's configuration file.
        public void ReloadLocalPaths()
        {
            _localPathsLoaded = true;

            if (!FileAccess.FileExists(LocalPathsFile))
            {
                return;
            }

            string contents = FileAccess.GetFileAsString(LocalPathsFile);

            if (string.IsNullOrEmpty(contents))
            {
                return;
            }

            Variant parsed = Json.ParseString(contents);

            if (parsed.VariantType != Variant.Type.Dictionary)
            {
                GD.PushError($"Malformed local config at {LocalPathsFile}");
                return;
            }

            _localPaths.Clear();

            Godot.Collections.Dictionary values = parsed.AsGodotDictionary();

            foreach (LocalPath path in System.Enum.GetValues<LocalPath>())
            {
                if (values.TryGetValue(path.ToString(), out Variant value))
                {
                    _localPaths[path] = value.AsString();
                }
            }

            NotifyPropertyListChanged();
        }

        /// Writes the current per-machine paths to this machine's configuration file.
        public void ExportLocalPaths()
        {
            if (_localPaths.Count == 0)
            {
                return;
            }

            Godot.Collections.Dictionary values = [];

            foreach (KeyValuePair<LocalPath, string> path in _localPaths)
            {
                values[path.Key.ToString()] = path.Value;
            }

            using FileAccess file = FileAccess.Open(LocalPathsFile, FileAccess.ModeFlags.Write);

            if (file == null)
            {
                GD.PushError($"Failed to open local config for writing at {LocalPathsFile}");
                return;
            }

            file.StoreLine(Json.Stringify(values));
            _localPathsLoaded = false;

            GD.Print("Saved settings to ", file.GetPathAbsolute());
        }

        /// <summary>
        /// Writes the icon, GameConfig.cfg, and FGD into the TrenchBroom game config folder.
        /// </summary>
        public void ExportFile()
        {
            string configFolder = TrenchBroomGameConfigFolder;

            if (string.IsNullOrEmpty(configFolder))
            {
                GD.PushError("Skipping export: No TrenchBroom Game folder");
                return;
            }

            if (FgdFile == null)
            {
                GD.PushError("Skipping export: No FGD file");
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
            FuncGodotFGDFile exportFgd = (FuncGodotFGDFile)FgdFile.Duplicate();
            exportFgd.GenerateModelPointClassModels = GenerateModelPointClassModels;

            if (exportFgd.DoExportFile(configFolder) != Error.Ok)
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
            string fgdName = $"\"{FgdFile.FgdName}.fgd\"";
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
    }
}
