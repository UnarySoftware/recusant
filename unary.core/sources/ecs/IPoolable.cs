namespace Unary.Core
{
    public interface IPoolable
    {
        public void Aquire();
        public void Release();
    }
}
