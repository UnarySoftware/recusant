#if TOOLS

using Godot;
using Unary.Core;

namespace Unary.Recusant
{
    [Tool]
    [GlobalClass]
    public partial class RuntimeGizmo : MeshInstance3D, IPoolable
    {
        private static readonly SurfaceTool tool = new();

        private ShaderMaterial _material;

        public void Init(ShaderMaterial material)
        {
            MaterialOverride = material;
            _material = (ShaderMaterial)MaterialOverride;
            Visible = false;
            CastShadow = ShadowCastingSetting.Off;
        }

        public void Aquire()
        {
            Visible = true;
        }

        public void Release()
        {
            Visible = false;
            Position = Vector3.Zero;
            Rotation = Vector3.Zero;
        }

        private static StringName color = nameof(color);

        public void IDrawBox(Vector3 origin, Vector3 size, Color newColor)
        {
            Mesh = Gizmos.GetBoxMesh(size);
            _material.SetShaderParameter(color, newColor);
            Position = origin;
        }

        public void IDrawBox(Aabb box, Color newColor)
        {
            (Vector3 center, Vector3 size) = Gizmos.DecomposeBox(box);
            Mesh = Gizmos.GetBoxMesh(size);
            _material.SetShaderParameter(color, newColor);
            Position = center;
        }

        public void IDrawBoxWireframe(Aabb box, Color color)
        {
            (Vector3 center, Vector3 size) = Gizmos.DecomposeBox(box);
            IDrawBoxWireframe(center, size, color);
        }

        public void IDrawBoxWireframe(Vector3 origin, Vector3 size, Color color)
        {
            Vector3[] points = Gizmos.CreateBox(size, Vector3.Zero);

            tool.Clear();
            tool.Begin(Mesh.PrimitiveType.Lines);

            foreach (var point in points)
            {
                tool.SetColor(color);
                tool.AddVertex(point);
            }

            Mesh = tool.Commit();
            Position = origin;
        }

        public void IDrawPath(Vector3[] points, Color color)
        {
            Vector3[] result = Gizmos.CreatePath(points);

            tool.Clear();
            tool.Begin(Mesh.PrimitiveType.Lines);

            foreach (var point in result)
            {
                tool.SetColor(color);
                tool.AddVertex(point);
            }

            Mesh = tool.Commit();
        }
    }
}

#endif
