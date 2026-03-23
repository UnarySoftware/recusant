using System;
using Godot;
using Unary.Core.Editor;

namespace Unary.Core
{
    public class EditorSettingVariableBase : EditorSettingBase
    {
#if TOOLS
        public EditorProperty Inspector;
#endif
        public PropertyHint PropertyHint = PropertyHint.None;
        public uint PropertyUsageFlags = (uint)Godot.PropertyUsageFlags.Editor;
        public string HintText = nameof(EditorSettingWrapper.Value);

        public Func<Variant, Variant, Variant> CustomSetter;

        public Type GenericType;
        public string Path;
        private Variant _savedValue;
        public Variant VariantEditorDefault;
        public Variant VariantRuntimeDefault;
        public Variant VariantValue
        {
            get
            {
#if TOOLS
                if (Engine.Singleton.IsEditorHint())
                {
                    EditorSettings settings = EditorInterface.Singleton.GetEditorSettings();

                    if (!settings.HasSetting(Path))
                    {
                        return VariantEditorDefault;
                    }
                    return settings.GetSetting(Path);
                }
                else
                {
                    return VariantEditorDefault;
                }
#else
                return VariantRuntimeDefault;
#endif
            }
            set
            {
#if TOOLS
                if (Engine.Singleton.IsEditorHint())
                {
                    if (Path == null)
                    {
                        PluginLogger.Error(this, "Path was null, you are not supposed to be setting your settings so early!");
                        return;
                    }

                    Variant newValue = value;

                    if (CustomSetter != null)
                    {
                        newValue = CustomSetter(VariantValue, newValue);
                    }

                    EditorSettings settings = EditorInterface.Singleton.GetEditorSettings();
                    settings.SetSetting(Path, newValue);
                }
#endif
            }
        }

        public void Reset()
        {
            _savedValue = VariantValue;
            VariantValue = VariantEditorDefault;
        }

        public void Restore()
        {
            VariantValue = _savedValue;
        }
    }

}
