using Godot;
using Unary.Core;

namespace Unary.Recusant
{
    [Tool]
    [GlobalClass]
    public partial class GameStateManager : Node, IModSystem
    {
        public GameState State { get; set; } = GameState.Loading;
    }
}
