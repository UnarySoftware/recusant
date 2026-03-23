
using System;
using Godot;

namespace Unary.Core.Editor
{
    public static class PluginLogger
    {
        public enum LoggerType
        {
            Basic,
            Toasts
        };

        public static LoggerType Type = LoggerType.Basic;
        private static EditorToaster _toaster;

        public static void Initialize()
        {
            Type = LoggerType.Basic;
            _toaster = EditorInterface.Singleton.GetEditorToaster();
        }

        public static void Deinitialize()
        {
            _toaster = null;
        }

        public static void Log(object source, string text)
        {
            string result;

            if (source == null)
            {
                result = text;
            }
            else
            {
                result = source.ResolvePrintType() + ": " + text;
            }

            switch (Type)
            {
                default:
                case LoggerType.Basic:
                    {
                        GD.Print(result);
                        break;
                    }
                case LoggerType.Toasts:
                    {
                        _toaster.PushToast(result, EditorToaster.Severity.Info);
                        break;
                    }
            }
        }

        public static void Warning(object source, string text)
        {
            string result;

            if (source == null)
            {
                result = text;
            }
            else
            {
                result = source.ResolvePrintType() + ": " + text;
            }

            switch (Type)
            {
                default:
                case LoggerType.Basic:
                    {
                        GD.PushWarning(result);
                        break;
                    }
                case LoggerType.Toasts:
                    {
                        _toaster.PushToast(result, EditorToaster.Severity.Warning);
                        break;
                    }
            }
        }

        public static void Error(object source, string text)
        {
            string result;

            if (source == null)
            {
                result = text;
            }
            else
            {
                result = source.ResolvePrintType() + ": " + text;
            }

            switch (Type)
            {
                default:
                case LoggerType.Basic:
                    {
                        GD.PushError(result);
                        break;
                    }
                case LoggerType.Toasts:
                    {
                        _toaster.PushToast(result, EditorToaster.Severity.Error);
                        break;
                    }
            }
        }

        public static bool Critical(object source, string text)
        {
            if (source == null)
            {
                OS.Singleton.Alert(text, "ERROR");
            }
            else
            {
                OS.Singleton.Alert(text, "ERROR: " + source.ResolvePrintType());
            }

            return false;
        }
    }
}
