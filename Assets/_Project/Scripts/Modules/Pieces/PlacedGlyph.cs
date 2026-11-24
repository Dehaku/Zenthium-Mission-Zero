using System.Collections.Generic;
using UnityEngine;
using Zenthium.Entities;

namespace Zenthium.Modules
{
    /// <summary>
    /// A placed glyph (Glyph Trap): when an opponent of its user steps within its radius, every opponent inside
    /// takes its effects at once, and the glyph is gone. Its visual is scaled to the radius.
    /// </summary>
    public sealed class PlacedGlyph : MonoBehaviour
    {
        [Tooltip("The flat disc showing the glyph, scaled to its radius.")]
        [SerializeField] private Transform disc;

        private readonly List<EntityBehaviour> reached = new List<EntityBehaviour>();
        private ModuleContext context;
        private HitEffects effects;
        private float radius;

        public void Arm(ModuleContext user, HitEffects hitEffects, float triggerRadius)
        {
            context = user;
            effects = hitEffects;
            radius = triggerRadius;
            if (disc != null)
            {
                disc.localScale = new Vector3(radius * 2f, disc.localScale.y, radius * 2f);
            }
        }

        private void Update()
        {
            if (context == null)
            {
                return;
            }

            Vector3 origin = transform.position;
            context.World.CollectEntities(origin, radius, reached);
            bool triggered = false;
            foreach (EntityBehaviour target in reached)
            {
                if (Opponents.Are(context.Owner, target.Entity) && !target.Entity.IsIncapacitated)
                {
                    effects.ApplyTo(context, target, MeleeStrikeModule.Away(origin, target, Vector3.forward));
                    triggered = true;
                }
            }

            if (triggered)
            {
                Destroy(gameObject);
            }
        }
    }
}
