#if TOOLS

using Godot;
using System.Runtime.CompilerServices;
using Unary.Core;

namespace Unary.Recusant
{
    public class RuntimeGizmo : IPoolable
    {
        private static readonly SurfaceTool tool = new();

        private readonly MeshInstance3D _meshInstance;

        public RuntimeGizmo(Node root, ShaderMaterial material)
        {
            _meshInstance = new()
            {
                MaterialOverride = material,
                Visible = true,
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
            };

            root.AddChild(_meshInstance);
        }

        public void Aquire()
        {
            _meshInstance?.Visible = true;
        }

        public void Release()
        {
            _meshInstance?.Visible = false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetLine(Vector3 start, Vector3 finish, Color color)
        {
            tool.SetColor(color);
            tool.AddVertex(start);
            tool.SetColor(color);
            tool.AddVertex(finish);
        }

        public void SetBox(Vector3 size, Color color)
        {
            size.X /= 2.0f;
            size.Y /= 2.0f;
            size.Z /= 2.0f;

            tool.Clear();
            tool.Begin(Mesh.PrimitiveType.Lines);

            SetLine(new(-size.X, -size.Y, -size.Z), new(size.X, -size.Y, -size.Z), color);
            SetLine(new(size.X, -size.Y, -size.Z), new(size.X, size.Y, -size.Z), color);
            SetLine(new(size.X, size.Y, -size.Z), new(-size.X, size.Y, -size.Z), color);
            SetLine(new(-size.X, size.Y, -size.Z), new(-size.X, -size.Y, -size.Z), color);

            SetLine(new(-size.X, -size.Y, size.Z), new(size.X, -size.Y, size.Z), color);
            SetLine(new(size.X, -size.Y, size.Z), new(size.X, size.Y, size.Z), color);
            SetLine(new(size.X, size.Y, size.Z), new(-size.X, size.Y, size.Z), color);
            SetLine(new(-size.X, size.Y, size.Z), new(-size.X, -size.Y, size.Z), color);

            SetLine(new(-size.X, -size.Y, -size.Z), new(-size.X, -size.Y, size.Z), color);
            SetLine(new(size.X, -size.Y, -size.Z), new(size.X, -size.Y, size.Z), color);
            SetLine(new(size.X, size.Y, -size.Z), new(size.X, size.Y, size.Z), color);
            SetLine(new(-size.X, size.Y, -size.Z), new(-size.X, size.Y, size.Z), color);

            _meshInstance.Mesh = tool.Commit();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetPosition(Vector3 position)
        {
            _meshInstance.Position = position;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetRotation(Vector3 rotation)
        {
            _meshInstance.Rotation = rotation;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetPositionRotation(Vector3 position, Vector3 rotation)
        {
            SetPosition(position);
            SetRotation(rotation);
        }
    }
}

#endif
