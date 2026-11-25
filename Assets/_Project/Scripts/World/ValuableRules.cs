using System;
using Zenthium.Data;

namespace Zenthium.World
{
    /// <summary>
    /// Carrying Valuables (world.md, Carrying): Carrying Treasure slows by a share per Valuable, up to a cap, and the
    /// money bag icon grows by a share per Valuable, up to its own cap.
    /// </summary>
    public static class ValuableRules
    {
        // Absorbs float error, so a cap that is a whole number of shares isn't floored below it.
        private const float WholeNumberTolerance = 0.0001f;

        /// <summary>Carrying Treasure's layers: one per Valuable, up to the number that reaches the slow cap.</summary>
        public static int CarryingTreasureLayers(int count, ValuableConstants constants)
        {
            return Math.Max(0, Math.Min(count, MaxCarryingTreasureLayers(constants)));
        }

        /// <summary>The layer count at which the slow reaches its cap.</summary>
        public static int MaxCarryingTreasureLayers(ValuableConstants constants)
        {
            if (!(constants.SlowPerValuable > 0f))
            {
                return 0;
            }

            return (int)Math.Floor(constants.SlowCap / constants.SlowPerValuable + WholeNumberTolerance);
        }

        /// <summary>The icon's size as a multiple of its base size; it's hidden at 0 Valuables.</summary>
        public static float IconScale(int count, ValuableConstants constants)
        {
            return 1f + Math.Min(Math.Max(0, count) * constants.IconGrowthPerValuable, constants.IconGrowthCap);
        }
    }
}
