using Godot;
using Unary.Core;

namespace Unary.Recusant
{
    [Tool]
    [GlobalClass]
    public partial class BrushManager : Node, IModSystem
    {
        bool ISystem.Initialize()
        {
            return true;
        }

        void ISystem.Deinitialize()
        {

        }
    }
}
