using Godot;
using Godot.Collections;

namespace Unary.Core
{
    [Tool]
    [GlobalClass]
    public partial class ModManifest : BaseResource
    {
        [Export]
        public string ModId;

        [Export]
        public string Version;

        [Export]
        public BuildManifest BuildManifest;

        [Export]
        public ulong SteamFileId;

        [Export]
        public ModManifestDependency[] Dependencies = [];

        [Export]
        public ModManifestSelector[] Incompatibilities = [];

        [Export]
        public ModManifestResolution[] Resolutions = [];

        // Resolved at game runtime
        public ModPathInfo PathInfo;

        public override void _ValidateProperty(Dictionary property)
        {
            property.MakeReadOnly(PropertyName.SteamFileId);
            base._ValidateProperty(property);
        }
    }
}
