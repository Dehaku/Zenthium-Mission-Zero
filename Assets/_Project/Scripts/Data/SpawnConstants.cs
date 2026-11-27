using System;
using UnityEngine;

namespace Zenthium.Data
{
    /// <summary>
    /// constants.csv's <c>mission</c> rows for spending the threat budget, squads, the Leader's entrance and waves
    /// (enemies.md).
    /// </summary>
    [Serializable]
    public sealed class SpawnConstants
    {
        [SerializeField] private float budgetSmall;
        [SerializeField] private float budgetMedium;
        [SerializeField] private float budgetLarge;
        [SerializeField] private RankTable championShare = new RankTable();
        [SerializeField] private int squadSizeMin;
        [SerializeField] private int squadSizeMax;
        [SerializeField] private float championSoloChance;
        [SerializeField] private float leaderEntranceSeconds;
        [SerializeField] private int populationCap;
        [SerializeField] private float reinforcementIntervalSeconds;
        [SerializeField] private float reinforcementAliveFraction;
        [SerializeField] private float reinforcementWaveFraction;
        [SerializeField] private int reinforcementWavesAtB;
        [SerializeField] private int reinforcementWavesPerRank;
        [SerializeField] private float waveEdgeMinDistance;
        [SerializeField] private float waveDropMinDistance;
        [SerializeField] private float waveLandingSeconds;
        [SerializeField] private float invasionStartFraction;
        [SerializeField] private float invasionWaveFraction;

        // Unity's serializer needs a parameterless constructor.
        public SpawnConstants()
        {
        }

        public SpawnConstants(
            float budgetSmall,
            float budgetMedium,
            float budgetLarge,
            RankTable championShare,
            int squadSizeMin,
            int squadSizeMax,
            float championSoloChance,
            float leaderEntranceSeconds,
            int populationCap,
            float reinforcementIntervalSeconds,
            float reinforcementAliveFraction,
            float reinforcementWaveFraction,
            int reinforcementWavesAtB,
            int reinforcementWavesPerRank,
            float waveEdgeMinDistance,
            float waveDropMinDistance,
            float waveLandingSeconds,
            float invasionStartFraction,
            float invasionWaveFraction)
        {
            this.budgetSmall = budgetSmall;
            this.budgetMedium = budgetMedium;
            this.budgetLarge = budgetLarge;
            this.championShare = championShare;
            this.squadSizeMin = squadSizeMin;
            this.squadSizeMax = squadSizeMax;
            this.championSoloChance = championSoloChance;
            this.leaderEntranceSeconds = leaderEntranceSeconds;
            this.populationCap = populationCap;
            this.reinforcementIntervalSeconds = reinforcementIntervalSeconds;
            this.reinforcementAliveFraction = reinforcementAliveFraction;
            this.reinforcementWaveFraction = reinforcementWaveFraction;
            this.reinforcementWavesAtB = reinforcementWavesAtB;
            this.reinforcementWavesPerRank = reinforcementWavesPerRank;
            this.waveEdgeMinDistance = waveEdgeMinDistance;
            this.waveDropMinDistance = waveDropMinDistance;
            this.waveLandingSeconds = waveLandingSeconds;
            this.invasionStartFraction = invasionStartFraction;
            this.invasionWaveFraction = invasionWaveFraction;
        }

        public int SquadSizeMin => squadSizeMin;
        public int SquadSizeMax => squadSizeMax;

        /// <summary>Chance a Champion spawns alone instead of leading a squad.</summary>
        public float ChampionSoloChance => championSoloChance;

        /// <summary>Game seconds into the mission before the Leader enters.</summary>
        public float LeaderEntranceSeconds => leaderEntranceSeconds;

        /// <summary>Most standard-spawner enemies alive at once.</summary>
        public int PopulationCap => populationCap;

        /// <summary>Game seconds between waves, unless the alive count drops first.</summary>
        public float ReinforcementIntervalSeconds => reinforcementIntervalSeconds;

        /// <summary>A wave comes early when enemies alive drop below this fraction of the starting force.</summary>
        public float ReinforcementAliveFraction => reinforcementAliveFraction;

        /// <summary>Each reinforcement wave's share of the starting budget.</summary>
        public float ReinforcementWaveFraction => reinforcementWaveFraction;

        /// <summary>Waves enter from a map edge at least this many meters from the player.</summary>
        public float WaveEdgeMinDistance => waveEdgeMinDistance;

        /// <summary>Drop-in waves land at a socket at least this many meters from the player.</summary>
        public float WaveDropMinDistance => waveDropMinDistance;

        /// <summary>Game seconds a drop-in wave's landing area counts down.</summary>
        public float WaveLandingSeconds => waveLandingSeconds;

        /// <summary>Invasion's starting force as a share of the budget.</summary>
        public float InvasionStartFraction => invasionStartFraction;

        /// <summary>Each Invasion wave's share of the budget.</summary>
        public float InvasionWaveFraction => invasionWaveFraction;

        /// <summary>The threat budget of a map this size; it doesn't grow with rank.</summary>
        public float Budget(MapSize size)
        {
            switch (size)
            {
                case MapSize.Small:
                    return budgetSmall;
                case MapSize.Medium:
                    return budgetMedium;
                default:
                    return budgetLarge;
            }
        }

        /// <summary>The most of the budget Champions can use at this rank.</summary>
        public float ChampionShare(int difficultyLevel)
        {
            return championShare.For(difficultyLevel);
        }

        /// <summary>Most reinforcement waves: none below rank B, then more each rank (enemies.md).</summary>
        public int ReinforcementWaves(int difficultyLevel)
        {
            if (!RankRules.HasReached(difficultyLevel, Rank.B))
            {
                return 0;
            }

            return reinforcementWavesAtB + reinforcementWavesPerRank * (difficultyLevel - (int)Rank.B);
        }
    }
}
