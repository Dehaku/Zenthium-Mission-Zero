using System.Collections.Generic;
using UnityEngine;
using Zenthium.Combat;
using Zenthium.Data;
using Zenthium.Entities;
using Zenthium.Modules;

namespace Zenthium.World
{
    /// <summary>
    /// An entity that crashes when it's defeated, such as a Tow Saucer (Factions/GalacticRepoAgency.md): once, it
    /// deals <c>crash_damage</c> to everything within <c>crash_radius_m</c> (its entities.csv values), its own side
    /// and citizens included, as a vehicle's explosion does (the user's decision, Milestone 7b). Physical damage. An
    /// entity without the values never crashes, so the component is harmless on any body.
    /// </summary>
    [RequireComponent(typeof(EntityBehaviour), typeof(EntityModules))]
    public sealed class CrashOnDefeat : MonoBehaviour
    {
        public const string DamageKey = "crash_damage";
        public const string RadiusKey = "crash_radius_m";

        private readonly List<EntityBehaviour> reachedEntities = new List<EntityBehaviour>();
        private readonly List<IDamageableStructure> reachedStructures = new List<IDamageableStructure>();
        private EntityBehaviour self;
        private EntityModules modules;
        private Entity entity;
        private bool hasCrashed;

        /// <summary>Raised when it crashes, with the blast radius in meters (presentation).</summary>
        public event System.Action<float> OnCrashed;

        /// <returns>False if the values don't have both keys.</returns>
        public static bool TryRead(DataValueList values, out float damage, out float radius)
        {
            radius = 0f;
            damage = 0f;
            return values != null && values.TryGetNumber(DamageKey, out damage) && values.TryGetNumber(RadiusKey, out radius);
        }

        private void Awake()
        {
            self = GetComponent<EntityBehaviour>();
            modules = GetComponent<EntityModules>();
        }

        private void OnEnable()
        {
            // The entity may be created before or after this, and again on reset.
            self.OnEntityCreated += Bind;
            if (self.Entity != null)
            {
                Bind(self.Entity);
            }
        }

        private void OnDisable()
        {
            self.OnEntityCreated -= Bind;
            Unbind();
        }

        private void Bind(Entity newEntity)
        {
            Unbind();
            entity = newEntity;
            hasCrashed = false;
            entity.OnIncapacitated += Crash;
        }

        private void Unbind()
        {
            if (entity != null)
            {
                entity.OnIncapacitated -= Crash;
                entity = null;
            }
        }

        private void Crash()
        {
            EntityData data = self.FactionEntity;
            ModuleWorld world = modules.World;
            if (hasCrashed || data == null || world == null || !TryRead(data.Values, out float damage, out float radius))
            {
                return;
            }

            hasCrashed = true;
            AreaBlast.Detonate(world, transform.position, radius, new Hit(null, damage, DamageType.Physical), entity,
                reachedEntities, reachedStructures);
            OnCrashed?.Invoke(radius);
        }
    }
}
