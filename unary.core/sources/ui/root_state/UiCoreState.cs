using Godot;
using System;

namespace Unary.Core
{
    [Tool]
    [GlobalClass]
    public partial class UiCoreState : UiState
    {
        public override Type GetBackState()
        {
            return null;
        }

        public override void Process(float delta)
        {
            if (InputManager.Singleton.IsActionJustReleased("ui_console", 0))
            {
                GetUnit<UiCoreConsole>().Toggle();
            }

            if (InputManager.Singleton.IsKeyPressed(Key.F1, 0))
            {
                RuntimeLogger.Log(this, "TEST");
            }

            if (InputManager.Singleton.IsKeyPressed(Key.F2, 0))
            {
                RuntimeLogger.Warning(this, "TEST");
            }

            if (InputManager.Singleton.IsKeyPressed(Key.F3, 0))
            {
                RuntimeLogger.Error(this, "TEST");
            }

            if (InputManager.Singleton.IsKeyPressed(Key.F5, 0))
            {
                int a = 1;
                int b = 0;
                int c = a / b;
            }
        }
    }
}
