using Godot;
using Unary.Core;

namespace Unary.Recusant
{
    [Tool]
    [GlobalClass]
    public partial class PlayerCamera : Component, IPoolable, IProcess
    {
        [ExportGroup("Settings")]
        [Export]
        public float UnrollStep = 25.0f;

        [Export]
        public float MaxRoll = 30.0f;

        [Export]
        public float RollMultiplier = 0.4f;

        [Export]
        public float LookSensitivity = 0.002f;

        [Export]
        public float HeadbobMoveAmount = 0.03f;

        [Export]
        public float HeadbobFrequency = 3.0f;

        [ExportGroup("Nodes")]
        [Export]
        public CharacterBody3D Body;

        [Export]
        public Camera3D Camera3D;

        [Export]
        public Node3D Head;

        [Export]
        public Node3D CameraSmooth;

        private Vector3? _savedCameraPosition = null;
        private float _headbobTime = 0.0f;

        private SlotHandle _processSlot;

        private PlayerMovement _movement;

        public Basis GetWishDir()
        {
            return CameraSmooth.Basis;
        }

        public override void Initialize()
        {
            _movement = GetComponent<PlayerMovement>();

            CameraSmooth.PhysicsInterpolationMode = PhysicsInterpolationModeEnum.Off;
        }

        public void Aquire()
        {
            Camera3D.Current = true;
            _processSlot = Updater.Singleton.Process.Subscribe(this);
        }

        public void Release()
        {
            Camera3D.Current = false;
            Updater.Singleton.Process.Unsubscribe(_processSlot);
        }

        void IProcess.Process(float delta)
        {
            Vector3 rotation = Camera3D.RotationDegrees;
            if (rotation.Z > 0.0f)
            {
                rotation.Z = Mathf.Clamp(rotation.Z - UnrollStep * delta, 0.0f, MaxRoll);
            }
            Camera3D.RotationDegrees = rotation;
        }

        public Camera3D GetActiveCamera()
        {
            return Camera3D;
        }

        public void DoRoll(float force)
        {
            force *= RollMultiplier;
            Vector3 rotation = Camera3D.RotationDegrees;
            rotation.Z = force;
            Camera3D.RotationDegrees = rotation;
        }

        public override void _Input(InputEvent @event)
        {
            if (!InputManager.Singleton.HasScope(InputScope.PlayerCamera))
            {
                return;
            }

            if (@event is InputEventMouseMotion mouseMotion)
            {
                CameraSmooth.RotateY(-mouseMotion.Relative.X * LookSensitivity);

                Head.RotateX(-mouseMotion.Relative.Y * LookSensitivity);

                // Clamp the X rotation to prevent flipping
                var degrees = Head.RotationDegrees;
                degrees.X = Mathf.Clamp(degrees.X, -89.9f, 89.9f);
                degrees.Y = 0.0f;
                degrees.Z = 0.0f;
                Head.RotationDegrees = degrees;
            }

            if (@event is InputEventMouseButton mouseButton && mouseButton.Pressed)
            {
                if (mouseButton.ButtonIndex == MouseButton.WheelUp)
                {
                    _movement.NoclipSpeedMultiplier = Mathf.Min(100.0f, _movement.NoclipSpeedMultiplier * 1.1f);
                }
                else if (mouseButton.ButtonIndex == MouseButton.WheelDown)
                {
                    _movement.NoclipSpeedMultiplier = Mathf.Max(0.1f, _movement.NoclipSpeedMultiplier * 0.9f);
                }
            }
        }

        public void HeadbobEffect(float delta)
        {
            _headbobTime += delta * Body.Velocity.Length();
            var camera3D = CameraSmooth;
            camera3D.Transform = new Transform3D(
                camera3D.Transform.Basis,
                new Vector3(
                    Mathf.Cos(_headbobTime * HeadbobFrequency * 0.5f) * HeadbobMoveAmount,
                    Mathf.Sin(_headbobTime * HeadbobFrequency) * HeadbobMoveAmount,
                    0
                )
            );
        }

        public void SaveCameraPosForSmoothing()
        {
            if (_savedCameraPosition == null)
            {
                _savedCameraPosition = CameraSmooth.GlobalPosition;
            }
        }

        public void SlideCameraSmoothBackToOrigin(float delta)
        {
            if (_savedCameraPosition == null)
            {
                return;
            }

            var cameraSmooth = CameraSmooth;
            cameraSmooth.GlobalPosition = new Vector3(
                cameraSmooth.GlobalPosition.X,
                _savedCameraPosition.Value.Y,
                cameraSmooth.GlobalPosition.Z
            );

            cameraSmooth.Position = new Vector3(
                cameraSmooth.Position.X,
                Mathf.Clamp(cameraSmooth.Position.Y, -_movement.CrouchTranslate, _movement.CrouchTranslate),
                cameraSmooth.Position.Z
            );

            var move_amount = Mathf.Max(Body.Velocity.Length() * delta, _movement.WalkSpeed / 2 * delta);
            cameraSmooth.Position = new Vector3(
                cameraSmooth.Position.X,
                Mathf.MoveToward(cameraSmooth.Position.Y, 0.0f, move_amount),
                cameraSmooth.Position.Z
            );

            _savedCameraPosition = cameraSmooth.GlobalPosition;

            if (cameraSmooth.Position.Y == 0)
            {
                _savedCameraPosition = null;
            }
        }

    }
}
