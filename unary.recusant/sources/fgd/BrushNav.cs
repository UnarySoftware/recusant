using Godot;
using System;
using System.Collections.Generic;
using Unary.Core;

namespace Unary.Recusant
{
    [Tool]
    [GlobalClass]
    [Icon("res://addons/unary.core.editor/icons/Brush.svg")]
    public partial class BrushNav : BaseFgd
    {
        public static LazyResource<Material> NavMaterial { get; } = new("uid://dsaupl4785chx");

        [Flags]
        public enum Flag : int
        {
            Start = 1 << 0,
            End = 1 << 1,
            Placeholder3 = 1 << 2,
            Placeholder4 = 1 << 3,
        }

        [Export]
        [FgdProperty]
        public Flag Flags;

        [Export]
        public Aabb Aabb;

        private static List<Tuple<Flag, Color>> _colors { get; } = new()
        {
            Tuple.Create(Flag.Start, Colors.Green),
            Tuple.Create(Flag.End, Colors.Red),
            Tuple.Create(Flag.Placeholder3, Colors.White),
            Tuple.Create(Flag.Placeholder4, Colors.White),
        };

        public override void _Ready()
        {
#if TOOLS
            if (Engine.Singleton.IsEditorHint())
            {
                return;
            }
#endif

            QueueFree();
        }

#if TOOLS

        public override void AppliedProperties()
        {
            Aabb aabb = GetAabb();

            if (aabb == default)
            {
                Aabb = default;
                return;
            }

            Aabb = aabb;

            Color targetColor = Colors.White;

            foreach (var color in _colors)
            {
                if (Flags.HasFlag(color.Item1))
                {
                    targetColor = color.Item2;
                    break;
                }
            }

            UnaryCollisionShape3D shape = new()
            {
                Name = "shape",
                Shape = new BoxShape3D { Size = aabb.Size },
                Position = aabb.GetCenter(),
                VisualizerMaterial = NavMaterial.Cache,
                VisualizerColor = targetColor,
                BleachColor = new Color(0.2f, 0.9f, 0.21f)
            };

            AddChild(shape);

            shape.Owner = FindOwner();
        }

#endif
    }

    public static class BrushNavFlagExtension
    {
        public static string ToStringPretty(this BrushNav.Flag flag)
        {
            if (flag == 0)
            {
                return "None";
            }

            return flag.ToString();
        }
    }
}
