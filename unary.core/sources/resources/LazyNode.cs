using Godot;
using System;

namespace Unary.Core
{
    public class LazyNode<T> where T : Node
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

        public LazyNode()
        {

        }

        public LazyNode(string value)
        {
            TargetValue = value;
        }

        public LazyNode(string value, Func<Node, T> processor)
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
