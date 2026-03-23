
namespace Unary.Core
{
    [SingletonProvider("Unary.Core.Bootstrap.Singleton.GetSystem")]
    public interface ISystem
    {
        public bool Initialize()
        {
            return true;
        }

        public bool PostInitialize()
        {
            return true;
        }

        public void Process(float delta)
        {

        }

        public void PhysicsProcess(float delta)
        {

        }

        public void Deinitialize()
        {

        }
    }
}
