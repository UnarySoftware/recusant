using Godot;

namespace Unary.Recusant
{
    [Tool]
    [GlobalClass]
    public partial class RandomBall : RigidBody3D
    {
        [Export]
        public double Timer = 1.0f;

        [Export]
        public float ImpulseForce = 40.0f;

        private double _timer = 0.0f;

        public Vector3 RandomSphere()
        {
            float x1 = (float)GD.RandRange(-1.0f, 1.0f);
            float x2 = (float)GD.RandRange(-1.0f, 1.0f);

            while (x1 * x1 + x2 * x2 >= 1)
            {
                x1 = (float)GD.RandRange(-1.0f, 1.0f);
                x2 = (float)GD.RandRange(-1.0f, 1.0f);
            }

            var result = new Vector3(
            2.0f * x1 * Mathf.Sqrt(1.0f - x1 * x1 - x2 * x2),
            2.0f * x2 * Mathf.Sqrt(1.0f - x1 * x1 - x2 * x2),
            1.0f - 2.0f * (x1 * x1 + x2 * x2));

            return result;
        }

        public override void _PhysicsProcess(double delta)
        {
            _timer += delta;

            if (_timer < Timer)
            {
                return;
            }

            _timer = 0.0;

            ApplyImpulse(RandomSphere() * ImpulseForce);
        }
    }
}
