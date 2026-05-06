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
        private static readonly StringName color = nameof(color);

        private static readonly LazyResource<ShaderMaterial> _unshadedMaterial = new("uid://5kb2d1tm58yv");
        private static readonly LazyResource<ShaderMaterial> _shadedMaterial = new("uid://d2nc0am547c14");

        public void Init()
        {
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
            Transform = Transform3D.Identity;
            SetInstanceShaderParameter(color, Colors.White);
        }

        private void SetMaterial(bool shaded, Color newColor, float textureScale)
        {
            Color targetColor = new();

            if (shaded)
            {
                MaterialOverride = _shadedMaterial.Cache;
                targetColor.R = newColor.R;
                targetColor.G = newColor.G;
                targetColor.B = newColor.B;
                targetColor.A8 = (int)textureScale;
            }
            else
            {
                MaterialOverride = _unshadedMaterial.Cache;
                targetColor = newColor;
            }

            SetInstanceShaderParameter(color, targetColor);
        }

        public void IDrawBox(Vector3 origin, Vector3 size, Color newColor, bool shaded, float textureScale)
        {
            SetMaterial(shaded, newColor, textureScale);
            Mesh = Gizmos.GetBoxMesh(size);
            Position = origin;
        }

        public void IDrawBox(Aabb box, Color newColor, bool shaded, float textureScale)
        {
            (Vector3 center, Vector3 size) = Gizmos.DecomposeBox(box);
            SetMaterial(shaded, newColor, textureScale);
            Mesh = Gizmos.GetBoxMesh(size);
            Position = center;
        }

        public void IDrawBoxWireframe(Aabb box, Color color)
        {
            (Vector3 center, Vector3 size) = Gizmos.DecomposeBox(box);
            IDrawBoxWireframe(center, size, color);
        }

        public void IDrawBoxWireframe(Vector3 origin, Vector3 size, Color newColor)
        {
            Vector3[] points = Gizmos.CreateBox(size, Vector3.Zero);

            tool.Clear();
            tool.Begin(Mesh.PrimitiveType.Lines);

            foreach (var point in points)
            {
                tool.SetColor(newColor);
                tool.AddVertex(point);
            }

            Mesh = tool.Commit();
            Position = origin;
            SetMaterial(false, newColor, 1.0f);
        }

        public void IDrawPath(Vector3[] points, Color newColor)
        {
            Vector3[] result = Gizmos.CreatePath(points);

            tool.Clear();
            tool.Begin(Mesh.PrimitiveType.Lines);

            foreach (var point in result)
            {
                tool.SetColor(newColor);
                tool.AddVertex(point);
            }

            Mesh = tool.Commit();
            SetMaterial(false, newColor, 1.0f);
        }

        public void IDrawMesh(Mesh mesh, Vector3 origin, Color newColor, bool shaded, float textureScale)
        {
            Mesh = mesh;
            SetMaterial(shaded, newColor, textureScale);
            Position = origin;
        }

        public void IDrawArrow(Vector3 origin, Vector3 end, Color newColor, bool shaded, float textureScale)
        {
            Transform3D transform = new()
            {
                Origin = origin,
                Basis = Basis.Identity
            };

            transform = transform.LookingAt(end, Vector3.Up);

            Mesh = Gizmos.ArrowMesh.Cache;
            SetMaterial(shaded, newColor, textureScale);
            Transform = transform;
        }
    }
}

#endif
