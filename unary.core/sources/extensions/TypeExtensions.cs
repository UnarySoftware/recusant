using System;

namespace Unary.Core
{
    public static class TypeExtensions
    {
        public static string GetModId(this Type source)
        {
            return (source.Namespace ?? "").Replace(".Editor", "").Trim('_');
        }
    }
}
