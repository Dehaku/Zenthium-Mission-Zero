using System.Collections.Generic;
using UnityEngine;
using Zenthium.Combat;
using Zenthium.Data;
using Zenthium.Entities;
using Zenthium.Movement;

namespace Zenthium.Modules
{
    /// <summary>
    /// Kick (modules.md, Kick row): hits every valid entity in a melee arc in front of the aim, for damage from its
    /// stat and damage tier, then knocks each one back by its knockback tier. The cooldown comes from Kick's data.
    /// </summary>
    [RequireComponent(typeof(EntityBehaviour), typeof(Rigidbody))]
    public sealed class KickAction : MonoBehaviour
    {
        private const int MaxReachHits = 32;

        // modules.csv has no damage type column yet (damage types are only in the markdown).
        private const DamageType KickDamageType = DamageType.Physical;

        [SerializeField] private GameDatabase database;
        [SerializeField] private CombatRulesBehaviour combatRules;

        [Tooltip("Degrees either side of the aim direction that Kick reaches. Its range is Kick's range tier.")]
        [SerializeField, Range(0f, 180f)] private float arcHalfAngle = 60f;

        private readonly Collider[] reachHits = new Collider[MaxReachHits];
        private readonly List<EntityBehaviour> targets = new List<EntityBehaviour>();
        private EntityBehaviour entityBehaviour;
        private Rigidbody body;
        private ModuleData kick;
        private Stat stat;
        private ModuleCooldown cooldown;

        /// <summary>Kicks toward the aim direction if the cooldown is ready.</summary>
        /// <returns>True if the kick happened, whether or not it hit anything.</returns>
        public bool TryKick(Vector3 aimDirection)
        {
            if (!isActiveAndEnabled)
            {
                return false;
            }

            Entity kicker = entityBehaviour.Entity;
            if (kicker == null || kicker.IsIncapacitated || !cooldown.IsReady || combatRules.Pipeline == null)
            {
                return false;
            }

            cooldown.Start();
            float damage = database.Formulas.TierDamage(kicker.Stats.Get(stat), kick.DamageTier);
            CollectTargets(aimDirection);
            foreach (EntityBehaviour target in targets)
            {
                DamageEvent result = combatRules.Pipeline.Apply(new Hit(kicker, damage, KickDamageType), target.Entity);
                if (combatRules.Knockback.TryGetDistance(kick.KnockbackTier, result, out float distance)
                    && target.TryGetComponent(out EntityMotor motor))
                {
                    motor.ApplyKnockback(AwayFromKicker(target, aimDirection), distance);
                }
            }

            return true;
        }

        private void Awake()
        {
            entityBehaviour = GetComponent<EntityBehaviour>();
            body = GetComponent<Rigidbody>();

            if (database == null || combatRules == null
                || !database.Modules.TryGet(DataIds.Modules.Kick, out kick)
                || !kick.TryGetStat(out stat) || !kick.TryGetCooldown(out float cooldownSeconds)
                || !kick.DamageTier.IsSet || !kick.RangeTier.IsSet)
            {
                Debug.LogError($"{name}: {nameof(KickAction)} needs Combat Rules and a Game Database with Kick's "
                    + "stat, cooldown, damage tier and range tier.", this);
                enabled = false;
                return;
            }

            cooldown = new ModuleCooldown(cooldownSeconds);
        }

        private void Update()
        {
            if (entityBehaviour.ClockDriver == null)
            {
                return;
            }

            // Clamped like Time.deltaTime, so a hitch doesn't skip a whole cooldown at once.
            float realDelta = Mathf.Min(Time.unscaledDeltaTime, Time.maximumDeltaTime);
            cooldown.Tick(entityBehaviour.ClockDriver.Clock.CooldownDelta(realDelta));
        }

        private void CollectTargets(Vector3 aimDirection)
        {
            targets.Clear();
            Vector3 origin = body.position;
            int count = Physics.OverlapSphereNonAlloc(
                origin, kick.RangeTier.Value, reachHits, Physics.AllLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                if (!TryGetEntity(reachHits[i], out EntityBehaviour target) || target == entityBehaviour
                    || target.Entity == null || targets.Contains(target)
                    || !MeleeArc.Contains(origin, aimDirection, target.transform.position, arcHalfAngle))
                {
                    continue;
                }

                // Dragged targets can't be hurt or moved, so Kick passes over them.
                if (!target.Entity.IsBeingDragged)
                {
                    targets.Add(target);
                }
            }
        }

        private Vector3 AwayFromKicker(EntityBehaviour target, Vector3 aimDirection)
        {
            Vector3 away = target.transform.position - body.position;
            away.y = 0f;
            return away.sqrMagnitude > 0f ? away : aimDirection;
        }

        private static bool TryGetEntity(Collider hit, out EntityBehaviour entity)
        {
            Rigidbody attached = hit.attachedRigidbody;
            if (attached != null)
            {
                return attached.TryGetComponent(out entity);
            }

            return hit.TryGetComponent(out entity);
        }
    }
}
