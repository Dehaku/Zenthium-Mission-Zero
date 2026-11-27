using System;
using UnityEngine;
using Zenthium.Entities;

namespace Zenthium.Modules
{
    /// <summary>
    /// Something Drag can grab: downed entities and citizens (<see cref="EntityDragTarget"/>), props and Valuables.
    /// Grabbing is instant for all of them; Stubborn citizens too (the user's decision, Milestone 6b).
    /// </summary>
    public interface IDragTarget
    {
        DragTargetKind Kind { get; }

        /// <summary>The body that follows the carrier. Its colliders stop colliding with the carrier while dragged.</summary>
        Rigidbody Body { get; }

        /// <summary>False for Valuables, which don't take up Drag capacity (world.md).</summary>
        bool UsesCapacity { get; }

        /// <summary>
        /// The carrier's slow while dragging this, as a fraction, such as a Stubborn citizen's (missions.md, Citizens).
        /// <see cref="DragAction"/> applies the sum of what it drags.
        /// </summary>
        float CarrierSlow { get; }

        /// <summary>Raised when this stops being draggable while dragged, so the carrier lets go.</summary>
        event Action<IDragTarget> OnDragBroken;

        bool CanBeGrabbedBy(Entity carrier);

        void BeginDrag(Entity carrier);

        /// <summary>Where the carrier wants this, this physics step.</summary>
        void FollowTo(Vector3 point);

        void EndDrag();
    }
}
