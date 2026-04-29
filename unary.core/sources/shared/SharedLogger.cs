using Godot;
using Unary.Core.Editor;

namespace Unary.Core
{
    public static class SharedLogger
    {
        private enum Type
        {
            None,
            Plugin,
            Runtime
        }

        private static Type _type = Type.None;

        private static void TryInitialize()
        {
            if (_type == Type.None)
            {
                return;
            }

            if (Engine.Singleton.IsEditorHint())
            {
                _type = Type.Plugin;
            }
            else
            {
                _type = Type.Runtime;
            }
        }

        public static void Log(object source, string text)
        {
            TryInitialize();

            if (_type == Type.Plugin)
            {
                PluginLogger.Log(source, text);
            }
            else
            {
                RuntimeLogger.Log(source, text);
            }
        }

        public static void Warning(object source, string text)
        {
            TryInitialize();

            if (_type == Type.Plugin)
            {
                PluginLogger.Warning(source, text);
            }
            else
            {
                RuntimeLogger.Warning(source, text);
            }
        }

        public static void Error(object source, string text)
        {
            TryInitialize();

            if (_type == Type.Plugin)
            {
                PluginLogger.Error(source, text);
            }
            else
            {
                RuntimeLogger.Error(source, text);
            }
        }

        public static bool Critical(object source, string text)
        {
            TryInitialize();

            if (_type == Type.Plugin)
            {
                return PluginLogger.Critical(source, text);
            }
            else
            {
                return RuntimeLogger.Critical(source, text);
            }
        }
    }
}
