#if TOOLS

using Godot;

namespace Unary.Core.Editor
{
    public static class IPluginSystemExtension
    {
        public static bool IsDebug(this IPluginSystem _)
        {
            if (PluginBootstrap.Singleton != null)
            {
                return PluginBootstrap.Singleton.Debug;
            }
            return false;
        }

        public static EditorPlugin GetPlugin(this IPluginSystem _)
        {
            return PluginBootstrap.Singleton;
        }
    }
}

#endif
