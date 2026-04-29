using Godot;
using System;

namespace Unary.Core
{
    [Tool]
    [GlobalClass]
    public abstract partial class BaseSelectorResource : BaseResource
    {
        [Export]
        public string TargetValue = string.Empty;

        // Only gets assigned and read by the editor visualizer to filter out types
        // Never use this for strong-typing the ResourceLoader or anything
        public Type BaseType;
    }
}
