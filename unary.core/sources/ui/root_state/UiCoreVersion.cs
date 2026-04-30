using Godot;
using System.Text;

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

            (string adjective, string noun) = result.ToString().GetAudibleHash();

            result.Prepend($"Mod list (Hash Code \"{adjective.Capitalize()} {noun.Capitalize()}\"):\n");

            Root.GetNode<Label>("%VersionText").Text = result.ToString();
        }
    }
}
