using Godot;

namespace Unary.Core
{
    public class LazyResource<T> where T : Resource
    {
        public T Cache { get; private set; }

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
            Cache ??= (T)Resources.Singleton.LoadPatched(TargetValue);
        }

        public T LoadWithoutCache()
        {
            return (T)Resources.Singleton.LoadPatched(TargetValue);
        }
    }
}
