#if TOOLS
using Godot;

namespace Unary.Core.Editor
{
    [Tool]
    public partial class PluginDockVariableHandler : RefCounted
    {
        private EditorSettingVariableBase _variable;
        private ulong _editorId;

        public void Setup(EditorSettingVariableBase variable, EditorProperty editor)
        {
            _variable = variable;
            _editorId = editor.GetInstanceId();
        }

        public void OnPropertyChanged(StringName property, Variant value, StringName field, bool changing)
        {
            _variable.Wrapper.Value = value;
            if (GodotObject.InstanceFromId(_editorId) is EditorProperty editor)
            {
                editor.UpdateProperty();
            }
        }
    }

    [Tool]
    public partial class PluginDockActionHandler : RefCounted
    {
        private EditorSettingAction _action;

        public void Setup(EditorSettingAction action)
        {
            _action = action;
        }

        public void OnPressed()
        {
            _action.MethodInfo.Invoke(null, null);
        }
    }
}
#endif
