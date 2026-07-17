using Godot;
using System;
using Unary.Core;

namespace Unary.Recusant
{
    [Tool]
    [GlobalClass]
    public partial class RopeMaterial : BaseResource
    {
        [Export(PropertyHint.Range, "3,128,1")]
        public int Segments
        {
            get;
            set
            {
                field = value;
                OnChanged?.Invoke();
            }
        } = 24;

        [Export(PropertyHint.Range, "3,32,1")]
        public int Sides
        {
            get; set
            {
                field = value;
                OnChanged?.Invoke();
            }
        } = 8;

        [Export(PropertyHint.Range, "0.005,0.5,0.001")]
        public float Thickness
        {
            get;
            set
            {
                field = value;
                OnChanged?.Invoke();
            }
        } = 0.04f;

        [Export(PropertyHint.Range, "0,5,0.01")]
        public float Sag
        {
            get;
            set
            {
                field = value;
                OnChanged?.Invoke();
            }
        } = 1.0f;

        [Export]
        public ShaderMaterial Material
        {
            get;
            set
            {
                field = value;
                OnChanged?.Invoke();
            }
        }

        public Action OnChanged;
    }
}
