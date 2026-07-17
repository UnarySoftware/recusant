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
        private struct MaterialKey : IEquatable<MaterialKey>
        {
            public bool Shaded;
            public float Scale;
            public int ColorHash;

            public readonly bool Equals(MaterialKey other)
            {
                return Shaded == other.Shaded &&
                    Scale == other.Scale &&
                    ColorHash == other.ColorHash;
            }

            public override readonly bool Equals(object obj)
            {
                return obj is MaterialKey other && Equals(other);
            }

            public override readonly int GetHashCode()
            {
                return HashCode.Combine(Shaded, Scale, ColorHash);
            }
        }

        private readonly Dictionary<MaterialKey, Material> _colorToMaterial = [];

        private static readonly byte[] _hashArray = new byte[4];

        private Material GetMaterial(Color color, bool shaded, float textureScale = 20.0f)
        {
            _hashArray[0] = (byte)color.R8;
            _hashArray[1] = (byte)color.G8;
            _hashArray[2] = (byte)color.B8;
            _hashArray[3] = (byte)color.A8;

            int hash = BitConverter.ToInt32(_hashArray, 0);

            MaterialKey newKey = new()
            {
                Shaded = shaded,
                ColorHash = hash,
                Scale = textureScale
            };

            if (_colorToMaterial.TryGetValue(newKey, out var result))
            {
                return result;
            }

            CreateMaterial(color.ToString(), color);

            StandardMaterial3D material = GetMaterial(color.ToString());

            if (shaded)
            {
                material.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
                material.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;

                material.AlbedoColor = color;
                material.AlbedoTexture = Gizmos.GridTexture.Cache;

                material.CullMode = BaseMaterial3D.CullModeEnum.Back;

                material.Uv1Scale = new Vector3(textureScale, textureScale, textureScale);
            }
            else
            {
                material.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
                material.DisableAmbientLight = true;
                material.AlbedoColor = color;

                if (_hashArray[3] == 255)
                {
                    material.Transparency = BaseMaterial3D.TransparencyEnum.Disabled;
                }
                else
                {
                    material.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
                }
            }

            material.DisableFog = true;
            material.SpecularMode = BaseMaterial3D.SpecularModeEnum.Disabled;
            material.DisableSpecularOcclusion = true;

            _colorToMaterial[newKey] = material;

            return material;
        }

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
            _currentGizmo.AddLines(Gizmos.CreateBox(size, origin), GetMaterial(color, false));
        }

        public void DrawBoxWireframe(Aabb box, Color color)
        {
            (Vector3 center, Vector3 size) = Gizmos.DecomposeBox(box);
            DrawBoxWireframe(center, size, color);
        }

        public void DrawBox(Vector3 origin, Vector3 size, Color color, bool shaded, float textureScale)
        {
            Transform3D transform = new()
            {
                Basis = Basis.Identity,
                Origin = origin
            };
            _currentGizmo.AddMesh(Gizmos.GetBoxMesh(size), GetMaterial(color, shaded, textureScale), transform);
        }

        public void DrawBox(Aabb box, Color color, bool shaded, float textureScale)
        {
            (Vector3 origin, Vector3 size) = Gizmos.DecomposeBox(box);
            Transform3D transform = new()
            {
                Basis = Basis.Identity,
                Origin = origin
            };
            _currentGizmo.AddMesh(Gizmos.GetBoxMesh(size), GetMaterial(color, shaded, textureScale), transform);
        }

        public void DrawPath(Vector3[] points, Color color)
        {
            _currentGizmo.AddLines(Gizmos.CreatePath(points), GetMaterial(color, false));
        }

        public void DrawMesh(Mesh mesh, Vector3 origin, Color color, bool shaded, float textureScale)
        {
            Transform3D transform = new()
            {
                Basis = Basis.Identity,
                Origin = origin
            };
            _currentGizmo.AddMesh(mesh, GetMaterial(color, shaded, textureScale), transform);
        }

        public void DrawArrow(Vector3 origin, Vector3 end, Color color, bool shaded, float textureScale)
        {
            Transform3D transform = new()
            {
                Origin = origin,
                Basis = Basis.Identity
            };

            transform = transform.LookingAt(end, Vector3.Up);

            _currentGizmo.AddMesh(Gizmos.ArrowMesh.Cache, GetMaterial(color, shaded, textureScale), transform);
        }
    }
}

#endif
