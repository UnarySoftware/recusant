// Copyright (c) 2023 func-godot
// C# port of func_godot (https://github.com/func-godot/func_godot_plugin),
// used under the MIT License. See addons/unary.core.editor/sources/func_godot/LICENSE.md.

#if TOOLS

using Godot;

namespace FuncGodot
{
    /// <summary>
    /// Imports .map files into <see cref="QuakeMapFile"/> resources, so their text survives into an exported
    /// project.
    /// </summary>
    [Tool]
    public partial class QuakeMapImportPlugin : EditorImportPlugin
    {
        public override string _GetImporterName()
        {
            return "func_godot_csharp.map";
        }

        public override string _GetVisibleName()
        {
            return "Quake Map";
        }

        public override string _GetResourceType()
        {
            return "Resource";
        }

        public override string[] _GetRecognizedExtensions()
        {
            return ["map"];
        }

        public override string _GetSaveExtension()
        {
            return "tres";
        }

        public override float _GetPriority()
        {
            return 1.0f;
        }

        public override int _GetImportOrder()
        {
            return 0;
        }

        public override int _GetPresetCount()
        {
            return 0;
        }

        public override string _GetPresetName(int presetIndex)
        {
            return string.Empty;
        }

        public override Godot.Collections.Array<Godot.Collections.Dictionary> _GetImportOptions(string path, int presetIndex)
        {
            return [];
        }

        public override bool _GetOptionVisibility(string path, StringName optionName, Godot.Collections.Dictionary options)
        {
            return true;
        }

        public override Error _Import(
            string sourceFile,
            string savePath,
            Godot.Collections.Dictionary options,
            Godot.Collections.Array<string> platformVariants,
            Godot.Collections.Array<string> genFiles)
        {
            string savePathString = $"{savePath}.{_GetSaveExtension()}";

            QuakeMapFile mapResource = null;

            // Reimports bump the revision, which is what tells Godot's cache the resource actually changed.
            if (ResourceLoader.Exists(savePathString)
                && ResourceLoader.Load(savePathString, cacheMode: ResourceLoader.CacheMode.Ignore) is QuakeMapFile existing)
            {
                mapResource = existing;
                mapResource.Revision += 1;
            }

            mapResource ??= new QuakeMapFile();

            using FileAccess file = FileAccess.Open(sourceFile, FileAccess.ModeFlags.Read);

            if (file == null)
            {
                GD.PushError($"Failed to open map file for import: {sourceFile}");
                return Error.FileCantOpen;
            }

            mapResource.MapData = file.GetAsText();

            return ResourceSaver.Save(mapResource, savePathString);
        }
    }
}

#endif
