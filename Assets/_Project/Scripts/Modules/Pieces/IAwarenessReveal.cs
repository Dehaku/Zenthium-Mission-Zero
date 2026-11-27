using UnityEngine;

namespace Zenthium.Modules
{
    /// <summary>
    /// An entity a module can make aware of someone at once, such as Audit's exposing the player (modules.md). The AI
    /// implements it, so Modules never references the AI.
    /// </summary>
    public interface IAwarenessReveal
    {
        /// <summary>It notices at once, as from an alert, and learns where the revealed one is.</summary>
        void Reveal(Vector3 position);
    }
}
