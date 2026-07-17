using Godot;
using Unary.Core;

namespace Unary.Recusant
{
    [Tool]
    [GlobalClass]
    public partial class PlayerSquisher : Component, IPhysicsProcess, IPoolable
    {
        [Export]
        public CharacterBody3D Body;

        private PlayerMovement _movement;
        private PlayerHealth _health;
        private PlayerFlow _flow;

        public static PlayerSquisher Instance;

        private CylinderShape3D _castShape;
        private ShapeCast3D _cast;

        public override void Initialize()
        {
            _castShape = new()
            {
                Height = PlayerConstants.PlayerShape.Cache.Height - 0.5f,
                Radius = PlayerConstants.PlayerShape.Cache.Radius * 0.5f
            };

            _cast = new()
            {
                Shape = _castShape,
                MaxResults = 6,
                Enabled = false,
                Position = new Vector3(0.0f, PlayerConstants.PlayerShape.Cache.Height / 2.0f, 0.0f),
                TargetPosition = Vector3.Zero,
                ExcludeParent = true,
                Margin = Body.SafeMargin
            };

            Body.AddChild(_cast);

            _movement = GetComponent<PlayerMovement>();
            _health = GetComponent<PlayerHealth>();
        }

        public void PhysicsProcess(float delta)
        {
            bool gotPenetrations = false;
            float damage = 0.0f;
            int collisionCount = 0;

            _cast.ForceShapecastUpdate();

            for (int i = 0; i < _cast.GetCollisionCount(); i++)
            {
                GodotObject collider = _cast.GetCollider(i);
                if (collider is PlatformBody3D platform)
                {
                    platform.ForbidMove();
                    gotPenetrations = true;

                    if (damage < platform.DamagePerTick)
                    {
                        damage = platform.DamagePerTick;
                    }

                    collisionCount++;
                }
            }

            _movement.CanMove = !gotPenetrations;

            if (gotPenetrations)
            {
                _health.TakeDamage(damage);
            }
        }

        public void Aquire()
        {
            Instance = this;
            Updater.Singleton.PhysicsProcess.Subscribe(this);
            _cast.Enabled = true;
        }

        public void Release()
        {
            Updater.Singleton.PhysicsProcess.Unsubscribe(this);
            Instance = null;
            _cast.Enabled = false;
        }
    }
}
