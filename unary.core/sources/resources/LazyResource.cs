using Godot;

namespace Unary.Core
{
    [Tool]
    [GlobalClass]
    public partial class LazyResource : BaseSelectorResource
    {
        private Resource _cache;

        public void Precache()
        {
            _cache ??= ResourceLoader.Singleton.Load(TargetValue);
        }

        public T Load<T>() where T : Resource
        {
            Precache();
            return (T)_cache;
        }

        public T LoadWithoutCache<T>() where T : Resource
        {
            return (T)ResourceLoader.Singleton.Load(TargetValue);
        }
    }
}
