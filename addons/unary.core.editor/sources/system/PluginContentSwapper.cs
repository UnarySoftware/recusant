#if TOOLS

using Godot;

namespace Unary.Core.Editor
{
    [Tool]
    public partial class PluginContentSwapper : IPluginSystem
    {
        bool ISystem.PostInitialize()
        {
            ContentSwapper.TryRevertSwap(PluginLogger.Critical);
            return true;
        }
    }
}

#endif
