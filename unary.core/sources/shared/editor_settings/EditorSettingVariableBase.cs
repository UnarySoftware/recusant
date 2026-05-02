using Godot;
using System;
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

        public string Path;

        private bool _gotHash = false;

        public int Hash
        {
            get
            {
                if (!_gotHash)
                {
                    _gotHash = true;
                    field = Path.GetDeterministicHashCode();
                }

                return field;
            }
        }

        private bool _gotValue = false;

        private static void Dummy(EditorSettingVariableBase value)
        {

        }

        public Action<EditorSettingVariableBase> OnValueChanged = Dummy;

        public override Variant GetField()
        {
            if (!_gotValue)
            {
                _gotValue = true;
#if TOOLS
                if (Engine.Singleton.IsEditorHint())
                {
                    EditorSettingManager.Initialize();

                    EditorSettingSaver.Singleton.GetVariable(Path, out Variant result, out bool found);

                    if (!found)
                    {
                        _field = VariantEditorDefault;
                        return _field;
                    }

                    _field = result;
                }
                else
                {
                    _field = VariantEditorDefault;
                }
#else
                    _field = VariantRuntimeDefault;
#endif
            }

            return _field;
        }

        public override void SetField(Variant value)
        {
#if TOOLS
            Variant newValue = value;

            if (Engine.Singleton.IsEditorHint())
            {
                if (Path == null)
                {
                    PluginLogger.Error(this, "Path was null, you are not supposed to be setting your settings so early!");
                    return;
                }

                if (CustomSetter != null)
                {
                    newValue = CustomSetter(_field, newValue);
                }

                EditorSettingSaver.Singleton.SetVariable(Path, newValue, false);
                EditorSettingSaver.Singleton.Save();
            }

            _field = newValue;
            OnSetValue(newValue);
            OnValueChanged(this);
#endif
        }

        public virtual bool IsDefault()
        {
            return true;
        }

        public virtual void OnSetValue(Variant value)
        {

        }

        public Variant VariantEditorDefault;
        public Variant VariantRuntimeDefault;
    }
}
