using System.Text;
using UnityEngine;
using Zenthium.Data;
using Zenthium.Entities;

namespace Zenthium.Modules
{
    /// <summary>
    /// An instant beam along the aim, as far as the range tier, such as Repulsor Blast: it hits the first entity the
    /// user can hurt, and anything solid stops it, taking its damage if it's a wall or prop.
    /// </summary>
    public sealed class BeamModule : ModuleBehaviour
    {
        private const int MaxBeamHits = 16;

        // Meters: how thick the beam's sweep is, so it can't slip between close targets.
        private const float BeamRadius = 0.25f;

        private readonly RaycastHit[] beamHits = new RaycastHit[MaxBeamHits];
        private readonly HitEffects effects;

        public BeamModule(ModuleData data, HitEffects effects) : base(data)
        {
            this.effects = effects;
        }

        public override void DescribeEffects(StringBuilder text)
        {
            effects.Describe(text, Context);
        }

        protected override bool CanActivateNow(ModuleAim aim)
        {
            return aim.Direction.sqrMagnitude > 0f;
        }

        protected override void Activate(ModuleAim aim)
        {
            Vector3 origin = Context.Position;
            Vector3 direction = aim.Direction.normalized;
            float range = Data.RangeTier.Value;
            float length = range;
            if (SweepHit.TryFindFirst(Context, origin, direction, BeamRadius, range, beamHits,
                    out EntityBehaviour target, out Collider solid, out float hitDistance))
            {
                length = hitDistance;
                if (target != null)
                {
                    effects.ApplyTo(Context, target, direction);
                }
                else if (ModuleWorld.TryGetStructure(solid, out IDamageableStructure structure))
                {
                    effects.ApplyToStructure(Context, structure);
                }
            }

            Context.World.ShowBeam(origin, origin + direction * length);
        }
    }
}
