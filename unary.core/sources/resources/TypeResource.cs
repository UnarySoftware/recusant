using Godot;
using System;

namespace Unary.Core
{
    [Tool]
    [GlobalClass]
    public partial class TypeResource : BaseSelectorResource
    {
        public Type ResolveType()
        {
            Type result = Types.GetTypeOfName(TargetValue);

            if (result == null)
            {
                this.Error($"Failed to resolve a type {TargetValue}");
            }

            return result;
        }
    }
}
