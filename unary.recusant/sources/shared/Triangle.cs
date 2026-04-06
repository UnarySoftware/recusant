using Godot;

namespace Unary.Recusant
{
    public static class Triangle
    {
        public static Vector3 GetPointInside(Vector3 a, Vector3 b, Vector3 c, Vector3 p)
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
    }
}
