using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Zenthium.Data;
using Zenthium.Entities;

namespace Zenthium.Modules
{
    /// <summary>How an <see cref="ExtractionModule"/> secures its target.</summary>
    public enum ExtractionMode
    {
        /// <summary>Evac Balloon: a downed citizen (rescued) or enemy (captured) floats off the map.</summary>
        FloatAway,

        /// <summary>Stasis Cuffs: a downed enemy is captured where it lies and stays there.</summary>
        CaptureInPlace,
    }

    /// <summary>
    /// Secures the nearest Incapacitated, uncaptured target within the range tier (modules.md): Evac Balloon floats a
    /// citizen or enemy off the map; Stasis Cuffs captures an enemy on the spot. With nothing in reach it isn't used,
    /// so the cooldown doesn't start. Standing citizens come with Milestone 6, and the mission counts with
    /// Milestone 9.
    /// </summary>
    public sealed class ExtractionModule : ModuleBehaviour
    {
        private readonly List<EntityBehaviour> reached = new List<EntityBehaviour>();
        private readonly ExtractionMode mode;
        private EntityBehaviour target;

        public ExtractionModule(ModuleData data, ExtractionMode mode) : base(data)
        {
            this.mode = mode;
        }

        public override void DescribeEffects(StringBuilder text)
        {
            text.Append(mode == ExtractionMode.FloatAway
                ? "Floats away a downed citizen (rescued) or enemy (captured) in reach\n"
                : "Captures a downed enemy in reach, where it lies\n");
        }

        protected override bool CanActivateNow(ModuleAim aim)
        {
            target = FindTarget();
            return target != null;
        }

        protected override void Activate(ModuleAim aim)
        {
            if (mode == ExtractionMode.CaptureInPlace)
            {
                Context.World.Capture(target);
            }
            else
            {
                ExtractionKind kind = target.Entity.Team == Team.Civilian
                    ? ExtractionKind.Rescued
                    : ExtractionKind.Captured;
                Context.World.Extract(target, kind);
            }

            target = null;
        }

        private EntityBehaviour FindTarget()
        {
            Vector3 origin = Context.Position;
            Context.World.CollectEntities(origin, Data.RangeTier.Value, reached);
            EntityBehaviour nearest = null;
            float nearestDistance = float.PositiveInfinity;
            foreach (EntityBehaviour candidate in reached)
            {
                Entity entity = candidate.Entity;
                bool evacuable = entity.IsIncapacitated && !entity.IsCaptured && (entity.Team == Team.Enemy
                    || (mode == ExtractionMode.FloatAway && entity.Team == Team.Civilian));
                if (!evacuable || candidate == Context.OwnerBehaviour)
                {
                    continue;
                }

                float distance = (candidate.transform.position - origin).sqrMagnitude;
                if (distance < nearestDistance)
                {
                    nearest = candidate;
                    nearestDistance = distance;
                }
            }

            return nearest;
        }
    }
}
