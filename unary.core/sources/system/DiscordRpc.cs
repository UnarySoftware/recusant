using Godot;

namespace Unary.Core
{
    [Tool]
    [GlobalClass]
    public partial class DiscordRpc : Node, ICoreSystem
    {
        public const string AppId = "1460660121662263338";

        bool ISystem.Initialize()
        {
            return true;
        }
    }
}
