using System.Collections.Generic;
using UnityEngine;
using Zenthium.Data;
using Zenthium.Entities;
using Zenthium.Statuses;

namespace Zenthium.Modules
{
    /// <summary>
    /// A smoke cloud (Smoke Bomb): for its duration, on game time, opponents of its user inside it are Stunned
    /// while they stay ("while inside", statuses.md). Each rolls the module's resist once on entering; one that
    /// resists is unaffected by this cloud. Its visual is scaled to the radius.
    /// </summary>
    public sealed class SmokeCloud : MonoBehaviour
    {
        [Tooltip("The visual showing the cloud, scaled to its radius.")]
        [SerializeField] private Transform puff;

        private readonly List<EntityBehaviour> reached = new List<EntityBehaviour>();
        private readonly HashSet<Entity> rolled = new HashSet<Entity>();
        private readonly HashSet<Entity> resisted = new HashSet<Entity>();
        private readonly List<Entity> inside = new List<Entity>();
        private readonly List<Entity> stillInside = new List<Entity>();
        private ModuleContext context;
        private ModuleData module;
        private float radius;
        private float remaining;

        public void Spread(ModuleContext user, ModuleData smokeModule, float cloudRadius, float seconds)
        {
            context = user;
            module = smokeModule;
            radius = cloudRadius;
            remaining = seconds;
            if (puff != null)
            {
                puff.localScale = new Vector3(radius * 2f, puff.localScale.y, radius * 2f);
            }
        }

        private void Update()
        {
            if (context == null)
            {
                return;
            }

            float realDelta = Mathf.Min(Time.unscaledDeltaTime, Time.maximumDeltaTime);
            remaining -= context.World.Clock.Clock.GameDelta(realDelta, false);
            if (remaining <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            stillInside.Clear();
            context.World.CollectEntities(transform.position, radius, reached);
            foreach (EntityBehaviour target in reached)
            {
                Entity entity = target.Entity;
                if (!Opponents.Are(context.Owner, entity) || entity.IsIncapacitated || IsResisted(entity))
                {
                    continue;
                }

                entity.Statuses.SetInsideArea(new StatusApplication(StatusType.Stun, context.Owner,
                    StatusApplication.UntilRemoved));
                stillInside.Add(entity);
            }

            foreach (Entity entity in inside)
            {
                if (!stillInside.Contains(entity))
                {
                    entity.Statuses.LeaveArea(StatusType.Stun, context.Owner);
                }
            }

            inside.Clear();
            inside.AddRange(stillInside);
        }

        private void OnDestroy()
        {
            if (context == null)
            {
                return;
            }

            foreach (Entity entity in inside)
            {
                entity.Statuses.LeaveArea(StatusType.Stun, context.Owner);
            }
        }

        /// <summary>Rolls the resist the first time the entity is inside.</summary>
        private bool IsResisted(Entity entity)
        {
            if (rolled.Add(entity) && module.ResistStats.Count > 0
                && ResistRules.Resists(module, module.ResistStats[0], context.Owner, entity, context.Random))
            {
                resisted.Add(entity);
            }

            return resisted.Contains(entity);
        }
    }
}
