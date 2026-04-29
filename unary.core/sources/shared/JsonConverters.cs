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
            new GodotJson<Vector3>(),
        ];
    }
}
