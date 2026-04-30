using Godot;
using System;
using Unary.Core;

namespace Unary.Recusant
{
    [Tool]
    [GlobalClass]
    public partial class UiGameplayState : UiState
    {
        public override void Open()
        {
            Input.Singleton.MouseMode = Input.MouseModeEnum.Captured;
            InputManager.Singleton.SetScope(InputScope.All);
        }

        public override void Close()
        {
            Input.Singleton.MouseMode = Input.MouseModeEnum.Visible;
            InputManager.Singleton.SetScope(InputScope.None);
        }

        public override Type GetBackState()
        {
            return typeof(UiBackMenuState);
        }
    }
}
