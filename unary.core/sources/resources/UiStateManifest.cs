using Godot;

namespace Unary.Core
{
    [Tool]
    [GlobalClass]
    public partial class UiStateManifest : BaseResource
    {
        [Export]
        public TypeResource Type
        {
            get => field; set => field = this.Filter(value, typeof(UiState));
        }

        [Export]
        public bool AlwaysEnabled = false;

        [Export]
        // Only relevant if AlwaysEnabled is set to true
        public LazyResource[] Underlaying
        {
            get => field; set => field = this.Filter(value, typeof(UiStateManifest));
        }

        [Export]
        // Only relevant if AlwaysEnabled is set to true
        public LazyResource[] Overlaying
        {
            get => field; set => field = this.Filter(value, typeof(UiStateManifest));
        }

        [Export]
        public LazyResource Scene
        {
            get => field; set => field = this.Filter(value, typeof(PackedScene));
        }
    }
}
