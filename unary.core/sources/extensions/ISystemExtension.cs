namespace Unary.Core
{
    public static class ISystemExtension
    {
        public static void Log(this ISystem system, string text)
        {
            RuntimeLogger.Log(system.GetType(), text);
        }

        public static void Warning(this ISystem system, string text)
        {
            RuntimeLogger.Warning(system.GetType(), text);
        }

        public static void Error(this ISystem system, string text)
        {
            RuntimeLogger.Error(system.GetType(), text);
        }

        public static bool Critical(this ISystem system, string text)
        {
            return RuntimeLogger.Critical(system.GetType(), text);
        }
    }
}
