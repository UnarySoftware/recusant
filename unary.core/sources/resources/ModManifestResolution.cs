using Godot;

namespace Unary.Core
{
    [Tool]
    [GlobalClass]
    public partial class ModManifestResolution : BaseResource
    {
        [Export]
        public ModManifestSelector First = new();

        [Export]
        public ModManifestSelector Second = new();
    }
}
