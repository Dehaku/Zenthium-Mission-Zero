using System;
using Zenthium.Data;

namespace Zenthium.Entities
{
    /// <summary>
    /// The shared constants every entity is built from.
    /// </summary>
    public sealed class EntityRules
    {
        public EntityRules(FormulaConstants formula, EntityConstants entity, RankScalingConstants rankScaling)
        {
            Formula = formula ?? throw new ArgumentNullException(nameof(formula));
            EntityConstants = entity ?? throw new ArgumentNullException(nameof(entity));
            RankScaling = rankScaling ?? throw new ArgumentNullException(nameof(rankScaling));
            Formulas = new BalanceFormulas(formula);
        }

        public FormulaConstants Formula { get; }
        public EntityConstants EntityConstants { get; }
        public RankScalingConstants RankScaling { get; }
        public BalanceFormulas Formulas { get; }

        public static EntityRules From(GameDatabase database)
        {
            return new EntityRules(database.FormulaConstants, database.EntityConstants, database.RankScaling);
        }
    }
}
