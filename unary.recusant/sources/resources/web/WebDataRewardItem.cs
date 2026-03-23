using Godot;
using Godot.Collections;

namespace Unary.Core
{
    [Tool]
    [GlobalClass]
    public partial class WebDataRewardItem : Resource
    {
        public WebDataReward Reward;

        [Export]
        public Resource Item
        {
            get;
            set
            {
                Reward?.UpdateTimestamp();
                field = value;
            }
        }

        [Export]
        public float Count
        {
            get;
            set
            {
                Reward?.UpdateTimestamp();
                field = value;
            }
        } = 1.0f;
    }
}
