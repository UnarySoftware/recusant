using Godot;
using System;
using Unary.Core;

namespace Unary.Recusant
{
    [Tool]
    [GlobalClass]
    public partial class UiRecusantState : UiState
    {
        public override Type GetBackState()
        {
            return null;
        }

        public override void Process(float delta)
        {

        }
    }
}
