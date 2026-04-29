using Godot;
using System.Collections.Generic;
using Unary.Core;

namespace Unary.Recusant
{
    [Tool]
    [GlobalClass]
    [StaticWebData("https://unarysoftware.github.io/recusant_rewards.tres")]
    public partial class WebDataRewards : BaseResource
    {
        [Export]
        public WebDataReward[] Rewards
        {
            get
            {
                return field;
            }
            set
            {
                field = value;
                HashSet<long> ids = [];

                foreach (var reward in Rewards)
                {
                    if (reward == null)
                    {
                        continue;
                    }

                    if (reward.Id == ResourceUid.InvalidId || ids.Contains(reward.Id))
                    {
                        while (true)
                        {
                            long newId = ResourceUid.Singleton.CreateId();

                            if (newId == ResourceUid.InvalidId || ids.Contains(newId))
                            {
                                continue;
                            }

                            reward.Id = newId;

                            break;
                        }
                    }

                    ids.Add(reward.Id);
                }

            }
        } = [];
    }
}
