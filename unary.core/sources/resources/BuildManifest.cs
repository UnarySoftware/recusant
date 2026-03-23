using Godot;
using Godot.Collections;

namespace Unary.Core
{
    [Tool]
    [GlobalClass]
    public partial class BuildManifest : BaseResource
    {
        [Export]
        public ulong BuildNumber = 0;

        [Export]
        public string BuildData = "Unknown";

        public override void _ValidateProperty(Dictionary property)
        {
            property.MakeReadOnly(PropertyName.BuildNumber, PropertyName.BuildData);
            base._ValidateProperty(property);
        }
    }
}
