// Copyright (c) 2023 func-godot
// C# port of func_godot (https://github.com/func-godot/func_godot_plugin),
// used under the MIT License. See addons/unary.core.editor/sources/func_godot/LICENSE.md.

using Godot;

namespace FuncGodot
{
    /// <summary>
    /// The imported form of a Valve 220 .map file. The raw text is held here so a <see cref="FuncGodotMap"/>
    /// can still build in an exported project, where the .map itself is no longer on disk.
    /// </summary>
    [Tool]
    [GlobalClass]
    public partial class QuakeMapFile : Resource
    {
        /// Times this map file has been reimported.
        [Export]
        public int Revision = 0;

        [Export(PropertyHint.MultilineText)]
        public string MapData = string.Empty;
    }
}
