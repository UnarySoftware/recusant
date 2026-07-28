using Godot;
using Godot.Collections;
using System.Collections.Generic;

namespace Unary.Core
{
    [Tool]
    [GlobalClass]
    [Icon("res://addons/unary.core.editor/icons/Brush.svg")]
    public partial class BaseFgd : Node3D
    {
        public static StringName func_godot_mesh_data { get; } = new("func_godot_mesh_data");

        [Export]
        [FgdProperty]
        public string TargetName = string.Empty;

        [Export]
        public int EntityIndex = -1;

        public HashSet<IFgdOwner> Owners = [];

        public override void _Ready()
        {
            if (Engine.Singleton.IsEditorHint())
            {
                return;
            }

            FgdManager.Singleton.AddFgd(this);
        }

        public override void _ExitTree()
        {
            if (Engine.Singleton.IsEditorHint())
            {
                return;
            }

            foreach (var owner in Owners)
            {
                owner.OnDestroy(this);
            }

            FgdManager.Singleton.RemoveFgd(this);
        }

#if TOOLS

        public Node FindOwner()
        {
            return Owner ?? (IsInsideTree() ? GetTree().EditedSceneRoot : null);
        }

        public Aabb GetAabb()
        {
            Dictionary meta = GetMeta(func_godot_mesh_data).AsGodotDictionary();
            SetMeta(func_godot_mesh_data, new());

            Vector3[] vertices = meta["vertices"].As<Vector3[]>();

            if (vertices.Length != 36)
            {
                SharedLogger.Warning(this, $"{GetType().Name} with entity index {EntityIndex} has a malformed shape, we only support basic boxes for AABB calculation.");
                return default;
            }

            const float mergeDistance = FuncGodot.FuncGodotUtil.VertexEpsilon * FuncGodot.FuncGodotUtil.VertexEpsilon;

            List<Vector3> corners = [];

            foreach (Vector3 vertex in vertices)
            {
                bool exists = false;

                foreach (Vector3 corner in corners)
                {
                    if (vertex.DistanceSquaredTo(corner) <= mergeDistance)
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                {
                    corners.Add(vertex);
                }
            }

            if (corners.Count != 8)
            {
                SharedLogger.Warning(this, $"{GetType().Name} with entity index {EntityIndex} resolved {corners.Count} corners instead of 8, we only support basic boxes for AABB calculations.");
                return default;
            }

            Aabb result = new(corners[0], Vector3.Zero);

            for (int i = 1; i < corners.Count; i++)
            {
                result = result.Expand(corners[i]);
            }

            return result;
        }

        public override void _ValidateProperty(Dictionary property)
        {
            property.MakeReadOnly(PropertyName.TargetName, PropertyName.EntityIndex);
            base._ValidateProperty(property);
        }

        public void ApplyProperties(Dictionary properties)
        {
            if (properties.TryGetValue("TargetName", out Variant TargetNameVariant))
            {
                TargetName = TargetNameVariant.As<string>();
            }

            if (properties.TryGetValue("EntityIndex", out Variant EntityIndexVariant))
            {
                EntityIndex = EntityIndexVariant.As<int>();
            }

            RunApplyProperties(properties);
            AppliedProperties();
        }

        public virtual void RunApplyProperties(Dictionary properties)
        {

        }

        public virtual void AppliedProperties()
        {

        }
#endif
    }
}
