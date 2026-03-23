using System;
using System.Collections.Generic;
using Godot;

namespace Unary.Core
{
    public static class GodotObjectExtensions
    {
        public static T Filter<T>(this GodotObject @object, T target, Type type) where T : class
        {
            if (!Engine.IsEditorHint())
            {
                return target;
            }

            if (target == null)
            {
                return null;
            }

            if (target is BaseSelectorResource typeResource)
            {
                typeResource.BaseType = type;
            }
            else if (target is BaseSelectorResource[] typeResourceArray)
            {
                foreach (var targetType in typeResourceArray)
                {
                    if (targetType == null)
                    {
                        continue;
                    }

                    targetType.BaseType = type;
                }
            }
            else if (target is Godot.Collections.Array<BaseSelectorResource> typeResourceCollectionArray)
            {
                foreach (var targetType in typeResourceCollectionArray)
                {
                    if (targetType == null)
                    {
                        continue;
                    }

                    targetType.BaseType = type;
                }
            }
            else if (target.GetType().IsGenericType && target.GetType().GetGenericTypeDefinition() == typeof(Godot.Collections.Dictionary<,>))
            {
                var keysProperty = target.GetType().GetProperty("Keys");
                var valuesProperty = target.GetType().GetProperty("Values");

                var keys = keysProperty.GetValue(target, null);
                var values = valuesProperty.GetValue(target, null);

                if (keys is ICollection<BaseSelectorResource> keysArray)
                {
                    foreach (var key in keysArray)
                    {
                        if (key == null)
                        {
                            continue;
                        }
                        key.BaseType = type;
                    }
                }

                if (values is ICollection<BaseSelectorResource> valuesArray)
                {
                    foreach (var value in valuesArray)
                    {
                        if (value == null)
                        {
                            continue;
                        }
                        value.BaseType = type;
                    }
                }
            }

            return target;
        }
    }
}
