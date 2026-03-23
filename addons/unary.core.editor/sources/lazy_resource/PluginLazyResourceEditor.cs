

using Godot;

namespace Unary.Core.Editor
{
    public partial class PluginLazyResourceEditor : EditorProperty
    {
        private PluginLazyResourcePicker _picker;

        private string _current_path;

        public PluginLazyResourceEditor(string baseType)
        {
            _picker = new();
            _picker.BaseType = baseType;
            _picker.ResourceChanged += OnResourceChanged;
            _picker.ResourceSelected += OnResourceSelected;
            AddChild(_picker);
        }

        public override void _UpdateProperty()
        {
            LazyResource resource = (LazyResource)GetEditedObject();

            if (resource != null)
            {
                string path = resource.TargetValue;
                if (_current_path != path)
                {
                    _current_path = path;
                    Pick(path);
                }
            }
            else
            {
                Pick(string.Empty);
            }
        }

        private void Pick(string path)
        {
            if (ResourceLoader.Singleton.Exists(path, _picker.BaseType))
            {
                _picker.EditedResource = ResourceLoader.Load(path);
            }
            else
            {
                _picker.EditedResource = null;
            }
        }

        private void OnResourceChanged(Resource resource)
        {
            if (resource == null)
            {
                return;
            }

            if (ResourceLoader.Exists(resource.ResourcePath, _picker.BaseType))
            {
                long id = ResourceLoader.GetResourceUid(resource.ResourcePath);

                LazyResource targetResource = (LazyResource)GetEditedObject();

                if (id != ResourceUid.InvalidId)
                {
                    targetResource.TargetValue = ResourceUid.IdToText(id);
                    EmitChanged(GetEditedProperty(), targetResource.TargetValue);
                }
                else
                {
                    PluginLogger.Warning(this, $"UID missing for {resource.ResourceName}, defaulting to res://");
                    targetResource.TargetValue = resource.ResourcePath;
                    EmitChanged(GetEditedProperty(), targetResource.TargetValue);
                }
            }
            else
            {
                Pick(_current_path);
                PluginLogger.Error(this, $"Property \"{Label}\" must be assigned a resource with a filename.");
            }
        }

        private void OnResourceSelected(Resource resource, bool inspect)
        {
            EditorInterface.Singleton.CallDeferred(EditorInterface.MethodName.EditResource, resource);
        }
    }
}
