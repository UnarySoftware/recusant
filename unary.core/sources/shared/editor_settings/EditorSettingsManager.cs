using System;
using System.Collections.Generic;
using System.Reflection;
using Godot;
#if TOOLS
using Unary.Core.Editor;
#endif

namespace Unary.Core
{
    public class EditorSettingsManager
    {

#if TOOLS
        private static readonly List<EditorSettingBase> _entries = [];

        public static IEnumerable<EditorSettingBase> GetEntries()
        {
            return _entries;
        }

        private static void InitializeVariables(Type[] types)
        {
            EditorSettings settings = EditorInterface.Singleton.GetEditorSettings();

            foreach (var type in types)
            {
                FieldInfo[] properties = type.GetFields(BindingFlags.NonPublic | BindingFlags.Static);

                foreach (var property in properties)
                {
                    if (property.FieldType != typeof(EditorSettingVariableBase) &&
                    property.FieldType.BaseType != typeof(EditorSettingVariableBase))
                    {
                        continue;
                    }

                    EditorSettingVariableBase fieldBase = (EditorSettingVariableBase)property.GetValue(null);

                    fieldBase.ModId = type.GetModId();
                    fieldBase.Group = type.Name.ToHumanReadable();
                    fieldBase.Name = property.Name.ToHumanReadable();

                    fieldBase.Path = fieldBase.ModId.ToSnakeCase().Replace(".", "_");
                    fieldBase.Path += '/' + type.Name.Trim('_').ToSnakeCase() + '/' + property.Name.Trim('_').ToSnakeCase();

                    if (!settings.HasSetting(fieldBase.Path))
                    {
                        settings.SetInitialValue(fieldBase.Path, fieldBase.VariantValue, true);
                    }

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

                        EditorSettingAction newAction = new()
                        {
                            MethodInfo = method,
                            ModId = type.GetModId(),
                            Group = type.Name.ToHumanReadable(),
                            Name = method.Name.ToHumanReadable()
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

            Type[] types = typeof(EditorSettingsManager).Assembly.GetTypes();

            InitializeVariables(types);
            InitializeActions(types);
        }

        public static void Reset()
        {
            foreach (var variable in _entries)
            {
                if (variable.Type == EditorSettingType.Variable)
                {
                    ((EditorSettingVariableBase)variable).Reset();
                }
            }
        }

        public static void Restore()
        {
            foreach (var variable in _entries)
            {
                if (variable.Type == EditorSettingType.Variable)
                {
                    ((EditorSettingVariableBase)variable).Restore();
                }
            }
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
            }

            _entries.Clear();
            _initialized = false;
        }
#endif
    }
}
