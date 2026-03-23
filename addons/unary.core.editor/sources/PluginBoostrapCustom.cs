#if TOOLS

using System.Threading.Tasks;
using Godot;

namespace Unary.Core.Editor
{
    [Tool]
    public partial class PluginBootstrapCustom : EditorPlugin
    {
        public virtual string GetPluginNamespace()
        {
            return "Unary.Core.Editor";
        }

        public override void _EnterTree()
        {
            if (PluginBootstrap.Singleton == null)
            {
                return;
            }

            if (!PluginBootstrap.Singleton.EnabledPlugins.Contains(GetPluginNamespace()))
            {
                Task.Run(() => ReinitializeCore());
            }
        }

        public async Task ReinitializeCore()
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

            if (EditorInterface.Singleton.IsPluginEnabled("unary.core.editor"))
            {
                EditorInterface.Singleton.SetPluginEnabled("unary.core.editor", false);
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                EditorInterface.Singleton.SetPluginEnabled("unary.core.editor", true);
            }
        }
    }
}

#endif
