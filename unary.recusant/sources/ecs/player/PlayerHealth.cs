using Godot;
using System;
using Unary.Core;

namespace Unary.Recusant
{
    [Tool]
    [GlobalClass]
    public partial class PlayerHealth : Component, IPoolable
    {
        [Export]
        public float Health { get; set; } = 100.0f;

        [Export]
        public float MaxHealth { get; set; } = 100.0f;

        public float Damage = 0.0f;

        private float fallBase = 1.0f;
        private float fallPow = 1.5f;

        public static PlayerHealth Instance { get; private set; }

        void IPoolable.Aquire()
        {
            Instance = this;
        }

        void IPoolable.Release()
        {
            Instance = null;
        }

        public void TakeDamage(float damage)
        {
            Health -= damage;
        }

        public float GetDamage(float verticalVelocity, float minVelocity)
        {
            float absVelocity = Mathf.Abs(verticalVelocity) - Mathf.Abs(minVelocity);
            return MathF.Ceiling(fallBase * Mathf.Pow(absVelocity, fallPow));
        }
    }
}
