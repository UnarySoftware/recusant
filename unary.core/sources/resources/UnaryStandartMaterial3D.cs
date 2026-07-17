using Godot;

namespace Unary.Core
{
    [Tool]
    [GlobalClass]
    public partial class UnaryStandartMaterial3D : StandardMaterial3D
    {
        public enum SurfaceType
        {
            None,
            Concrete,
            Wood
        };

        [Export]
        public SurfaceType Type = SurfaceType.Concrete;
    }
}
