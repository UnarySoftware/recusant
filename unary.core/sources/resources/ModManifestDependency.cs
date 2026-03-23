using Godot;

namespace Unary.Core
{
    [Tool]
    [GlobalClass]
    public partial class ModManifestDependency : BaseResource
    {
        [Export]
        public bool Required = true;

        [Export]
        public string ModId;

        [Export]
        public string Version;

        [Export]
        public ModVersionSelector.SelectionType Type = ModVersionSelector.SelectionType.Exact;
    }
}
