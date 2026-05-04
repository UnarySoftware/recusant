using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using Unary.Core;

namespace Unary.Recusant
{
    [Tool]
    [GlobalClass]
    public partial class PlayerMovement : Component, IPoolable, IPhysicsProcess
    {
        private static readonly InputAction _forward = new()
        {
            Scope = InputScope.PlayerMovement,
            ActionType = InputActionBase.InputActionType.Hold,
            AllowedActionTypes = InputActionBase.InputActionType.All,
            Key = Key.W,
            Type = InputActionBase.InputType.Keyboard,
            Group = "Movement",
            Name = "Forward",
            Toggle = false,
        };

        private static readonly InputAction _back = new()
        {
            Scope = InputScope.PlayerMovement,
            ActionType = InputActionBase.InputActionType.Hold,
            AllowedActionTypes = InputActionBase.InputActionType.All,
            Key = Key.S,
            Type = InputActionBase.InputType.Keyboard,
            Group = "Movement",
            Name = "Back",
            Toggle = false,
        };

        private static readonly InputAction _left = new()
        {
            Scope = InputScope.PlayerMovement,
            ActionType = InputActionBase.InputActionType.Hold,
            AllowedActionTypes = InputActionBase.InputActionType.All,
            Key = Key.A,
            Type = InputActionBase.InputType.Keyboard,
            Group = "Movement",
            Name = "Left",
            Toggle = false,
        };

        private static readonly InputAction _right = new()
        {
            Scope = InputScope.PlayerMovement,
            ActionType = InputActionBase.InputActionType.Hold,
            AllowedActionTypes = InputActionBase.InputActionType.All,
            Key = Key.D,
            Type = InputActionBase.InputType.Keyboard,
            Group = "Movement",
            Name = "Right",
            Toggle = false,
        };

        private static readonly InputAction _sprint = new()
        {
            Scope = InputScope.PlayerMovement,
            ActionType = InputActionBase.InputActionType.Hold,
            AllowedActionTypes = InputActionBase.InputActionType.All,
            Key = Key.Shift,
            Type = InputActionBase.InputType.Keyboard,
            Group = "Movement",
            Name = "Sprint",
            Toggle = false,
        };

        private static readonly InputAction _jump = new()
        {
            Scope = InputScope.PlayerMovement,
            ActionType = InputActionBase.InputActionType.Press,
            AllowedActionTypes = InputActionBase.InputActionType.NoHold,
            Key = Key.Space,
            Type = InputActionBase.InputType.Keyboard,
            Group = "Movement",
            Name = "Jump",
            Toggle = false,
        };

        private static readonly InputAction _crouch = new()
        {
            Scope = InputScope.PlayerMovement,
            ActionType = InputActionBase.InputActionType.Hold,
            AllowedActionTypes = InputActionBase.InputActionType.All,
            Key = Key.C,
            Type = InputActionBase.InputType.Keyboard,
            Group = "Movement",
            Name = "Crouch",
            Toggle = false,
        };

        private static readonly InputAction _noclip = new()
        {
            Scope = InputScope.PlayerMovement,
            ActionType = InputActionBase.InputActionType.Press,
            AllowedActionTypes = InputActionBase.InputActionType.All,
            Key = Key.N,
            Type = InputActionBase.InputType.Keyboard,
            Group = "Movement",
            Name = "Noclip",
            Toggle = false,
        };

        [ExportGroup("Ground Movement")]
        [Export]
        public float WalkSpeed = 5.6f;

        [Export]
        public float SprintSpeed = 6.5f;

        [Export]
        public float GroundAccel = 11.0f;

        [Export]
        public float GroundDecel = 7.0f;

        [Export]
        public float GroundFriction = 2.0f;

        [Export]
        public float MaxVelocityGround = 7.0f;

        [Export]
        public float MaxStepHeight = 0.46f;

        [Export]
        public float CrouchTranslate = 0.422f;

        [Export]
        public float CrouchJumpMultiplier = 0.9f;

        [Export]
        public float CroushSpeedMultiplier = 0.34f;

        private float _crouchResolvedMultiplier;

        [ExportGroup("Air Movement")]
        [Export]
        public float AirCap = 0.85f;

        [Export]
        public float AirAccel = 8.0f;

        [Export]
        public float AirMoveSpeed = 5.0f;

        [Export]
        public float AirGravity = 20.32f;

        [Export]
        public float JumpVelocity = 7.675f;

        [Export]
        public bool AutoBhop = true;

        [ExportGroup("Other Movement")]
        [Export]
        public float SwimUpSpeed = 10.0f;

        [Export]
        public float SwimGravity = 1.524f;

        [Export]
        public float ClimbSpeed = 7.0f;

        [Export]
        public float ClimbEjectSpeedMultiplier = 1.5f;

        public float NoclipSpeedMultiplier { get; set; } = 3.0f;

        [ExportGroup("Nodes")]
        [Export]
        public CharacterBody3D Body;

        [Export]
        public CollisionShape3D CollisionShape3D;

        [Export]
        public RayCast3D StairsBelowRayCast3D;

        [Export]
        public RayCast3D StairsAheadRayCast3D;

        [Export]
        public Node3D Head;

        private Vector3 _wishDir = Vector3.Zero;
        private Vector3 _camAlignedWishDir = Vector3.Zero;

        private bool _isCrouched = false;

        private bool _isNoclip = false;

        private bool _wasSnappedToStairs = false;
        private ulong _wasFlooredFrame = 0;

        private Area3D _ladderClimbing = null;
        private float _originalHeight = 0.0f;

        private List<Area3D> _ladders = [];
        private List<Area3D> _water = [];

        private Vector3 _previousVelocity;
        private bool _wasInAir = false;

        public static PlayerMovement Instance;

        private SlotHandle physicsProcessSlot;

        private PlayerCamera _camera;
        private PlayerHealth _health;
        private PlayerStatus _status;

        void IPoolable.Aquire()
        {
            Instance = this;
            physicsProcessSlot = Updater.Singleton.PhysicsProcess.Subscribe(this);
        }

        void IPoolable.Release()
        {
            Instance = null;
            Updater.Singleton.PhysicsProcess.Unsubscribe(physicsProcessSlot);
        }

        public override void Initialize()
        {
            _crouchResolvedMultiplier = CrouchTranslate * CrouchJumpMultiplier;

            _camera = GetComponent<PlayerCamera>();
            _health = GetComponent<PlayerHealth>();
            _status = GetComponent<PlayerStatus>();

            _ladders = [.. GetTree().GetNodesInGroup("ladder_area3d").Cast<Area3D>()];
            _water = [.. GetTree().GetNodesInGroup("water_area").Cast<Area3D>()];

            _originalHeight = (CollisionShape3D.Shape as CylinderShape3D).Height;
        }

        private void PushAwayRigidBodies()
        {
            for (int i = 0; i < Body.GetSlideCollisionCount(); i++)
            {
                var collision = Body.GetSlideCollision(i);
                if (collision.GetCollider() is RigidBody3D rigidBody)
                {
                    var pushDir = -collision.GetNormal();
                    var velocityDiff = Body.Velocity.Dot(pushDir) - rigidBody.LinearVelocity.Dot(pushDir);
                    velocityDiff = Mathf.Max(0.0f, velocityDiff);

                    var massRatio = Mathf.Min(1.0f, _status.Mass / rigidBody.Mass);

                    if (massRatio < 0.25f)
                    {
                        continue;
                    }

                    pushDir.Y = 0;
                    var push_force = massRatio * 5.0f;
                    rigidBody.ApplyImpulse(pushDir * velocityDiff * push_force, collision.GetPosition() - rigidBody.GlobalPosition);
                }
            }
        }

        public float GetMoveSpeed(float delta)
        {
            if (_isCrouched)
            {
                return WalkSpeed * CroushSpeedMultiplier;
            }

            return _sprint.Poll(delta) ? SprintSpeed : WalkSpeed;
        }

        private void SnapDownToStairsCheck()
        {
            bool didSnap = false;
            var stairsBelowRaycast = StairsBelowRayCast3D;
            stairsBelowRaycast.ForceRaycastUpdate();

            var floorBelow = stairsBelowRaycast.IsColliding() && !IsSurfaceTooSteep(stairsBelowRaycast.GetCollisionNormal());
            var wasOnFloorLastFrame = Engine.GetPhysicsFrames() == _wasFlooredFrame;

            if (!Body.IsOnFloor() && Body.Velocity.Y <= 0 && (wasOnFloorLastFrame || _wasSnappedToStairs) && floorBelow)
            {
                var bodyTestResult = new KinematicCollision3D();
                if (Body.TestMove(Body.GlobalTransform, new Vector3(0, -MaxStepHeight, 0), bodyTestResult))
                {
                    _camera.SaveCameraPosForSmoothing();
                    var translate_y = bodyTestResult.GetTravel().Y;
                    Body.Position = new Vector3(Body.Position.X, Body.Position.Y + translate_y, Body.Position.Z);
                    Body.ApplyFloorSnap();
                    didSnap = true;
                }
            }

            _wasSnappedToStairs = didSnap;
        }

        private bool SnapUpStairsCheck(float delta)
        {
            if (!Body.IsOnFloor() && !_wasSnappedToStairs)
            {
                return false;
            }

            if (Body.Velocity.Y > 0 || (Body.Velocity * new Vector3(1.0f, 0.0f, 1.0f)).Length() == 0)
            {
                return false;
            }

            var expectedMoveMotion = Body.Velocity * new Vector3(1, 0, 1) * delta;
            var stepWithClearance = Body.GlobalTransform.Translated(expectedMoveMotion + new Vector3(0, MaxStepHeight * 2, 0));

            var downCheckResult = new KinematicCollision3D();
            if (Body.TestMove(stepWithClearance, new Vector3(0, -MaxStepHeight * 2, 0), downCheckResult) &&
                (downCheckResult.GetCollider().IsClass("StaticBody3D")))
            {
                var step_height = (stepWithClearance.Origin + downCheckResult.GetTravel() - Body.GlobalPosition).Y;

                if (step_height > MaxStepHeight || step_height <= 0.01f || (downCheckResult.GetPosition() - Body.GlobalPosition).Y > MaxStepHeight)
                {
                    return false;
                }

                var stairsAheadRaycast = StairsAheadRayCast3D;
                stairsAheadRaycast.GlobalPosition = downCheckResult.GetPosition() + new Vector3(0, MaxStepHeight, 0) + expectedMoveMotion.Normalized() * 0.1f;
                stairsAheadRaycast.ForceRaycastUpdate();

                if (stairsAheadRaycast.IsColliding() && !IsSurfaceTooSteep(stairsAheadRaycast.GetCollisionNormal()))
                {
                    _camera.SaveCameraPosForSmoothing();
                    Body.GlobalPosition = stepWithClearance.Origin + downCheckResult.GetTravel();
                    Body.ApplyFloorSnap();
                    _wasSnappedToStairs = true;
                    return true;
                }
            }

            return false;
        }

        private bool HandleLadderPhysics(float delta)
        {
            var wasClimbing = _ladderClimbing != null && _ladderClimbing.OverlapsBody(Body);

            if (!wasClimbing)
            {
                _ladderClimbing = null;
                foreach (var ladder in _ladders)
                {
                    if (ladder.OverlapsBody(Body))
                    {
                        _ladderClimbing = ladder;
                        break;
                    }
                }
            }

            if (_ladderClimbing == null)
                return false;

            var ladderTransform = _ladderClimbing.GlobalTransform;
            var positionRelativeToLadder = ladderTransform.AffineInverse() * Body.GlobalPosition;

            Vector2 inputDir = InputManager.Singleton.GetVector(_left, _right, _forward, _back, InputScope.PlayerMovement, delta);

            var activeCamera = _camera.GetActiveCamera();
            var ladderForwardMove = ladderTransform.AffineInverse().Basis * activeCamera.GlobalTransform.Basis * new Vector3(0.0f, 0.0f, inputDir.Y);
            var ladderSideMove = ladderTransform.AffineInverse().Basis * activeCamera.GlobalTransform.Basis * new Vector3(inputDir.X, 0.0f, 0.0f);

            var ladderStrafeVelocity = ClimbSpeed * (ladderSideMove.X + ladderForwardMove.X);
            var ladderClimbVelocity = ClimbSpeed * -ladderSideMove.Z;

            var upWish = Vector3.Up.Rotated(Vector3.Right, Mathf.DegToRad(-45)).Dot(ladderForwardMove);
            ladderClimbVelocity += ClimbSpeed * upWish;

            var shouldDismount = false;

            if (!wasClimbing)
            {
                var topOfLadder = _ladderClimbing.GetNode<Node3D>("TopOfLadder");
                var mounting_from_top = positionRelativeToLadder.Y > topOfLadder.Position.Y;

                if (mounting_from_top)
                {
                    if (ladderClimbVelocity > 0)
                    {
                        shouldDismount = true;
                    }
                }
                else
                {
                    if ((ladderTransform.AffineInverse().Basis * _wishDir).Z >= 0.0f)
                    {
                        shouldDismount = true;
                    }
                }

                if (Mathf.Abs(positionRelativeToLadder.Z) > 0.1f)
                {
                    shouldDismount = true;
                }
            }

            if (Body.IsOnFloor() && ladderClimbVelocity <= 0.0f)
            {
                shouldDismount = true;
            }

            if (shouldDismount)
            {
                _ladderClimbing = null;
                return false;
            }

            if (wasClimbing && _jump.Poll(delta))
            {
                Body.Velocity = _ladderClimbing.GlobalTransform.Basis.Z * JumpVelocity * ClimbEjectSpeedMultiplier;
                _ladderClimbing = null;
                return false;
            }

            Body.Velocity = ladderTransform.Basis * new Vector3(ladderStrafeVelocity, ladderClimbVelocity, 0.0f);

            positionRelativeToLadder.Z = 0.0f;
            Body.GlobalPosition = ladderTransform * positionRelativeToLadder;

            Body.MoveAndSlide();
            return true;
        }

        private bool HandleWaterPhysics(float delta)
        {
            bool overlap = false;

            foreach (var water in _water)
            {
                if (water.OverlapsBody(Body))
                {
                    overlap = true;
                    break;
                }
            }

            if (!overlap)
            {
                return false;
            }

            if (!Body.IsOnFloor())
            {
                Body.Velocity = new Vector3(Body.Velocity.X, Body.Velocity.Y - SwimGravity * delta, Body.Velocity.Z);
            }

            Body.Velocity += _camAlignedWishDir * GetMoveSpeed(delta) * delta;

            if (_jump.Poll(delta, InputActionBase.InputActionType.Hold))
            {
                Body.Velocity = new Vector3(Body.Velocity.X, Body.Velocity.Y + SwimUpSpeed * delta, Body.Velocity.Z);
            }

            Body.Velocity = Body.Velocity.Lerp(Vector3.Zero, 2.0f * delta);

            return true;
        }

        private void HandleCrouch(float delta)
        {
            var wasCrouched = _isCrouched;

            if (_crouch.Poll(delta))
            {
                _isCrouched = true;
            }
            else if (_isCrouched && !Body.TestMove(Body.GlobalTransform, new Vector3(0.0f, CrouchTranslate, 0.0f)))
            {
                _isCrouched = false;
            }

            var translateIfPossible = 0.0f;
            if (wasCrouched != _isCrouched && !Body.IsOnFloor() && !_wasSnappedToStairs)
            {
                translateIfPossible = _isCrouched ? _crouchResolvedMultiplier : -_crouchResolvedMultiplier;
            }

            if (translateIfPossible != 0.0f)
            {
                var result = new KinematicCollision3D();
                Body.TestMove(Body.GlobalTransform, new Vector3(0, translateIfPossible, 0), result);
                Body.Position = new Vector3(Body.Position.X, Body.Position.Y + result.GetTravel().Y, Body.Position.Z);

                Head.Position = new Vector3(Head.Position.X, Head.Position.Y - result.GetTravel().Y, Head.Position.Z);
                Head.Position = new Vector3(Head.Position.X, Mathf.Clamp(Head.Position.Y, -CrouchTranslate, 0), Head.Position.Z);
            }

            var targetY = _isCrouched ? -CrouchTranslate : 0.0f;
            Head.Position = new Vector3(Head.Position.X, Mathf.MoveToward(Head.Position.Y, targetY, 7.0f * delta), Head.Position.Z);

            var capsule_shape = CollisionShape3D.Shape as CylinderShape3D;
            capsule_shape.Height = _isCrouched ? _originalHeight - CrouchTranslate : _originalHeight;
            CollisionShape3D.Position = new Vector3(CollisionShape3D.Position.X, capsule_shape.Height / 2.0f, CollisionShape3D.Position.Z);
        }

        private bool HandleNoclip(float delta)
        {
            if (_noclip.Poll(delta))
            {
                _isNoclip = !_isNoclip;
                NoclipSpeedMultiplier = 3.0f;
            }

            CollisionShape3D.Disabled = _isNoclip;

            if (!_isNoclip)
            {
                return false;
            }

            var speed = GetMoveSpeed(delta) * NoclipSpeedMultiplier;
            if (_sprint.Poll(delta))
            {
                speed *= 3.0f;
            }

            Body.Velocity = _camAlignedWishDir * speed;
            Body.GlobalPosition += Body.Velocity * delta;

            return true;
        }

        private void ClipVelocity(Vector3 normal, float overbounce, float _delta)
        {
            var backoff = Body.Velocity.Dot(normal) * overbounce;

            if (backoff >= 0.0f)
            {
                return;
            }

            var change = normal * backoff;

            Body.Velocity -= change;

            var adjust = Body.Velocity.Dot(normal);

            if (adjust < 0.0f)
            {
                Body.Velocity -= normal * adjust;
            }
        }

        private bool IsSurfaceTooSteep(Vector3 normal)
        {
            return normal.AngleTo(Vector3.Up) > Body.FloorMaxAngle;
        }

        private void HandleAirPhysics(float delta)
        {
            Body.Velocity = new Vector3(Body.Velocity.X, Body.Velocity.Y - AirGravity * delta, Body.Velocity.Z);

            var normalizedWishDir = _wishDir.Normalized();
            var currentSpeed = Body.Velocity.Dot(normalizedWishDir);
            var cappedSpeed = Mathf.Min((AirMoveSpeed * normalizedWishDir).Length(), AirCap);
            var addSpeedToCap = cappedSpeed - currentSpeed;

            if (addSpeedToCap > 0)
            {
                var accelSpeed = AirAccel * AirMoveSpeed * delta;
                accelSpeed = Mathf.Min(accelSpeed, addSpeedToCap);
                Body.Velocity += accelSpeed * normalizedWishDir;
            }

            if (Body.IsOnWall())
            {
                var wallNormal = Body.GetWallNormal();
                var isWallVertical = Mathf.Abs(wallNormal.Dot(Vector3.Up)) < 0.1f;

                if (IsSurfaceTooSteep(wallNormal) && !isWallVertical)
                {
                    Body.MotionMode = CharacterBody3D.MotionModeEnum.Floating;
                }
                else
                {
                    Body.MotionMode = CharacterBody3D.MotionModeEnum.Grounded;
                }

                ClipVelocity(wallNormal, 1.0f, delta);
            }
        }

        private void ClampSpeed(float delta)
        {
            float maxSpeed = GetMoveSpeed(delta);
            Vector2 velocityPlanar = new(Body.Velocity.X, Body.Velocity.Z);

            if (velocityPlanar.Length() > maxSpeed)
            {
                float clampedSpeed = velocityPlanar.Length() / maxSpeed;
                velocityPlanar /= clampedSpeed;
            }

            Body.Velocity = new(velocityPlanar.X, Body.Velocity.Y, velocityPlanar.Y);
        }

        private void HandleGroundPhysics(float delta)
        {
            var control = Mathf.Max(Body.Velocity.Length(), GroundDecel);
            var drop = control * GroundFriction * delta;
            var newSpeed = Mathf.Max(Body.Velocity.Length() - drop, 0.0f);

            if (Body.Velocity.Length() > 0)
            {
                newSpeed /= Body.Velocity.Length();
            }

            Body.Velocity *= newSpeed;

            var normalizedWishDir = _wishDir.Normalized();
            var speedWishDir = Body.Velocity.Dot(normalizedWishDir);
            var addSpeedTillCap = GetMoveSpeed(delta) - speedWishDir;

            if (addSpeedTillCap > 0)
            {
                var accelerationSpeed = GroundAccel * delta * GetMoveSpeed(delta);
                accelerationSpeed = Mathf.Min(accelerationSpeed, addSpeedTillCap);
                Body.Velocity += accelerationSpeed * normalizedWishDir;
            }

            if (Mathf.IsZeroApprox(Body.Velocity.Y))
            {
                ClampSpeed(delta);
            }

            if (Body.Velocity.Length() > 1.0f)
            {
                _camera.HeadbobEffect(delta);
            }
        }

        private void ApplySlopeBoost()
        {
            var slopeDetect = StairsBelowRayCast3D;

            if (slopeDetect.IsColliding())
            {
                Vector3 floorNormal = Body.GetFloorNormal();
                floorNormal.Y = 0.0f;

                Vector3 normalize = Body.Velocity.Normalized();
                normalize.Y = 0.0f;

                float dotProduct = floorNormal.Dot(normalize);

                if (dotProduct > 0.0f)
                {
                    float length = Body.Velocity.Length();
                    Body.Velocity += floorNormal * length;
                }
            }
        }

        void IPhysicsProcess.PhysicsProcess(float delta)
        {
            if (Body.IsOnFloor())
            {
                _wasFlooredFrame = Engine.GetPhysicsFrames();
            }

            Vector2 inputDir = InputManager.Singleton.GetVector(_left, _right, _forward, _back, InputScope.PlayerMovement, delta);

            _wishDir = _camera.GetWishDir() * new Vector3(inputDir.X, 0.0f, inputDir.Y);
            _camAlignedWishDir = _camera.GetActiveCamera().GlobalTransform.Basis * new Vector3(inputDir.X, 0.0f, inputDir.Y);

            HandleCrouch(delta);

            if (!HandleNoclip(delta) && !HandleLadderPhysics(delta))
            {
                if (!HandleWaterPhysics(delta))
                {
                    if (Body.IsOnFloor() || _wasSnappedToStairs)
                    {
                        if (_wasInAir)
                        {
                            _wasInAir = false;
                            _camera.DoRoll(Mathf.Abs(_previousVelocity.Y));
                            _health.DoFallDamage(Mathf.Abs(_previousVelocity.Y));
                        }

                        if (_jump.Poll(delta) || (AutoBhop && _jump.Poll(delta, InputActionBase.InputActionType.Hold)))
                        {
                            Body.Velocity = new Vector3(Body.Velocity.X, JumpVelocity, Body.Velocity.Z);
                            ApplySlopeBoost();
                        }

                        HandleGroundPhysics(delta);
                    }
                    else
                    {
                        HandleAirPhysics(delta);
                        _wasInAir = true;
                        _previousVelocity = Body.Velocity;
                    }
                }

                if (!SnapUpStairsCheck(delta))
                {
                    PushAwayRigidBodies();
                    Body.MoveAndSlide();
                    SnapDownToStairsCheck();
                }
            }

            _camera.SlideCameraSmoothBackToOrigin(delta);
        }
    }
}
