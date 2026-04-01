using Godot;

namespace Unary.Core
{
    public class LazyResource<T> where T : Resource
    {
        private T _cache;

        public string TargetValue { get; private set; } = string.Empty;

        public LazyResource()
        {

        }

        public LazyResource(string value)
        {
            TargetValue = value;
        }

        public void Precache()
        {
            _cache ??= (T)Resources.Singleton.LoadPatched(TargetValue);
        }

        public T Load()
        {
            Precache();
            return _cache;
        }

        public T LoadWithoutCache()
        {
            return (T)Resources.Singleton.LoadPatched(TargetValue);
        }
    }
}
