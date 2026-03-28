using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Godot;

namespace Unary.Core
{
    public partial class Types
    {
        private static Type[] _types;
        private static Dictionary<Type, HashSet<Type>> _attributeTypes;
        private static Dictionary<Type, HashSet<Attribute>> _typesToAttributes;
        private static Dictionary<Type, HashSet<Type>> _baseTypes;

        protected static Func<object, string, bool> Reporter;

        protected static bool InitializeBase(Func<object, string, bool> reporter)
        {
            Reporter = reporter;

            return InitializeTypes();
        }

        private static bool InitializeTypes()
        {
            HashSet<Type> result = [];

            var ourTypes = typeof(Types).Assembly.GetTypes();

            foreach (var type in ourTypes)
            {
                result.Add(type);
            }

            var godotTypes = typeof(GodotObject).Assembly.GetTypes();

            foreach (var type in godotTypes)
            {
                result.Add(type);
            }

            _types = [.. result];

            return true;
        }

        public static Type[] GetTypes()
        {
            return _types;
        }

        private static void InitializeAttributes()
        {
            if (_attributeTypes != null)
            {
                return;
            }

            _attributeTypes = [];
            _typesToAttributes = [];

            foreach (var type in _types)
            {
                if (!_typesToAttributes.TryGetValue(type, out var attributes))
                {
                    attributes = [];
                    _typesToAttributes.Add(type, attributes);
                }

                foreach (var attribute in type.GetCustomAttributes())
                {
                    Type attributeType = attribute.GetType();
                    if (!_attributeTypes.TryGetValue(attributeType, out var entries))
                    {
                        entries = [];
                        _attributeTypes.Add(attributeType, entries);
                    }
                    entries.Add(type);
                    attributes.Add(attribute);
                }
            }
        }

        public static HashSet<Type> GetTypesWithAttribute(Type attributeType)
        {
            InitializeAttributes();

            if (_attributeTypes.TryGetValue(attributeType, out var entries))
            {
                return entries;
            }
            return null;
        }

        public static HashSet<Attribute> GetTypeAttributes(Type type)
        {
            InitializeAttributes();

            if (_typesToAttributes.TryGetValue(type, out var entries))
            {
                return entries;
            }
            return null;
        }

        public static T GetTypeAttribute<T>(Type type) where T : Attribute
        {
            Type attributeType = typeof(T);

            HashSet<Attribute> attributes = GetTypeAttributes(type);

            foreach (var attribute in attributes)
            {
                if (attribute.GetType() == attributeType)
                {
                    return (T)attribute;
                }
            }

            return null;
        }

        // Lazy-evaluate base types on demand, since this is 
        // an O(N^N) operation if we were to do this by default on boot
        public static HashSet<Type> GetTypesOfBase(Type baseType)
        {
            _baseTypes ??= [];

            if (_baseTypes.TryGetValue(baseType, out var entries))
            {
                return entries;
            }

            HashSet<Type> result = [];

            foreach (var type in _types)
            {
                if (baseType.IsAssignableFrom(type))
                {
                    result.Add(type);
                }
            }

            _baseTypes.Add(baseType, result);
            return result;
        }

        protected static void DeinitializeBase()
        {
            _types = null;
            _attributeTypes = null;
            _typesToAttributes = null;
            _baseTypes = null;
        }
    }
}
