using System;
using Godot;

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
                RuntimeLogger.Error(this, $"Failed to resolve a type {TargetValue}");
            }

            return result;
        }
    }
}
