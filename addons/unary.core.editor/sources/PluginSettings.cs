#if TOOLS
using Godot;

namespace Unary.Core.Editor
{
    [Tool]
    public partial class PluginSettings : IPluginSystem
    {
        bool ISystem.Initialize()
        {
            return true;
        }

        void ISystem.Deinitialize()
        {
            EditorSettingManager.Deinitialize();
        }
    }
}

#endif
