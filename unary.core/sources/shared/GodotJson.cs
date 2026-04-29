using Godot;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Unary.Core
{
    public class GodotJson<[MustBeVariant] T> : JsonConverter<T>
    {
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return GD.StrToVar(reader.GetString()).As<T>();
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(GD.VarToStr(Variant.From(value)));
        }
    }
}
