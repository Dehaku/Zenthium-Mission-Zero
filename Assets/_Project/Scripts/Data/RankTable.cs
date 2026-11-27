using System;
using UnityEngine;

namespace Zenthium.Data
{
    /// <summary>
    /// One number per rank from D to S, read from constants.csv rows named <c>name.Rank</c>. S2 and up use S's
    /// (difficulty.md).
    /// </summary>
    [Serializable]
    public sealed class RankTable
    {
        [SerializeField] private float[] values = new float[RankCount];

        private static int RankCount => Enum.GetValues(typeof(Rank)).Length;

        // Unity's serializer needs a parameterless constructor.
        public RankTable()
        {
        }

        /// <param name="values">One per <see cref="Rank"/>, in its order.</param>
        public RankTable(float[] values)
        {
            this.values = values;
        }

        /// <param name="difficultyLevel">Mission ranks above D (0 at D).</param>
        public float For(int difficultyLevel)
        {
            if (values == null || values.Length == 0)
            {
                return 0f;
            }

            return values[Mathf.Clamp(difficultyLevel, 0, values.Length - 1)];
        }
    }
}
