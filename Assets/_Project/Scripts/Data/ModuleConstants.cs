using System;
using UnityEngine;

namespace Zenthium.Data
{
    /// <summary>
    /// The <c>formula</c> rows of constants.csv for module slots and statuses (heroes.md, Slots; statuses.md).
    /// </summary>
    [Serializable]
    public sealed class ModuleConstants
    {
        [SerializeField] private int slotsPerType;
        [SerializeField] private int presetSlotsUnlocked;
        [SerializeField] private float crowdControlDurationMultiplier;
        [SerializeField] private float crowdControlPulseOnSeconds;
        [SerializeField] private float crowdControlPulseOffSeconds;
        [SerializeField] private float damageOverTimeTickSeconds;

        // Unity's serializer needs a parameterless constructor.
        public ModuleConstants()
        {
        }

        public ModuleConstants(
            int slotsPerType,
            int presetSlotsUnlocked,
            float crowdControlDurationMultiplier,
            float crowdControlPulseOnSeconds,
            float crowdControlPulseOffSeconds,
            float damageOverTimeTickSeconds)
        {
            this.slotsPerType = slotsPerType;
            this.presetSlotsUnlocked = presetSlotsUnlocked;
            this.crowdControlDurationMultiplier = crowdControlDurationMultiplier;
            this.crowdControlPulseOnSeconds = crowdControlPulseOnSeconds;
            this.crowdControlPulseOffSeconds = crowdControlPulseOffSeconds;
            this.damageOverTimeTickSeconds = damageOverTimeTickSeconds;
        }

        /// <summary>Active, passive and gear slots each.</summary>
        public int SlotsPerType => slotsPerType;

        /// <summary>Slots of each type a preset hero starts with unlocked.</summary>
        public int PresetSlotsUnlocked => presetSlotsUnlocked;

        /// <summary>Crowd control length on Leaders and the player, as a fraction of the full length.</summary>
        public float CrowdControlDurationMultiplier => crowdControlDurationMultiplier;

        /// <summary>"While inside" crowd control on Leaders and the player: seconds on per pulse.</summary>
        public float CrowdControlPulseOnSeconds => crowdControlPulseOnSeconds;

        /// <summary>"While inside" crowd control on Leaders and the player: seconds off per pulse.</summary>
        public float CrowdControlPulseOffSeconds => crowdControlPulseOffSeconds;

        /// <summary>Game seconds between damage-over-time ticks.</summary>
        public float DamageOverTimeTickSeconds => damageOverTimeTickSeconds;
    }
}
