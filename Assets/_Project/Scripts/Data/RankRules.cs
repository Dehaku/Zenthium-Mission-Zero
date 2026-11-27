namespace Zenthium.Data
{
    /// <summary>
    /// Mission ranks as difficulty levels: ranks above D, so D is 0 and S is 4; S2 and up continue past S
    /// (difficulty.md).
    /// </summary>
    public static class RankRules
    {
        /// <summary>Whether a mission at this difficulty level is at the rank or above it.</summary>
        public static bool HasReached(int difficultyLevel, Rank rank)
        {
            return difficultyLevel >= (int)rank;
        }
    }
}
