#if TOOLS
using Godot;
using Unary.Core.Editor;

namespace Unary.Recusant.Editor
{
    [Tool]
    public partial class PluginBoostrap : PluginBootstrapCustom
    {
        public override string GetPluginNamespace()
        {
            return "Unary.Recusant.Editor";
        }
    }
}

#endif
