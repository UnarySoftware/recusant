using Godot;

namespace Unary.Core
{
    public partial class EditorLauncher : Node, ICoreSystem
    {
        bool ISystem.Initialize()
        {

#if DEBUG

#endif

            return true;
        }
    }
}
