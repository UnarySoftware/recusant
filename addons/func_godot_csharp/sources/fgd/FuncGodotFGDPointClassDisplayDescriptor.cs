// Copyright (c) 2023 func-godot
// C# port of func_godot (https://github.com/func-godot/func_godot_plugin),
// used under the MIT License. See addons/func_godot_csharp/LICENSE.

using Godot;

namespace FuncGodot
{
    /// <summary>
    /// Describes how a <see cref="FuncGodotFGDPointClass"/> is displayed in TrenchBroom. Values are written
    /// into the FGD literally: paths must be quoted, class property keys and integers must not be.
    /// </summary>
    [Tool]
    [GlobalClass]
    public partial class FuncGodotFGDPointClassDisplayDescriptor : Resource
    {
        /// <summary>
        /// Either a quoted path to the display asset relative to TrenchBroom's game path
        /// (e.g. <c>"models/marsfrog.glb"</c>), or an unquoted class property key holding such a path
        /// (e.g. <c>display_model_path</c>).
        /// </summary>
        [Export]
        public string DisplayAssetPath = string.Empty;

        /// <summary>
        /// Scale of the display asset: a number, a class property key, or a TrenchBroom scale expression.
        /// Leave blank to use the game config's default scale expression.
        /// </summary>
        [Export]
        public string Scale = string.Empty;

        /// Skin of the display asset: a number or a class property key.
        [Export]
        public string Skin = string.Empty;

        /// <summary>
        /// Frame of the display asset: a number or a class property key. For GLB models this selects the
        /// animation at that index.
        /// </summary>
        [Export]
        public string Frame = string.Empty;

        /// <summary>
        /// Expression that, when true, forces this descriptor's asset to be displayed, in the form
        /// <c>property == value</c>. Exactly one descriptor in a point class may leave this blank; it becomes
        /// the default.
        /// </summary>
        [Export]
        public string Conditional = string.Empty;
    }
}
