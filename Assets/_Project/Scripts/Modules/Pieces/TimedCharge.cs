using System;
using System.Collections.Generic;
using UnityEngine;
using Zenthium.Combat;
using Zenthium.Entities;

namespace Zenthium.Modules
{
    /// <summary>
    /// A placed explosive (Breaching Charge): its fuse burns on game time, so Time Slow stretches it and Time Stop
    /// holds it. Then every entity within the radius takes the charge's hit effects, pushed away from it, and so does
    /// every wall, door, window and prop (Breaching Charge's Demolition tag hits buildings harder). Presentation
    /// listens to its events.
    /// </summary>
    public sealed class TimedCharge : MonoBehaviour
    {
        private readonly List<EntityBehaviour> reached = new List<EntityBehaviour>();
        private readonly List<IDamageableStructure> reachedStructures = new List<IDamageableStructure>();
        private ModuleContext context;
        private HitEffects effects;
        private ModuleWorld blastWorld;
        private Hit blast;
        private float radius;
        private float remaining;

        /// <summary>Raised once when placed, with the blast radius in meters.</summary>
        public event Action<float> OnArmed;

        /// <summary>Raised every frame with the fuse's seconds left.</summary>
        public event Action<float> OnFuseChanged;

        public void Arm(ModuleContext user, HitEffects hitEffects, float blastRadius, float fuseSeconds)
        {
            context = user;
            effects = hitEffects;
            radius = blastRadius;
            remaining = fuseSeconds;
            OnArmed?.Invoke(radius);
            OnFuseChanged?.Invoke(remaining);
        }

        /// <summary>
        /// Armed with no user, such as a Mothership Strike: the blast is environmental (<see cref="AreaBlast"/>), with
        /// no knockback.
        /// </summary>
        public void Arm(ModuleWorld world, Hit environmentalBlast, float blastRadius, float fuseSeconds)
        {
            blastWorld = world;
            blast = environmentalBlast;
            radius = blastRadius;
            remaining = fuseSeconds;
            OnArmed?.Invoke(radius);
            OnFuseChanged?.Invoke(remaining);
        }

        private void Update()
        {
            ModuleWorld world = context != null ? context.World : blastWorld;
            if (world == null)
            {
                return;
            }

            float realDelta = Mathf.Min(Time.unscaledDeltaTime, Time.maximumDeltaTime);
            remaining -= world.Clock.Clock.GameDelta(realDelta, false);
            OnFuseChanged?.Invoke(Mathf.Max(0f, remaining));
            if (remaining <= 0f)
            {
                Explode();
            }
        }

        private void Explode()
        {
            Vector3 origin = transform.position;
            if (context == null)
            {
                AreaBlast.Detonate(blastWorld, origin, radius, blast, null, reached, reachedStructures);
                Destroy(gameObject);
                return;
            }

            context.World.CollectEntities(origin, radius, reached);
            foreach (EntityBehaviour target in reached)
            {
                effects.ApplyTo(context, target, MeleeStrikeModule.Away(origin, target, Vector3.forward));
            }

            context.World.CollectStructures(origin, radius, reachedStructures);
            foreach (IDamageableStructure structure in reachedStructures)
            {
                effects.ApplyToStructure(context, structure);
            }

            Destroy(gameObject);
        }
    }
}
