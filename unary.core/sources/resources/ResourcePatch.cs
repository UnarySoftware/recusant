using System;
using Godot;

namespace Unary.Core
{
    [Tool]
    [GlobalClass]
    public partial class ResourcePatch : BaseResource
    {
        [Export]
        public LazyResource Target
        {
            get => field; set => field = this.Filter(value, typeof(Resource));
        }

        [Export]
        public Godot.Collections.Dictionary<string, Variant> Properties;
    }
}
