#if TOOLS

using Godot;
using System.Collections.Generic;

namespace Unary.Recusant
{
    public static class Gizmos
    {
        private static Dictionary<Vector3, Mesh> _boxMeshes = [];

        public static Mesh GetBoxMesh(Vector3 size)
        {
            if (_boxMeshes.TryGetValue(size, out var result))
            {
                return result;
            }

            BoxMesh mesh = new()
            {
                Size = size
            };

            _boxMeshes[size] = mesh;
            return mesh;
        }

        public static Vector3[] CreateBox(Vector3 size, Vector3 origin)
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
                lines[i] += origin;
            }

            return lines;
        }

        public static (Vector3 center, Vector3 size) DecomposeBox(Aabb box)
        {
            Vector3 corner1 = box.Position;
            Vector3 corner2 = box.Position + box.Size;

            Vector3 center = (corner1 + corner2) / 2.0f;
            Vector3 size = (corner2 - corner1) / 2.0f;

            return (center, size);
        }

        public static Vector3[] CreatePath(Vector3[] points)
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

            return lines;
        }
    }
}

#endif
