using Godot;

namespace Unary.Core
{
    public class LazyResource<T> where T : Resource
    {
        public T Cache
        {
            get
            {
                field ??= (T)Resources.Singleton.LoadPatched(TargetValue);
                return field;
            }
        }

        public string TargetValue { get; private set; } = string.Empty;

        public LazyResource()
        {

        }

        public LazyResource(string value)
        {
            TargetValue = value;
        }

        public T LoadWithoutCache()
        {
            return (T)Resources.Singleton.LoadPatched(TargetValue);
        }
    }
}
