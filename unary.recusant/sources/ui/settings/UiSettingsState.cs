using Godot;
using System;
using Unary.Core;

namespace Unary.Recusant
{
    [Tool]
    [GlobalClass]
    public partial class UiSettingsState : UiState
    {
        private static readonly LazyResource<ShaderMaterial> _blurMaterial = new("uid://cgxwenjv21f3h");

        public override void Initialize()
        {

        }

        public override void Deinitialize()
        {

        }

        public override void Open()
        {
            if (GameStateManager.Singleton.State == GameState.Game)
            {
                PostProcessManager.Singleton.SetLayer(PlayerCamera3D.PostProcessSlot.GeneralBlur, _blurMaterial.Cache);
            }
        }

        public override void Close()
        {
            if (GameStateManager.Singleton.State == GameState.Game)
            {
                PostProcessManager.Singleton.ClearLayer(PlayerCamera3D.PostProcessSlot.GeneralBlur);
            }
        }

        public override Type GetBackState()
        {
            if (GameStateManager.Singleton.State == GameState.BackgroundDynamic ||
                GameStateManager.Singleton.State == GameState.BackgroundStatic)
            {
                return typeof(UiMainMenuState);
            }
            else
            {
                return typeof(UiBackMenuState);
            }
        }
    }
}
