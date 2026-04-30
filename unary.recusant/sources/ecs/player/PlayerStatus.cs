using Godot;
using Unary.Core;

namespace Unary.Recusant
{
    [Tool]
    [GlobalClass]
    public partial class PlayerStatus : Component, IPoolable
    {
        public static PlayerStatus Instance { get; private set; }

        [Export]
        public float Mass = 80.0f;

        void IPoolable.Aquire()
        {
            Instance = this;
        }

        void IPoolable.Release()
        {
            Instance = null;
        }
    }
}
