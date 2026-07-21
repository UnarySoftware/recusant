using Godot;
using System;
using System.Collections.Generic;
using System.Text;

namespace Unary.Core
{
    [Tool]
    [GlobalClass]
    [Icon("res://addons/unary.core.editor/icons/Brush.svg")]
    public partial class BaseFgd : Node3D
    {
        public void _func_godot_apply_properties(Godot.Collections.Dictionary properties)
        {
            ApplyProperties(properties);
        }

        public virtual void ApplyProperties(Godot.Collections.Dictionary properties)
        {

        }
    }
}
