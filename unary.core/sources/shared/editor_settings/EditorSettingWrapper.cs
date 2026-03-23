using Godot;

namespace Unary.Core
{
    [Tool]
    public partial class EditorSettingWrapper : GodotObject
    {
        public EditorSettingVariableBase Variable;

        public Variant Value
        {
            get
            {
                if (Variable == null)
                {
                    return default;
                }

                return Variable.VariantValue;
            }
            set
            {
                Variable?.VariantValue = value;
            }
        }
    }
}
