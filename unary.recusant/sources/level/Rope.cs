using Godot;
using Godot.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unary.Core;

namespace Unary.Recusant
{
    [Tool]
    [GlobalClass]
    [Icon("res://addons/unary.recusant.editor/icons/Rope.svg")]
    public partial class Rope : MeshInstance3D
    {
        [Export]
        public Node3D Next
        {
            get;
            set
            {
                field = value;

                if (field != null && _initialized)
                {
                    RecalculateCenterAndLength();
                    RegenerateMesh();
                }
            }
        }

        [Export]
        public bool Static { get; set; } = true;

        public Vector3 Center { get; private set; }
        public float Length
        {
            get;
            private set
            {
                field = value;
                LengthSquared = value * value;

                Vector4 data2 = GetInstanceShaderParameter(unique_data2).AsVector4();
                data2.X = value;
                SetInstanceShaderParameter(unique_data2, data2);
            }
        }

        public float LengthSquared { get; private set; }

        private bool _initialized = false;

        [Export]
        public RopeMaterial RopeMaterial
        {
            get;
            set
            {
                field?.OnChanged -= RegenerateMesh;
                field = value;
                value?.OnChanged += RegenerateMesh;
            }
        }

        private ArrayMesh _mesh;

        public void RecalculateCenterAndLength()
        {
            if (Next == null)
            {
                Center = Vector3.Zero;
                Length = 1.0f;
                return;
            }

            Center = (GlobalPosition - Next.GlobalPosition) / 2.0f;
            Length = GlobalPosition.DistanceTo(Next.GlobalPosition);
        }

        UpdaterHandle _moveUpdate;
        UpdaterHandle _windUpdate;

        public override void _Ready()
        {
            _mesh = new ArrayMesh();
            Mesh = _mesh;
            RecalculateCenterAndLength();
            RegenerateMesh();
            _initialized = true;

            WindManager.Instance?.AddRope(this);

#if TOOLS
            if (!Engine.Singleton.IsEditorHint())
            {
                _moveUpdate = Updater.Singleton.Process.SubscribeDelayed(0.05f, MoveUpdate);
                _windUpdate = Updater.Singleton.Process.SubscribeDelayed(0.23f, WindUpdate);
            }
#endif
        }

        public override void _ExitTree()
        {
#if TOOLS
            if (!Engine.Singleton.IsEditorHint())
            {
                Updater.Singleton.Process.UnsubscribeRange(_moveUpdate);
                Updater.Singleton.Process.UnsubscribeRange(_windUpdate);
            }
#endif

            WindManager.Instance?.RemoveRope(this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void MoveUpdate(float delta)
        {
            if (Next == null)
            {
                return;
            }

            if (Static)
            {
#if TOOLS
                if (Engine.Singleton.IsEditorHint() && HasMoved())
                {
                    RecalculateCenterAndLength();
                    RegenerateMesh();
                }
#else
                return;
#endif
            }

            if (HasMoved())
            {
                RecalculateCenterAndLength();
                RegenerateMesh();
            }
        }

#if TOOLS
        public override void _Process(double delta)
        {
            if (Engine.Singleton.IsEditorHint())
            {
                MoveUpdate((float)delta);
                WindUpdate((float)delta);
            }
        }
#endif

        /*
            // TODO Create a separation based on proximity for wind sources and ONLY CHANGE WHEN WE GOT DELTA
            Vector3 position = Vector3.Zero;
            float strength = 0.0f;

            Vector4 uniqueData1 = new(position.X, position.Y, position.Z, strength);
            float radius = 0.0f;

            foreach (var rope in _ropes)
            {
                float length = rope.Length;
                Vector4 uniqueData2 = new(radius, length, 0.0f, 0.0f);

                rope.SetInstanceShaderParameter(unique_data1, uniqueData1);
                rope.SetInstanceShaderParameter(unique_data2, uniqueData2);
            }
        */

        /// Normalises each value from [0, 50] → [0, 1], then uses
        /// packUnorm2x16 semantics: each channel gets 16 bits → 65535 steps.
        /// Precision ≈ 50 / 65535 ≈ 0.000763 per step.
        /// Unpack with: unpackUnorm2x16(floatBitsToUint(v)) * 50.0

        public static float PackHalf2x16(float x, float y)
        {
            uint hx = FloatToHalf(x);   // 16-bit half → lower word
            uint hy = FloatToHalf(y);   // 16-bit half → upper word
            uint packed = (hy << 16) | hx;
            return UIntBitsToFloat(packed);
        }

        // ── Float32 → Float16 (IEEE 754 half-precision) ───────────────
        private static uint FloatToHalf(float f)
        {
            uint bits = FloatBitsToUInt(f);

            uint sign = (bits >> 16) & 0x8000u;
            int exponent = (int)((bits >> 23) & 0xFFu) - 127 + 15;
            uint mantissa = (bits >> 13) & 0x03FFu;

            if (exponent <= 0) return sign;                   // underflow → ±0
            if (exponent >= 31) return sign | 0x7C00u;          // overflow  → ±Inf

            return sign | ((uint)exponent << 10) | mantissa;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct FloatUIntUnion
        {
            [FieldOffset(0)] public float FloatValue;
            [FieldOffset(0)] public uint UIntValue;
        }

        private static uint FloatBitsToUInt(float f) => new FloatUIntUnion { FloatValue = f }.UIntValue;
        private static float UIntBitsToFloat(uint u) => new FloatUIntUnion { UIntValue = u }.FloatValue;

        private WindSource _pickedPrevious = null;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WindUpdate(float delta)
        {
#if TOOLS
            if (WindManager.Instance == null)
            {
                this.Error($"WindManager was null");
                return;
            }
#endif

            IReadOnlyList<WindSource> windSources = WindManager.Instance.GetWindSources();

            WindSource picked = null;
            float strength = 0.0f;

            for (int i = 0; i < windSources.Count; i++)
            {
                WindSource windSource = windSources[i];

                if (Center.DistanceSquaredTo(windSource.Position) < windSource.RadiusSquared + LengthSquared)
                {
                    if (windSource.Strength > strength)
                    {
                        picked = windSource;
                        strength = windSource.Strength;
                    }
                }
            }

            // We do nothing
            if (picked == null && _pickedPrevious == null)
            {

            }
            // Reset once since we no longer got a reference
            else if (picked == null && _pickedPrevious != null)
            {
                SetInstanceShaderParameter(unique_data2, Vector4.Zero);
            }
            // Update every frame
            else
            {
                Vector3 position = picked.GlobalPosition;
                Vector4 data = new(position.X, position.Y, position.Z, PackHalf2x16(picked.Strength, picked.Radius));
                SetInstanceShaderParameter(unique_data2, data);
            }

            _pickedPrevious = picked;
        }

        private Vector3 _position1;
        private Vector3 _position2;

        private bool HasMoved()
        {
            Vector3 position1 = GlobalPosition;
            Vector3 position2 = Next.GlobalPosition;

            if (_position1 != position1 || _position2 != position2)
            {
                _position1 = position1;
                _position2 = position2;
                return true;
            }

            return false;
        }

        private static readonly StringName mesh = new(nameof(mesh));
        private static readonly StringName unique_data1 = new(nameof(unique_data1));
        private static readonly StringName unique_data2 = new(nameof(unique_data2));

        public override void _ValidateProperty(Dictionary property)
        {
            property.MakeNone(mesh);
            base._ValidateProperty(property);
        }

#if TOOLS

        public override void _Notification(int what)
        {
            if (what == NotificationEditorPreSave)
            {
                // Set runtime-only uniforms to something that wont create git diffs
                SetInstanceShaderParameter(unique_data1, false);
                SetInstanceShaderParameter(unique_data2, false);
            }
        }

#endif

        public void RegenerateMesh()
        {
            TryBuildingCache();

            if (_mesh == null || RopeMaterial == null ||
                Next == null || !IsInsideTree() || !Next.IsInsideTree())
            {
                return;
            }

            while (_mesh.GetSurfaceCount() > 0)
            {
                _mesh.SurfaceRemove(0);
            }

            Vector3 start = Vector3.Zero;
            Vector3 end = Next != null ? ToLocal(Next.GlobalPosition) : Vector3.Zero;

            SampleCatenary(start, end, RopeMaterial.Segments);

            BuildTubeMesh();
        }

        private Vector3[] SampleCatenary(Vector3 p0, Vector3 p1, int segments)
        {
            SetupSpineCache(segments + 1);

            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;

                Vector3 lerped = p0.Lerp(p1, t);

                float sagOffset = 4f * RopeMaterial.Sag * t * (1f - t);

                lerped.Y -= sagOffset;

                _spineCache[i] = lerped;
            }

            return _spineCache;
        }

        private void BuildTubeMesh()
        {
            int rings = _spineCache.Length;
            int sides = RopeMaterial.Sides;
            int vCount = rings * (sides + 1);

            SetupVerticesCache(vCount);
            SetupNormalsCache(vCount);
            SetupUvCache(vCount);

            for (int r = 0; r < rings; r++)
            {
                Vector3 forward;
                if (r < rings - 1)
                {
                    forward = (_spineCache[r + 1] - _spineCache[r]).Normalized();
                }
                else
                {
                    forward = (_spineCache[r] - _spineCache[r - 1]).Normalized();
                }

                Vector3 up = Vector3.Up;
                // Avoid degeneracy when forward is approximately up
                if (Mathf.Abs(forward.Dot(up)) > 0.99f)
                {
                    up = Vector3.Right;
                }

                Vector3 right = forward.Cross(up).Normalized();
                up = right.Cross(forward).Normalized();

                float vCoord = (float)r / (rings - 1);

                for (int s = 0; s <= sides; s++)
                {
                    float angle = (float)s / sides * Mathf.Tau;
                    float cosA = Mathf.Cos(angle);
                    float sinA = Mathf.Sin(angle);

                    Vector3 normal = (right * cosA + up * sinA).Normalized();
                    Vector3 pos = _spineCache[r] + normal * RopeMaterial.Thickness;

                    int idx = r * (sides + 1) + s;
                    _verticesCache[idx] = pos;
                    _normalsCache[idx] = normal;
                    _uvCache[idx] = new Vector2((float)s / sides, vCoord);
                }
            }

            int triCount = (rings - 1) * sides * 2;

            SetupIndicesCache(triCount * 3);

            int ti = 0;

            for (int r = 0; r < rings - 1; r++)
            {
                for (int s = 0; s < sides; s++)
                {
                    int curr = r * (sides + 1) + s;
                    int next = r * (sides + 1) + s + 1;
                    int currN = (r + 1) * (sides + 1) + s;
                    int nextN = (r + 1) * (sides + 1) + s + 1;

                    // Triangle 1
                    _indicesCache[ti++] = curr;
                    _indicesCache[ti++] = currN;
                    _indicesCache[ti++] = next;

                    // Triangle 2
                    _indicesCache[ti++] = next;
                    _indicesCache[ti++] = currN;
                    _indicesCache[ti++] = nextN;
                }
            }

            PassArraysToSurface();

            _mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, _sufraceArrayCache);

            if (RopeMaterial.Material != null)
            {
                _mesh.SurfaceSetMaterial(0, RopeMaterial.Material);
            }
        }
    }
}
