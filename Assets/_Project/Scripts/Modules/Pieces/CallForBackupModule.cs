using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Zenthium.Data;
using Zenthium.Entities;

namespace Zenthium.Modules
{
    /// <summary>
    /// Call for Backup (modules.md): summons goons and Champions of the user's faction around it, at the user's mission
    /// rank (<see cref="BackupRoster"/>). They stay until defeated and don't count toward enemy totals (the user's
    /// decision, Milestone 7: it's basically a summon). The faction's arrival tag is a stub until Milestone 9.
    /// </summary>
    public sealed class CallForBackupModule : ModuleBehaviour
    {
        // Meters from the user's center the backup appears, in a ring.
        private const float SpawnRingRadius = 2.5f;

        private readonly List<EntityData> eligible = new List<EntityData>();
        private readonly List<EntityData> picked = new List<EntityData>();
        private readonly DataValue goons;
        private readonly DataValue champions;

        public CallForBackupModule(ModuleData data, DataValue goons, DataValue champions) : base(data)
        {
            this.goons = goons;
            this.champions = champions;
        }

        public override void DescribeEffects(StringBuilder text)
        {
            text.Append("Summons ").Append(Describe(goons)).Append(" goons and ").Append(Describe(champions))
                .Append(" Champions of its faction\n");
        }

        protected override bool CanActivateNow(ModuleAim aim)
        {
            return Context.World.TryGetBehaviour(Owner, out EntityBehaviour body) && body.FactionEntity != null
                && body.FactionEntity.Faction != null;
        }

        protected override void Activate(ModuleAim aim)
        {
            if (!Context.World.TryGetBehaviour(Owner, out EntityBehaviour body) || body.FactionEntity == null)
            {
                return;
            }

            FactionData faction = body.FactionEntity.Faction;
            int level = body.DifficultyLevel;
            IReadOnlyList<EntityData> entities = Context.World.Database.Entities.Entries;
            picked.Clear();
            BackupRoster.Pick(entities, faction, RankTier.Goon, level, BackupRoster.RollCount(goons, Context.Random),
                Context.Random, eligible, picked);
            BackupRoster.Pick(entities, faction, RankTier.Champion, level,
                BackupRoster.RollCount(champions, Context.Random), Context.Random, eligible, picked);

            Vector3 forward = aim.Direction.sqrMagnitude > 0f ? aim.Direction.normalized : Vector3.forward;
            for (int i = 0; i < picked.Count; i++)
            {
                float angle = (360f / picked.Count) * i;
                Vector3 offset = Quaternion.Euler(0f, angle, 0f) * (forward * SpawnRingRadius);
                Context.World.SpawnFactionEntity(picked[i], Owner.Team, level, Context.Position + offset);
            }
        }

        private static string Describe(DataValue value)
        {
            if (value.Kind == DataValueKind.Range)
            {
                return $"{value.RangeMin:0}-{value.RangeMax:0}";
            }

            return value.Number.ToString("0");
        }
    }
}
