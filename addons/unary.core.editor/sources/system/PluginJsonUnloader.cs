#if TOOLS

using System.Reflection;
using System.Text.Json;

namespace Unary.Core.Editor
{
    internal class PluginJsonUnloader
    {
#pragma warning disable CA2255
        // This makes perfect sense to be used in our context, we know what we want, why we want it here and it just works
        [System.Runtime.CompilerServices.ModuleInitializer]
#pragma warning restore CA2255
        public static void Initialize()
        {
            System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(Assembly.GetExecutingAssembly()).Unloading += alc =>
            {
                var assembly = typeof(JsonSerializerOptions).Assembly;
                var updateHandlerType = assembly.GetType("System.Text.Json.JsonSerializerOptionsUpdateHandler");
                var clearCacheMethod = updateHandlerType?.GetMethod("ClearCache", BindingFlags.Static | BindingFlags.Public);
                clearCacheMethod?.Invoke(null, [null]);
            };
        }
    }
}

#endif
