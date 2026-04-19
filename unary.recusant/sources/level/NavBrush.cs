using Godot;
using System;

namespace Unary.Recusant
{
    [Tool]
    [GlobalClass]
    public partial class NavBrush : Node3D
    {
        public static StringName NavBrushGroup { get; } = new(nameof(NavBrush));

        private BoxShape3D _collisionShape;

        private Aabb _aabb;
        private bool _gotAabb = false;

        public Aabb GetAabb()
        {
            if (_gotAabb)
            {
                return _aabb;
            }

            _collisionShape ??= (BoxShape3D)GetNode<CollisionShape3D>("Area3D/CollisionShape3D").Shape;
            Vector3 global = GlobalPosition;
            Vector3 size = _collisionShape.Size;

            _aabb = new Aabb(new Vector3()
            {
                X = global.X - (size.X / 2.0f),
                Y = global.Y - (size.Y / 2.0f),
                Z = global.Z - (size.Z / 2.0f),
            }, size);

            _gotAabb = true;

            return _aabb;
        }

        [Flags]
        public enum Flag : int
        {
            Start = 1 << 0,
            End = 1 << 1,
            Placeholder3 = 1 << 2,
            Placeholder4 = 1 << 3,
        }

        [Export]
        public Flag Flags;

        private void InitializeGroup()
        {
            if (!IsInGroup(NavBrushGroup))
            {
                AddToGroup(NavBrushGroup, true);
            }
        }

        public override void _Ready()
        {
            if (Engine.Singleton.IsEditorHint())
            {
                CallDeferred(MethodName.InitializeGroup);
                return;
            }

            // TODO Add brush management to NavMeshManager
            //PlayerManager.Singleton.AddMarker(this);
        }

        public override void _ExitTree()
        {
            if (Engine.IsEditorHint())
            {
                return;
            }

            //PlayerManager.Singleton.RemoveMarker(this);
        }

    }
}
