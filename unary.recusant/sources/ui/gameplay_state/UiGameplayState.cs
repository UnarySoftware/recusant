using Godot;
using System;
using Unary.Core;

namespace Unary.Recusant
{
    [Tool]
    [GlobalClass]
    public partial class UiGameplayState : UiState
    {
        public override Type GetBackState()
        {
            return null;
        }
    }
}
