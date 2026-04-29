using Godot;
using Unary.Core;

namespace Unary.Recusant
{
    [Tool]
    [GlobalClass]
    public partial class LevelDefinition : NetworkedResource
    {
        [Export]
        public string Name = "Test";

        [Export]
        public LazyResource Scene
        {
            get => field; set => field = this.Filter(value, typeof(PackedScene));
        }

        [Export]
        public bool Background = false;
    }
}
