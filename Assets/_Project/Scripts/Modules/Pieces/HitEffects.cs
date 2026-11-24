using System.Globalization;
using System.Text;
using UnityEngine;
using Zenthium.Combat;
using Zenthium.Data;
using Zenthium.Entities;
using Zenthium.Movement;
using Zenthium.Statuses;

namespace Zenthium.Modules
{
    /// <summary>
    /// What a module does to each entity it reaches, in order: damage from its stat formula, knockback from its
    /// knockback tier, then a status. Every delivery (melee arc, area, projectile) uses it, so the shared rules
    /// (damage pipeline, knockback, resists) apply the same way everywhere.
    /// </summary>
    public sealed class HitEffects
    {
        private const string NumberFormat = "0.##";

        private readonly ModuleData module;
        private readonly StatFormula damage;
        private readonly DamageType damageType;
        private readonly StatusType? status;
        private readonly float statusSeconds;
        private readonly bool usesSummonerStats;

        /// <param name="damage">Null for modules that deal no damage, such as Megaphone.</param>
        /// <param name="status">A status applied when the hit lands; resisted by the module's Resist tag.</param>
        /// <param name="usesSummonerStats">A summon's attack that scales with its summoner's stat (Ink Lash).</param>
        public HitEffects(ModuleData module, StatFormula damage, DamageType damageType, StatusType? status = null,
            float statusSeconds = 0f, bool usesSummonerStats = false)
        {
            this.usesSummonerStats = usesSummonerStats;
            this.module = module;
            this.damage = damage;
            this.damageType = damageType;
            this.status = status;
            this.statusSeconds = statusSeconds;
        }

        /// <param name="direction">Which way the target is knocked back.</param>
        /// <returns>True if the hit landed.</returns>
        public bool ApplyTo(ModuleContext context, EntityBehaviour target, Vector3 direction)
        {
            Entity user = context.Owner;
            Entity victim = target.Entity;
            CombatRulesBehaviour rules = context.World.CombatRules;
            if (victim == null || rules.Pipeline == null)
            {
                return false;
            }

            DamageEvent result;
            if (damage != null)
            {
                float amount = damage.Damage(StatOwner(user), context.World.Database.Formulas)
                    + SpellDamageBonus.For(user, damageType);
                result = rules.Pipeline.Apply(new Hit(user, amount, damageType), victim);
            }
            else if (!victim.IsIncapacitated && !victim.IsBeingDragged && rules.FriendlyFire.CanHurt(user, victim))
            {
                // No damage to deal, but it lands on whoever the user could hurt.
                result = new DamageEvent(user, victim, damageType, false, DamageOutcome.Dealt, 0f);
            }
            else
            {
                return false;
            }

            if (rules.Knockback.TryGetDistance(module.KnockbackTier, result, out float distance)
                && target.TryGetComponent(out EntityMotor motor))
            {
                motor.ApplyKnockback(direction, distance);
            }

            bool landed = result.Outcome == DamageOutcome.Dealt;
            if (landed && status.HasValue && !victim.IsIncapacitated)
            {
                TryApplyStatus(context, victim);
            }

            return landed;
        }

        public void Describe(StringBuilder text, ModuleContext context)
        {
            if (damage != null)
            {
                damage.Describe(text, StatOwner(context.Owner), context.World.Database.FormulaConstants,
                    context.World.Database.Formulas);
                float spellBonus = SpellDamageBonus.For(context.Owner, damageType);
                if (spellBonus > 0f)
                {
                    text.Append("+ ").Append(spellBonus.ToString(NumberFormat, CultureInfo.InvariantCulture))
                        .Append(" from spell tomes\n");
                }
            }

            if (module.KnockbackTier.IsSet)
            {
                text.Append("Knockback ").Append(module.KnockbackTier.Word).Append(" (")
                    .Append(module.KnockbackTier.Value.ToString(NumberFormat, CultureInfo.InvariantCulture))
                    .Append(" m)\n");
            }

            if (status.HasValue)
            {
                text.Append(status.Value).Append(' ')
                    .Append(statusSeconds.ToString(NumberFormat, CultureInfo.InvariantCulture)).Append(" s");
                if (module.ResistStats.Count > 0)
                {
                    text.Append(", resisted by ").Append(module.ResistStats[0]);
                }

                text.Append('\n');
            }
        }

        private Entity StatOwner(Entity user)
        {
            return usesSummonerStats && user.Summoner != null ? user.Summoner : user;
        }

        private void TryApplyStatus(ModuleContext context, Entity victim)
        {
            if (module.ResistStats.Count > 0
                && ResistRules.Resists(module, module.ResistStats[0], context.Owner, victim, context.Random))
            {
                return;
            }

            victim.Statuses.TryApply(new StatusApplication(status.Value, context.Owner, statusSeconds));
        }
    }
}
