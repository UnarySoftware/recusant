using Godot;
using Unary.Core;

namespace Unary.Recusant
{
    [Tool]
    [GlobalClass]
    public partial class UiInfoUnit : UiUnit<UiGameplayState>
    {
        [UiElement("%Label1")]
        private Label _label1;

        [UiElement("%Label2")]
        private Label _label2;

        [UiElement("%Label3")]
        private Label _label3;

        public override void Process(float delta)
        {
            if (Player.Instance != null)
            {
                _label1.Text = Player.Instance.Body.Velocity.Length().ToString("0.0 m/s");
                _label2.Text = Player.Instance.Damage + " dmg";

                if (PlayerFlow.Instance != null)
                {
                    _label3.Text = "Flow: " + PlayerFlow.Instance.Flow.ToString("0.0") + "\n" +
                        "Flags: " + PlayerFlow.Instance.Flags.ToStringPretty() + "\n" +
                        "Triangle: " + PlayerFlow.Instance.Triangle;
                }
            }
        }
    }
}
