using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Zenthium.Data;
using Zenthium.Entities;
using Zenthium.Statuses;

namespace Zenthium.Modules
{
    /// <summary>
    /// Charming (modules.md), the "on approach" trigger: each enemy goon or Stubborn citizen that comes within its
    /// range rolls once, for <c>goon_convert</c> or <c>stubborn_to_scared</c>, and the hidden Charmed marker records
    /// that it rolled. A successful roll calls its <see cref="ICharmable"/>: the goon converts and hands over its
    /// Valuables, the citizen turns Scared.
    /// </summary>
    public sealed class CharmingPassive : ModuleBehaviour
    {
        public const string GoonConvertKey = "goon_convert";
        public const string StubbornToScaredKey = "stubborn_to_scared";

        private readonly List<EntityBehaviour> reached = new List<EntityBehaviour>();
        private readonly float goonConvertChance;
        private readonly float stubbornToScaredChance;

        public CharmingPassive(ModuleData data, float goonConvertChance, float stubbornToScaredChance) : base(data)
        {
            this.goonConvertChance = goonConvertChance;
            this.stubbornToScaredChance = stubbornToScaredChance;
        }

        public override void DescribeEffects(StringBuilder text)
        {
            text.Append("Goons within ").Append(Data.RangeTier.Value.ToString("0.##", CultureInfo.InvariantCulture))
                .Append(" m: ").Append((goonConvertChance * 100f).ToString("0.#", CultureInfo.InvariantCulture))
                .Append("% chance to convert, once each\n")
                .Append("Stubborn citizens: ")
                .Append((stubbornToScaredChance * 100f).ToString("0.#", CultureInfo.InvariantCulture))
                .Append("% chance to turn Scared, once each\n");
        }

        protected internal override void Tick(float gameDelta)
        {
            Context.World.CollectEntities(Context.Position, Data.RangeTier.Value, reached);
            foreach (EntityBehaviour candidate in reached)
            {
                Entity target = candidate.Entity;
                if (target == null || target.IsIncapacitated || target.Statuses.Has(StatusType.Charmed)
                    || !candidate.TryGetComponent(out ICharmable charmable)
                    || !charmable.TryGetCharmTarget(out CharmTarget kind))
                {
                    continue;
                }

                target.Statuses.TryApply(
                    new StatusApplication(StatusType.Charmed, Owner, StatusApplication.UntilRemoved));
                float chance = kind == CharmTarget.Goon ? goonConvertChance : stubbornToScaredChance;
                if (Context.Random.Value01() < chance)
                {
                    charmable.Charm(Owner);
                }
            }
        }
    }
}
