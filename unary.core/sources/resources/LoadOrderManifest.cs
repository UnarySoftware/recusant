using Godot;

namespace Unary.Core
{
    [Tool]
    [GlobalClass]
    public partial class LoadOrderManifest : BaseResource
    {
        [Export]
        public ModManifestSelector[] Enabled = [];
    }
}
