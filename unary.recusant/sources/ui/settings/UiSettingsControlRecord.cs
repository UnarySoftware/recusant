using Godot;
using System.Text;
using Unary.Core;

namespace Unary.Recusant
{
    [Tool]
    [GlobalClass]
    public partial class UiSettingsControlRecord : UiUnit<UiSettingsState>
    {
        //[UiElement("%Loading")]
        //private Control _loading;

        private bool _shown = false;

        public void Show()
        {
            if(_shown)
            {
                return;
            }

            _shown = true;
        }

        public override void _Input(InputEvent @event)
        {
            // TODO
        }
    }
}
