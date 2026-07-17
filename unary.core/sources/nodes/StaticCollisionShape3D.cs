using Godot;

namespace Unary.Core
{
    [Tool]
    [GlobalClass]
    public partial class StaticCollisionShape3D : CollisionShape3D
    {
        [Export]
        public UnaryStandartMaterial3D.SurfaceType Type;
    }
}
