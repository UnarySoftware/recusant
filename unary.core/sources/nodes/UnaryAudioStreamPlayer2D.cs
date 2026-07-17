using Godot;
using Godot.Collections;

namespace Unary.Core
{
    [Tool]
    [GlobalClass]
    public partial class UnaryAudioStreamPlayer2D : AudioStreamPlayer2D
    {
        [Export]
        public AudioBusDeclaration TargetBus;

        public override void _Ready()
        {
            if (TargetBus == null || string.IsNullOrEmpty(TargetBus.Name))
            {
                return;
            }

            Bus = TargetBus.Name;
        }

        public override void _ValidateProperty(Dictionary property)
        {
            property.MakeHidden(AudioStreamPlayer2D.PropertyName.Bus);
            base._ValidateProperty(property);
        }
    }
}
