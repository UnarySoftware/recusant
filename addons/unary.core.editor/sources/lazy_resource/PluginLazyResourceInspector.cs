#if TOOLS

using Godot;

namespace Unary.Core.Editor
{
    [Tool]
    public partial class PluginLazyResourceInspector : EditorInspectorPlugin, IPluginSystem
    {
        bool ISystem.Initialize()
        {
            this.GetPlugin().AddInspectorPlugin(this);
            return true;
        }

        void ISystem.Deinitialize()
        {
            this.GetPlugin().RemoveInspectorPlugin(this);
        }

        public override bool _CanHandle(GodotObject @object)
        {
            return @object is LazyResource);
        }

        public override bool _ParseProperty(GodotObject @object, Variant.Type type, string name, PropertyHint hintType, string hintString, PropertyUsageFlags usageFlags, bool wide)
        {
            if (name != nameof(LazyResource.TargetValue))
            {
                return false;
            }

            var lazyResource = (LazyResource)@object;

            if (!typeof(Resource).IsAssignableFrom(lazyResource.BaseType))
            {
                return false;
            }

            AddPropertyEditor(name, new PluginLazyResourceEditor(lazyResource.BaseType.Name), false, "Value");
            return true;
        }
    }
}

#endif
