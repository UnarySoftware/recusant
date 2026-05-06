using Godot;
using Unary.Core;

namespace Unary.Recusant
{
    public static class PlayerConstants
    {
        public static LazyResource<NavigationMesh> DefaultMesh { get; } = new("uid://bbsl0o6evupdh");
        public static LazyNode<PlayerMovement> PlayerMovement { get; } = new("uid://dudm3ef31bxc0");
        public static LazyResource<CylinderShape3D> PlayerShape { get; } = new("uid://wh4y4fij4d0l");
    }
}
