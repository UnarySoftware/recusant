// Copyright (c) 2023 func-godot
// C# port of func_godot (https://github.com/func-godot/func_godot_plugin),
// used under the MIT License. See addons/func_godot_csharp/LICENSE.

using Godot;
using System.Globalization;
using System.Text;

namespace FuncGodot
{
    /// <summary>
    /// TrenchBroom game configuration. Exports the icon, the GameConfig.cfg, and the FGD into TrenchBroom's
    /// games folder.
    /// </summary>
    [Tool]
    [GlobalClass]
    public partial class TrenchBroomGameConfig : Resource
    {
        public enum GameConfigVersions
        {
            Latest,
            Version4,
            Version8,
            Version9,
        }

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

        [ExportGroup("Compatibility")]

        /// Config format matching the TrenchBroom version in use.
        [Export]
        public GameConfigVersions GameConfigVersion = GameConfigVersions.Latest;

        /// <summary>
        /// Writes the icon, GameConfig.cfg, and FGD into the TrenchBroom game config folder from the local
        /// config.
        /// </summary>
        public void ExportFile()
        {
            string configFolder = FuncGodotLocalConfig.GetSetting(FuncGodotLocalConfig.Property.TrenchBroomGameConfigFolder);

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

            // TrenchBroom renamed textures to materials in version 9.
            if (GameConfigVersion is GameConfigVersions.Version4 or GameConfigVersions.Version8)
            {
                return result.ToString().Replace("material", "texture");
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

            StringBuilder exclusions = new();

            for (int i = 0; i < TextureExclusionPatterns.Count; i++)
            {
                if (i > 0)
                {
                    exclusions.Append(", ");
                }

                exclusions.Append('"').Append(TextureExclusionPatterns[i]).Append('"');
            }

            string fgdName = $"\"{FgdFile.FgdName}.fgd\"";
            string brushTags = BuildTagsText(BrushTags);
            string brushFaceTags = BuildTagsText(BrushFaceTags);

            string uvScale = string.Format(
                CultureInfo.InvariantCulture,
                "\"scale\": [{0}, {1}]",
                DefaultUvScale.X,
                DefaultUvScale.Y);

            return GameConfigVersion switch
            {
                GameConfigVersions.Version4 => BuildVersion4Text(
                    mapFormats, exclusions.ToString(), fgdName, brushTags, brushFaceTags, uvScale),
                _ => BuildVersion9Text(
                    mapFormats, exclusions.ToString(), fgdName, brushTags, brushFaceTags, uvScale),
            };
        }

        private string BuildVersion9Text(
            string mapFormats,
            string exclusions,
            string fgdName,
            string brushTags,
            string brushFaceTags,
            string uvScale)
        {
            string text = $$"""
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

            if (GameConfigVersion == GameConfigVersions.Version8)
            {
                text = text.Replace("\"version\": 9,", "\"version\": 8,").Replace("material", "texture");
            }

            return text;
        }

        private string BuildVersion4Text(
            string mapFormats,
            string exclusions,
            string fgdName,
            string brushTags,
            string brushFaceTags,
            string uvScale)
        {
            return $$"""
            {
            	"version": 4,
            	"name": "{{GameName}}",
            	"icon": "icon.png",
            	"fileformats": [
            		{{mapFormats}}
            	],
            	"filesystem": {
            		"searchpath": ".",
            		"packageformat": { "extension": ".zip", "format": "zip" }
            	},
            	"textures": {
            		"package": { "type": "directory", "root": "{{TexturesRootFolder}}" },
            		"format": { "extensions": ["jpg", "jpeg", "tga", "png"], "format": "image" },
            		"excludes": [ {{exclusions}} ],
            		"attribute": ["_tb_textures", "wad"]
            	},
            	"entities": {
            		"definitions": [ {{fgdName}} ],
            		"defaultcolor": "0.6 0.6 0.6 1.0",
            		"modelformats": [ "bsp, mdl, md2" ],
            		"scale": {{EntityScale}}
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
