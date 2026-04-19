#if TOOLS

using Godot;

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
            int segCount = points.Length - 1;
            int writeCount = 16;

            Vector3[] lines = new Vector3[(segCount) * writeCount];

            int readIndex = 0;
            int writeIndex = 0;

            while (true)
            {
                Vector3 start = points[readIndex];
                Vector3 end = points[readIndex + 1];

                lines[writeIndex++] = start;
                lines[writeIndex++] = end;

                Vector3 mid = (start + end) * 0.5f;
                Vector3 forward = (end - start).Normalized();

                Vector3 up = Mathf.Abs(forward.Dot(Vector3.Up)) < 0.99f
                                ? forward.Cross(Vector3.Up).Normalized()
                                : forward.Cross(Vector3.Right).Normalized();

                Vector3 right = forward.Cross(up).Normalized();

                float distance = start.DistanceTo(end);

                float s = distance * 0.1f;

                Vector3 arrowTip = mid + forward * s;
                Vector3 arrowBase = mid - forward * s;

                lines[writeIndex++] = arrowBase;
                lines[writeIndex++] = arrowTip;

                lines[writeIndex++] = arrowTip;
                lines[writeIndex++] = arrowBase + right * s;

                lines[writeIndex++] = arrowTip;
                lines[writeIndex++] = arrowBase - right * s;

                lines[writeIndex++] = arrowBase + right * s;
                lines[writeIndex++] = arrowBase - right * s;

                lines[writeIndex++] = arrowTip;
                lines[writeIndex++] = arrowBase + up * s;

                lines[writeIndex++] = arrowTip;
                lines[writeIndex++] = arrowBase - up * s;

                lines[writeIndex++] = arrowBase + up * s;
                lines[writeIndex++] = arrowBase - up * s;

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
