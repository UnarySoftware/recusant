using Godot;
using System;
using Unary.Core;

namespace Unary.Recusant
{
    [Tool]
    [GlobalClass]
    public partial class UiBackMenuState : UiState
    {
        [UiElement("%Resume")]
        private Button _resume;

        [UiElement("%Quit")]
        private Button _quit;

        public override void Initialize()
        {
            _resume.Pressed += OnResume;
            _quit.Pressed += OnQuit;
        }

        public override void Deinitialize()
        {
            _resume.Pressed -= OnResume;
            _quit.Pressed -= OnQuit;
        }

        private void OnResume()
        {
            UiManager.Singleton.GoBack();
        }

        private void OnQuit()
        {
            Bootstrap.Singleton.Quit(0);
        }

        public override Type GetBackState()
        {
            return typeof(UiGameplayState);
        }
    }
}
