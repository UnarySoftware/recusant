using System;
using System.Diagnostics.CodeAnalysis;
using Godot;

namespace Unary.Core
{
    public static class RuntimeLogger
    {
        public enum LogType
        {
            Log,
            Warning,
            Error
        };

        public struct LogEventData : IEquatable<LogEventData>
        {
            public LogType Type;
            public string Message;
            public string StackTrace;

            // Equality has to be implemented to properly store logging entries within the RuntimeLogger.OnLog event
            // There is a possibiltity of going multithreaded with LevelManager before dispatching any log data events,
            // so properly storing/capturing those log messages durring loading (especially verbose) is crucial.

            public override readonly bool Equals(object obj)
            {
                if (obj is LogEventData data)
                {
                    return Equals(data);
                }
                return false;
            }

            public static bool operator ==(LogEventData left, LogEventData right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(LogEventData left, LogEventData right)
            {
                return !left.Equals(right);
            }

            public override readonly int GetHashCode()
            {
                return HashCode.Combine(Type, Message, StackTrace);
            }

            public readonly bool Equals(LogEventData other)
            {
                if (Type == other.Type &&
                Message == other.Message &&
                StackTrace == other.StackTrace)
                {
                    return true;
                }

                return false;
            }
        }

        public static EventFunc<LogEventData> OnLog { get; } = new();

        private static EngineLogger _engineLogger;

        public static int LogCount { get; private set; } = 0;
        public static int WarningCount { get; private set; } = 0;
        public static int ErrorCount { get; private set; } = 0;

        public static bool Initialize()
        {
            _engineLogger = new();
            OS.Singleton.AddLogger(_engineLogger);
            return true;
        }

        public static void Deinitialize()
        {
            OS.Singleton.RemoveLogger(_engineLogger);
        }

        public static void Log(object source, string text)
        {
            if (source == null)
            {
                GD.Print(text);
            }
            else
            {
                GD.Print(source.ResolvePrintType() + ": " + text);
            }
        }

        public static void Warning(object source, string text)
        {
            if (source == null)
            {
                GD.PushWarning(text);
            }
            else
            {
                GD.PushWarning(source.ResolvePrintType() + ": " + text);
            }
        }

        public static void Error(object source, string text)
        {
            if (source == null)
            {
                GD.PushError(text);
            }
            else
            {
                GD.PushError(source.ResolvePrintType() + ": " + text);
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
