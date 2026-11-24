using System;
using Zenthium.Data;

namespace Zenthium.Statuses
{
    /// <summary>The fixed rules of one status: its kind, stacking and what it stops.</summary>
    public readonly struct StatusInfo
    {
        public StatusInfo(StatusKind kind, StatusStacking stacking, bool isHostile, bool stopsMoving = false,
            bool stopsActing = false, SlotType? disablesSlot = null)
        {
            Kind = kind;
            Stacking = stacking;
            IsHostile = isHostile;
            StopsMoving = stopsMoving;
            StopsActing = stopsActing;
            DisablesSlot = disablesSlot;
        }

        public StatusKind Kind { get; }
        public StatusStacking Stacking { get; }

        /// <summary>
        /// A debuff from an opponent: Invulnerable blocks it, resists apply, and Defiance cleanses it. Carrying
        /// Treasure and hidden markers aren't (stats.md, Defiance).
        /// </summary>
        public bool IsHostile { get; }

        public bool StopsMoving { get; }

        /// <summary>No attacks or powers, Kick and Drag included.</summary>
        public bool StopsActing { get; }

        /// <summary>For disables that switch off one random slot per application.</summary>
        public SlotType? DisablesSlot { get; }

        public bool IsCrowdControl => Kind == StatusKind.CrowdControl;
    }

    /// <summary>
    /// The rules of every status in statuses.md's catalog. Amounts and durations come from the applying module.
    /// </summary>
    public static class StatusCatalog
    {
        public static StatusInfo Get(StatusType type)
        {
            switch (type)
            {
                case StatusType.Stun:
                    return new StatusInfo(StatusKind.CrowdControl, StatusStacking.Refresh, true, stopsMoving: true,
                        stopsActing: true);
                case StatusType.Root:
                    return new StatusInfo(StatusKind.CrowdControl, StatusStacking.Refresh, true, stopsMoving: true);
                case StatusType.Taunted:
                case StatusType.Hypnotized:
                    return new StatusInfo(StatusKind.CrowdControl, StatusStacking.Refresh, true);
                case StatusType.Engulfed:
                    return new StatusInfo(StatusKind.CrowdControl, StatusStacking.Refresh, true, stopsMoving: true,
                        stopsActing: true);
                case StatusType.Marked:
                    return new StatusInfo(StatusKind.Debuff, StatusStacking.Refresh, true);
                case StatusType.Burn:
                    return new StatusInfo(StatusKind.DamageOverTime, StatusStacking.Independent, true);
                case StatusType.Infected:
                    return new StatusInfo(StatusKind.DamageOverTime, StatusStacking.Refresh, true);
                case StatusType.BrokenLimb:
                case StatusType.Gooed:
                    return new StatusInfo(StatusKind.Slow, StatusStacking.Refresh, true);
                case StatusType.CarryingTreasure:
                    return new StatusInfo(StatusKind.Slow, StatusStacking.Intensity, false);
                case StatusType.SlotDisable:
                    return new StatusInfo(StatusKind.Disable, StatusStacking.Independent, true,
                        disablesSlot: SlotType.Gear);
                case StatusType.Hex:
                    return new StatusInfo(StatusKind.Disable, StatusStacking.Independent, true,
                        disablesSlot: SlotType.Active);
                case StatusType.Silenced:
                    return new StatusInfo(StatusKind.Disable, StatusStacking.Refresh, true);
                case StatusType.NumbingToxin:
                    return new StatusInfo(StatusKind.Disable, StatusStacking.Independent, true,
                        disablesSlot: SlotType.Passive);
                case StatusType.Pied:
                    return new StatusInfo(StatusKind.ScreenEffect, StatusStacking.Intensity, true);
                case StatusType.DeathlyRage:
                    return new StatusInfo(StatusKind.Buff, StatusStacking.NoStack, false);
                case StatusType.Spotlight:
                case StatusType.Notarized:
                case StatusType.GelCoated:
                case StatusType.Overdrive:
                case StatusType.Mutate:
                    return new StatusInfo(StatusKind.Buff, StatusStacking.Refresh, false);
                case StatusType.GoldPlatedChrome:
                case StatusType.ReactivePlating:
                case StatusType.SoulShard:
                    return new StatusInfo(StatusKind.Buff, StatusStacking.Intensity, false);
                case StatusType.Charmed:
                    return new StatusInfo(StatusKind.HiddenMarker, StatusStacking.NoStack, false);
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
    }
}
