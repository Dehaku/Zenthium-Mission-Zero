using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Zenthium.Data;
using Zenthium.Entities;
using Zenthium.Statuses;

namespace Zenthium.Modules
{
    /// <summary>
    /// Charming (modules.md), the "on approach" trigger: each goon that comes within its range rolls once for
    /// <c>goon_convert</c>, and the hidden Charmed marker records that it rolled. A successful roll calls the goon's
    /// <see cref="ICharmable"/>; conversion and its Valuables hand-over come with Milestone 6's AI, and Stubborn
    /// citizens (<c>stubborn_to_scared</c>) with Milestone 6's citizens, so citizens don't roll yet.
    /// </summary>
    public sealed class CharmingPassive : ModuleBehaviour
    {
        public const string GoonConvertKey = "goon_convert";

        private readonly List<EntityBehaviour> reached = new List<EntityBehaviour>();
        private readonly float goonConvertChance;

        public CharmingPassive(ModuleData data, float goonConvertChance) : base(data)
        {
            this.goonConvertChance = goonConvertChance;
        }

        public override void DescribeEffects(StringBuilder text)
        {
            text.Append("Goons within ").Append(Data.RangeTier.Value.ToString("0.##", CultureInfo.InvariantCulture))
                .Append(" m: ").Append((goonConvertChance * 100f).ToString("0.#", CultureInfo.InvariantCulture))
                .Append("% chance to convert, once each\n");
        }

        protected internal override void Tick(float gameDelta)
        {
            Context.World.CollectEntities(Context.Position, Data.RangeTier.Value, reached);
            foreach (EntityBehaviour candidate in reached)
            {
                Entity target = candidate.Entity;
                if (target.Team != Team.Enemy || target.RankTier != RankTier.Goon || target.IsIncapacitated
                    || target.Statuses.Has(StatusType.Charmed))
                {
                    continue;
                }

                target.Statuses.TryApply(
                    new StatusApplication(StatusType.Charmed, Owner, StatusApplication.UntilRemoved));
                if (Context.Random.Value01() < goonConvertChance && candidate.TryGetComponent(out ICharmable charmable))
                {
                    charmable.Convert(Owner);
                }
            }
        }
    }
}
