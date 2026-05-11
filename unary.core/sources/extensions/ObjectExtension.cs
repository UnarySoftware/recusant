using System;

namespace Unary.Core
{
    public static class ObjectExtensions
    {
        public static string ResolvePrintType(this object source)
        {
            if (source is Type type)
            {
                return type.FullName;
            }
            else if (source is string stringType)
            {
                return stringType;
            }
            else
            {
                return source.GetType().FullName;
            }
        }

        public static void Log(this object source, string text)
        {
            SharedLogger.Log(source, text);
        }

        public static void Warning(this object source, string text)
        {
            SharedLogger.Warning(source, text);
        }

        public static void Error(this object source, string text)
        {
            SharedLogger.Error(source, text);
        }

        public static bool Critical(this object source, string text)
        {
            return SharedLogger.Critical(source, text);
        }

        public static string GetModId(this object source)
        {
            return source.GetType().GetModId();
        }
    }
}
