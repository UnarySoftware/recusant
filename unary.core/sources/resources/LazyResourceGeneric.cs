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

                Resource resource;

#if TOOLS
                if (Engine.Singleton.IsEditorHint())
                {
                    resource = (T)ResourceLoader.Singleton.Load(TargetValue);
                }
                else
                {
                    resource = (T)Resources.Singleton.LoadPatched(TargetValue);
                }
#else
                resource = (T)Resources.Singleton.LoadPatched(TargetValue);
#endif

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

        public T LoadWithoutCache()
        {
            Resource resource;

#if TOOLS
            if (Engine.Singleton.IsEditorHint())
            {
                resource = ResourceLoader.Singleton.Load(TargetValue);
            }
            else
            {
                resource = Resources.Singleton.LoadPatched(TargetValue);
            }
#else
            resource = Resources.Singleton.LoadPatched(TargetValue);
#endif

            if (Processor != null)
            {
                return Processor(resource);
            }
            else
            {
                return (T)resource;
            }
        }
    }

    public class LazyResourceNode<T> where T : Node
    {
        public T Cache
        {
            get
            {
                if (field != null)
                {
                    return field;
                }

                PackedScene scene;

#if TOOLS
                if (Engine.Singleton.IsEditorHint())
                {
                    scene = (PackedScene)ResourceLoader.Singleton.Load(TargetValue);
                }
                else
                {
                    scene = (PackedScene)Resources.Singleton.LoadPatched(TargetValue);
                }
#else
                scene = (PackedScene)Resources.Singleton.LoadPatched(TargetValue);
#endif

                Node result = scene.Instantiate();

                if (Processor != null)
                {
                    field = Processor(result);
                }
                else
                {
                    field = (T)result;
                }

                return field;
            }
        }

        public string TargetValue { get; private set; } = string.Empty;
        public Func<Node, T> Processor { get; private set; } = null;

        public LazyResourceNode()
        {

        }

        public LazyResourceNode(string value)
        {
            TargetValue = value;
        }

        public LazyResourceNode(string value, Func<Node, T> processor)
        {
            TargetValue = value;
            Processor = processor;
        }

        public T LoadWithoutCache()
        {
            PackedScene scene;

#if TOOLS
            if (Engine.Singleton.IsEditorHint())
            {
                scene = (PackedScene)ResourceLoader.Singleton.Load(TargetValue);
            }
            else
            {
                scene = (PackedScene)Resources.Singleton.LoadPatched(TargetValue);
            }
#else
            scene = (PackedScene)Resources.Singleton.LoadPatched(TargetValue);
#endif

            Node result = scene.Instantiate();

            if (Processor != null)
            {
                return Processor(result);
            }
            else
            {
                return (T)result;
            }
        }
    }
}
