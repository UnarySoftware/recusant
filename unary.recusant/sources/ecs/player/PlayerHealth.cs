using Godot;
using System;
using Unary.Core;

namespace Unary.Recusant
{
    [Tool]
    [GlobalClass]
    public partial class PlayerHealth : Component, IPoolable, IPhysicsProcess
    {
        [ExportGroup("Health")]
        [Export]
        public float Health { get; set; } = 100.0f;

        [Export]
        public float MaxHealth { get; set; } = 100.0f;

        [ExportGroup("Fall Damage")]
        [Export]
        public float MinMagnitude = 10.0f;

        private float fallBase = 1.0f;
        private float fallPow = 1.5f;

        private PlayerUnstucker _unstucker;

        public static PlayerHealth Instance { get; private set; }

        public override void Initialize()
        {
            _unstucker = GetComponent<PlayerUnstucker>();
        }

        void IPoolable.Aquire()
        {
            Instance = this;
            Updater.Singleton.PhysicsProcess.Subscribe(this);
        }

        void IPoolable.Release()
        {
            Instance = null;
            Updater.Singleton.PhysicsProcess.Unsubscribe(this);
        }

        private float _resetHealthTimer = 0.0f;

        public void PhysicsProcess(float delta)
        {
            if (Health == MaxHealth)
            {
                return;
            }

            if (Health == 0.0f)
            {
                Health = MaxHealth;
                _resetHealthTimer = 0.0f;
                _unstucker.Unstuck();
            }

            if (_resetHealthTimer == 0.0f)
            {
                return;
            }

            _resetHealthTimer -= delta;

            if (_resetHealthTimer < 0.0f)
            {
                _resetHealthTimer = 0.0f;
                Health = MaxHealth;
            }
        }

        public void TakeDamage(float damage)
        {
            Health -= damage;
            Health = Mathf.Clamp(Health, 0.0f, MaxHealth);
            _resetHealthTimer = 2.0f;
        }

        private float GetFallDamage(float magnitude)
        {
            float absVelocity = magnitude - MinMagnitude;
            return MathF.Ceiling(fallBase * Mathf.Pow(absVelocity, fallPow));
        }

        public void DoFallDamage(float magnitude)
        {
            if (magnitude >= MinMagnitude)
            {
                TakeDamage(GetFallDamage(magnitude));
            }
        }

    }
}
