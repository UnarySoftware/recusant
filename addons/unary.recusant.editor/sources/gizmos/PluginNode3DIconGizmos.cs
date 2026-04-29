#if TOOLS

using Godot;
using System;
using System.Collections.Generic;
using Unary.Core;
using Unary.Core.Editor;

namespace Unary.Recusant.Editor
{
    [Tool]
    [GlobalClass]
    public partial class PluginNode3DIconGizmos : EditorNode3DGizmoPlugin, IPluginSystem
    {
        // TODO Rework on https://github.com/godotengine/godot/pull/118790 merge
        //private Dictionary<Type, Material> _selectedTargetTypes = [];
        private Dictionary<Type, Material> _basicTargetTypes = [];

        private void CreateMaterial(Type type, Texture2D texture) // bool selected)
        {
            CreateMaterial(type.FullName, new Color());

            StandardMaterial3D material = GetMaterial(type.FullName);

            // Taken straight from godot/editor/scene/3d/node_3d_editor_gizmos.cpp for parity
            material.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
            material.VertexColorUseAsAlbedo = true;
            material.VertexColorIsSrgb = true;
            material.DisableFog = true;
            material.Transparency = BaseMaterial3D.TransparencyEnum.AlphaScissor;
            material.AlphaScissorThreshold = 0.1f;
            material.AlbedoTexture = texture;
            material.FixedSize = true;
            material.BillboardMode = BaseMaterial3D.BillboardModeEnum.Enabled;
            material.RenderPriority = (int)Material.RenderPriorityMin;

            /*
            if (selected)
            {
                _selectedTargetTypes[type] = material;
            }
            else
            */
            {
                //material.AlbedoColor = new Color(0.6f, 0.6f, 0.6f);
                _basicTargetTypes[type] = material;
            }
        }

        bool ISystem.Initialize()
        {
            var targetTypes = Types.GetTypesWithAttribute(typeof(IconAttribute)) ?? [];

            Type node3DType = typeof(Node3D);

            foreach (var type in targetTypes)
            {
                if (!node3DType.IsAssignableFrom(type))
                {
                    continue;
                }

                IconAttribute attribute = Types.GetTypeAttribute<IconAttribute>(type);

                Resource target = ResourceLoader.Singleton.Load(attribute.Path, nameof(Texture2D));

                if (target == null)
                {
                    this.Warning($"Tried loading an invalid icon path for a class {type.FullName}");
                    continue;
                }

                Texture2D texture = (Texture2D)target;

                CreateMaterial(type, texture);
            }

            PluginBootstrap.Singleton.AddNode3DGizmoPlugin(this);
            return true;
        }

        void ISystem.Deinitialize()
        {
            PluginBootstrap.Singleton.RemoveNode3DGizmoPlugin(this);
        }

        public override string _GetGizmoName()
        {
            return "Node3D Attribute Icons";
        }

        public override bool _HasGizmo(Node3D node)
        {
            return node is not null && _basicTargetTypes.ContainsKey(node.GetType());
        }

        public override void _Redraw(EditorNode3DGizmo gizmo)
        {
            gizmo.Clear();

            Node3D node3d = gizmo.GetNode3D();
            Type type = node3d.GetType();

            /*
            if (gizmo.IsSelected())
            {
                if (_selectedTargetTypes.TryGetValue(type, out var material))
                {
                    gizmo.AddUnscaledBillboard(material, 0.05f);
                }
            }
            else
            */
            {
                if (_basicTargetTypes.TryGetValue(type, out var material))
                {
                    gizmo.AddUnscaledBillboard(material, 0.05f);
                }
            }
        }
    }
}

#endif
