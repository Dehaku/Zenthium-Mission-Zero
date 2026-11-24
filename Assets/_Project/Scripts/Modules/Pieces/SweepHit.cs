using UnityEngine;
using Zenthium.Entities;

namespace Zenthium.Modules
{
    /// <summary>
    /// Finds what a projectile or beam hits first along a sweep: the nearest entity its user can hurt, or anything
    /// solid. It passes through the user, and through entities it can't hurt, Incapacitated or dragged ones.
    /// </summary>
    public static class SweepHit
    {
        /// <param name="target">The entity hit, or null if something solid stopped it.</param>
        /// <param name="solid">What stopped it when it isn't an entity, such as a wall to damage; null otherwise.</param>
        /// <param name="hitDistance">How far along the sweep it stopped.</param>
        /// <returns>True if anything stops it within the distance.</returns>
        public static bool TryFindFirst(ModuleContext context, Vector3 origin, Vector3 direction, float radius,
            float distance, RaycastHit[] buffer, out EntityBehaviour target, out Collider solid, out float hitDistance)
        {
            target = null;
            solid = null;
            hitDistance = float.PositiveInfinity;
            int count = Physics.SphereCastNonAlloc(origin, radius, direction, buffer, distance, Physics.AllLayers,
                QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = buffer[i];
                if (hit.distance >= hitDistance || hit.collider.attachedRigidbody == context.Body)
                {
                    continue;
                }

                EntityBehaviour hitEntity = null;
                if (ModuleWorld.TryGetEntity(hit.collider, out EntityBehaviour entity))
                {
                    if (!CanHit(context, entity))
                    {
                        continue;
                    }

                    hitEntity = entity;
                }

                target = hitEntity;
                solid = hitEntity == null ? hit.collider : null;
                hitDistance = hit.distance;
            }

            return hitDistance < float.PositiveInfinity;
        }

        private static bool CanHit(ModuleContext context, EntityBehaviour entity)
        {
            Entity victim = entity.Entity;
            return victim != null && !victim.IsIncapacitated && !victim.IsBeingDragged
                && context.World.CombatRules.FriendlyFire.CanHurt(context.Owner, victim);
        }
    }
}
