using Godot;
using System;
using Unary.Core;

namespace Unary.Recusant
{
    [Tool]
    [GlobalClass]
    public partial class PlayerHealth : Component, IPoolable
    {
        [ExportGroup("Health")]
        [Export]
        public float Health { get; set; } = 100.0f;

        [Export]
        public float MaxHealth { get; set; } = 100.0f;

        [ExportGroup("Fall Damage")]
        [Export]
        public float MinMagnitude = 10.0f;

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

        private float GetFallDamage(float magnitude)
        {
            float absVelocity = magnitude - MinMagnitude;
            return MathF.Ceiling(fallBase * Mathf.Pow(absVelocity, fallPow));
        }

        public void DoFallDamage(float magnitude)
        {
            if (magnitude >= MinMagnitude)
            {
                Damage = GetFallDamage(magnitude);
            }
        }
    }
}
