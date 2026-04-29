
using Godot;
using System.Collections.Generic;

namespace Unary.Core
{
    public class EditorSettingVariable<[MustBeVariant] T> : EditorSettingVariableBase
    {
        public EditorSettingVariable()
        {
            Type = EditorSettingType.Variable;
            GenericType = typeof(T);
        }

        private bool _gotValue = false;

        public T Value
        {
            get
            {
                if (!_gotValue)
                {
                    _gotValue = true;
                    field = VariantValue.As<T>();
                }

                return field;
            }
            private set
            {
                field = value;
            }
        }

        public override bool IsDefault()
        {
            return EqualityComparer<T>.Default.Equals(Value, EditorDefault);
        }

        public override void OnSetValue(Variant value)
        {
            Value = value.As<T>();
        }

        private bool _gotValueEditor = false;

        public T EditorDefault
        {
            set
            {
                field = value;
                VariantEditorDefault = Variant.From(value);
            }
            get
            {
                if (!_gotValueEditor)
                {
                    _gotValueEditor = true;
                    field = VariantEditorDefault.As<T>();
                }

                return field;
            }
        }

        private bool _gotValueRuntime = false;

        public T RuntimeDefault
        {
            set
            {
                field = value;
                VariantRuntimeDefault = Variant.From(value);
            }
            get
            {
                if (!_gotValueRuntime)
                {
                    _gotValueRuntime = true;
                    field = VariantRuntimeDefault.As<T>();
                }

                return field;
            }
        }
    }
}
