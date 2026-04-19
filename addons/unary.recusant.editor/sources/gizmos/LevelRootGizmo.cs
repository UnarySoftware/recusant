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
            Green,
            White
        };

        private static Dictionary<ColorType, (Color color, string name)> _colors = new()
        {
            { ColorType.Red, (new(1.0f, 0.0f, 0.0f, 1.0f), nameof(ColorType.Red)) },
            { ColorType.Orange, (new(1.0f, 0.7f, 0.0f, 1.0f), nameof(ColorType.Orange)) },
            { ColorType.Yellow, (new(1.0f, 1.0f, 0.0f, 1.0f), nameof(ColorType.Yellow)) },
            { ColorType.Green, (new(0.0f, 1.0f, 0.0f, 1.0f), nameof(ColorType.Green)) },
            { ColorType.White, (new(1.0f, 1.0f, 1.0f, 1.0f), nameof(ColorType.White)) },
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

            Vector3 pointBox = new(0.2f, 0.2f, 0.2f);

            if (levelRoot.VisualPaths != null && levelRoot.VisualPaths.Count > 0)
            {
                foreach (var entry in levelRoot.VisualPaths)
                {
                    if (entry.Points != null && entry.Points.Length > 0)
                    {
                        gizmo.AddLinePath(entry.Points, GetMaterial(ColorType.Green));
                    }

                    gizmo.AddBoxWithSize(entry.RealStart, pointBox, GetMaterial(ColorType.Green));
                    gizmo.AddBoxWithSize(entry.ResolvedStart, pointBox, GetMaterial(ColorType.Red));
                }
            }

            if (levelRoot.FromStartToFinish != null && levelRoot.FromStartToFinish.Length > 0)
            {
                gizmo.AddBoxWithSize(levelRoot.FromStartToFinish[0], pointBox, GetMaterial(ColorType.Yellow));
                gizmo.AddBoxWithSize(levelRoot.FromStartToFinish[^1], pointBox, GetMaterial(ColorType.Yellow));
                gizmo.AddLinePath(levelRoot.FromStartToFinish, GetMaterial(ColorType.Yellow));
            }

            Vector3 size = new(levelRoot.BoundsSize / 2.0f, levelRoot.BoundsSize / 2.0f, levelRoot.BoundsSize / 2.0f);

            if (levelRoot.Bounds != null)
            {
                foreach (var bound in levelRoot.Bounds)
                {
                    gizmo.AddBoxWithSize(bound, size, GetMaterial(ColorType.White));
                }
            }
        }
    }
}

#endif
