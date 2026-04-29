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
            if (InstanceFromId(_editorId) is EditorProperty editor)
            {
                editor.UpdateProperty();
            }
        }

        public void MoveNode(VBoxContainer container, EditorProperty editor, int index)
        {
            container.MoveChild(editor, index);
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

    [Tool]
    public partial class PluginDockFilterHandler : RefCounted
    {
        private PluginDock _dock;
        private string _modId;

        public void Setup(PluginDock dock, string modId)
        {
            _dock = dock;
            _modId = modId;
        }

        public void OnTextChanged(string value)
        {
            _dock.Filter(_modId, value);
        }
    }
}

#endif
