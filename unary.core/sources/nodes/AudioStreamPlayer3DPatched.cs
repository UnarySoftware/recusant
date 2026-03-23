
using Godot;

namespace Unary.Core
{
    [Tool]
    [GlobalClass]
    public partial class AudioStreamPlayer3DPatched : AudioStreamPlayer3D
    {
        [Export]
        public AudioBusDeclaration TargetBus;

        public override void _Ready()
        {
            Bus = TargetBus.Name;
        }
    }
}
