using UnityEngine;
using Zenthium.Data;

namespace Zenthium.Modules
{
    /// <summary>
    /// A place on the map where entities with its <c>LootDestination:&lt;place&gt;</c> tag take Valuables and the
    /// citizens they drag, such as a Repo Agency crop circle (world.md, Goon behavior). The map edge isn't one: it's
    /// the map's bounds. It registers with <see cref="ModuleWorld"/> while enabled, so modules and the AI can find it.
    /// </summary>
    public interface ILootDestination
    {
        /// <summary>The tag of the entities that go here, such as <see cref="EntityTag.LootDestinationCropCircle"/>.</summary>
        EntityTag Tag { get; }

        /// <summary>Its center.</summary>
        Vector3 Position { get; }

        /// <summary>Whether a point is inside it, on the XZ plane, so something towed there has arrived.</summary>
        bool Contains(Vector3 point);
    }
}
