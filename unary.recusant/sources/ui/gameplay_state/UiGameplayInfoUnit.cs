
using System;
using Godot;
using Unary.Core;

namespace Unary.Recusant
{
    [Tool]
    [GlobalClass]
    public partial class UiInfoUnit : UiUnit<UiGameplayState>
    {
        private Label _label1;
        private Label _label2;

        public override void Initialize()
        {
            _label1 = Root.GetNode<Label>("%Label1");
            _label2 = Root.GetNode<Label>("%Label2");
        }

        public override void Process(float delta)
        {
            if (Player.Instance != null)
            {
                _label1.Text = Player.Instance.Velocity.Length().ToString("0.0 m/s");
                _label2.Text = Player.Instance.Damage + " dmg";
            }

        }
    }
}
