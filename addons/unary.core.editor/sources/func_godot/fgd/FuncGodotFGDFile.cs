// Copyright (c) 2023 func-godot
// C# port of func_godot (https://github.com/func-godot/func_godot_plugin),
// used under the MIT License. See addons/unary.core.editor/sources/func_godot/LICENSE.md.

using Godot;
using System.Collections.Generic;
using System.Text;

namespace FuncGodot
{
    /// <summary>
    /// A set of entity definitions, exportable as a TrenchBroom .fgd and used by
    /// <see cref="FuncGodotMapSettings"/> to turn map classnames into Godot nodes.
    /// </summary>
    [Tool]
    [GlobalClass]
    public partial class FuncGodotFGDFile : Resource
    {
        public FuncGodotFGDFile()
        {
            // Initialized here rather than inline: inline initializers make the source generator construct these
            // typed arrays while this class is still being registered - BaseFgdFiles references its own type and
            // EntityDefinitions an abstract one, neither of which has a registered script yet - which fails with
            // "Script class can only be set together with base class name."
            BaseFgdFiles = [];
            EntityDefinitions = [];
        }

        [ExportToolButton("Export FGD")]
        public Callable ExportFileButton => Callable.From(() => DoExportFile());

        [ExportGroup("FGD")]

        /// FGD output filename, without the extension.
        [Export]
        public string FgdName = "FuncGodot";

        /// <summary>
        /// Other FGD files to include in the output. Their entities are prepended to this file's own.
        /// </summary>
        [Export]
        public Godot.Collections.Array<FuncGodotFGDFile> BaseFgdFiles;

        /// The entities written to the FGD and built by a <see cref="FuncGodotMap"/>.
        [Export]
        public Godot.Collections.Array<FuncGodotFGDEntityClass> EntityDefinitions;

        /// Whether <see cref="FuncGodotFGDModelPointClass"/> entities export display models on FGD export.
        [Export]
        public bool GenerateModelPointClassModels = true;

        /// <summary>
        /// Writes the FGD into <paramref name="fgdOutputFolder"/>. Exporting the FGD on its own defaults to
        /// the TrenchBroom game config folder, which is where <see cref="FuncGodotConfig.ExportFile"/> also
        /// puts it, so both routes land in the same place.
        /// </summary>
        public Error DoExportFile(string fgdOutputFolder = "")
        {
            if (string.IsNullOrEmpty(fgdOutputFolder))
            {
                fgdOutputFolder = FuncGodotConfig.Load()?.GetGameConfigFolder() ?? string.Empty;
            }

            // No-op when ExportFile already handed us an absolute folder.
            fgdOutputFolder = FuncGodotConfig.ResolvePath(fgdOutputFolder);

            if (string.IsNullOrEmpty(fgdOutputFolder))
            {
                GD.PushError("Skipping export: No FGD output folder");
                return Error.DoesNotExist;
            }

            if (string.IsNullOrEmpty(FgdName))
            {
                GD.PushError("Skipping export: Empty FGD name");
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

            string fgdPath = fgdOutputFolder.PathJoin(FgdName + ".fgd");

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

        /// Builds the full FGD text, including every base FGD file's entities.
        public string BuildClassText()
        {
            StringBuilder result = new();

            foreach (FuncGodotFGDFile baseFgd in BaseFgdFiles)
            {
                if (baseFgd == null)
                {
                    GD.PushError("FGD base files array contains a null element - skipping");
                    continue;
                }

                result.Append(baseFgd.BuildClassText());
            }

            List<FuncGodotFGDEntityClass> entities = GetFgdClasses();

            Dictionary<string, int> classnames = [];
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
                    GD.PushError($"FGD class cannot be exported with empty classname (in position {index})");
                    failure = true;
                    continue;
                }

                if (classnames.TryGetValue(entity.Classname, out int existing))
                {
                    GD.PushError($"Duplicate class name found: {entity.Classname} (in positions {existing} and {index})");
                    failure = true;
                    continue;
                }

                classnames[entity.Classname] = index;

                result.Append(entity.BuildDefText());

                if (index < entities.Count - 1)
                {
                    result.Append('\n');
                }
            }

            return failure ? string.Empty : result.ToString();
        }

        /// The non-null entity definitions in this file.
        public List<FuncGodotFGDEntityClass> GetFgdClasses()
        {
            List<FuncGodotFGDEntityClass> result = [];

            for (int index = 0; index < EntityDefinitions.Count; index++)
            {
                FuncGodotFGDEntityClass definition = EntityDefinitions[index];

                if (definition == null)
                {
                    continue;
                }

                result.Add(definition);
            }

            return result;
        }

        /// <summary>
        /// The buildable entity definitions, keyed by classname, with every inherited meta property flattened
        /// onto a copy of each definition. Class properties and descriptions are compiled on demand from each
        /// definition's NodeType (see <see cref="FuncGodotFGDEntityClass.RetrieveAllClassProperties"/>). This is
        /// what the parser matches map entities against.
        /// </summary>
        public Dictionary<string, FuncGodotFGDEntityClass> GetEntityDefinitions()
        {
            Dictionary<string, FuncGodotFGDEntityClass> result = [];

            foreach (FuncGodotFGDFile baseFgd in BaseFgdFiles)
            {
                if (baseFgd == null)
                {
                    continue;
                }

                foreach (KeyValuePair<string, FuncGodotFGDEntityClass> entry in baseFgd.GetEntityDefinitions())
                {
                    result[entry.Key] = entry.Value;
                }
            }

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
    }
}
