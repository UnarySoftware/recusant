using Godot;
using System;
using Unary.Core;

namespace Unary.Recusant
{
    [Tool]
    [GlobalClass]
    public partial class PlayerFlow : Component, IPoolable
    {
        [Export]
        public CharacterBody3D body;

        public float Flow = -1.0f;

        private Rid _navRid;
        private RuntimeGizmo _gizmo;
        private UpdaterHandle _handle;
        private static readonly CylinderShape3D _castShape = new()
        {
            Height = 0.01f,
            Radius = PlayerConstants.PlayerRadius
        };

        public static PlayerFlow Instance;

        private ShapeCast3D _cast;

        public override void Initialize()
        {
            _cast = new()
            {
                Shape = _castShape,
                TargetPosition = new(0.0f, -20.0f, 0.0f),
                MaxResults = 1,
                Enabled = false,
            };
            _cast.AddException(body);

            AddChild(_cast);
        }

        void IPoolable.Aquire()
        {
            Instance = this;
            _handle = Updater.Singleton.PhysicsProcess.SubscribeDelayed(0.05f, PhysicsProcessDelayed);
            _navRid = LevelManager.Singleton.Root.NavigationRegion.GetNavigationMap();
            _gizmo = RuntimeGizmos.Singleton.Aquire();
            _gizmo.SetBox(new(0.25f, 0.25f, 0.25f), new Color(1.0f, 0.0f, 0.0f, 1.0f));
            _cast.Enabled = true;
        }

        void IPoolable.Release()
        {
            Instance = null;
            Updater.Singleton.PhysicsProcess.UnsubscribeDelayed(_handle);
            RuntimeGizmos.Singleton.Release(_gizmo);
            _cast.Enabled = false;
        }

        private void PhysicsProcessDelayed(float delta)
        {
            Vector3 newPosition = body.Position;
            newPosition.Y += 0.01f;
            _cast.Position = newPosition;

            _cast.ForceShapecastUpdate();

            if (!_cast.IsColliding())
            {
                return;
            }

            Vector3 target = NavigationServer3D.Singleton.MapGetClosestPoint(_navRid, _cast.GetCollisionPoint(0));

            _gizmo.SetPosition(target);

            float newFlow = NavMeshManager.Singleton.GetFlow(target);

            if (newFlow > -1.0f)
            {
                Flow = newFlow;
            }
        }
    }
}
