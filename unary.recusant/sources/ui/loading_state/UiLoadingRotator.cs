using System;
using Godot;
using Unary.Core;

namespace Unary.Recusant
{
    [Tool]
    [GlobalClass]
    public partial class UiLoadingRotator : UiUnit<UiLoadingState>
    {
        private TextureRect _rotator;

        public const float Speed = 100.0f;

        public override void Initialize()
        {
            _rotator = Root.GetNode<TextureRect>("%Rotator");
        }

        public override void Process(float delta)
        {
            if (!Opened)
            {
                return;
            }

            _rotator.RotationDegrees += delta * Speed;

            if (_rotator.RotationDegrees > 360.0f)
            {
                _rotator.RotationDegrees -= 360.0f;
            }
        }
    }
}
