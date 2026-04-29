#if TOOLS
using Godot;
using System.Collections.Generic;

namespace Unary.Core.Editor
{
    [Tool]
    public partial class PluginCompiler : IPluginSystem
    {
        private static Node FindBuildButton()
        {
            var editorInterface = EditorInterface.Singleton;
            var baseControl = editorInterface.GetBaseControl();

            if (baseControl == null)
                return null;

            var queue = new Queue<Node>();
            queue.Enqueue(baseControl);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                if (current.HasMethod("BuildProject"))
                {
                    return current;
                }

                foreach (Node child in current.GetChildren())
                {
                    queue.Enqueue(child);
                }
            }

            return null;
        }

        private static void Compile()
        {
            var build_button = FindBuildButton();
            build_button?.Call("BuildProject");
        }

        bool IPluginSystem.PostExport()
        {
            // This is a good idea, but unfortunatelly it causes random issues with bindings.
            //Compile();
            return true;
        }
    }
}

#endif
