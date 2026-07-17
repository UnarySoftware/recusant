using Godot;

namespace Unary.Recusant
{
    [Tool]
    [GlobalClass]
    public partial class WindSource : Node3D
    {
        [Export(PropertyHint.Range, "0.0,50.0,0.01")]
        public float Strength
        {
            get;
            set
            {
                field = Mathf.Clamp(value, 0.0f, 50.0f);
            }
        } = 8.0f;

        [Export(PropertyHint.Range, "0.0,50.0,0.01")]
        public float Radius
        {
            get;
            set
            {
                field = Mathf.Clamp(value, 0.0f, 50.0f);
                RadiusSquared = field * field;
            }
        } = 6.0f;

        public float RadiusSquared = 0.0f;

        public override void _Ready()
        {
            WindManager.Instance.AddWindSource(this);
        }

        public override void _ExitTree()
        {
            WindManager.Instance.RemoveWindSource(this);
        }
    }
}
