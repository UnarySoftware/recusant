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

        public static void Log(this IPluginSystem system, string text)
        {
            PluginLogger.Log(system, text);
        }

        public static void Warning(this IPluginSystem system, string text)
        {
            PluginLogger.Warning(system, text);
        }

        public static void Error(this IPluginSystem system, string text)
        {
            PluginLogger.Error(system, text);
        }

        public static bool Critical(this IPluginSystem system, string text)
        {
            return PluginLogger.Critical(system, text);
        }
    }
}

#endif
