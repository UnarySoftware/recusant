using Godot;

namespace Unary.Core
{
    [Tool]
    [GlobalClass]
    public partial class ContentSwapManifest : Resource
    {
        [Export]
        public string[] Originals;
        [Export]
        public string[] Replacements;
    }
}
