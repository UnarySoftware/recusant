using Godot;
using Godot.Collections;
using System;
using Unary.Core;

namespace Unary.Recusant
{
    [Tool]
    [GlobalClass]
    public partial class WebDataReward : Resource
    {
        [Export]
        public ulong Time = 0;

        [Export]
        public long Id = ResourceUid.InvalidId;

        public void UpdateTimestamp()
        {
            Time = (ulong)DateTimeOffset.Now.ToUnixTimeSeconds();
        }

        [Export]
        public string ProfileGuid
        {
            get;
            set
            {
                UpdateTimestamp();
                field = value;
            }
        } = string.Empty;

        [Export]
        public uint Type
        {
            get;
            set
            {
                UpdateTimestamp();
                field = value;
            }
        } = 0;

        [Export]
        public string Comment
        {
            get;
            set
            {
                UpdateTimestamp();
                field = value;
            }
        } = string.Empty;

        [Export]
        public WebDataRewardItem[] Rewards
        {
            get;
            set
            {
                UpdateTimestamp();

                foreach (var reward in value)
                {
                    if (reward != null && reward.Reward != this)
                    {
                        reward.Reward = this;
                    }
                }

                field = value;
            }
        } = [];

        public override void _ValidateProperty(Dictionary property)
        {
            property.MakeHidden(PropertyName.Time, PropertyName.Id);
            base._ValidateProperty(property);
        }
    }
}
