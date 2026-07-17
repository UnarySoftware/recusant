using Godot;
using System.Collections.Generic;

namespace Unary.Recusant
{
    [Tool]
    [GlobalClass]
    public partial class WindManager : Node
    {
        private bool _initialized = false;

        public static WindManager Instance { get; private set; }

        [Export]
        public Vector3 WindDirection
        {
            get;
            set
            {
                field = value;
                if (_initialized)
                {
                    PushUniforms();
                }
            }
        } = new Vector3(1f, 0f, 0f);

        [Export(PropertyHint.Range, "0,10,0.01")]
        public float WindSpeed
        {
            get;
            set
            {
                field = value;
                if (_initialized)
                {
                    PushUniforms();
                }
            }
        } = 1.2f;

        [Export(PropertyHint.Range, "10,100,1.0")]
        public float MaxRopeLength
        {
            get;
            set
            {
                field = value;
                if (_initialized)
                {
                    PushUniforms();
                }
            }
        } = 20.0f;

        [Export(PropertyHint.Range, "0,10,0.01")]
        public float GustStrength
        {
            get;
            set
            {
                field = value;
                if (_initialized)
                {
                    PushUniforms();
                }
            }
        } = 0.3f;

        [Export(PropertyHint.Range, "0,5,0.01")]
        public float GustAmplitude
        {
            get;
            set
            {
                field = value;
                if (_initialized)
                {
                    PushUniforms();
                }
            }
        } = 0.1f;

        [Export(PropertyHint.Range, "0.01,5,0.01")]
        public float GustFrequency
        {
            get;
            set
            {
                field = value;
                if (_initialized)
                {
                    PushUniforms();
                }
            }
        } = 0.2f;

        private readonly Dictionary<ShaderMaterial, HashSet<Rope>> _ropeMaterials = [];
        private readonly List<Rope> _ropes = [];
        private readonly List<WindSource> _windSources = [];

        private static readonly StringName global_wind_dir = new(nameof(global_wind_dir));
        private static readonly StringName global_wind_speed = new(nameof(global_wind_speed));
        private static readonly StringName global_wind_strength = new(nameof(global_wind_strength));
        private static readonly StringName global_wind_amplitude = new(nameof(global_wind_amplitude));
        private static readonly StringName global_wind_frequency = new(nameof(global_wind_frequency));

        private void PushUniforms(ShaderMaterial target)
        {
            target.SetShaderParameter(global_wind_dir, WindDirection);
            target.SetShaderParameter(global_wind_speed, WindSpeed);
            target.SetShaderParameter(global_wind_strength, GustStrength);
            target.SetShaderParameter(global_wind_amplitude, GustAmplitude);
            target.SetShaderParameter(global_wind_frequency, GustFrequency);
        }

        private void PushUniforms()
        {
            foreach (var material in _ropeMaterials)
            {
                PushUniforms(material.Key);
            }
        }

        public IReadOnlyList<Rope> GetRopes()
        {
            return _ropes;
        }

        public IReadOnlyList<WindSource> GetWindSources()
        {
            return _windSources;
        }

        public void UpdateRopeMaterial(Rope rope, ShaderMaterial previousMaterial, ShaderMaterial newMaterial)
        {
            if (rope == null)
            {
                return;
            }

            PushUniforms(newMaterial);

            if (!_ropeMaterials.TryGetValue(newMaterial, out var newEntries))
            {
                newEntries = [];
                _ropeMaterials.Add(newMaterial, newEntries);
            }

            newEntries.Add(rope);

            if (previousMaterial == null)
            {
                return;
            }

            if (_ropeMaterials.TryGetValue(previousMaterial, out var previousEntries))
            {
                previousEntries.Remove(rope);

                if (previousEntries.Count == 0)
                {
                    _ropeMaterials.Remove(previousMaterial);
                }
            }
        }

        public void AddRope(Rope rope)
        {
            _ropes.Add(rope);

            if (rope.RopeMaterial == null || rope.RopeMaterial.Material == null)
            {
                return;
            }

            ShaderMaterial material = rope.RopeMaterial.Material;
            PushUniforms(material);

            if (!_ropeMaterials.TryGetValue(material, out var newEntries))
            {
                newEntries = [];
                _ropeMaterials.Add(material, newEntries);
            }

            newEntries.Add(rope);
        }

        public void RemoveRope(Rope rope)
        {
            _ropes.Remove(rope);

            if (rope.RopeMaterial == null || rope.RopeMaterial.Material == null)
            {
                return;
            }

            ShaderMaterial material = rope.RopeMaterial.Material;

            if (!_ropeMaterials.TryGetValue(material, out var newEntries))
            {
                newEntries.Remove(rope);

                if (newEntries.Count == 0)
                {
                    _ropeMaterials.Remove(material);
                }
            }
        }

        public void AddWindSource(WindSource source)
        {
            _windSources.Add(source);
        }

        public void RemoveWindSource(WindSource source)
        {
            _windSources.Remove(source);
        }

        public override void _Ready()
        {
            // TODO Manage generated meshes by saving/loading them and try reusing those per different ropes
            Instance = this;

            PushUniforms();
            _initialized = true;
        }

        public override void _Process(double delta)
        {
            
        }

        public override void _ExitTree()
        {
            Instance = null;
        }
    }
}
