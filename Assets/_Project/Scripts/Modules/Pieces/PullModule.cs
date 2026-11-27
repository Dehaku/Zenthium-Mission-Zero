using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Zenthium.Data;
using Zenthium.Entities;

namespace Zenthium.Modules
{
    /// <summary>Where a <see cref="PullModule"/> pulls what it hits.</summary>
    public enum PullDestination
    {
        /// <summary>
        /// The user's nearest loot destination to the target, such as a Repo Agency crop circle (Tractor Pull);
        /// with none on the map, toward the user (the user's decision, Milestone 7).
        /// </summary>
        LootDestination,

        /// <summary>Toward the user (Tractor Beam).</summary>
        User,
    }

    /// <summary>
    /// An instant beam along the aim, as far as the range tier, that pulls with the module's knockback tier as the
    /// distance, through the shared knockback rules (so Immovable and dragged targets don't move). The pull stops at
    /// its point instead of overshooting it.
    /// <list type="bullet">
    /// <item>Tractor Pull: the first entity the user can hurt, toward the loot destination.</item>
    /// <item>Tractor Beam: every opponent in its path, toward the user (only opponents: the user's decision,
    /// Milestone 7), and every prop, harmlessly. Walls stop it.</item>
    /// </list>
    /// </summary>
    public sealed class PullModule : ModuleBehaviour
    {
        private const int MaxBeamHits = 16;

        // Meters: how thick the beam's sweep is, as BeamModule's.
        private const float BeamRadius = 0.25f;

        // Meters short of the user a pull toward it stops, so the target lands in front of it rather than on it.
        private const float UserGap = 1.5f;

        private readonly RaycastHit[] beamHits = new RaycastHit[MaxBeamHits];
        private readonly HashSet<IPullable> pulledProps = new HashSet<IPullable>();
        private readonly HitEffects effects;
        private readonly PullDestination destination;
        private readonly bool pullsEveryOpponent;

        /// <param name="pullsEveryOpponent">True to pull every opponent in its path; false for the first entity hit.</param>
        public PullModule(ModuleData data, HitEffects effects, PullDestination destination, bool pullsEveryOpponent)
            : base(data)
        {
            this.effects = effects;
            this.destination = destination;
            this.pullsEveryOpponent = pullsEveryOpponent;
        }

        public override void DescribeEffects(StringBuilder text)
        {
            text.Append(pullsEveryOpponent ? "Pulls every enemy in its path" : "Pulls the first one it hits")
                .Append(destination == PullDestination.User ? " toward you\n" : " toward the nearest crop circle\n");
            effects.Describe(text, Context);
        }

        protected override bool CanActivateNow(ModuleAim aim)
        {
            return aim.Direction.sqrMagnitude > 0f;
        }

        protected override void Activate(ModuleAim aim)
        {
            Vector3 origin = Context.Position;
            Vector3 direction = aim.Direction.normalized;
            float range = Data.RangeTier.Value;
            float length = pullsEveryOpponent ? PullEveryOpponent(origin, direction, range) : PullFirst(origin, direction, range);
            Context.World.ShowBeam(origin, origin + direction * length);
        }

        /// <returns>How far the beam reached.</returns>
        private float PullFirst(Vector3 origin, Vector3 direction, float range)
        {
            if (!SweepHit.TryFindFirst(Context, origin, direction, BeamRadius, range, beamHits,
                    out EntityBehaviour target, out _, out float hitDistance))
            {
                return range;
            }

            if (target != null)
            {
                Pull(target);
            }

            return hitDistance;
        }

        /// <returns>How far the beam reached: to the first wall or other solid thing, or its range.</returns>
        private float PullEveryOpponent(Vector3 origin, Vector3 direction, float range)
        {
            int count = Physics.SphereCastNonAlloc(origin, BeamRadius, direction, beamHits, range, Physics.AllLayers,
                QueryTriggerInteraction.Ignore);
            pulledProps.Clear();
            float wallDistance = range;
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = beamHits[i];
                if (hit.collider.attachedRigidbody != Context.Body && !ModuleWorld.TryGetEntity(hit.collider, out _)
                    && !TryGetPullable(hit.collider, out _))
                {
                    wallDistance = Mathf.Min(wallDistance, hit.distance);
                }
            }

            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = beamHits[i];
                if (hit.distance <= wallDistance && TryGetPullable(hit.collider, out IPullable prop)
                    && prop is Component component)
                {
                    PullProp(prop, component.transform.position);
                }
            }

            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = beamHits[i];
                if (hit.distance > wallDistance || !ModuleWorld.TryGetEntity(hit.collider, out EntityBehaviour target)
                    || target.Entity == null || target.Entity.IsIncapacitated || target.Entity.IsBeingDragged
                    || !Opponents.Are(Owner, target.Entity))
                {
                    continue;
                }

                // A body with several colliders is pulled once: only from its nearest hit.
                if (IsNearestHitOf(target, i, count))
                {
                    Pull(target);
                }
            }

            return wallDistance;
        }

        /// <summary>A prop in the path slides toward the user, harmlessly (the user's decision, Milestone 7b).</summary>
        private void PullProp(IPullable prop, Vector3 from)
        {
            if (!pulledProps.Add(prop))
            {
                return;
            }

            Vector3 offset = Context.Position - from;
            offset.y = 0f;
            float distance = Mathf.Min(Data.KnockbackTier.Value, offset.magnitude - UserGap);
            if (distance > 0f)
            {
                prop.Pull(offset.normalized, distance);
            }
        }

        private static bool TryGetPullable(Collider hit, out IPullable pullable)
        {
            Rigidbody attached = hit.attachedRigidbody;
            if (attached != null)
            {
                return attached.TryGetComponent(out pullable);
            }

            return hit.TryGetComponent(out pullable);
        }

        private bool IsNearestHitOf(EntityBehaviour target, int index, int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (i != index && beamHits[i].distance < beamHits[index].distance
                    && ModuleWorld.TryGetEntity(beamHits[i].collider, out EntityBehaviour other) && other == target)
                {
                    return false;
                }
            }

            return true;
        }

        private void Pull(EntityBehaviour target)
        {
            Vector3 from = target.transform.position;
            Vector3 point = Context.Position;
            float gap = UserGap;
            if (destination == PullDestination.LootDestination && TryFindLootDestination(from, out Vector3 place))
            {
                point = place;
                gap = 0f;
            }

            Vector3 offset = point - from;
            offset.y = 0f;
            float distance = offset.magnitude - gap;
            if (distance <= 0f)
            {
                return;
            }

            effects.ApplyTo(Context, target, offset.normalized, distance);
        }

        /// <summary>
        /// The nearest crop circle to the target, for a user with <c>LootDestination:CropCircle</c>. It's the only loot
        /// destination that's a place on the map; the map edge isn't one.
        /// </summary>
        private bool TryFindLootDestination(Vector3 from, out Vector3 place)
        {
            place = default;
            if (!Owner.HasTag(EntityTag.LootDestinationCropCircle)
                || !Context.World.TryFindLootDestination(EntityTag.LootDestinationCropCircle, from,
                    out ILootDestination found))
            {
                return false;
            }

            place = found.Position;
            return true;
        }
    }
}
