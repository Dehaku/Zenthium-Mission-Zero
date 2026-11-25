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
    /// resists is unaffected by this cloud. Its visual is scaled to the radius. It blocks line of sight through it,
    /// and when it spreads, opponents inside lose their awareness bars (modules.md, Smoke Bomb).
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

            context.World.RegisterSmoke(this);
            ResetAwarenessInside();
        }

        /// <summary>Whether the line between two points passes through the cloud, on the XZ plane.</summary>
        public bool BlocksSight(Vector3 from, Vector3 to)
        {
            if (context == null)
            {
                return false;
            }

            var center = new Vector2(transform.position.x, transform.position.z);
            var start = new Vector2(from.x, from.z);
            Vector2 segment = new Vector2(to.x, to.z) - start;
            float lengthSquared = segment.sqrMagnitude;
            float along = lengthSquared > 0f ? Mathf.Clamp01(Vector2.Dot(center - start, segment) / lengthSquared) : 0f;
            return (start + segment * along - center).sqrMagnitude <= radius * radius;
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

            if (context.World != null)
            {
                context.World.UnregisterSmoke(this);
            }

            foreach (Entity entity in inside)
            {
                entity.Statuses.LeaveArea(StatusType.Stun, context.Owner);
            }
        }

        private void ResetAwarenessInside()
        {
            context.World.CollectEntities(transform.position, radius, reached);
            foreach (EntityBehaviour target in reached)
            {
                if (Opponents.Are(context.Owner, target.Entity) && target.TryGetComponent(out IAwarenessReset awareness))
                {
                    awareness.ResetAwarenessBar();
                }
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
