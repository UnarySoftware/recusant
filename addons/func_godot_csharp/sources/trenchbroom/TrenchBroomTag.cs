// Copyright (c) 2023 func-godot
// C# port of func_godot (https://github.com/func-godot/func_godot_plugin),
// used under the MIT License. See addons/func_godot_csharp/LICENSE.

using Godot;

namespace FuncGodot
{
    /// <summary>
    /// A pattern matching tag for <see cref="TrenchBroomGameConfig"/>, controlling how matching faces or brush
    /// entities look and filter in TrenchBroom. Has no effect on what Godot builds.
    /// </summary>
    [Tool]
    [GlobalClass]
    public partial class TrenchBroomTag : Resource
    {
        public enum TagMatchTypes
        {
            /// Matches any brush face whose texture name matches the pattern.
            Texture,

            /// Matches any brush entity whose classname matches the pattern.
            Classname,
        }

        /// Names the tag. Not used for matching.
        [Export]
        public string TagName = string.Empty;

        /// <summary>
        /// Attributes applied to matching faces or brush entities. TrenchBroom only supports
        /// <c>_transparent</c>, which renders the match transparent.
        /// </summary>
        [Export]
        public Godot.Collections.Array<string> TagAttributes = ["transparent"];

        [Export]
        public TagMatchTypes TagMatchType;

        /// <summary>
        /// Filters which classname or texture receives the attributes. <c>*</c> acts as a wildcard, so
        /// <c>trigger*</c> with a Classname match type tags every brush entity prefixed with <c>trigger</c>.
        /// </summary>
        [Export]
        public string TagPattern = string.Empty;

        /// Filters which textures receive the attributes. Only used with a Texture match type.
        [Export]
        public string TextureName = string.Empty;
    }
}
