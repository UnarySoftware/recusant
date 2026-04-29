using Godot;

namespace Unary.Core
{
    [SingletonProvider("Unary.Core.Bootstrap.Singleton.GetSystem<UiManager>().GetState<{0}>().GetUnit", 0)]
    public abstract partial class UiUnit<T> : UiUnitBase where T : UiState
    {
        private T _stateCache;
        protected T State
        {
            get
            {
                _stateCache ??= Bootstrap.Singleton.GetSystem<UiManager>().GetState<T>();
                return _stateCache;
            }
        }

        public bool Opened
        {
            get
            {
                return State.Opened;
            }
        }

        public Control Root
        {
            get
            {
                return State.Root;
            }
        }
    }
}
