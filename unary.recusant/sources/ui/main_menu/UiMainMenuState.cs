using Godot;
using System;
using Unary.Core;

namespace Unary.Recusant
{
    [Tool]
    [GlobalClass]
    public partial class UiMainMenuState : UiState
    {
        [UiElement("%Host")]
        private Button _host;

        [UiElement("%Join")]
        private Button _join;

        [UiElement("%Quit")]
        private Button _quit;

        public override void Initialize()
        {
            _quit.Pressed += OnQuit;
        }

        public override void Deinitialize()
        {
            _quit.Pressed -= OnQuit;
        }

        private void OnQuit()
        {
            Bootstrap.Singleton.Quit(0);
        }

        public override Type GetBackState()
        {
            return null;
        }
    }
}
