using System;
using Godot;

namespace Unary.Core
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
