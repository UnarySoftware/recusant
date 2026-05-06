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

        [UiElement("%Settings")]
        private Button _settings;

        [UiElement("%Quit")]
        private Button _quit;

        public override void Initialize()
        {
            _host.Pressed += OnHost;
            _settings.Pressed += OnSettings;
            _quit.Pressed += OnQuit;
        }

        public override void Deinitialize()
        {
            _host.Pressed -= OnHost;
            _settings.Pressed -= OnSettings;
            _quit.Pressed -= OnQuit;
        }

        private void OnHost()
        {
            LevelManager.Singleton.LoadLevel("Streets");
        }

        private void OnSettings()
        {
            UiManager.Singleton.Open(typeof(UiSettingsState));
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
