using Godot;
using System;
using Unary.Core;

namespace Unary.Recusant
{
    [Tool]
    [GlobalClass]
    public partial class UiLoadingState : UiState
    {
        /* TODO Turn this into a dedicated option since this actually improves loading performance
        int targetFps = 0;

        int previousFps = -1;

        float _timer;
        ulong _start;

        public override void Open()
        {
            _start = Time.GetTicksMsec();

            previousFps = Engine.Singleton.MaxFps;

            if (targetFps == 0)
            {
                targetFps = (int)DisplayServer.Singleton.ScreenGetRefreshRate(DisplayServer.Singleton.WindowGetCurrentScreen());
            }

            targetFps = (int)(targetFps * 0.33f);

            Engine.Singleton.MaxFps = targetFps;
        }

        public override void Close()
        {
            Engine.Singleton.MaxFps = previousFps;

            ulong result = Time.GetTicksMsec() - _start;
            RuntimeLogger.Log(this, $"Took {result} msec at {targetFps} FPS");
        }
        */

        public override Type GetBackState()
        {
            return null;
        }
    }
}
