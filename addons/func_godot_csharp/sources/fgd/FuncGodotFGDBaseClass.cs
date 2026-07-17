// Copyright (c) 2023 func-godot
// C# port of func_godot (https://github.com/func-godot/func_godot_plugin),
// used under the MIT License. See addons/func_godot_csharp/LICENSE.

using Godot;

namespace FuncGodot
{
    /// <summary>
    /// Inheritance-only entity definition. Holds properties and descriptions shared by several
    /// <see cref="FuncGodotFGDSolidClass"/> or <see cref="FuncGodotFGDPointClass"/> definitions.
    /// Never generates a node.
    /// </summary>
    [Tool]
    [GlobalClass]
    public partial class FuncGodotFGDBaseClass : FuncGodotFGDEntityClass
    {
        public FuncGodotFGDBaseClass()
        {
            Prefix = "@BaseClass";
        }
    }
}
