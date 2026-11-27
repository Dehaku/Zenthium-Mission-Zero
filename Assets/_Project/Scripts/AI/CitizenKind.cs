namespace Zenthium.AI
{
    /// <summary>The citizen types of missions.md, Citizens.</summary>
    public enum CitizenKind
    {
        /// <summary>Flees from enemies it can see within its fear range.</summary>
        Scared,

        /// <summary>Starts the mission down and can't move on its own; revived, it gets up Scared.</summary>
        Incapacitated,

        /// <summary>Won't move, and slows whoever drags it.</summary>
        Stubborn,
    }
}
