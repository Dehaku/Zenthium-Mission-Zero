using System.Collections.Generic;
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
    /// Taunt (modules.md): every enemy within vision range that can see the user is Taunted and Marked for
    /// <c>duration_s</c>, one Will resist roll covering both. While taunting, the user takes less damage, and each
    /// enemy that hits them is knocked back with the module's knockback (Kick's force).
    /// </summary>
    public sealed class TauntModule : ModuleBehaviour
    {
        public const string DurationKey = "duration_s";
        public const string MarkedDealtKey = "marked_damage_dealt";
        public const string MarkedTakenKey = "marked_damage_taken";
        public const string SelfReductionKey = "self_damage_reduction";

        private const int MaxSightHits = 16;
        private const string NumberFormat = "0.##";

        private readonly List<EntityBehaviour> reached = new List<EntityBehaviour>();
        private readonly RaycastHit[] sightHits = new RaycastHit[MaxSightHits];
        private readonly StatusModifier[] markedModifiers;
        private readonly float seconds;
        private readonly float selfReduction;
        private Entity taunter;
        private ModifierHandle reductionHandle;
        private float remaining;

        public TauntModule(ModuleData data, float seconds, float markedDealt, float markedTaken, float selfReduction)
            : base(data)
        {
            this.seconds = seconds;
            this.selfReduction = selfReduction;
            markedModifiers = new[]
            {
                new StatusModifier(StatusModifierTarget.DamageDealt, markedDealt),
                new StatusModifier(StatusModifierTarget.DamageTaken, markedTaken),
            };
        }

        public override void DescribeEffects(StringBuilder text)
        {
            text.Append("Enemies in sight: Taunted and Marked (deal ")
                .Append(Percent(markedModifiers[0].Value)).Append(", take +").Append(Percent(markedModifiers[1].Value))
                .Append(") for ").Append(seconds.ToString(NumberFormat, CultureInfo.InvariantCulture))
                .Append(" s\nYou take ").Append(Percent(selfReduction)).Append(" less; attackers are knocked back\n");
        }

        protected override void Activate(ModuleAim aim)
        {
            Vector3 origin = Context.Position;
            Context.World.CollectEntities(origin, Context.World.Database.FormulaConstants.VisionRange, reached);
            foreach (EntityBehaviour target in reached)
            {
                Entity enemy = target.Entity;
                if (!Opponents.Are(Owner, enemy) || enemy.IsIncapacitated || !CanSee(origin, target))
                {
                    continue;
                }

                if (Data.ResistStats.Count > 0
                    && ResistRules.Resists(Data, Data.ResistStats[0], Owner, enemy, Context.Random))
                {
                    continue;
                }

                enemy.Statuses.TryApply(new StatusApplication(StatusType.Taunted, Owner, seconds));
                enemy.Statuses.TryApply(new StatusApplication(StatusType.Marked, Owner, seconds, markedModifiers));
            }

            if (taunter == null)
            {
                taunter = Owner;
                reductionHandle = taunter.Defenses.AddDamageReduction(selfReduction);
            }

            remaining = seconds;
        }

        protected internal override void Tick(float gameDelta)
        {
            if (taunter == null)
            {
                return;
            }

            remaining -= gameDelta;
            if (remaining <= 0f)
            {
                EndTaunt();
            }
        }

        protected internal override void OnDamageTaken(DamageEvent damage)
        {
            if (taunter == null || damage.Outcome != DamageOutcome.Dealt || damage.Source == null
                || !Context.World.TryGetBehaviour(damage.Source, out EntityBehaviour attacker)
                || !attacker.TryGetComponent(out EntityMotor motor))
            {
                return;
            }

            // The resolver needs a hit to judge, so it judges a push from the taunter landing on the attacker.
            var push = new DamageEvent(Owner, damage.Source, damage.Type, false, DamageOutcome.Dealt, 0f);
            KnockbackResolver knockback = Context.World.CombatRules.Knockback;
            if (knockback.TryGetDistance(Data.KnockbackTier, push, out float distance))
            {
                motor.ApplyKnockback(MeleeStrikeModule.Away(Context.Position, attacker, Vector3.forward), distance,
                    ImpactDamage.Cause(Owner, Data));
            }
        }

        protected override void OnDisabled()
        {
            EndTaunt();
        }

        private void EndTaunt()
        {
            if (taunter == null)
            {
                return;
            }

            taunter.Defenses.RemoveDamageReduction(reductionHandle);
            taunter = null;
        }

        private bool CanSee(Vector3 origin, EntityBehaviour target)
        {
            return Context.World.CanSee(origin, target.transform.position, sightHits);
        }

        private static string Percent(float fraction)
        {
            return (fraction * 100f).ToString("0.#", CultureInfo.InvariantCulture) + "%";
        }
    }
}
