using Zenthium.Data;

namespace Zenthium.Missions
{
    /// <summary>A mission to play and the hero who plays it.</summary>
    public sealed class MissionLaunch
    {
        /// <param name="unlockedSlots">Slots of each type unlocked; 0 uses the preset's starting slots.</param>
        public MissionLaunch(MissionSetup setup, HeroData hero, int unlockedSlots)
        {
            Setup = setup;
            Hero = hero;
            UnlockedSlots = unlockedSlots;
        }

        public MissionSetup Setup { get; }
        public HeroData Hero { get; }
        public int UnlockedSlots { get; }
    }

    /// <summary>
    /// Carries a mission from the scene that starts it (HQ, or the debug launcher) into the Mission scene, which
    /// takes it when it loads (CLAUDE.md, Scenes and flow).
    /// </summary>
    public static class PendingMission
    {
        private static MissionLaunch pending;

        public static void Set(MissionLaunch launch)
        {
            pending = launch;
        }

        /// <summary>Takes the pending mission, leaving none.</summary>
        public static bool TryTake(out MissionLaunch launch)
        {
            launch = pending;
            pending = null;
            return launch != null;
        }
    }
}
