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
    public partial class PluginGizmoDrawer : EditorNode3DGizmoPlugin, IPluginSystem
    {
        private readonly Dictionary<int, Material> _colorToMaterial = [];

        private static readonly byte[] _hashArray = new byte[4];

        private Material GetMaterial(Color color)
        {
            _hashArray[0] = (byte)color.R8;
            _hashArray[1] = (byte)color.G8;
            _hashArray[2] = (byte)color.B8;
            _hashArray[3] = (byte)color.A8;

            int hash = BitConverter.ToInt32(_hashArray, 0);

            if (_colorToMaterial.TryGetValue(hash, out var result))
            {
                return result;
            }

            CreateMaterial(color.ToString(), color);

            StandardMaterial3D material = GetMaterial(color.ToString());

            material.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
            material.AlbedoColor = color;
            material.DisableFog = true;

            if (_hashArray[3] == 255)
            {
                material.Transparency = BaseMaterial3D.TransparencyEnum.Disabled;
            }
            else
            {
                material.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
            }

            _colorToMaterial[hash] = material;

            return material;
        }

        bool ISystem.Initialize()
        {
            PluginBootstrap.Singleton.AddNode3DGizmoPlugin(this);
            return true;
        }

        void ISystem.Deinitialize()
        {
            _colorToMaterial.Clear();
            PluginBootstrap.Singleton.RemoveNode3DGizmoPlugin(this);
        }

        public override string _GetGizmoName()
        {
            return "Node3D Custom Gizmos";
        }

        public override bool _HasGizmo(Node3D node)
        {
            return node is not null && node is IGizmo;
        }

        private EditorNode3DGizmo _currentGizmo;

        public override void _Redraw(EditorNode3DGizmo gizmo)
        {
            gizmo.Clear();

            Node3D node3d = gizmo.GetNode3D();

            if (node3d is IGizmo gizmoInterface)
            {
                _currentGizmo = gizmo;
                gizmoInterface.DrawGizmo();
                _currentGizmo = null;
            }
        }

        public void AddCollision(TriangleMesh mesh)
        {
            _currentGizmo.AddCollisionTriangles(mesh);
        }

        public void DrawBoxWireframe(Vector3 origin, Vector3 size, Color color)
        {
            _currentGizmo.AddLines(Gizmos.CreateBox(size, origin), GetMaterial(color));
        }

        public void DrawBoxWireframe(Aabb box, Color color)
        {
            (Vector3 center, Vector3 size) = Gizmos.DecomposeBox(box);
            DrawBoxWireframe(center, size, color);
        }

        public void DrawBox(Vector3 origin, Vector3 size, Color color)
        {
            Transform3D transform = new()
            {
                Origin = origin
            };
            _currentGizmo.AddMesh(Gizmos.GetBoxMesh(size), GetMaterial(color), transform);
        }

        public void DrawBox(Aabb box, Color color)
        {
            (Vector3 origin, Vector3 size) = Gizmos.DecomposeBox(box);
            Transform3D transform = new()
            {
                Origin = origin
            };
            _currentGizmo.AddMesh(Gizmos.GetBoxMesh(size), GetMaterial(color), transform);
        }

        public void DrawPath(Vector3[] points, Color color)
        {
            _currentGizmo.AddLines(Gizmos.CreatePath(points), GetMaterial(color));
        }
    }
}

#endif
