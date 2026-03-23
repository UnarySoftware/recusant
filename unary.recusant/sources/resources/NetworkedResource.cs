using Godot;
using Unary.Core;

namespace Unary.Recusant
{
    [Tool]
    [GlobalClass]
    // Beware, all types that inherit from NetworkedResource will be loaded and stored automatically by NetworkResources
    public partial class NetworkedResource : BaseResource
    {
        // This is always assigned at runtime-only
        public uint NetworkId = 0;
    }
}
