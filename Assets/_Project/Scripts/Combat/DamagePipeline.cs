using System;
using Zenthium.Core;
using Zenthium.Data;
using Zenthium.Entities;

namespace Zenthium.Combat
{
    /// <summary>
    /// The one way damage reaches an entity (combat.md), in this order:
    /// <list type="number">
    /// <item>Friendly fire rules; an Incapacitated target takes nothing.</item>
    /// <item>The source's rank damage bonus and dealt modifiers, and the target's taken modifiers (Marked).</item>
    /// <item>Invulnerable, or being dragged (modules.md, Drag), blocks it.</item>
    /// <item>Dodge, for direct hits with a source only.</item>
    /// <item>The target's incoming hit handlers (Weather the Storm), for direct hits.</item>
    /// <item>Armor, then Damage Reduction, with at least a minimum fraction of the damage dealt. DoTs skip this.</item>
    /// <item>The target's HP, where Defiance may save it. A hit that lands raises the source's on-hit events.</item>
    /// </list>
    /// </summary>
    public sealed class DamagePipeline
    {
        private readonly FormulaConstants constants;
        private readonly FriendlyFireRules friendlyFire;
        private readonly IRandomSource random;

        public DamagePipeline(FormulaConstants constants, FriendlyFireRules friendlyFire, IRandomSource random)
        {
            this.constants = constants ?? throw new ArgumentNullException(nameof(constants));
            this.friendlyFire = friendlyFire ?? throw new ArgumentNullException(nameof(friendlyFire));
            this.random = random ?? throw new ArgumentNullException(nameof(random));
        }

        public DamageEvent Apply(Hit hit, Entity target)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (target.IsIncapacitated || !friendlyFire.CanHurt(hit.Source, target))
            {
                return Result(hit, target, DamageOutcome.NotAllowed, 0f);
            }

            float incoming = hit.BaseDamage * target.DamageModifiers.TakenMultiplier;
            if (!hit.IsEnvironmental)
            {
                incoming *= hit.Source.OutgoingDamageMultiplier * hit.Source.DamageModifiers.DealtMultiplier;
            }

            DamageEvent result;
            if (target.IsInvulnerable || target.IsBeingDragged)
            {
                result = Result(hit, target, DamageOutcome.Blocked, 0f);
            }
            else if (Dodges(hit, target))
            {
                result = Result(hit, target, DamageOutcome.Dodged, 0f);
            }
            else if (hit.IsDamageOverTime)
            {
                result = Result(hit, target, DamageOutcome.Dealt, incoming);
            }
            else
            {
                var incomingHit = new IncomingHit(hit.Source, hit.Type);
                target.HandleIncomingHit(ref incomingHit);
                float amount = Mitigate(incoming, target.Defenses, incomingHit.ExtraDamageReduction);
                result = new DamageEvent(hit.Source, target, hit.Type, false, DamageOutcome.Dealt, amount,
                    incomingHit.PreventKnockback);
            }

            target.ReceiveDamage(result);
            if (result.Outcome == DamageOutcome.Dealt && !hit.IsEnvironmental)
            {
                hit.Source.NotifyHitDealt(result);
            }

            return result;
        }

        private bool Dodges(Hit hit, Entity target)
        {
            if (hit.IsDamageOverTime || hit.IsEnvironmental)
            {
                return false;
            }

            float chance = target.Defenses.DodgeChance;
            return chance > 0f && random.Value01() < chance;
        }

        private float Mitigate(float incoming, Defenses defenses, float extraDamageReduction)
        {
            float reduced = (incoming - defenses.Armor) * (1f - defenses.DamageReduction) * (1f - extraDamageReduction);
            return Math.Max(reduced, incoming * constants.MinDamageFraction);
        }

        private static DamageEvent Result(Hit hit, Entity target, DamageOutcome outcome, float amount)
        {
            return new DamageEvent(hit.Source, target, hit.Type, hit.IsDamageOverTime, outcome, amount);
        }
    }
}
