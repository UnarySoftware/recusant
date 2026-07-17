using Godot;
using System;

namespace Unary.Core
{
    public class LazyResource<T> where T : Resource
    {
        public T Cache
        {
            get
            {
                if (field != null)
                {
                    return field;
                }

                Resource resource = (T)ResourceLoader.Singleton.Load(TargetValue);

                if (Processor != null)
                {
                    field = Processor(resource);
                }
                else
                {
                    field = (T)resource;
                }

                return field;
            }
            private set
            {
                field = value;
            }
        }

        public string TargetValue { get; private set; } = string.Empty;
        public Func<Resource, T> Processor { get; private set; } = null;

        public LazyResource()
        {

        }

        public LazyResource(string value)
        {
            TargetValue = value;
        }

        public LazyResource(string value, Func<Resource, T> processor)
        {
            TargetValue = value;
            Processor = processor;
        }

        public void Precache()
        {
            _ = Cache;
        }

        public T LoadWithoutCache()
        {
            Resource resource = ResourceLoader.Singleton.Load(TargetValue);

            if (Processor != null)
            {
                return Processor(resource);
            }
            else
            {
                return (T)resource;
            }
        }

        public void ClearCache()
        {
            Cache = null;
        }
    }
}
