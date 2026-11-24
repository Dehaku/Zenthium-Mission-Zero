using System;
using System.Collections.Generic;
using UnityEngine;

namespace Zenthium.World
{
    /// <summary>What a socket can spawn (maps.md, Sockets). Unity serializes enums as numbers: add new values at the end.</summary>
    public enum SocketContent
    {
        /// <summary>Just outside a door or window: goons stand here to set the building on fire.</summary>
        Arson,
        Valuables,
        Citizen,
        Goon,
        HeavyTurret,
        CountdownTarget,
        ExitZone,
        CropCircle,
    }

    /// <summary>One thing a socket can spawn, with its weight against the socket's other options.</summary>
    [Serializable]
    public struct SocketOption
    {
        [SerializeField] private SocketContent content;
        [SerializeField, Min(0f)] private float weight;

        public SocketOption(SocketContent content, float weight)
        {
            this.content = content;
            this.weight = weight;
        }

        public SocketContent Content => content;
        public float Weight => weight;
    }

    /// <summary>
    /// A designer-placed spawn point on a block or building (maps.md, Sockets), with weighted options for what can
    /// spawn in it. Each mission fills sockets by its own rules (Milestone 9).
    /// </summary>
    public sealed class MapSocket : MonoBehaviour
    {
        private const float GizmoRadius = 0.4f;

        [SerializeField] private List<SocketOption> options = new List<SocketOption>();

        public IReadOnlyList<SocketOption> Options => options;

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, GizmoRadius);
        }
    }
}
