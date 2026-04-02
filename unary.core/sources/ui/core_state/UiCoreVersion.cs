
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
                result.Append('\"').Append(mod.ModId).Append("\" changed: ").Append(mod.BuildManifest.BuildData).Append(" (build ").Append(mod.BuildManifest.BuildNumber).Append(")\n");
            }

            string code = result.ToString().GetAudibleHash();

            result.Prepend($"Mod list (Hash Code \"{code.Capitalize()}\"):\n");

            Root.GetNode<Label>("%VersionText").Text = result.ToString();
        }
    }
}
