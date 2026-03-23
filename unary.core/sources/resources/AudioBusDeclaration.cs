using Godot;

namespace Unary.Core
{
    [Tool]
    [GlobalClass]
    public partial class AudioBusDeclaration : BaseResource
    {
        [Export]
        public string Name;

        [Export(PropertyHint.Range, "0.0,1.0,0.05")]
        public float Volume = 0.5f;

        [Export]
        public AudioBusDeclaration Parent;
    }
}
