#if TOOLS

using Godot;
using System;
using Unary.Core;
using Unary.Core.Editor;

namespace Unary.Recusant.Editor
{
    [Tool]
    public partial class PluginRewardsEditor : EditorInspectorPlugin, IPluginSystem
    {
        bool ISystem.Initialize()
        {
            //plugin.AddInspectorPlugin(this);
            return true;
        }

        void ISystem.Deinitialize()
        {
            //plugin.RemoveInspectorPlugin(this);
        }

        private void OnResourceSelected(Resource resource, string path)
        {
            GD.Print($"Type: {resource.GetType().FullName} Path: {path}");
        }

        private void OnPropertyEdited(string path)
        {
            GD.Print(path);
        }

        private WebDataRewards selectedRewards;

        private static void OnResourceChanged(WebDataReward reward)
        {
            reward.Time = (ulong)DateTimeOffset.Now.ToUnixTimeSeconds();
        }

    }
}

#endif
