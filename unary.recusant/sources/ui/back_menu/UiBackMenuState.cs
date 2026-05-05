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

        [UiElement("%Settings")]
        private Button _settings;

        [UiElement("%Quit")]
        private Button _quit;

        private static readonly LazyResource<ShaderMaterial> _blurMaterial = new("uid://cgxwenjv21f3h");

        public override void Initialize()
        {
            _resume.Pressed += OnResume;
            _settings.Pressed += OnSettings;
            _quit.Pressed += OnQuit;
        }

        public override void Deinitialize()
        {
            _resume.Pressed -= OnResume;
            _settings.Pressed -= OnSettings;
            _quit.Pressed -= OnQuit;
        }

        private void OnResume()
        {
            UiManager.Singleton.GoBack();
        }

        private void OnSettings()
        {
            UiManager.Singleton.Open(typeof(UiSettingsState));
        }

        private void OnQuit()
        {
            Bootstrap.Singleton.Quit(0);
        }

        public override void Open()
        {
            PostProcessManager.Singleton.SetLayer(PlayerCamera3D.PostProcessSlot.GeneralBlur, _blurMaterial.Cache);
        }

        public override void Close()
        {
            PostProcessManager.Singleton.ClearLayer(PlayerCamera3D.PostProcessSlot.GeneralBlur);
        }

        public override Type GetBackState()
        {
            return typeof(UiGameplayState);
        }
    }
}
