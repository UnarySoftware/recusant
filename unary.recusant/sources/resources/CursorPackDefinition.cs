using Godot;
using Unary.Core;

namespace Unary.Recusant
{
    [Tool]
    [GlobalClass]
    public partial class CursorPackDefinition : BaseResource
    {
        [Export]
        public float TextureSize = 64.0f;

        [Export]
        public LazyResource ArrowTexture
        {
            get; set => field = this.Filter(value, typeof(Texture2D));
        }

        [Export]
        public LazyResource IBeamTexture
        {
            get; set => field = this.Filter(value, typeof(Texture2D));
        }

        [Export]
        public LazyResource PointingHandTexture
        {
            get; set => field = this.Filter(value, typeof(Texture2D));
        }

        [Export]
        public LazyResource CrossTexture
        {
            get; set => field = this.Filter(value, typeof(Texture2D));
        }

        [Export]
        public LazyResource WaitTexture
        {
            get; set => field = this.Filter(value, typeof(Texture2D));
        }

        [Export]
        public LazyResource BusyTexture
        {
            get; set => field = this.Filter(value, typeof(Texture2D));
        }

        [Export]
        public LazyResource DragTexture
        {
            get; set => field = this.Filter(value, typeof(Texture2D));
        }

        [Export]
        public LazyResource CanDropTexture
        {
            get; set => field = this.Filter(value, typeof(Texture2D));
        }

        [Export]
        public LazyResource ForbiddenTexture
        {
            get; set => field = this.Filter(value, typeof(Texture2D));
        }

        [Export]
        public LazyResource VSizeTexture
        {
            get; set => field = this.Filter(value, typeof(Texture2D));
        }

        [Export]
        public LazyResource HSizeTexture
        {
            get; set => field = this.Filter(value, typeof(Texture2D));
        }

        [Export]
        public LazyResource BDiagSizeTexture
        {
            get; set => field = this.Filter(value, typeof(Texture2D));
        }

        [Export]
        public LazyResource FDiagSizeTexture
        {
            get; set => field = this.Filter(value, typeof(Texture2D));
        }

        [Export]
        public LazyResource HelpTexture
        {
            get; set => field = this.Filter(value, typeof(Texture2D));
        }
    }
}
