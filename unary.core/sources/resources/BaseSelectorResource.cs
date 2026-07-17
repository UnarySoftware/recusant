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

        public static bool IsValid(BaseSelectorResource resource)
        {
            if (resource == null)
            {
                return false;
            }

            if (resource.TargetValue == null || resource.TargetValue == string.Empty || string.IsNullOrEmpty(resource.TargetValue))
            {
                return false;
            }

            return true;
        }

        // Only gets assigned and read by the editor visualizer to filter out types
        // Never use this for strong-typing the ResourceLoader or anything
        public Type BaseType;
    }
}
