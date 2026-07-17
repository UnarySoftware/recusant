using Godot;
using Godot.Collections;

namespace Unary.Core
{
    public static class GodotDictionaryExtensions
    {
        private static bool Matches(Dictionary property, StringName[] names)
        {
            StringName propertyName = property["name"].AsStringName();

            foreach (var name in names)
            {
                if (propertyName == name)
                {
                    return true;
                }
            }

            return false;
        }

        public static void MakeReadOnly(this Dictionary property, params StringName[] names)
        {
            if (!Matches(property, names))
            {
                return;
            }

            property["usage"] = (int)(property["usage"].As<PropertyUsageFlags>() | PropertyUsageFlags.ReadOnly);
        }

        public static void MakeHidden(this Dictionary property, params StringName[] names)
        {
            if (!Matches(property, names))
            {
                return;
            }

            property["usage"] = (int)(property["usage"].As<PropertyUsageFlags>() & ~PropertyUsageFlags.Editor);
        }

        public static void MakeNone(this Dictionary property, params StringName[] names)
        {
            if (!Matches(property, names))
            {
                return;
            }

            property["usage"] = (int)PropertyUsageFlags.None;
        }
    }
}
