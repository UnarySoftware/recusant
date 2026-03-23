using System;
using Godot;
using Unary.Core;

namespace Unary.Recusant
{
    [Tool]
    [GlobalClass]
    public partial class UiMainMenuState : UiState
    {
        public override Type GetBackState()
        {
            return null;
        }
    }
}
