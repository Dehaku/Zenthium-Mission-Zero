using System;
using Zenthium.Data;
using Zenthium.Entities;

namespace Zenthium.Combat
{
    /// <summary>
    /// Whether a hit knocks its target back, and how far (combat.md, Knockback). The distance is the tier's,
    /// never scaled by stats.
    /// <list type="bullet">
    /// <item>A hit that lands knocks back. Dodged and blocked hits don't.</item>
    /// <item>An Incapacitated target still flies, if friendly fire would let the source hurt it, so knockback
    /// can carry it into an exit zone (missions.md, Capture).</item>
    /// <item><see cref="ModuleTag.KnockbackResistant"/> and dragged targets never move.</item>
    /// <item>A hit whose knockback the target cancelled (Weather the Storm) doesn't move it.</item>
    /// <item>Captured targets never move, nor do <see cref="EntityTag.Immobile"/> ones (Heavy Turrets).</item>
    /// </list>
    /// </summary>
    public sealed class KnockbackResolver
    {
        private readonly FriendlyFireRules friendlyFire;

        public KnockbackResolver(FriendlyFireRules friendlyFire)
        {
            this.friendlyFire = friendlyFire ?? throw new ArgumentNullException(nameof(friendlyFire));
        }

        /// <param name="knockbackTier">The module's knockback tier. Unset means no knockback.</param>
        /// <param name="hit">The damage pipeline's result for the same hit.</param>
        /// <param name="distance">Meters; 0 when there's no knockback.</param>
        public bool TryGetDistance(TierValue knockbackTier, DamageEvent hit, out float distance)
        {
            distance = 0f;
            Entity target = hit.Target;
            if (!knockbackTier.IsSet || knockbackTier.Value <= 0f || target == null || hit.KnockbackPrevented
                || target.IsCaptured || target.HasModuleTag(ModuleTag.KnockbackResistant)
                || target.HasTag(EntityTag.Immobile) || target.IsBeingDragged)
            {
                return false;
            }

            bool knocksBack = hit.Outcome == DamageOutcome.Dealt
                || (hit.Outcome == DamageOutcome.NotAllowed && target.IsIncapacitated
                    && friendlyFire.CanHurt(hit.Source, target));
            if (!knocksBack)
            {
                return false;
            }

            distance = knockbackTier.Value;
            return true;
        }
    }
}
