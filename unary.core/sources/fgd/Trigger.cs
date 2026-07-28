using Godot;
using System.Collections.Generic;

namespace Unary.Core
{
    [Tool]
    [GlobalClass]
    [Icon("res://addons/unary.core.editor/icons/Brush.svg")]
    public partial class Trigger : BaseFgd
    {
        public static LazyResource<Material> TriggerMaterial { get; } = new("uid://c6628qs2op72q");

        [FgdProperty]
        public Color Color = Colors.White;

#if TOOLS

        public override void AppliedProperties()
        {
            Aabb aabb = GetAabb();

            if (aabb == default)
            {
                return;
            }

            Area3D area = new()
            {
                Name = "area",
                Monitorable = false
            };

            UnaryCollisionShape3D shape = new()
            {
                Name = "collision_shape",
                Shape = new BoxShape3D { Size = aabb.Size },
                Position = aabb.GetCenter(),
                VisualizerMaterial = TriggerMaterial.Cache
            };

            if (Color != Colors.White)
            {
                shape.VisualizerColor = Color;
                shape.BleachColor = new Color(0.97f, 0.61f, 0.0f);
            }

            area.AddChild(shape);
            AddChild(area);

            Node owner = FindOwner();
            area.Owner = owner;
            shape.Owner = owner;
        }

#endif
    }
}
