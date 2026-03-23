using Godot;

namespace Unary.Core
{
    [Tool]
    [GlobalClass]
    public partial class UiCoreConsoleData : BaseResource
    {
        [Export]
        public AudioStream Warning;

        [Export]
        public AudioStream Error;
    }
}
