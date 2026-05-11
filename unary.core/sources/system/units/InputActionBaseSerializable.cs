using Godot;

namespace Unary.Core
{
    public struct InputActionBaseSerializable
    {
        public string Group { get; set; }
        public string Name { get; set; }
        public InputActionBase.InputActionType ActionType { get; set; }
        public InputActionBase.InputType Type { get; set; }
        public bool Toggle { get; set; }
        public Key Key { get; set; }
        public MouseButton MouseButton { get; set; }
    }
}
