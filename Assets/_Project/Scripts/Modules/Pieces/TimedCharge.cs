using System;
using System.Collections.Generic;
using UnityEngine;
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

        private void Update()
        {
            if (context == null)
            {
                return;
            }

            float realDelta = Mathf.Min(Time.unscaledDeltaTime, Time.maximumDeltaTime);
            remaining -= context.World.Clock.Clock.GameDelta(realDelta, false);
            OnFuseChanged?.Invoke(Mathf.Max(0f, remaining));
            if (remaining <= 0f)
            {
                Explode();
            }
        }

        private void Explode()
        {
            Vector3 origin = transform.position;
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
