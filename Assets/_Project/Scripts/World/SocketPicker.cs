using System.Collections.Generic;

namespace Zenthium.World
{
    /// <summary>Weighted choices over sockets' options (maps.md, Sockets), as plain rules.</summary>
    public static class SocketPicker
    {
        /// <summary>How strongly a socket offers this content: the sum of its options' weights for it.</summary>
        public static float WeightFor(IReadOnlyList<SocketOption> options, SocketContent content)
        {
            float weight = 0f;
            foreach (SocketOption option in options)
            {
                if (option.Content == content && option.Weight > 0f)
                {
                    weight += option.Weight;
                }
            }

            return weight;
        }

        /// <summary>A weighted pick: each index's chance is its share of the total weight.</summary>
        /// <param name="roll">A number from 0 to 1.</param>
        /// <returns>-1 if no weight is above 0.</returns>
        public static int Pick(IReadOnlyList<float> weights, float roll)
        {
            float total = 0f;
            int last = -1;
            for (int i = 0; i < weights.Count; i++)
            {
                if (weights[i] > 0f)
                {
                    total += weights[i];
                    last = i;
                }
            }

            if (last < 0)
            {
                return -1;
            }

            float target = roll * total;
            float reached = 0f;
            for (int i = 0; i < weights.Count; i++)
            {
                if (weights[i] <= 0f)
                {
                    continue;
                }

                reached += weights[i];
                if (target < reached)
                {
                    return i;
                }
            }

            // A roll of exactly 1, or float rounding at the top end.
            return last;
        }

        /// <summary>What a socket spawns when it rolls its own content by its options' weights.</summary>
        /// <param name="roll">A number from 0 to 1.</param>
        /// <returns>False if it has no option with a weight above 0.</returns>
        public static bool TryRollContent(IReadOnlyList<SocketOption> options, float roll, out SocketContent content)
        {
            var weights = new float[options.Count];
            for (int i = 0; i < options.Count; i++)
            {
                weights[i] = options[i].Weight;
            }

            int index = Pick(weights, roll);
            content = index >= 0 ? options[index].Content : default;
            return index >= 0;
        }
    }
}
