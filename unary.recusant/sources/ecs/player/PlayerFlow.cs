using Godot;
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
        public NavBrush.Flag Flags;
        public int Triangle = -1;

        private Rid _navRid;
        private UpdaterHandle _handle;
        private static readonly CylinderShape3D _castShape = new()
        {
            Height = 0.01f,
            Radius = PlayerConstants.PlayerRadius
        };

        public static PlayerFlow Instance;

        private ShapeCast3D _cast;
        private RayCast3D _rayCast;

        public override void Initialize()
        {
            _cast = new()
            {
                Shape = _castShape,
                TargetPosition = new(0.0f, -20.0f, 0.0f),
                MaxResults = 1,
                Enabled = false,
                Position = new Vector3(0.0f, 0.01f, 0.0f),
                ExcludeParent = true
            };
            body.AddChild(_cast);

            _rayCast = new()
            {
                TargetPosition = new(0.0f, -2.0f, 0.0f),
                Enabled = false,
                Position = new Vector3(0.0f, 0.01f, 0.0f),
                ExcludeParent = true
            };
            body.AddChild(_rayCast);
        }

        private RuntimeGizmo _gizmo;

        void IPoolable.Aquire()
        {
            Instance = this;
            _handle = Updater.Singleton.PhysicsProcess.SubscribeDelayed(0.05f, PhysicsProcessDelayed);
            _navRid = LevelManager.Singleton.Root.NavigationRegion.GetNavigationMap();
            _cast.Enabled = true;
            _rayCast.Enabled = true;
            //_gizmo = RuntimeGizmos.Singleton.Aquire();
            //_gizmo.SetBox(new Vector3(0.2f, 0.2f, 0.2f), new Color(1.0f, 0.0f, 0.0f, 1.0f));
        }

        void IPoolable.Release()
        {
            Instance = null;
            Updater.Singleton.PhysicsProcess.UnsubscribeDelayed(_handle);
            _cast.Enabled = false;
            _rayCast.Enabled = false;
            //RuntimeGizmos.Singleton.Release(_gizmo);
        }

        private void PhysicsProcessDelayed(float delta)
        {
            Vector3 collision;

            _rayCast.ForceRaycastUpdate();

            if (_rayCast.IsColliding())
            {
                collision = _rayCast.GetCollisionPoint();
            }
            else
            {
                _cast.ForceShapecastUpdate();

                if (!_cast.IsColliding())
                {
                    return;
                }

                collision = _cast.GetCollisionPoint(0);
            }

            Vector3 target = NavigationServer3D.Singleton.MapGetClosestPoint(_navRid, collision);

            //_gizmo.SetPosition(target);

            (float flow, NavBrush.Flag flags, int triangle) = NavMeshManager.Singleton.GetFlow(target);

            if (flow > -1.0f)
            {
                Flow = flow;
                Flags = flags;
                Triangle = triangle;
            }
        }
    }
}
