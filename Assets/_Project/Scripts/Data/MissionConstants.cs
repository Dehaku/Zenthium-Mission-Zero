using System;
using System.Collections.Generic;
using UnityEngine;

namespace Zenthium.Data
{
    /// <summary>
    /// constants.csv's <c>mission</c> rows for the rules each rank sets (difficulty.md) and the mission types' goals
    /// (missions.md).
    /// </summary>
    [Serializable]
    public sealed class MissionConstants
    {
        [Tooltip("Per MapSize: the rank (as a difficulty level) from which missions use it.")]
        [SerializeField] private int[] mapSizeFrom = Array.Empty<int>();

        [Tooltip("Per StructureMaterial: the rank (as a difficulty level) from which locked doors can be made of it.")]
        [SerializeField] private int[] lockedDoorFrom = Array.Empty<int>();

        [SerializeField] private RankTable exitZones = new RankTable();
        [SerializeField] private RankTable leaderChance = new RankTable();
        [SerializeField] private int rescueTargetsSmall;
        [SerializeField] private int rescueTargetsMedium;
        [SerializeField] private int rescueTargetsLarge;
        [SerializeField] private float rescueFailFraction;
        [SerializeField] private float rescueFailFractionPerRank;
        [SerializeField] private float countdownSeconds;
        [SerializeField] private float countdownReductionPerRank;
        [SerializeField] private float countdownMinSeconds;
        [SerializeField] private float countdownGuardFraction;
        [SerializeField] private float countdownTargetHp;
        [SerializeField] private float arsonFailFraction;
        [SerializeField] private float arsonFailReductionPerRank;
        [SerializeField] private float arsonFailMin;
        [SerializeField] private float igniteSeconds;
        [SerializeField] private float invasionLossSeconds;
        [SerializeField] private float invasionSpeedupPerEnemy;
        [SerializeField] private TierValue invasionRadius;
        [SerializeField] private float valuablesGoalFraction;
        [SerializeField] private float captureGoalFraction;

        // Unity's serializer needs a parameterless constructor.
        public MissionConstants()
        {
        }

        public MissionConstants(
            int[] mapSizeFrom,
            int[] lockedDoorFrom,
            RankTable exitZones,
            RankTable leaderChance,
            int rescueTargetsSmall,
            int rescueTargetsMedium,
            int rescueTargetsLarge,
            float rescueFailFraction,
            float rescueFailFractionPerRank,
            float countdownSeconds,
            float countdownReductionPerRank,
            float countdownMinSeconds,
            float countdownGuardFraction,
            float countdownTargetHp,
            float arsonFailFraction,
            float arsonFailReductionPerRank,
            float arsonFailMin,
            float igniteSeconds,
            float invasionLossSeconds,
            float invasionSpeedupPerEnemy,
            TierValue invasionRadius,
            float valuablesGoalFraction,
            float captureGoalFraction)
        {
            this.mapSizeFrom = mapSizeFrom;
            this.lockedDoorFrom = lockedDoorFrom;
            this.exitZones = exitZones;
            this.leaderChance = leaderChance;
            this.rescueTargetsSmall = rescueTargetsSmall;
            this.rescueTargetsMedium = rescueTargetsMedium;
            this.rescueTargetsLarge = rescueTargetsLarge;
            this.rescueFailFraction = rescueFailFraction;
            this.rescueFailFractionPerRank = rescueFailFractionPerRank;
            this.countdownSeconds = countdownSeconds;
            this.countdownReductionPerRank = countdownReductionPerRank;
            this.countdownMinSeconds = countdownMinSeconds;
            this.countdownGuardFraction = countdownGuardFraction;
            this.countdownTargetHp = countdownTargetHp;
            this.arsonFailFraction = arsonFailFraction;
            this.arsonFailReductionPerRank = arsonFailReductionPerRank;
            this.arsonFailMin = arsonFailMin;
            this.igniteSeconds = igniteSeconds;
            this.invasionLossSeconds = invasionLossSeconds;
            this.invasionSpeedupPerEnemy = invasionSpeedupPerEnemy;
            this.invasionRadius = invasionRadius;
            this.valuablesGoalFraction = valuablesGoalFraction;
            this.captureGoalFraction = captureGoalFraction;
        }

        /// <summary>Share of the threat budget that guards the Countdown target.</summary>
        public float CountdownGuardFraction => countdownGuardFraction;

        /// <summary>HP of the Countdown target structure.</summary>
        public float CountdownTargetHp => countdownTargetHp;

        /// <summary>Game seconds a goon takes at an arson socket to set a building on fire.</summary>
        public float IgniteSeconds => igniteSeconds;

        /// <summary>Invasion's loss timer, in game seconds at normal speed.</summary>
        public float InvasionLossSeconds => invasionLossSeconds;

        /// <summary>How much faster the loss timer runs per enemy inside the location's radius.</summary>
        public float InvasionSpeedupPerEnemy => invasionSpeedupPerEnemy;

        /// <summary>The radius tier around the Invasion location that counts as inside.</summary>
        public TierValue InvasionRadius => invasionRadius;

        /// <summary>The Valuables secondary goal fails once this fraction of the map's Valuables is lost.</summary>
        public float ValuablesGoalFraction => valuablesGoalFraction;

        /// <summary>Capture goals need this fraction of the mission's spawned enemies of that kind.</summary>
        public float CaptureGoalFraction => captureGoalFraction;

        /// <summary>The largest map size the rank has reached.</summary>
        public MapSize MapSizeFor(int difficultyLevel)
        {
            var size = MapSize.Small;
            for (int i = 0; i < mapSizeFrom.Length; i++)
            {
                if (difficultyLevel >= mapSizeFrom[i])
                {
                    size = (MapSize)i;
                }
            }

            return size;
        }

        /// <summary>The materials a locked door can be made of at this rank: each rank keeps the ones below it.</summary>
        public void LockedDoorMaterials(int difficultyLevel, List<StructureMaterial> results)
        {
            results.Clear();
            for (int i = 0; i < lockedDoorFrom.Length; i++)
            {
                if (difficultyLevel >= lockedDoorFrom[i])
                {
                    results.Add((StructureMaterial)i);
                }
            }
        }

        /// <summary>Exit zones on the map; always at least 1 (difficulty.md).</summary>
        public int ExitZones(int difficultyLevel)
        {
            return Mathf.Max(1, Mathf.RoundToInt(exitZones.For(difficultyLevel)));
        }

        /// <summary>Chance the mission is led by the faction's Leader.</summary>
        public float LeaderChance(int difficultyLevel)
        {
            return leaderChance.For(difficultyLevel);
        }

        /// <summary>Target citizens in the key building for a Rescue on a map of this size.</summary>
        public int RescueTargets(MapSize size)
        {
            switch (size)
            {
                case MapSize.Small:
                    return rescueTargetsSmall;
                case MapSize.Medium:
                    return rescueTargetsMedium;
                default:
                    return rescueTargetsLarge;
            }
        }

        /// <summary>Rescue fails with fewer than this fraction of its targets rescued; it rises with rank.</summary>
        public float RescueFailFraction(int difficultyLevel)
        {
            return Mathf.Min(1f, rescueFailFraction + rescueFailFractionPerRank * Mathf.Max(0, difficultyLevel));
        }

        /// <summary>The Countdown timer: shorter each rank, never under the minimum.</summary>
        public float CountdownSeconds(int difficultyLevel)
        {
            return Mathf.Max(countdownMinSeconds,
                countdownSeconds - countdownReductionPerRank * Mathf.Max(0, difficultyLevel));
        }

        /// <summary>Arson Riot fails at this fraction of buildings burning or burned out: lower each rank, to a floor.</summary>
        public float ArsonFailFraction(int difficultyLevel)
        {
            return Mathf.Max(arsonFailMin, arsonFailFraction - arsonFailReductionPerRank * Mathf.Max(0, difficultyLevel));
        }
    }
}
