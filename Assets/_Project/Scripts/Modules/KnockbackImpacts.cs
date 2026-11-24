using UnityEngine;
using Zenthium.Combat;
using Zenthium.Data;
using Zenthium.Entities;
using Zenthium.Movement;

namespace Zenthium.Modules
{
    /// <summary>
    /// When this entity is knocked into a wall, door, window or prop, the thing it hits takes impact damage and so
    /// does the entity, from the stat of whoever knocked it (world.md, Walls). Running into another entity does
    /// nothing. A knockback with no source deals nothing. Nobody can dodge a wall.
    /// </summary>
    [RequireComponent(typeof(EntityMotor), typeof(EntityModules))]
    public sealed class KnockbackImpacts : MonoBehaviour
    {
        private EntityMotor motor;
        private EntityModules modules;
        private ImpactDamage impactDamage;

        private void Awake()
        {
            motor = GetComponent<EntityMotor>();
            modules = GetComponent<EntityModules>();
        }

        private void OnEnable()
        {
            motor.OnKnockbackObstacleHit += DealImpact;
        }

        private void OnDisable()
        {
            motor.OnKnockbackObstacleHit -= DealImpact;
        }

        private void DealImpact(KnockbackObstacleHit hit)
        {
            ModuleWorld world = modules.World;
            Entity victim = modules.Entity;
            if (!hit.Cause.HasSource || victim == null || world == null || world.CombatRules.Pipeline == null
                || ModuleWorld.TryGetEntity(hit.Obstacle, out _))
            {
                return;
            }

            if (impactDamage == null)
            {
                impactDamage = new ImpactDamage(world.Database.Formulas, world.Database.WorldConstants);
            }

            float stat = hit.Cause.StatValue;
            if (ModuleWorld.TryGetStructure(hit.Obstacle, out IDamageableStructure structure) && structure.CanBeHit)
            {
                var structureHit = new Hit(hit.Cause.Source, impactDamage.ToStructure(stat), DamageType.Physical);
                structure.TakeHit(new StructureHit(structureHit, StructureHitKind.Impact));
            }

            world.CombatRules.Pipeline.Apply(
                new Hit(hit.Cause.Source, impactDamage.ToEntity(stat), DamageType.Physical, canBeDodged: false), victim);
        }
    }
}
