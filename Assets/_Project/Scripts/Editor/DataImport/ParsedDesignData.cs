using System.Collections.Generic;
using Zenthium.Data;

namespace Zenthium.Editor.DataImport
{
    /// <summary>
    /// Everything parsed from the design CSVs, checked and ready to write to assets.
    /// </summary>
    public sealed class ParsedDesignData
    {
        public FormulaConstants Formula { get; internal set; }
        public EntityConstants Entity { get; internal set; }
        public ModuleConstants ModuleConstants { get; internal set; }
        public RankScalingConstants RankScaling { get; internal set; }
        public EconomyConstants Economy { get; internal set; }
        public WorldConstants World { get; internal set; }
        public FireConstants Fire { get; internal set; }
        public ValuableConstants Valuables { get; internal set; }
        public AiConstants Ai { get; internal set; }
        public MapConstants Map { get; internal set; }
        public TierTables Tiers { get; internal set; }
        public List<ModuleRecord> Modules { get; } = new List<ModuleRecord>();

        /// <summary>Distinct faction names from entities.csv, in file order.</summary>
        public List<string> FactionIds { get; } = new List<string>();

        public List<EntityRecord> Entities { get; } = new List<EntityRecord>();

        /// <summary>factions.csv's rows; a faction can have none.</summary>
        public List<FactionRecord> Factions { get; } = new List<FactionRecord>();

        public List<HeroRecord> Heroes { get; } = new List<HeroRecord>();
        public List<PropRecord> Props { get; } = new List<PropRecord>();
        public List<PriceRecord> Prices { get; } = new List<PriceRecord>();
    }

    public sealed class DesignDataParseResult
    {
        internal DesignDataParseResult(ParsedDesignData data, IReadOnlyList<string> errors)
        {
            Data = data;
            Errors = errors;
        }

        /// <summary>Null when there are errors, so nothing partial can be written.</summary>
        public ParsedDesignData Data { get; }

        public IReadOnlyList<string> Errors { get; }
    }

    public sealed class ValueRecord
    {
        public string Key { get; internal set; }
        public DataValueKind Kind { get; internal set; }
        public float Number { get; internal set; }
        public float RangeMin { get; internal set; }
        public float RangeMax { get; internal set; }
        public TierValue Tier { get; internal set; }
        public string ModuleId { get; internal set; }

        /// <summary>The file and line it came from, for error messages.</summary>
        public string Location { get; internal set; }
    }

    public sealed class ModuleRecord
    {
        public string Id { get; internal set; }
        public ModuleCategory Category { get; internal set; }
        public Stat? Stat { get; internal set; }
        public float? CooldownSeconds { get; internal set; }
        public ResourceType? Resource { get; internal set; }
        public float ResourceCost { get; internal set; }
        public TierValue DamageTier { get; internal set; }
        public TierValue UtilityTier { get; internal set; }
        public TierValue DurationTier { get; internal set; }
        public TierValue RadiusTier { get; internal set; }
        public TierValue KnockbackTier { get; internal set; }
        public TierValue RangeTier { get; internal set; }
        public List<ValueRecord> Values { get; internal set; }
        public DamageType? DamageType { get; internal set; }
        public List<ModuleTag> Tags { get; internal set; }
        public List<Stat> ResistStats { get; internal set; }
        public List<ResourceType> NoRegenResources { get; internal set; }
    }

    public sealed class EntityRecord
    {
        public string Id { get; internal set; }
        public string FactionId { get; internal set; }
        public RankTier RankTier { get; internal set; }
        public Rank? MinRank { get; internal set; }
        public int? BudgetCost { get; internal set; }
        public List<ValueRecord> Values { get; internal set; }

        /// <summary>Module ids in AI priority order.</summary>
        public List<string> ModuleIds { get; internal set; }

        public List<ModuleLockRecord> Locks { get; internal set; }
        public List<EntityTag> Tags { get; internal set; }

        /// <summary>The file and line it came from, for error messages.</summary>
        public string Location { get; internal set; }
    }

    public sealed class ModuleLockRecord
    {
        public string ModuleId { get; internal set; }
        public Rank Rank { get; internal set; }
    }

    public sealed class FactionRecord
    {
        public string Id { get; internal set; }
        public List<EntityTag> Tags { get; internal set; }
        public ArrivalTag? Arrival { get; internal set; }
        public List<ValueRecord> MissionWeights { get; internal set; }
        public List<ValueRecord> LocationWeights { get; internal set; }
        public List<ValueRecord> Values { get; internal set; }

        /// <summary>The file and line it came from, for error messages.</summary>
        public string Location { get; internal set; }
    }

    public sealed class HeroRecord
    {
        public string Id { get; internal set; }
        public int Strength { get; internal set; }
        public int Will { get; internal set; }
        public int Intelligence { get; internal set; }
        public int Finesse { get; internal set; }
        public int Toughness { get; internal set; }
        public bool IsStarter { get; internal set; }

        /// <summary>Kit module ids per slot type, in slot order.</summary>
        public List<string> ActiveIds { get; internal set; }
        public List<string> PassiveIds { get; internal set; }
        public List<string> GearIds { get; internal set; }

        /// <summary>The file and line it came from, for error messages.</summary>
        public string Location { get; internal set; }
    }

    public sealed class PropRecord
    {
        public string Id { get; internal set; }
        public PropSize Size { get; internal set; }
        public float StrengthNeeded { get; internal set; }
        public float Hp { get; internal set; }
        public float ThrownDamageXKick { get; internal set; }
        public float Armor { get; internal set; }
        public float DamageReduction { get; internal set; }
        public float ExplosionDamage { get; internal set; }
        public float ExplosionRadius { get; internal set; }
    }

    public sealed class PriceRecord
    {
        public string Id { get; internal set; }
        public float Missions { get; internal set; }
        public ZennyFamily Currency { get; internal set; }
    }
}
