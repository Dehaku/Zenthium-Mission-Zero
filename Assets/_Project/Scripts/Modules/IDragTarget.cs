using System;
using UnityEngine;
using Zenthium.Entities;

namespace Zenthium.Modules
{
    /// <summary>
    /// Something Drag can grab. Incapacitated entities implement it now (<see cref="EntityDragTarget"/>); citizens
    /// (Milestone 6), and props and Valuables (Milestone 5) implement it when they arrive.
    /// </summary>
    public interface IDragTarget
    {
        DragTargetKind Kind { get; }

        /// <summary>The body that follows the carrier. Its colliders stop colliding with the carrier while dragged.</summary>
        Rigidbody Body { get; }

        /// <summary>False for Valuables, which don't take up Drag capacity (world.md).</summary>
        bool UsesCapacity { get; }

        /// <summary>
        /// The carrier's slow while dragging this, as a fraction. Stubborn citizens' hook (missions.md, Citizens);
        /// <see cref="DragAction"/> applies it from Milestone 6, when citizens and slows exist.
        /// </summary>
        float CarrierSlow { get; }

        /// <summary>
        /// Seconds to grab this; 0 is instant. Stubborn citizens' hook (missions.md, Citizens), applied from Milestone 6.
        /// </summary>
        float GrabSeconds { get; }

        /// <summary>Raised when this stops being draggable while dragged, so the carrier lets go.</summary>
        event Action<IDragTarget> OnDragBroken;

        bool CanBeGrabbedBy(Entity carrier);

        void BeginDrag(Entity carrier);

        /// <summary>Where the carrier wants this, this physics step.</summary>
        void FollowTo(Vector3 point);

        void EndDrag();
    }
}
