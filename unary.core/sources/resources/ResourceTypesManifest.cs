using System;
using Godot;
using Godot.Collections;

namespace Unary.Core
{
    [Tool]
    [GlobalClass]
    [Icon("res://addons/unary.core.editor/icons/ResourceManifest.svg")]
    public partial class ResourceTypesManifest : BaseResource
    {
        [Export]
        public string[] Paths = null;

        [Export]
        public string[] Types = null;

        public override void _ValidateProperty(Dictionary property)
        {
            property.MakeReadOnly(PropertyName.Paths, PropertyName.Types);
            base._ValidateProperty(property);
        }
    }
}
