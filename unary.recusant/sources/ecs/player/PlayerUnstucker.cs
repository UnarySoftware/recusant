using Godot;
using Unary.Core;

namespace Unary.Recusant
{
    [Tool]
    [GlobalClass]
    public partial class PlayerUnstucker : Component, IProcess, IPoolable
    {
        [Export]
        public CharacterBody3D Body;

        Vector3 _lastPosition;

        void IPoolable.Aquire()
        {
            _lastPosition = Body.Position;
        }

        void IPoolable.Release()
        {

        }

        public void Process(float delta)
        {

        }
    }
}
