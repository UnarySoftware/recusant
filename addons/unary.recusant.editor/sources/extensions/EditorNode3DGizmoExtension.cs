#if TOOLS

using Godot;
using System.Runtime.CompilerServices;

namespace Unary.Recusant.Editor
{
    public static class EditorNode3DGizmoExtensions
    {
        private static Vector3[] CreateBox(Vector3 size, Vector3 origin)
        {
            Vector3[] lines =
            [
                new(-size.X, -size.Y, -size.Z),
                new(size.X, -size.Y, -size.Z),
                new(size.X, -size.Y, -size.Z),
                new(size.X, size.Y, -size.Z),
                new(size.X, size.Y, -size.Z),
                new(-size.X, size.Y, -size.Z),
                new(-size.X, size.Y, -size.Z),
                new(-size.X, -size.Y, -size.Z),
                new(-size.X, -size.Y, size.Z),
                new(size.X, -size.Y, size.Z),
                new(size.X, -size.Y, size.Z),
                new(size.X, size.Y, size.Z),
                new(size.X, size.Y, size.Z),
                new(-size.X, size.Y, size.Z),
                new(-size.X, size.Y, size.Z),
                new(-size.X, -size.Y, size.Z),
                new(-size.X, -size.Y, -size.Z),
                new(-size.X, -size.Y, size.Z),
                new(size.X, -size.Y, -size.Z),
                new(size.X, -size.Y, size.Z),
                new(size.X, size.Y, -size.Z),
                new(size.X, size.Y, size.Z),
                new(-size.X, size.Y, -size.Z),
                new(-size.X, size.Y, size.Z),
            ];

            for (int i = 0; i < lines.Length; i++)
            {
                lines[i].X += origin.X;
                lines[i].Y += origin.Y;
                lines[i].Z += origin.Z;
            }

            return lines;
        }

        public static void AddBoxWithSize(this EditorNode3DGizmo value, Vector3 origin, Vector3 size, Material material)
        {
            value.AddLines(CreateBox(size, origin), material);
        }

        public static void AddAabbBox(this EditorNode3DGizmo value, Aabb box, Material material)
        {
            value.AddBoxFromCorners(box.Position, box.End, material);
        }

        public static void AddBoxFromCorners(this EditorNode3DGizmo value, Vector3 largest, Vector3 smallest, Material material)
        {
            Vector3 center = (largest + smallest) / 2.0f;
            Vector3 size = (largest - smallest) / 2.0f;

            value.AddLines(CreateBox(size, center), material);
        }

        public static void AddLinePath(this EditorNode3DGizmo value, Vector3[] points, Material material)
        {
            Vector3[] lines = new Vector3[(points.Length - 1) * 2];

            int readIndex = 0;
            int writeIndex = 0;

            while (true)
            {
                lines[writeIndex] = points[readIndex];
                writeIndex++;
                lines[writeIndex] = points[readIndex + 1];
                writeIndex++;

                readIndex++;

                if (readIndex == points.Length - 1)
                {
                    break;
                }
            }

            value.AddLines(lines, material);
        }
    }
}

#endif
