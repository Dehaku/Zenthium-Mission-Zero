using System;
using System.Collections.Generic;
using Zenthium.Data;

namespace Zenthium.Modules
{
    /// <summary>
    /// What Drag can grab, in the order it looks for them (modules.md, Drag row). Earlier values win.
    /// </summary>
    public enum DragTargetKind
    {
        Citizen,
        Valuable,
        IncapacitatedEntity,
        Prop,
    }

    /// <summary>One thing in Drag's reach, as <see cref="DragRules.SelectTarget"/> sees it.</summary>
    public readonly struct DragCandidate
    {
        public DragCandidate(DragTargetKind kind, float distance, bool usesCapacity)
        {
            Kind = kind;
            Distance = distance;
            UsesCapacity = usesCapacity;
        }

        public DragTargetKind Kind { get; }
        public float Distance { get; }

        /// <summary>False for Valuables, which don't take up Drag capacity (world.md).</summary>
        public bool UsesCapacity { get; }
    }

    /// <summary>
    /// Drag's numbers and target choice (modules.md, Drag row): carry capacity grows with Strength, and targets are
    /// picked by kind first, then by distance.
    /// </summary>
    public sealed class DragRules
    {
        // Keys of Drag's values column in modules.csv.
        public const string CapacityBaseKey = "capacity_base";
        public const string CapacityPerStrengthKey = "capacity_per_strength";

        // Absorbs float error, so base + per-Strength x Strength landing on a whole number isn't floored below it.
        private const float WholeNumberTolerance = 0.0001f;

        private readonly float capacityBase;
        private readonly float capacityPerStrength;

        public DragRules(float capacityBase, float capacityPerStrength)
        {
            this.capacityBase = capacityBase;
            this.capacityPerStrength = capacityPerStrength;
        }

        /// <returns>False if Drag's data is missing either capacity value.</returns>
        public static bool TryCreate(ModuleData drag, out DragRules rules)
        {
            rules = null;
            if (drag == null
                || !drag.Values.TryGetNumber(CapacityBaseKey, out float capacityBase)
                || !drag.Values.TryGetNumber(CapacityPerStrengthKey, out float capacityPerStrength))
            {
                return false;
            }

            rules = new DragRules(capacityBase, capacityPerStrength);
            return true;
        }

        /// <summary>How many capacity-using targets can be dragged at once: the whole part of the formula.</summary>
        public int Capacity(float strength)
        {
            return Math.Max(0, (int)Math.Floor(capacityBase + capacityPerStrength * strength + WholeNumberTolerance));
        }

        /// <param name="capacityLeft">Free capacity. Candidates that use capacity are skipped when it's 0.</param>
        /// <returns>The index of the candidate to grab, or -1 if none can be grabbed.</returns>
        public static int SelectTarget(IReadOnlyList<DragCandidate> candidates, int capacityLeft)
        {
            int best = -1;
            for (int i = 0; i < candidates.Count; i++)
            {
                DragCandidate candidate = candidates[i];
                if (candidate.UsesCapacity && capacityLeft <= 0)
                {
                    continue;
                }

                if (best < 0 || IsBetter(candidate, candidates[best]))
                {
                    best = i;
                }
            }

            return best;
        }

        private static bool IsBetter(DragCandidate candidate, DragCandidate current)
        {
            if (candidate.Kind != current.Kind)
            {
                return candidate.Kind < current.Kind;
            }

            return candidate.Distance < current.Distance;
        }
    }
}
