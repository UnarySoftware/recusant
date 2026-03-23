using Godot;

namespace Unary.Core
{
    [Tool]
    [GlobalClass, Icon("res://addons/unary.core.editor/icons/Component.svg")]
    public partial class Component : Node
    {
        private Entity _entity;

        public Entity Entity
        {
            get
            {
                _entity ??= Entity.Find(this);
                return _entity;
            }
        }
    }
}
