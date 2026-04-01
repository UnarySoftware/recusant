using Godot;

namespace Unary.Core
{
    [Tool]
    [GlobalClass, Icon("res://addons/unary.core.editor/icons/Component.svg")]
    public partial class Component : Node
    {
        private Entity _entityCache;

        public Entity Entity
        {
            get
            {
                _entityCache ??= Entity.Find(this);
                return _entityCache;
            }
        }

        public virtual void Initialize()
        {

        }

        public virtual void Deinitialize()
        {

        }
    }
}
