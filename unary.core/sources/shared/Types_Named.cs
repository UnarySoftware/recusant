using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Godot;

namespace Unary.Core
{
    public partial class Types
    {
        private static Dictionary<string, Type> _namedTypes = [];
        private static Dictionary<string, Dictionary<Type, Dictionary<string, Attribute>>> _fieldAttributes;

        public struct ClassData
        {
            public string Type;
            public string BaseType;
            public string Icon;
            public string Language;
            public string Path;
            public string Uid;
        }

        // Has to be exposed for TypeResource inspector
        // TODO Maybe rewrite this with a better accessor API some day

        public static Dictionary<string, ClassData> TypeToData
        {
            get
            {
                BuildClassData();
                return field;
            }
            private set;
        }

        public static Dictionary<string, ClassData> UidToData
        {
            get
            {
                BuildClassData();
                return field;
            }
            private set;
        }

        private static bool _builtData = false;

        private static void BuildClassData()
        {
            if (_builtData)
            {
                return;
            }

            _builtData = true;

            TypeToData = [];
            UidToData = [];

            var data = ProjectSettings.Singleton.GetGlobalClassList();

            foreach (var classEntry in data)
            {
                ClassData newData = new()
                {
                    Type = classEntry["class"].AsString(),
                    BaseType = classEntry["base"].AsString(),
                    Icon = classEntry["icon"].AsString(),
                    Language = classEntry["language"].AsString(),
                    Path = classEntry["path"].AsString(),
                };

                newData.Uid = ResourceUid.Singleton.IdToText(ResourceLoader.Singleton.GetResourceUid(newData.Path));

                TypeToData[newData.Type] = newData;
                UidToData[newData.Uid] = newData;
            }

            var classList = ClassDB.Singleton.GetClassList();

            foreach (var classEntry in classList)
            {
                if (TypeToData.ContainsKey(classEntry))
                {
                    continue;
                }

                ClassData newData = new()
                {
                    Type = classEntry,
                    BaseType = ClassDB.Singleton.GetParentClass(classEntry),
                    Icon = string.Empty, // Build-in
                    Language = string.Empty, // Build-in
                    Path = string.Empty, // Build-in
                    Uid = string.Empty // Build-in
                };

                TypeToData[newData.Type] = newData;
            }
        }

        public static bool Initialize(Func<object, string, bool> reporter)
        {
            if (!InitializeBase(reporter))
            {
                return false;
            }

            BuildClassData();

            _namedTypes = [];

            Dictionary<Type, Type> collisions = [];

            var types = GetTypes();

            foreach (var type in types)
            {
                string name = type.Name;

                if (_namedTypes.TryGetValue(name, out var collision))
                {
                    collisions.Add(type, collision);
                    continue;
                }

                _namedTypes[name] = type;
            }

            if (collisions.Count > 0)
            {
                StringBuilder errorBuilder = new();

                errorBuilder.Append("Detected colliding types with same names using [GlobalClass] attribute:\n\n");

                foreach (var collision in collisions)
                {
                    errorBuilder.Append(collision.Key.FullName).Append(" => ").Append(collision.Value.FullName).Append('\n');
                }

                errorBuilder.Append("\nThis will cause unpredictable issues everywhere within the engine.");

                return Reporter(typeof(Types), errorBuilder.ToString());
            }

            return true;
        }

        public static void Deinitialize()
        {
            DeinitializeBase();
            TypeToData = null;
            UidToData = null;
            _namedTypes = null;
            _fieldAttributes = null;
            Reporter = null;
        }

        public static Type GetTypeOfName(string name)
        {
            if (UidToData.TryGetValue(name, out var data))
            {
                name = data.Type;
            }

            if (_namedTypes.TryGetValue(name, out var resultType))
            {
                return resultType;
            }
            return null;
        }

        private static void InitializeFieldAttributes()
        {
            if (_fieldAttributes == null)
            {
                _fieldAttributes = [];
            }
            else
            {
                return;
            }

            var types = GetTypes();

            foreach (var type in types)
            {
                if (!_fieldAttributes.TryGetValue(type.Name, out var entries))
                {
                    entries = [];
                    _fieldAttributes.Add(type.Name, entries);
                }

                foreach (var field in type.GetFields())
                {
                    List<Attribute> attributes = [];

                    foreach (var attribute in field.GetCustomAttributes())
                    {
                        attributes.Add(attribute);
                    }

                    if (attributes.Count == 0)
                    {
                        continue;
                    }

                    foreach (var attribute in attributes)
                    {
                        Type attributeType = attribute.GetType();

                        if (!entries.TryGetValue(attributeType, out var dictionary))
                        {
                            dictionary = [];
                            entries.Add(attributeType, dictionary);
                        }

                        dictionary.Add(field.Name, attribute);
                    }
                }

            }
        }

        public static Dictionary<string, Attribute> GetTypesWithAttributedFields(string type, Type attributeType)
        {
            InitializeFieldAttributes();

            if (!_fieldAttributes.TryGetValue(type, out var entries))
            {
                return null;
            }

            if (!entries.TryGetValue(attributeType, out var attributes))
            {
                return null;
            }

            return attributes;
        }
    }
}