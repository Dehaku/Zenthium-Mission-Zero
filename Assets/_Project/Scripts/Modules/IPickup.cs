using Zenthium.Entities;

namespace Zenthium.Modules
{
    /// <summary>
    /// A Drag target the player picks up at once instead of dragging, such as a Valuable (modules.md, Drag row;
    /// world.md, Picking up). <see cref="DragAction"/> picks it like any target, then calls this instead of dragging.
    /// The World assembly implements it.
    /// </summary>
    public interface IPickup
    {
        /// <returns>True if the picker took it.</returns>
        bool PickUp(EntityBehaviour picker);
    }
}
