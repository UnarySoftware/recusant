using Godot;
using Godot.Collections;

namespace Unary.Core
{
    public static class GodotDictionaryExtensions
    {
        public static void MakeReadOnly(this Dictionary property, params StringName[] names)
        {
            StringName propertyName = property["name"].AsStringName();

            foreach (var name in names)
            {
                if (propertyName == name)
                {
                    var usageFlags = property["usage"].As<PropertyUsageFlags>();
                    usageFlags |= PropertyUsageFlags.ReadOnly;
                    property["usage"] = (int)usageFlags;
                    break;
                }
            }
        }

        public static void MakeHidden(this Dictionary property, params StringName[] names)
        {
            StringName propertyName = property["name"].AsStringName();

            foreach (var name in names)
            {
                if (propertyName == name)
                {
                    var usageFlags = property["usage"].As<PropertyUsageFlags>();
                    usageFlags &= ~PropertyUsageFlags.Editor;
                    property["usage"] = (int)usageFlags;
                    break;
                }
            }
        }
    }
}
