#if TOOLS

using Godot;
using System;
using System.Collections.Generic;
using System.Text;
using Unary.Core;
using Unary.Core.Editor;

namespace Unary.Recusant.Editor
{
    [Tool]
    [GlobalClass]
    public partial class LevelRootGizmo : EditorNode3DGizmoPlugin, IPluginSystem
    {
        bool ISystem.Initialize()
        {
            PluginBootstrap.Singleton.AddNode3DGizmoPlugin(this);
            return true;
        }

        void ISystem.Deinitialize()
        {
            PluginBootstrap.Singleton.RemoveNode3DGizmoPlugin(this);
        }

        public override string _GetGizmoName()
        {
            return nameof(LevelRoot);
        }

        public override bool _HasGizmo(Node3D node)
        {
            return node is LevelRoot;
        }

        private enum ColorType
        {
            Red,
            Orange,
            Yellow,
            Green
        };

        private static Dictionary<ColorType, (Color color, string name)> _colors = new()
        {
            { ColorType.Red, (new(1.0f, 0.0f, 0.0f, 1.0f), nameof(ColorType.Red)) },
            { ColorType.Orange, (new(1.0f, 0.7f, 0.0f, 1.0f), nameof(ColorType.Orange)) },
            { ColorType.Yellow, (new(1.0f, 1.0f, 0.0f, 1.0f), nameof(ColorType.Yellow)) },
            { ColorType.Green, (new(0.0f, 1.0f, 0.0f, 1.0f), nameof(ColorType.Green)) },
        };

        public LevelRootGizmo()
        {
            foreach (var color in _colors)
            {
                CreateMaterial(color.Value.name, color.Value.color);

                StandardMaterial3D material = GetMaterial(color.Value.name);

                material.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
                material.AlbedoColor = color.Value.color;
                material.DisableFog = true;
                material.Transparency = BaseMaterial3D.TransparencyEnum.Disabled;
            }
        }

        private StandardMaterial3D GetMaterial(ColorType type)
        {
            return GetMaterial(_colors[type].name);
        }

        public override void _Redraw(EditorNode3DGizmo gizmo)
        {
            gizmo.Clear();

            LevelRoot levelRoot = (LevelRoot)gizmo.GetNode3D();

            if (levelRoot.Points != null && levelRoot.Points.Length > 0)
            {
                gizmo.AddLinePath(levelRoot.Points, GetMaterial(ColorType.Green));
            }

            Vector3 size = new(levelRoot.BoundsSize / 2.0f, levelRoot.BoundsSize / 2.0f, levelRoot.BoundsSize / 2.0f);

            if (levelRoot.Bounds != null)
            {
                foreach (var bound in levelRoot.Bounds)
                {
                    gizmo.AddBoxWithSize(bound, size, GetMaterial(ColorType.Red));
                }
            }
        }
    }
}

#endif
