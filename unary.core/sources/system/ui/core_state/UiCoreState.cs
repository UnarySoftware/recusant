using System;
using Godot;

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
            if (Input.Singleton.IsActionJustReleased("ui_console"))
            {
                GetUnit<UiCoreConsole>().Toggle();
            }

            if (Input.Singleton.IsKeyPressed(Key.F1))
            {
                RuntimeLogger.Log(this, "TEST");
            }

            if (Input.Singleton.IsKeyPressed(Key.F2))
            {
                RuntimeLogger.Warning(this, "TEST");
            }

            if (Input.Singleton.IsKeyPressed(Key.F3))
            {
                RuntimeLogger.Error(this, "TEST");
            }

            if (Input.Singleton.IsKeyPressed(Key.F5))
            {
                int a = 1;
                int b = 0;
                int c = a / b;
            }
        }
    }
}
