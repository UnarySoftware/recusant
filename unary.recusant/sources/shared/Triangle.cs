using Godot;

namespace Unary.Recusant
{
    public static class Triangle
    {
        public static Vector3 GetBarycentricCoords(Vector3 a, Vector3 b, Vector3 c, Vector3 p)
        {
            Vector3 barycentric = Geometry3D.GetTriangleBarycentricCoords(p, a, b, c);

            if (barycentric.X >= -Mathf.Epsilon && barycentric.Y >= -Mathf.Epsilon && barycentric.Z >= -Mathf.Epsilon &&
                barycentric.X + barycentric.Y + barycentric.Z <= 1.0f + Mathf.Epsilon)
            {
                return barycentric;
            }
            else
            {
                return default;
            }
        }

        public static float GetPointDistance(Vector3 a, Vector3 b, Vector3 c, Vector3 p)
        {
            Plane plane = new(a, b, c);
            return Mathf.Abs(plane.DistanceTo(p));
        }

        public static bool IntersectsBounds(Vector3 v0, Vector3 v1, Vector3 v2, Aabb aabb)
        {
            Vector3 center = aabb.Position + aabb.Size * 0.5f;
            Vector3 halfSize = aabb.Size * 0.5f;

            v0 -= center;
            v1 -= center;
            v2 -= center;

            Vector3 e0 = v1 - v0;
            Vector3 e1 = v2 - v1;
            Vector3 e2 = v0 - v2;

            float p0;
            float p1;
            float p2;
            float r;

            p0 = e0.Z * v0.Y - e0.Y * v0.Z;
            p2 = e0.Z * v2.Y - e0.Y * v2.Z;
            r = halfSize.Y * Mathf.Abs(e0.Z) + halfSize.Z * Mathf.Abs(e0.Y);

            if (Mathf.Max(-Mathf.Max(p0, p2), Mathf.Min(p0, p2)) > r)
            {
                return false;
            }

            p0 = -e0.Z * v0.X + e0.X * v0.Z;
            p2 = -e0.Z * v2.X + e0.X * v2.Z;
            r = halfSize.X * Mathf.Abs(e0.Z) + halfSize.Z * Mathf.Abs(e0.X);

            if (Mathf.Max(-Mathf.Max(p0, p2), Mathf.Min(p0, p2)) > r)
            {
                return false;
            }

            p0 = e0.Y * v0.X - e0.X * v0.Y;
            p2 = e0.Y * v2.X - e0.X * v2.Y;
            r = halfSize.X * Mathf.Abs(e0.Y) + halfSize.Y * Mathf.Abs(e0.X);

            if (Mathf.Max(-Mathf.Max(p0, p2), Mathf.Min(p0, p2)) > r)
            {
                return false;
            }

            p0 = e1.Z * v0.Y - e1.Y * v0.Z;
            p2 = e1.Z * v2.Y - e1.Y * v2.Z;
            r = halfSize.Y * Mathf.Abs(e1.Z) + halfSize.Z * Mathf.Abs(e1.Y);

            if (Mathf.Max(-Mathf.Max(p0, p2), Mathf.Min(p0, p2)) > r)
            {
                return false;
            }

            p0 = -e1.Z * v0.X + e1.X * v0.Z;
            p2 = -e1.Z * v2.X + e1.X * v2.Z;
            r = halfSize.X * Mathf.Abs(e1.Z) + halfSize.Z * Mathf.Abs(e1.X);

            if (Mathf.Max(-Mathf.Max(p0, p2), Mathf.Min(p0, p2)) > r)
            {
                return false;
            }

            p0 = e1.Y * v0.X - e1.X * v0.Y;
            p2 = e1.Y * v2.X - e1.X * v2.Y;
            r = halfSize.X * Mathf.Abs(e1.Y) + halfSize.Y * Mathf.Abs(e1.X);

            if (Mathf.Max(-Mathf.Max(p0, p2), Mathf.Min(p0, p2)) > r)
            {
                return false;
            }

            p0 = e2.Z * v0.Y - e2.Y * v0.Z;
            p1 = e2.Z * v1.Y - e2.Y * v1.Z;
            r = halfSize.Y * Mathf.Abs(e2.Z) + halfSize.Z * Mathf.Abs(e2.Y);

            if (Mathf.Max(-Mathf.Max(p0, p1), Mathf.Min(p0, p1)) > r)
            {
                return false;
            }

            p0 = -e2.Z * v0.X + e2.X * v0.Z;
            p1 = -e2.Z * v1.X + e2.X * v1.Z;
            r = halfSize.X * Mathf.Abs(e2.Z) + halfSize.Z * Mathf.Abs(e2.X);

            if (Mathf.Max(-Mathf.Max(p0, p1), Mathf.Min(p0, p1)) > r)
            {
                return false;
            }

            p0 = e2.Y * v0.X - e2.X * v0.Y;
            p1 = e2.Y * v1.X - e2.X * v1.Y;
            r = halfSize.X * Mathf.Abs(e2.Y) + halfSize.Y * Mathf.Abs(e2.X);

            if (Mathf.Max(-Mathf.Max(p0, p1), Mathf.Min(p0, p1)) > r)
            {
                return false;
            }

            if (Mathf.Max(v0.X, Mathf.Max(v1.X, v2.X)) < -halfSize.X || Mathf.Min(v0.X, Mathf.Min(v1.X, v2.X)) > halfSize.X)
            {
                return false;
            }

            if (Mathf.Max(v0.Y, Mathf.Max(v1.Y, v2.Y)) < -halfSize.Y || Mathf.Min(v0.Y, Mathf.Min(v1.Y, v2.Y)) > halfSize.Y)
            {
                return false;
            }

            if (Mathf.Max(v0.Z, Mathf.Max(v1.Z, v2.Z)) < -halfSize.Z || Mathf.Min(v0.Z, Mathf.Min(v1.Z, v2.Z)) > halfSize.Z)
            {
                return false;
            }

            Vector3 normal = e0.Cross(e1);

            float d = normal.Dot(v0);

            r = halfSize.X * Mathf.Abs(normal.X)
              + halfSize.Y * Mathf.Abs(normal.Y)
              + halfSize.Z * Mathf.Abs(normal.Z);

            if (Mathf.Abs(d) > r)
            {
                return false;
            }

            return true;
        }

        public static float GetSurfaceArea(Vector3 p1, Vector3 p2, Vector3 p3)
        {
            Vector3 sideA = p2 - p1;
            Vector3 sideB = p3 - p1;
            Vector3 crossProduct = sideA.Cross(sideB);
            float magnitude = crossProduct.Length();
            return magnitude * 0.5f;
        }
    }
}
