using Godot;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Unary.Core
{
    public static class JsonConverters
    {
        public static List<JsonConverter> Value { get; } =
        [
            new GodotJson<Variant>(),
            new GodotJson<Vector2>(),
            new GodotJson<Vector2I>(),
            new GodotJson<Rect2>(),
            new GodotJson<Rect2I>(),
            new GodotJson<Vector3>(),
            new GodotJson<Vector3I>(),
            new GodotJson<Transform2D>(),
            new GodotJson<Vector4>(),
            new GodotJson<Vector4I>(),
            new GodotJson<Plane>(),
            new GodotJson<Quaternion>(),
            new GodotJson<Aabb>(),
            new GodotJson<Basis>(),
            new GodotJson<Transform3D>(),
            new GodotJson<Projection>(),
            new GodotJson<Color>(),
        ];
    }
}
