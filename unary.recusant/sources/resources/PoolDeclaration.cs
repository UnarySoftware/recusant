using Godot;
using Unary.Core;

namespace Unary.Recusant
{
    [Tool]
    [GlobalClass]
    public partial class PoolDeclaration : BaseResource
    {
        [Export]
        public LazyResource Scene
        {
            get => field; set => field = this.Filter(value, typeof(PackedScene));
        }

        [Export]
        public int Count = 4;

        [Export]
        public Godot.Collections.Dictionary<LazyResource, int> InfluencedCount
        {
            get => field; set => field = this.Filter(value, typeof(PoolDeclaration));
        }
    }
}
