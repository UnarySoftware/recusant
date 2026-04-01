using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using Unary.Core;

namespace Unary.Recusant
{
    [Tool]
    [GlobalClass]
    public partial class Player : Component, IPoolable, IProcess, IPhysicsProcess
    {
        [Export]
        public CharacterBody3D Body;

        [Export]
        public float LookSensitivity { get; set; } = 0.002f;

        [Export]
        public float JumpVelocity { get; set; } = 7.675f;

        [Export]
        public bool AutoBhop { get; set; } = true;

        [Export]
        public float HEADBOB_MOVE_AMOUNT = 0.03f;

        [Export]
        public float HEADBOB_FREQUENCY = 3.0f;

        private float headbob_time = 0.0f;

        // Ground movement settings
        [Export]
        public float WalkSpeed { get; set; } = 5.6f;

        [Export]
        public float SprintSpeed { get; set; } = 6.5f;

        [Export]
        public float GroundAccel { get; set; } = 11.0f;

        [Export]
        public float GroundDecel { get; set; } = 7.0f;

        [Export]
        public float GroundFriction { get; set; } = 2.0f;

        [Export]
        public float MaxVelocityGround { get; set; } = 7.0f;

        // Air movement settings
        [Export]
        public float AirCap { get; set; } = 0.85f;

        [Export]
        public float AirAccel { get; set; } = 8.0f;

        [Export]
        public float AirMoveSpeed { get; set; } = 5.0f;

        [Export]
        public float SwimUpSpeed { get; set; } = 10.0f;

        [Export]
        public float ClimbSpeed { get; set; } = 7.0f;

        [Export]
        public float Health { get; set; } = 100.0f;

        [Export]
        public float MaxHealth { get; set; } = 100.0f;

        public void TakeDamage(float damage)
        {
            Health -= damage;
        }

        private Vector3 wish_dir = Vector3.Zero;
        private Vector3 cam_aligned_wish_dir = Vector3.Zero;

        private const float CROUCH_TRANSLATE = 0.422f;
        private const float CROUCH_JUMP_ADD = CROUCH_TRANSLATE * 0.9f;
        private bool is_crouched = false;

        private float noclip_speed_mult = 3.0f;
        private bool noclip = false;

        private const float MAX_STEP_HEIGHT = 0.46f;
        private bool _snapped_to_stairs_last_frame = false;
        private ulong _last_frame_was_on_floor = 0;

        private Vector3? _saved_camera_global_pos = null;
        private Area3D _cur_ladder_climbing = null;
        private float _original_capsule_height = 0.0f;

        [Export]
        public CollisionShape3D CollisionShape3D;

        [Export]
        public Camera3D Camera3D;

        [Export]
        public Node3D CameraSmooth;

        [Export]
        public RayCast3D StairsBelowRayCast3D;

        [Export]
        public RayCast3D StairsAheadRayCast3D;

        [Export]
        public Node3D Head;

        //[Export]
        //public Label Label;

        //[Export]
        //public Label Label2;

        public float GetMoveSpeed()
        {
            if (is_crouched)
                return WalkSpeed * 0.34f;
            return Input.Singleton.IsActionPressed("sprint") ? SprintSpeed : WalkSpeed;
        }

        private List<Area3D> _ladders = [];
        private List<Area3D> _water = [];

        public static Player Instance;

        SlotHandle processSlot;
        SlotHandle physicsProcessSlot;

        void IPoolable.Aquire()
        {
            Camera3D.Current = true;
            Instance = this;
            processSlot = Updater.Singleton.Process.Subscribe(this);
            physicsProcessSlot = Updater.Singleton.PhysicsProcess.Subscribe(this);
        }

        void IPoolable.Release()
        {
            Camera3D.Current = false;
            Instance = null;
            Updater.Singleton.Process.Unsubscribe(processSlot);
            Updater.Singleton.PhysicsProcess.Unsubscribe(physicsProcessSlot);
        }

        public override void Initialize()
        {
            _ladders = [.. GetTree().GetNodesInGroup("ladder_area3d").Cast<Area3D>()];
            _water = [.. GetTree().GetNodesInGroup("water_area").Cast<Area3D>()];

            _original_capsule_height = (CollisionShape3D.Shape as CylinderShape3D).Height;
            CameraSmooth.PhysicsInterpolationMode = Node.PhysicsInterpolationModeEnum.Off;

            Input.Singleton.MouseMode = Input.MouseModeEnum.Captured;
        }

        [Export]
        public float UnrollStep = 25.0f;

        void IProcess.Process(float delta)
        {
            Vector3 rotation = Camera3D.RotationDegrees;
            if (rotation.Z > 0.0f)
            {
                rotation.Z = Mathf.Clamp(rotation.Z - UnrollStep * delta, 0.0f, 30.0f);
            }
            Camera3D.RotationDegrees = rotation;
        }

        public override void _Input(InputEvent @event)
        {
            if (Input.Singleton.MouseMode == Input.MouseModeEnum.Captured)
            {
                if (@event is InputEventMouseMotion mouseMotion)
                {
                    var camera3D = Camera3D;

                    Body.RotateY(-mouseMotion.Relative.X * LookSensitivity);

                    camera3D.RotateX(-mouseMotion.Relative.Y * LookSensitivity);

                    // Clamp the X rotation to prevent flipping
                    var rot_deg = camera3D.RotationDegrees;
                    rot_deg.X = Mathf.Clamp(rot_deg.X, -89.9f, 89.9f);
                    rot_deg.Y = 0.0f;
                    rot_deg.Z = Mathf.Clamp(rot_deg.Z, 0.0f, 30.0f); ;
                    camera3D.RotationDegrees = rot_deg;
                }

                if (@event is InputEventMouseButton mouseButton && mouseButton.Pressed)
                {
                    if (mouseButton.ButtonIndex == MouseButton.WheelUp)
                        noclip_speed_mult = Mathf.Min(100.0f, noclip_speed_mult * 1.1f);
                    else if (mouseButton.ButtonIndex == MouseButton.WheelDown)
                        noclip_speed_mult = Mathf.Max(0.1f, noclip_speed_mult * 0.9f);
                }
            }
        }

        private Camera3D GetActiveCamera()
        {
            return Camera3D;
        }

        private void HeadbobEffect(float delta)
        {
            headbob_time += delta * Body.Velocity.Length();
            var camera3D = Camera3D;
            camera3D.Transform = new Transform3D(
                camera3D.Transform.Basis,
                new Vector3(
                    Mathf.Cos(headbob_time * HEADBOB_FREQUENCY * 0.5f) * HEADBOB_MOVE_AMOUNT,
                    Mathf.Sin(headbob_time * HEADBOB_FREQUENCY) * HEADBOB_MOVE_AMOUNT,
                    0
                )
            );
        }

        private void SaveCameraPosForSmoothing()
        {
            if (_saved_camera_global_pos == null)
                _saved_camera_global_pos = CameraSmooth.GlobalPosition;
        }

        private void SlideCameraSmoothBackToOrigin(float delta)
        {
            if (_saved_camera_global_pos == null)
                return;

            var cameraSmooth = CameraSmooth;
            cameraSmooth.GlobalPosition = new Vector3(
                cameraSmooth.GlobalPosition.X,
                _saved_camera_global_pos.Value.Y,
                cameraSmooth.GlobalPosition.Z
            );

            cameraSmooth.Position = new Vector3(
                cameraSmooth.Position.X,
                Mathf.Clamp(cameraSmooth.Position.Y, -CROUCH_TRANSLATE, CROUCH_TRANSLATE),
                cameraSmooth.Position.Z
            );

            var move_amount = Mathf.Max(Body.Velocity.Length() * delta, WalkSpeed / 2 * delta);
            cameraSmooth.Position = new Vector3(
                cameraSmooth.Position.X,
                Mathf.MoveToward(cameraSmooth.Position.Y, 0.0f, move_amount),
                cameraSmooth.Position.Z
            );

            _saved_camera_global_pos = cameraSmooth.GlobalPosition;

            if (cameraSmooth.Position.Y == 0)
                _saved_camera_global_pos = null;
        }

        private void PushAwayRigidBodies()
        {
            for (int i = 0; i < Body.GetSlideCollisionCount(); i++)
            {
                var collision = Body.GetSlideCollision(i);
                if (collision.GetCollider() is RigidBody3D rigidBody)
                {
                    var push_dir = -collision.GetNormal();
                    var velocity_diff_in_push_dir = Body.Velocity.Dot(push_dir) - rigidBody.LinearVelocity.Dot(push_dir);
                    velocity_diff_in_push_dir = Mathf.Max(0.0f, velocity_diff_in_push_dir);

                    const float MY_APPROX_MASS_KG = 80.0f;
                    var mass_ratio = Mathf.Min(1.0f, MY_APPROX_MASS_KG / rigidBody.Mass);

                    if (mass_ratio < 0.25f)
                        continue;

                    push_dir.Y = 0;
                    var push_force = mass_ratio * 5.0f;
                    rigidBody.ApplyImpulse(push_dir * velocity_diff_in_push_dir * push_force, collision.GetPosition() - rigidBody.GlobalPosition);
                }
            }
        }

        private void SnapDownToStairsCheck()
        {
            bool did_snap = false;
            var stairsBelowRaycast = StairsBelowRayCast3D;
            stairsBelowRaycast.ForceRaycastUpdate();

            var floor_below = stairsBelowRaycast.IsColliding() && !IsSurfaceTooSteep(stairsBelowRaycast.GetCollisionNormal());
            var was_on_floor_last_frame = Engine.GetPhysicsFrames() == _last_frame_was_on_floor;

            if (!Body.IsOnFloor() && Body.Velocity.Y <= 0 && (was_on_floor_last_frame || _snapped_to_stairs_last_frame) && floor_below)
            {
                var body_test_result = new KinematicCollision3D();
                if (Body.TestMove(Body.GlobalTransform, new Vector3(0, -MAX_STEP_HEIGHT, 0), body_test_result))
                {
                    SaveCameraPosForSmoothing();
                    var translate_y = body_test_result.GetTravel().Y;
                    Body.Position = new Vector3(Body.Position.X, Body.Position.Y + translate_y, Body.Position.Z);
                    Body.ApplyFloorSnap();
                    did_snap = true;
                }
            }

            _snapped_to_stairs_last_frame = did_snap;
        }

        private bool SnapUpStairsCheck(float delta)
        {
            if (!Body.IsOnFloor() && !_snapped_to_stairs_last_frame)
                return false;

            if (Body.Velocity.Y > 0 || (Body.Velocity * new Vector3(1, 0, 1)).Length() == 0)
                return false;

            var expected_move_motion = Body.Velocity * new Vector3(1, 0, 1) * delta;
            var step_pos_with_clearance = Body.GlobalTransform.Translated(expected_move_motion + new Vector3(0, MAX_STEP_HEIGHT * 2, 0));

            var down_check_result = new KinematicCollision3D();
            if (Body.TestMove(step_pos_with_clearance, new Vector3(0, -MAX_STEP_HEIGHT * 2, 0), down_check_result) &&
                (down_check_result.GetCollider().IsClass("StaticBody3D") || down_check_result.GetCollider().IsClass("CSGShape3D")))
            {
                var step_height = (step_pos_with_clearance.Origin + down_check_result.GetTravel() - Body.GlobalPosition).Y;

                if (step_height > MAX_STEP_HEIGHT || step_height <= 0.01f || (down_check_result.GetPosition() - Body.GlobalPosition).Y > MAX_STEP_HEIGHT)
                    return false;

                var stairsAheadRaycast = StairsAheadRayCast3D;
                stairsAheadRaycast.GlobalPosition = down_check_result.GetPosition() + new Vector3(0, MAX_STEP_HEIGHT, 0) + expected_move_motion.Normalized() * 0.1f;
                stairsAheadRaycast.ForceRaycastUpdate();

                if (stairsAheadRaycast.IsColliding() && !IsSurfaceTooSteep(stairsAheadRaycast.GetCollisionNormal()))
                {
                    SaveCameraPosForSmoothing();
                    Body.GlobalPosition = step_pos_with_clearance.Origin + down_check_result.GetTravel();
                    Body.ApplyFloorSnap();
                    _snapped_to_stairs_last_frame = true;
                    return true;
                }
            }

            return false;
        }

        private bool HandleLadderPhysics()
        {
            var was_climbing_ladder = _cur_ladder_climbing != null && _cur_ladder_climbing.OverlapsBody(Body);

            if (!was_climbing_ladder)
            {
                _cur_ladder_climbing = null;
                foreach (var ladder in _ladders)
                {
                    if (ladder.OverlapsBody(Body))
                    {
                        _cur_ladder_climbing = ladder;
                        break;
                    }
                }
            }

            if (_cur_ladder_climbing == null)
                return false;

            var ladder_gtransform = _cur_ladder_climbing.GlobalTransform;
            var pos_rel_to_ladder = ladder_gtransform.AffineInverse() * Body.GlobalPosition;

            var forward_move = Input.Singleton.GetActionStrength("up") - Input.Singleton.GetActionStrength("down");
            var side_move = Input.Singleton.GetActionStrength("right") - Input.Singleton.GetActionStrength("left");

            var active_camera = GetActiveCamera();
            var ladder_forward_move = ladder_gtransform.AffineInverse().Basis * active_camera.GlobalTransform.Basis * new Vector3(0, 0, -forward_move);
            var ladder_side_move = ladder_gtransform.AffineInverse().Basis * active_camera.GlobalTransform.Basis * new Vector3(side_move, 0, 0);

            var ladder_strafe_vel = ClimbSpeed * (ladder_side_move.X + ladder_forward_move.X);
            var ladder_climb_vel = ClimbSpeed * -ladder_side_move.Z;

            var up_wish = Vector3.Up.Rotated(Vector3.Right, Mathf.DegToRad(-45)).Dot(ladder_forward_move);
            ladder_climb_vel += ClimbSpeed * up_wish;

            var should_dismount = false;

            if (!was_climbing_ladder)
            {
                var topOfLadder = _cur_ladder_climbing.GetNode<Node3D>("TopOfLadder");
                var mounting_from_top = pos_rel_to_ladder.Y > topOfLadder.Position.Y;

                if (mounting_from_top)
                {
                    if (ladder_climb_vel > 0)
                        should_dismount = true;
                }
                else
                {
                    if ((ladder_gtransform.AffineInverse().Basis * wish_dir).Z >= 0)
                        should_dismount = true;
                }

                if (Mathf.Abs(pos_rel_to_ladder.Z) > 0.1f)
                    should_dismount = true;
            }

            if (Body.IsOnFloor() && ladder_climb_vel <= 0)
                should_dismount = true;

            if (should_dismount)
            {
                _cur_ladder_climbing = null;
                return false;
            }

            if (was_climbing_ladder && Input.Singleton.IsActionJustPressed("jump"))
            {
                Body.Velocity = _cur_ladder_climbing.GlobalTransform.Basis.Z * JumpVelocity * 1.5f;
                _cur_ladder_climbing = null;
                return false;
            }

            Body.Velocity = ladder_gtransform.Basis * new Vector3(ladder_strafe_vel, ladder_climb_vel, 0);

            pos_rel_to_ladder.Z = 0;
            Body.GlobalPosition = ladder_gtransform * pos_rel_to_ladder;

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

            // Gravity constant
            if (!Body.IsOnFloor())
                Body.Velocity = new Vector3(Body.Velocity.X, Body.Velocity.Y - 15.24f * 0.1f * delta, Body.Velocity.Z);

            Body.Velocity += cam_aligned_wish_dir * GetMoveSpeed() * delta;

            if (Input.Singleton.IsActionPressed("jump"))
                Body.Velocity = new Vector3(Body.Velocity.X, Body.Velocity.Y + SwimUpSpeed * delta, Body.Velocity.Z);

            Body.Velocity = Body.Velocity.Lerp(Vector3.Zero, 2 * delta);

            return true;
        }

        private void HandleCrouch(float delta)
        {
            var was_crouched_last_frame = is_crouched;

            if (Input.Singleton.IsActionPressed("crouch"))
                is_crouched = true;
            else if (is_crouched && !Body.TestMove(Body.GlobalTransform, new Vector3(0, CROUCH_TRANSLATE, 0)))
                is_crouched = false;

            var translate_y_if_possible = 0.0f;
            if (was_crouched_last_frame != is_crouched && !Body.IsOnFloor() && !_snapped_to_stairs_last_frame)
                translate_y_if_possible = is_crouched ? CROUCH_JUMP_ADD : -CROUCH_JUMP_ADD;

            if (translate_y_if_possible != 0.0f)
            {
                var result = new KinematicCollision3D();
                Body.TestMove(Body.GlobalTransform, new Vector3(0, translate_y_if_possible, 0), result);
                Body.Position = new Vector3(Body.Position.X, Body.Position.Y + result.GetTravel().Y, Body.Position.Z);

                var head = Head;
                head.Position = new Vector3(head.Position.X, head.Position.Y - result.GetTravel().Y, head.Position.Z);
                head.Position = new Vector3(head.Position.X, Mathf.Clamp(head.Position.Y, -CROUCH_TRANSLATE, 0), head.Position.Z);
            }

            var head_node = Head;
            var target_y = is_crouched ? -CROUCH_TRANSLATE : 0.0f;
            head_node.Position = new Vector3(head_node.Position.X, Mathf.MoveToward(head_node.Position.Y, target_y, 7.0f * delta), head_node.Position.Z);

            var collision_shape = CollisionShape3D;
            var capsule_shape = collision_shape.Shape as CylinderShape3D;
            capsule_shape.Height = is_crouched ? _original_capsule_height - CROUCH_TRANSLATE : _original_capsule_height;
            collision_shape.Position = new Vector3(collision_shape.Position.X, capsule_shape.Height / 2, collision_shape.Position.Z);
        }

        private bool HandleNoclip(float delta)
        {
            if (Input.Singleton.IsActionJustPressed("noclip") && OS.HasFeature("debug"))
            {
                noclip = !noclip;
                noclip_speed_mult = 3.0f;
            }

            CollisionShape3D.Disabled = noclip;

            if (!noclip)
                return false;

            var speed = GetMoveSpeed() * noclip_speed_mult;
            if (Input.Singleton.IsActionPressed("sprint"))
                speed *= 3.0f;

            Body.Velocity = cam_aligned_wish_dir * speed;
            Body.GlobalPosition += Body.Velocity * delta;

            return true;
        }

        private void ClipVelocity(Vector3 normal, float overbounce, float _delta)
        {
            var backoff = Body.Velocity.Dot(normal) * overbounce;

            if (backoff >= 0)
                return;

            var change = normal * backoff;
            Body.Velocity -= change;

            var adjust = Body.Velocity.Dot(normal);
            if (adjust < 0.0f)
                Body.Velocity -= normal * adjust;
        }

        private bool IsSurfaceTooSteep(Vector3 normal)
        {
            return normal.AngleTo(Vector3.Up) > Body.FloorMaxAngle;
        }

        private void HandleAirPhysics(float delta)
        {
            // Gravity contstant
            Body.Velocity = new Vector3(Body.Velocity.X, Body.Velocity.Y - 20.32f * delta, Body.Velocity.Z);

            var normalized_wish_dir = wish_dir.Normalized();
            var cur_speed_in_wish_dir = Body.Velocity.Dot(normalized_wish_dir);
            var capped_speed = Mathf.Min((AirMoveSpeed * normalized_wish_dir).Length(), AirCap);
            var add_speed_till_cap = capped_speed - cur_speed_in_wish_dir;

            if (add_speed_till_cap > 0)
            {
                var accel_speed = AirAccel * AirMoveSpeed * delta;
                accel_speed = Mathf.Min(accel_speed, add_speed_till_cap);
                Body.Velocity += accel_speed * normalized_wish_dir;
            }

            if (Body.IsOnWall())
            {
                var wall_normal = Body.GetWallNormal();
                var is_wall_vertical = Mathf.Abs(wall_normal.Dot(Vector3.Up)) < 0.1f;

                if (IsSurfaceTooSteep(wall_normal) && !is_wall_vertical)
                    Body.MotionMode = CharacterBody3D.MotionModeEnum.Floating;
                else
                    Body.MotionMode = CharacterBody3D.MotionModeEnum.Grounded;

                ClipVelocity(wall_normal, 1.0f, delta);
            }
        }

        private void _clamp_speed()
        {
            float max_speed = GetMoveSpeed();
            Vector2 velocity_planar = new(Body.Velocity.X, Body.Velocity.Z);

            if (velocity_planar.Length() > max_speed)
            {
                float clamped_speed = velocity_planar.Length() / max_speed;
                velocity_planar /= clamped_speed;
            }

            Body.Velocity = new(velocity_planar.X, Body.Velocity.Y, velocity_planar.Y);
        }

        private void HandleGroundPhysics(float delta)
        {
            var control = Mathf.Max(Body.Velocity.Length(), GroundDecel);
            var drop = control * GroundFriction * delta;
            var new_speed = Mathf.Max(Body.Velocity.Length() - drop, 0.0f);

            if (Body.Velocity.Length() > 0)
                new_speed /= Body.Velocity.Length();

            Body.Velocity *= new_speed;

            var normalized_wish_dir = wish_dir.Normalized();
            var cur_speed_in_wish_dir = Body.Velocity.Dot(normalized_wish_dir);
            var add_speed_till_cap = GetMoveSpeed() - cur_speed_in_wish_dir;

            if (add_speed_till_cap > 0)
            {
                var accel_speed = GroundAccel * delta * GetMoveSpeed();
                accel_speed = Mathf.Min(accel_speed, add_speed_till_cap);
                Body.Velocity += accel_speed * normalized_wish_dir;
            }

            if (Mathf.IsZeroApprox(Body.Velocity.Y))
            {
                _clamp_speed();
            }

            if (Body.Velocity.Length() > 1.0f)
            {
                HeadbobEffect(delta);
            }
        }

        private void ApplySlopeBoost()
        {
            var slope_detect = StairsBelowRayCast3D;

            if (slope_detect.IsColliding())
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

        Vector3 _previousVelocity;
        bool InAir = false;

        private float fallBase = 3.0f;
        private float fallPow = 2.1f;

        private float GetDamage(float verticalVelocity, float minVelocity)
        {
            float absVelocity = Mathf.Abs(verticalVelocity) - Mathf.Abs(minVelocity);
            return MathF.Ceiling(fallBase * Mathf.Pow(absVelocity, fallPow));
        }

        public float Damage = 0.0f;

        private StringName left = new(nameof(left));
        private StringName right = new(nameof(right));
        private StringName up = new(nameof(up));
        private StringName down = new(nameof(down));
        private StringName jump = new(nameof(jump));

        void IPhysicsProcess.PhysicsProcess(float delta)
        {
            float deltaFloat = (float)delta;

            if (Body.IsOnFloor())
                _last_frame_was_on_floor = Engine.GetPhysicsFrames();

            var input_dir = Input.Singleton.GetVector(left, right, up, down);
            wish_dir = Body.GlobalTransform.Basis * new Vector3(input_dir.X, 0.0f, input_dir.Y);
            cam_aligned_wish_dir = GetActiveCamera().GlobalTransform.Basis * new Vector3(input_dir.X, 0.0f, input_dir.Y);

            HandleCrouch(deltaFloat);

            if (!HandleNoclip(deltaFloat) && !HandleLadderPhysics())
            {
                if (!HandleWaterPhysics(deltaFloat))
                {
                    if (Body.IsOnFloor() || _snapped_to_stairs_last_frame)
                    {
                        if (InAir)
                        {
                            InAir = false;

                            float Remap = Mathf.Remap(Mathf.Abs(_previousVelocity.Y), 0.0f, 75.0f, 0.0f, 30.0f);

                            Vector3 rotation = Camera3D.RotationDegrees;
                            rotation.Z = Remap;

                            Camera3D.RotationDegrees = rotation;

                            if (_previousVelocity.Y <= -9.9f)
                            {
                                Damage = GetDamage(_previousVelocity.Y, -9.9f);
                            }
                        }

                        if (Input.Singleton.IsActionJustPressed(jump) || (AutoBhop && Input.Singleton.IsActionPressed(jump)))
                        {
                            Body.Velocity = new Vector3(Body.Velocity.X, JumpVelocity, Body.Velocity.Z);
                            ApplySlopeBoost();
                        }

                        HandleGroundPhysics(deltaFloat);
                    }
                    else
                    {
                        HandleAirPhysics(deltaFloat);
                        InAir = true;
                        _previousVelocity = Body.Velocity;
                    }
                }

                if (!SnapUpStairsCheck(deltaFloat))
                {
                    PushAwayRigidBodies();
                    Body.MoveAndSlide();
                    SnapDownToStairsCheck();
                }
            }

            SlideCameraSmoothBackToOrigin(deltaFloat);

            //Label.Text = Velocity.Length().ToString("0.0 m/s");
            //Label2.Text = Damage + " dmg";
        }
    }
}
