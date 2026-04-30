using System;

namespace Unary.Recusant
{
    [Flags]
    public enum InputScope : int
    {
        None = 0,
        PlayerCamera = 1 << 0,
        PlayerMovement = 1 << 1,
        Player = PlayerCamera | PlayerMovement,
        All = Player
    };
}
