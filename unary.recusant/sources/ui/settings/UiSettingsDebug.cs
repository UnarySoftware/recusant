using Godot;
using Unary.Core;

namespace Unary.Recusant
{
    [Tool]
    [GlobalClass]
    public partial class UiSettingsDebug : UiSettingsTabBase
    {
        [UiElement("%RootTabs")]
        private TabBar _rootTabs;

        public override void Initialize()
        {

        }

        public override void Deinitialize()
        {

        }

    }
}
