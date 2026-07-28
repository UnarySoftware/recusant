using Godot;

namespace Unary.Core
{
    /// <summary>
    /// Implemented by entity nodes that want the map build's collision shapes but are not themselves a
    /// <see cref="CollisionObject3D"/> - typically <see cref="BaseFgd"/> derivatives, which are Node3D. The
    /// build calls <see cref="CreateCollisionBody"/> once, parents the returned body under the entity node,
    /// and adds the generated shapes to it instead of dropping them. The entity's visuals - mesh, shadow
    /// mesh and occluder - are parented to the body as well, so they ride it when it moves.
    /// </summary>
    public interface IFgdCollisionBody
    {
        /// <summary>
        /// Returns a fresh, unparented body to hold this entity's collision shapes. Only called when the
        /// entity definition's CollisionShapeType actually produced shapes. Returning null skips collision.
        /// </summary>
        public CollisionObject3D CreateCollisionBody();
    }
}
