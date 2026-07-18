using System;

namespace Unary.Core
{
    public static class TypeExtensions
    {
        public static string GetModId(this Type source)
        {
            return (source.Namespace ?? "").Replace(".Editor", "").Trim('_');
        }

        public static bool IsEditorType(this Type source)
        {
            if (string.IsNullOrEmpty(source.Namespace))
            {
                return false;
            }

            return source.Namespace.ToLower().EndsWith(".editor");
        }
    }
}
