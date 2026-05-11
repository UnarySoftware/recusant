using Godot;
using Unary.Core;

namespace Unary.Recusant
{
    [Tool]
    [GlobalClass]
    public partial class PlayerInteracts : Component, IPoolable, IProcess
    {
        private static readonly InputAction _flashlight = new()
        {
            Scope = InputScope.PlayerCamera,
            ActionType = InputActionBase.InputActionType.FastPress,
            AllowedActionTypes = InputActionBase.InputActionType.All,
            Key = Key.F,
            Type = InputActionBase.InputType.Keyboard,
            Group = "Interactions",
            Name = "Flashlight",
            Toggle = false,
        };

        [Export]
        public SpotLight3D Flashlight;

        public static PlayerInteracts Instance { get; private set; }

        private SlotHandle _processSlot;

        void IPoolable.Aquire()
        {
            Instance = this;
            _processSlot = Updater.Singleton.Process.Subscribe(this);
        }

        void IPoolable.Release()
        {
            Instance = null;
            Updater.Singleton.PhysicsProcess.Unsubscribe(_processSlot);
        }

        public void Process(float delta)
        {
            if (_flashlight.Poll(delta))
            {
                Flashlight.Visible = !Flashlight.Visible;
            }
        }
    }
}
