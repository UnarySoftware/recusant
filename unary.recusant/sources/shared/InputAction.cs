using Godot;
using System;
using System.Runtime.CompilerServices;
using Unary.Core;

namespace Unary.Recusant
{
    public class InputAction : InputActionBase
    {
        public InputScope Scope
        {
            set
            {
                base.BaseScope = (int)value;
            }
        }
    }
}
