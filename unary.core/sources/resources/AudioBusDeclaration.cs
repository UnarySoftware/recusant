using Godot;

namespace Unary.Core
{
    [Tool]
    [GlobalClass]
    public partial class AudioBusDeclaration : BaseResource
    {
        [Export]
        public string Name;

        [Export(PropertyHint.Range, "0.0,100.0,5.00")]
        public float Volume = 50.0f;

        [Export]
        public AudioBusDeclaration Parent;
    }
}
