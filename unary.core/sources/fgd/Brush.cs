using Godot;
using System.Collections.Generic;

namespace Unary.Core
{
    [Tool]
    [GlobalClass]
    [Icon("res://addons/unary.core.editor/icons/Brush.svg")]
    public partial class Brush : BaseFgd
    {
        public enum CollisionType
        {
            Static,
            Dynamic
        };

        [FgdProperty]
        public CollisionType Collision = CollisionType.Static;

#if TOOLS
        public override void AppliedProperties()
        {
            PhysicsBody3D body;

            if (Collision == CollisionType.Static)
            {
                body = new StaticBody3D()
                {
                    Name = "static"
                };
            }
            else
            {
                body = new CharacterBody3D()
                {
                    Name = "dynamic"
                };
            }

            AddChild(body);

            Node sceneRoot = Owner ?? (IsInsideTree() ? GetTree().EditedSceneRoot : null);
            body.Owner = sceneRoot;
        }

#endif
    }
}
