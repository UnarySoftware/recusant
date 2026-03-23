#if TOOLS

using Godot;

namespace Unary.Core.Editor
{
    [SingletonProvider("Unary.Core.Editor.PluginBootstrap.Singleton.GetSystem")]
    public interface IPluginSystem : ISystem
    {
        /*
        public bool PreBuild()
        {
            return true;
        }

        public bool Build()
        {
            return true;
        }

        public bool PostBuild()
        {
            return true;
        }
        */
        public bool PreExport()
        {
            return true;
        }

        public bool Export()
        {
            return true;
        }

        public bool PostExport()
        {
            return true;
        }
    }
}

#endif
