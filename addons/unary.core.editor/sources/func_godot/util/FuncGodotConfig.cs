// Copyright (c) 2023 func-godot
// C# port of func_godot (https://github.com/func-godot/func_godot_plugin),
// used under the MIT License. See addons/unary.core.editor/sources/func_godot/LICENSE.md.

using Godot;
using System.Globalization;
using System.Text;

namespace FuncGodot
{
    /// <summary>
    /// The single func_godot configuration resource. Since Valve 220 is the only supported map format and
    /// TrenchBroom the only supported editor, project defaults, the TrenchBroom game config, and the export
    /// paths all live here. Load the shared instance with <see cref="Load"/> rather than constructing new
    /// ones.
    /// </summary>
    [Tool]
    [GlobalClass]
    public partial class FuncGodotConfig : Resource
    {
        public const string ConfigPath = "res://func_godot/config.tres";

        [ExportToolButton("Export GameConfig")]
        public Callable ExportFileButton => Callable.From(ExportFile);

        /// Name of the game in TrenchBroom's game list.
        [Export]
        public string GameName = "Recusant";

        /// Icon for TrenchBroom's game list.
        [Export]
        public Texture2D Icon;

        public const string InitialMap = "initial_valve.map";

        [ExportGroup("Project")]

        /// Map settings assigned to a newly created <see cref="FuncGodotMap"/> node.
        [Export]
        public FuncGodotMapSettings DefaultMapSettings;

        /// <summary>
        /// Inverse scale factor a <see cref="FuncGodotFGDModelPointClass"/> falls back to when its own scale
        /// expression is empty.
        /// </summary>
        public const float DefaultInverseScaleFactor = 39.37f;

        /// Folder that <see cref="FuncGodotFGDModelPointClass"/> display models are exported into.
        [Export]
        public string ModelPointClassSavePath = string.Empty;

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
