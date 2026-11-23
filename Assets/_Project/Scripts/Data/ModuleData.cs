using UnityEngine;

namespace Zenthium.Data
{
    /// <summary>
    /// The numbers of one module, from a row of modules.csv. Its behavior is code, linked by <see cref="DataEntry.Id"/>.
    /// </summary>
    public sealed class ModuleData : DataEntry
    {
        [SerializeField] private ModuleCategory category;
        [SerializeField] private bool hasStat;
        [SerializeField] private Stat stat;
        [SerializeField] private bool hasCooldown;
        [SerializeField] private float cooldownSeconds;
        [SerializeField] private bool hasResourceCost;
        [SerializeField] private ResourceType resource;
        [SerializeField] private float resourceCost;
        [SerializeField] private TierValue damageTier;
        [SerializeField] private TierValue utilityTier;
        [SerializeField] private TierValue durationTier;
        [SerializeField] private TierValue radiusTier;
        [SerializeField] private TierValue knockbackTier;
        [SerializeField] private TierValue rangeTier;
        [SerializeField] private DataValueList values = new DataValueList();

        public ModuleCategory Category => category;

        /// <summary>Unset tiers have <see cref="TierValue.IsSet"/> false.</summary>
        public TierValue DamageTier => damageTier;

        /// <inheritdoc cref="DamageTier"/>
        public TierValue UtilityTier => utilityTier;

        /// <inheritdoc cref="DamageTier"/>
        public TierValue DurationTier => durationTier;

        /// <inheritdoc cref="DamageTier"/>
        public TierValue RadiusTier => radiusTier;

        /// <inheritdoc cref="DamageTier"/>
        public TierValue KnockbackTier => knockbackTier;

        /// <inheritdoc cref="DamageTier"/>
        public TierValue RangeTier => rangeTier;

        public DataValueList Values => values;

        /// <summary>The stat the module scales with, if it names one.</summary>
        public bool TryGetStat(out Stat scalingStat)
        {
            scalingStat = stat;
            return hasStat;
        }

        /// <summary>The cooldown in real seconds, if the module has one.</summary>
        public bool TryGetCooldown(out float seconds)
        {
            seconds = cooldownSeconds;
            return hasCooldown;
        }

        /// <summary>The resource spent per use, if any.</summary>
        public bool TryGetResourceCost(out ResourceType spentResource, out float cost)
        {
            spentResource = resource;
            cost = resourceCost;
            return hasResourceCost;
        }

        internal void Populate(
            ModuleCategory newCategory,
            Stat? newStat,
            float? newCooldownSeconds,
            ResourceType? newResource,
            float newResourceCost,
            TierValue newDamageTier,
            TierValue newUtilityTier,
            TierValue newDurationTier,
            TierValue newRadiusTier,
            TierValue newKnockbackTier,
            TierValue newRangeTier,
            DataValueList newValues)
        {
            category = newCategory;
            hasStat = newStat.HasValue;
            stat = newStat.GetValueOrDefault();
            hasCooldown = newCooldownSeconds.HasValue;
            cooldownSeconds = newCooldownSeconds.GetValueOrDefault();
            hasResourceCost = newResource.HasValue;
            resource = newResource.GetValueOrDefault();
            resourceCost = newResource.HasValue ? newResourceCost : 0f;
            damageTier = newDamageTier;
            utilityTier = newUtilityTier;
            durationTier = newDurationTier;
            radiusTier = newRadiusTier;
            knockbackTier = newKnockbackTier;
            rangeTier = newRangeTier;
            values = newValues;
        }
    }
}
