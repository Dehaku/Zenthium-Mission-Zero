using System.Globalization;
using System.Text;
using UnityEngine;
using Zenthium.Data;
using Zenthium.Entities;

namespace Zenthium.Modules
{
    /// <summary>
    /// Tow Beam (modules.md): grabs the nearest prop, citizen or Valuable within the range tier, as Drag grabs, but
    /// from range. A Valuable goes straight into the user's Valuables; a prop or citizen is towed behind the user
    /// until it lets go, which any hit that lands on the user makes it do (the user's decision, Milestone 7b). Heroes
    /// and other downed entities can't be towed. The AI takes what it tows to its loot destination.
    /// </summary>
    public sealed class TowBeamModule : ModuleBehaviour
    {
        private const int MaxReachHits = 32;
        private const string NumberFormat = "0.#";

        // Meters behind the user a towed thing trails, so it doesn't bump into the user.
        private const float TowSpacing = 2f;

        private readonly Collider[] reachHits = new Collider[MaxReachHits];
        private IDragTarget towed;

        public TowBeamModule(ModuleData data) : base(data)
        {
        }

        public bool IsTowing => towed != null;

        /// <summary>Where the towed thing is; the user's own position when it tows nothing.</summary>
        public Vector3 TowedPosition => towed != null ? towed.Body.position : Context.Position;

        public override void DescribeEffects(StringBuilder text)
        {
            text.Append("Tows the nearest prop, citizen or Valuable within ")
                .Append(Data.RangeTier.Value.ToString(NumberFormat, CultureInfo.InvariantCulture))
                .Append(" m. Any hit makes it let go\n");
        }

        /// <summary>The nearest thing within range it could tow.</summary>
        public bool TryFindTarget(out IDragTarget target)
        {
            target = null;
            if (Context == null)
            {
                return false;
            }

            Vector3 origin = Context.Position;
            float nearest = float.PositiveInfinity;
            int count = Physics.OverlapSphereNonAlloc(origin, Data.RangeTier.Value, reachHits, Physics.AllLayers,
                QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
            {
                Collider hit = reachHits[i];
                if (hit.attachedRigidbody == Context.Body || !TryGetDragTarget(hit, out IDragTarget candidate)
                    || !TowRules.CanTow(candidate.Kind) || !candidate.CanBeGrabbedBy(Owner))
                {
                    continue;
                }

                Vector3 offset = candidate.Body.position - origin;
                offset.y = 0f;
                if (offset.sqrMagnitude < nearest)
                {
                    nearest = offset.sqrMagnitude;
                    target = candidate;
                }
            }

            return target != null;
        }

        /// <summary>Lets go of what it tows, where it is.</summary>
        public void Release()
        {
            if (towed == null)
            {
                return;
            }

            IDragTarget letGo = towed;
            towed = null;
            letGo.OnDragBroken -= HandleDragBroken;
            letGo.EndDrag();
        }

        /// <summary>At its loot destination: what it tows is taken off the map for good, if it can be.</summary>
        public void StealTowed()
        {
            IDragTarget taken = towed;
            Release();
            if (taken is IStealable stealable)
            {
                stealable.Steal();
            }
        }

        protected override bool CanActivateNow(ModuleAim aim)
        {
            return !IsTowing && !Owner.IsIncapacitated && TryFindTarget(out _);
        }

        protected override void Activate(ModuleAim aim)
        {
            if (!TryFindTarget(out IDragTarget target))
            {
                return;
            }

            Context.World.ShowBeam(Context.Position, target.Body.position);
            if (target is IPickup pickup)
            {
                if (Context.World.TryGetBehaviour(Owner, out EntityBehaviour user))
                {
                    pickup.PickUp(user);
                }

                return;
            }

            towed = target;
            target.BeginDrag(Owner);
            target.OnDragBroken += HandleDragBroken;
        }

        protected internal override void Tick(float gameDelta)
        {
            if (towed == null)
            {
                return;
            }

            if (Owner.IsIncapacitated)
            {
                Release();
                return;
            }

            // Pulled only once the gap is wider than the spacing, like Drag's slack rope.
            Vector3 ahead = Context.Position;
            Vector3 position = towed.Body.position;
            Vector3 offset = position - ahead;
            offset.y = 0f;
            if (offset.sqrMagnitude > TowSpacing * TowSpacing)
            {
                Vector3 point = ahead + offset.normalized * TowSpacing;
                point.y = position.y;
                towed.FollowTo(point);
            }
            else
            {
                towed.FollowTo(position);
            }
        }

        protected internal override void OnDamageTaken(DamageEvent damage)
        {
            if (damage.Outcome == DamageOutcome.Dealt)
            {
                Release();
            }
        }

        protected override void OnDisabled()
        {
            Release();
        }

        private void HandleDragBroken(IDragTarget target)
        {
            if (target == towed)
            {
                Release();
            }
        }

        private static bool TryGetDragTarget(Collider hit, out IDragTarget target)
        {
            Rigidbody attached = hit.attachedRigidbody;
            if (attached != null)
            {
                return attached.TryGetComponent(out target);
            }

            return hit.TryGetComponent(out target);
        }
    }
}
