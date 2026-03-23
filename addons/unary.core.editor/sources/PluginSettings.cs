#if TOOLS
using Godot;

namespace Unary.Core.Editor
{
    [Tool]
    public partial class PluginSettings : IPluginSystem
    {
        bool ISystem.Initialize()
        {
            EditorSettingsManager.Initialize();
            return true;
        }

        void ISystem.Deinitialize()
        {
            EditorSettingsManager.Deinitialize();
        }
    }
}

#endif
