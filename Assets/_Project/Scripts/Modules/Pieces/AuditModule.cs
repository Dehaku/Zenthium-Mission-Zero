using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using Zenthium.Data;
using Zenthium.Entities;

namespace Zenthium.Modules
{
    /// <summary>
    /// Audit (modules.md): reveals the player to every entity on the user's side within the range tier, measured from
    /// the user and needing no line of sight (the user's decisions, Milestone 7b). They become aware at once, as from
    /// an alert, and learn where the player is. The player is the nearest player-team entity.
    /// </summary>
    public sealed class AuditModule : ModuleBehaviour
    {
        private const string NumberFormat = "0.#";

        private readonly List<EntityBehaviour> everyone = new List<EntityBehaviour>();
        private readonly List<EntityBehaviour> reached = new List<EntityBehaviour>();

        public AuditModule(ModuleData data) : base(data)
        {
        }

        public override void DescribeEffects(StringBuilder text)
        {
            text.Append("Reveals the player to every ally within ")
                .Append(Data.RangeTier.Value.ToString(NumberFormat, CultureInfo.InvariantCulture)).Append(" m\n");
        }

        protected override bool CanActivateNow(ModuleAim aim)
        {
            return TryFindPlayer(out _);
        }

        protected override void Activate(ModuleAim aim)
        {
            if (!TryFindPlayer(out EntityBehaviour player))
            {
                return;
            }

            Vector3 playerPosition = player.transform.position;
            Context.World.CollectEntities(Context.Position, Data.RangeTier.Value, reached);
            foreach (EntityBehaviour body in reached)
            {
                if (SideRules.IsOnUsersSide(Owner, body.Entity) && body.TryGetComponent(out IAwarenessReveal reveal))
                {
                    reveal.Reveal(playerPosition);
                }
            }
        }

        private bool TryFindPlayer(out EntityBehaviour player)
        {
            player = null;
            float nearest = float.PositiveInfinity;
            Vector3 origin = Context.Position;
            Context.World.CollectAll(everyone);
            foreach (EntityBehaviour body in everyone)
            {
                Entity other = body.Entity;
                if (other == null || other.Team != Team.Player || other.IsIncapacitated)
                {
                    continue;
                }

                Vector3 offset = body.transform.position - origin;
                offset.y = 0f;
                if (offset.sqrMagnitude < nearest)
                {
                    nearest = offset.sqrMagnitude;
                    player = body;
                }
            }

            return player != null;
        }
    }
}
