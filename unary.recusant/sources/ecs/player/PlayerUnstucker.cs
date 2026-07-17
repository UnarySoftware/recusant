using Godot;
using Unary.Core;

namespace Unary.Recusant
{
    [Tool]
    [GlobalClass]
    public partial class PlayerUnstucker : Component, IPhysicsProcess, IPoolable
    {
        [Export]
        public float StuckTimer = 3.0f;

        [Export]
        public float CoeffLimitTimer = 100.0f;

        [Export]
        public CharacterBody3D Body;

        private Vector3 spawnPosition;
        private Vector3 _lastPosition;

        private float _timer = 0.0f;

        public float ActualVelocity = 0.0f;
        public float Coeff = 0.0f;
        public bool IsAirborn = false;

        public static PlayerUnstucker Instance;

        private PlayerMovement _movement;

        public override void Initialize()
        {
            _movement = GetComponent<PlayerMovement>();
        }

        void IPoolable.Aquire()
        {
            Instance = this;
            Updater.Singleton.PhysicsProcess.Subscribe(this);
            spawnPosition = Body.Position;
            _lastPosition = spawnPosition;
        }

        void IPoolable.Release()
        {
            Updater.Singleton.PhysicsProcess.Unsubscribe(this);
            Instance = null;
        }

        public void Unstuck()
        {
            Body.Velocity = Vector3.Zero;
            Body.Position = spawnPosition;
            Body.MoveAndSlide();
            _lastPosition = spawnPosition;
            _timer = 0.0f;
        }

        public void PhysicsProcess(float delta)
        {
            if (!_movement.CanMove)
            {
                return;
            }

            IsAirborn = !Body.IsOnCeiling() && !Body.IsOnFloor();

            Vector3 currentPosition = Body.Position;

            float reportedSpeed = Body.Velocity.Length();

            ActualVelocity = currentPosition.DistanceTo(_lastPosition) / delta;

            if (ActualVelocity == 0.0f)
            {
                ActualVelocity = float.Epsilon;
            }

            Coeff = reportedSpeed / ActualVelocity;

            if (Coeff > CoeffLimitTimer)
            {
                _timer += delta;
            }
            else
            {
                _timer = 0.0f;
            }

            if (_timer >= StuckTimer)
            {
                Unstuck();
            }
            else
            {
                _lastPosition = currentPosition;
            }
        }
    }
}
