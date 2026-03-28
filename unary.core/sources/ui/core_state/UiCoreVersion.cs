
using System;
using System.Text;
using Godot;

namespace Unary.Core
{
    [Tool]
    [GlobalClass]
    public partial class UiCoreVersion : UiUnit<UiCoreState>
    {
        public override void Initialize()
        {
            var mods = ModLoader.Singleton.EnabledMods;

            StringBuilder result = new();

            foreach (var mod in mods)
            {
                result.Append(mod.ModId).Append(' ').Append(mod.BuildManifest.BuildData).Append(" (").Append(mod.BuildManifest.BuildNumber).Append(")\n");
            }

            string code = result.ToString().GetAudibleHash();

            result.Prepend($"Code: \"{code.Capitalize()}\"\n");

            Root.GetNode<Label>("%VersionText").Text = result.ToString();
        }
    }
}
