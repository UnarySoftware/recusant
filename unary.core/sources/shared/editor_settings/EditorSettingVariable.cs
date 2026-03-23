
using Godot;

namespace Unary.Core
{
    public class EditorSettingVariable<[MustBeVariant] T> : EditorSettingVariableBase
    {
        public T Value
        {
            get
            {
                return VariantValue.As<T>();
            }
            set
            {
                VariantValue = Variant.From(value);
            }
        }

        public T EditorDefault
        {
            get
            {
                return VariantEditorDefault.As<T>();
            }
            set
            {
                VariantEditorDefault = Variant.From(value);
            }
        }

        public T RuntimeDefault
        {
            get
            {
                return VariantRuntimeDefault.As<T>();
            }
            set
            {
                VariantRuntimeDefault = Variant.From(value);
            }
        }

        public EditorSettingVariable()
        {
            Type = EditorSettingType.Variable;
            GenericType = typeof(T);
        }
    }
}
