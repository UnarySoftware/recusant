using System;
using Godot;
using Unary.Core;

namespace Unary.Recusant
{
    [Tool]
    [GlobalClass]
    public partial class UiLoadingState : UiState
    {
        public override Type GetBackState()
        {
            return null;
        }
    }
}
