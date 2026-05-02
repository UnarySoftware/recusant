#if TOOLS

using Godot;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Unary.Core
{
    public class EditorSettingManager
    {
        private static readonly List<EditorSettingBase> _entries = [];
        private static readonly List<FieldInfo> _settingFields = [];

        public static IEnumerable<EditorSettingBase> GetEntries()
        {
            Initialize();
            return _entries;
        }

        private static void InitializeVariables(Type[] types)
        {
            foreach (var type in types)
            {
                FieldInfo[] properties = type.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);

                foreach (var property in properties)
                {
                    if (property.FieldType != typeof(EditorSettingVariableBase) &&
                    property.FieldType.BaseType != typeof(EditorSettingVariableBase))
                    {
                        continue;
                    }

                    _settingFields.Add(property);

                    EditorSettingVariableBase fieldBase = (EditorSettingVariableBase)property.GetValue(null);

                    string groupVisual;
                    string groupPath;

                    if (fieldBase.Group != null &&
                        fieldBase.Group != string.Empty)
                    {
                        groupVisual = fieldBase.Group;
                        groupPath = fieldBase.Group.ToPath();
                    }
                    else
                    {
                        groupVisual = type.Name.ToHumanReadable();
                        groupPath = type.Name.Trim('_').ToSnakeCase();
                    }

                    string nameVisual;
                    string namePath;

                    if (fieldBase.Name != null &&
                        fieldBase.Name != string.Empty)
                    {
                        nameVisual = fieldBase.Name;
                        namePath = fieldBase.Name.ToPath();
                    }
                    else
                    {
                        nameVisual = property.Name.ToHumanReadable();
                        namePath = property.Name.Trim('_').ToSnakeCase();
                    }

                    fieldBase.ModId = type.GetModId();
                    fieldBase.Group = groupVisual;
                    fieldBase.Name = nameVisual;

                    fieldBase.Path = fieldBase.ModId.ToSnakeCase().Replace(".", "_");
                    fieldBase.Path += '/' + groupPath + '/' + namePath;

                    EditorSettingSaver.Singleton.SetVariable(fieldBase.Path, fieldBase.GetField(), true);

                    fieldBase.Wrapper = new()
                    {
                        Variable = fieldBase
                    };

                    _entries.Add(fieldBase);
                }
            }
        }

        private static void InitializeActions(Type[] types)
        {
            foreach (var type in types)
            {
                MethodInfo[] methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Static);

                foreach (var method in methods)
                {
                    IEnumerable<Attribute> attributes = method.GetCustomAttributes();

                    foreach (var attribute in attributes)
                    {
                        if (attribute is not EditorSettingActionAttribute)
                        {
                            continue;
                        }

                        if (method.GetParameters().Length > 0)
                        {
                            continue;
                        }

                        EditorSettingActionAttribute casted = (EditorSettingActionAttribute)attribute;

                        string groupVisual;

                        if (casted.Group != "")
                        {
                            groupVisual = casted.Group;
                        }
                        else
                        {
                            groupVisual = type.Name.ToHumanReadable();
                        }

                        string nameVisual;

                        if (casted.Name != "")
                        {
                            nameVisual = casted.Name;
                        }
                        else
                        {
                            nameVisual = method.Name.ToHumanReadable();
                        }

                        EditorSettingAction newAction = new()
                        {
                            MethodInfo = method,
                            ModId = type.GetModId(),
                            Group = groupVisual,
                            Name = nameVisual,
                            Description = casted.Description
                        };

                        _entries.Add(newAction);
                    }
                }
            }
        }

        private static bool _initialized = false;

        public static void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;

            Type[] types = typeof(EditorSettingManager).Assembly.GetTypes();

            InitializeVariables(types);
            InitializeActions(types);
        }

        public static void Deinitialize()
        {
            if (!_initialized)
            {
                return;
            }

            foreach (var entry in _entries)
            {
                if (entry.Wrapper != null)
                {
                    entry.Wrapper.Variable = null;
                    entry.Wrapper.Free();
                    entry.Wrapper = null;
                }

                if (entry is EditorSettingVariableBase variable)
                {
                    variable.Inspector = null;
                }
                else if (entry is EditorSettingAction action)
                {
                    action.MethodInfo = null;
                }
            }

            _settingFields.Clear();
            _entries.Clear();
            _initialized = false;
        }
    }
}

#endif
