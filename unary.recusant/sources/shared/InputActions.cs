using Godot;

namespace Unary.Recusant
{
    public static class InputActions
    {
        public static StringName Left { get; } = new("left");
        public static StringName Right { get; } = new("right");
        public static StringName Up { get; } = new("up");
        public static StringName Down { get; } = new("down");
        public static StringName Jump { get; } = new("jump");
        public static StringName Sprint { get; } = new("sprint");
        public static StringName Crouch { get; } = new("crouch");
        public static StringName Noclip { get; } = new("noclip");

    }
}
