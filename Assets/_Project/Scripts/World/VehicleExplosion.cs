using System;
using System.Collections.Generic;
using UnityEngine;
using Zenthium.Combat;
using Zenthium.Data;
using Zenthium.Entities;
using Zenthium.Modules;

namespace Zenthium.World
{
    /// <summary>
    /// A vehicle's explosion (world.md, Props): a delay after it's destroyed, it deals its explosion damage to every
    /// entity in its radius, the player and civilians included, and to every wall, door, window and prop there.
    /// It's environmental damage: Armor and Damage Reduction apply, dodging doesn't. It sets the other vehicles in
    /// the blast on fire. Its fuse runs on game time.
    /// Presentation shows the radius from <see cref="OnFuseLit"/>.
    /// </summary>
    [RequireComponent(typeof(PropBody), typeof(Structure))]
    public sealed class VehicleExplosion : MonoBehaviour
    {
        private readonly List<EntityBehaviour> reachedEntities = new List<EntityBehaviour>();
        private readonly List<IDamageableStructure> reachedStructures = new List<IDamageableStructure>();
        private PropBody prop;
        private Structure structure;
        private bool isLit;
        private float remaining;

        /// <summary>Raised when the vehicle is destroyed, with the blast radius in meters.</summary>
        public event Action<float> OnFuseLit;

        public event Action OnExploded;

        private void Awake()
        {
            prop = GetComponent<PropBody>();

            // Its own lookup: PropBody may not have woken yet when this is enabled.
            structure = GetComponent<Structure>();
        }

        private void OnEnable()
        {
            structure.OnBroken += LightFuse;
        }

        private void OnDisable()
        {
            structure.OnBroken -= LightFuse;
        }

        private void Update()
        {
            ModuleWorld world = structure.World;
            if (!isLit || world == null)
            {
                return;
            }

            float realDelta = Mathf.Min(Time.unscaledDeltaTime, Time.maximumDeltaTime);
            remaining -= world.Clock.Clock.GameDelta(realDelta, false);
            if (remaining <= 0f)
            {
                Explode(world);
            }
        }

        private void LightFuse()
        {
            ModuleWorld world = structure.World;
            if (isLit || world == null || prop.Data == null || !PropRules.Explodes(prop.Data))
            {
                return;
            }

            isLit = true;
            remaining = world.Database.WorldConstants.VehicleExplosionDelay;
            OnFuseLit?.Invoke(prop.Data.ExplosionRadius);
        }

        private void Explode(ModuleWorld world)
        {
            isLit = false;
            Vector3 origin = transform.position;
            var blast = new Hit(null, prop.Data.ExplosionDamage, DamageType.Physical);
            world.CollectEntities(origin, prop.Data.ExplosionRadius, reachedEntities);
            if (world.CombatRules.Pipeline != null)
            {
                foreach (EntityBehaviour target in reachedEntities)
                {
                    world.CombatRules.Pipeline.Apply(blast, target.Entity);
                }
            }

            world.CollectStructures(origin, prop.Data.ExplosionRadius, reachedStructures);
            FireConstants fire = world.Database.FireConstants;
            foreach (IDamageableStructure solid in reachedStructures)
            {
                solid.TakeHit(new StructureHit(blast, StructureHitKind.Direct));

                // world.md: the blast sets the vehicles in it on fire, but not buildings.
                if (solid is Component other && other.TryGetComponent(out PropBody vehicle) && vehicle.Data != null
                    && PropRules.Explodes(vehicle.Data) && other.TryGetComponent(out PropBurning burning))
                {
                    burning.Ignite(fire.BurnDamage, fire.BurnSeconds);
                }
            }

            OnExploded?.Invoke();
            gameObject.SetActive(false);
        }
    }
}
