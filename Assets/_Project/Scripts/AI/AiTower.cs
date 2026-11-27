using UnityEngine;
using Zenthium.Data;
using Zenthium.Modules;

namespace Zenthium.AI
{
    /// <summary>
    /// An AI entity's towing with a slotted <see cref="TowBeamModule"/>, such as a Tow Saucer's (modules.md):
    /// <see cref="AiBrain"/> decides when, as with looting, and takes what it tows to its loot destination. An entity
    /// without Tow Beam ignores it, so the component is harmless on any AI body.
    /// </summary>
    [RequireComponent(typeof(EntityModules))]
    public sealed class AiTower : MonoBehaviour
    {
        private static readonly SlotType[] ActivatedSlotTypes = { SlotType.Active, SlotType.Gear };

        private EntityModules modules;

        public bool IsTowing => TryGetTowBeam(out TowBeamModule tow, out _, out _) && tow.IsTowing;

        /// <summary>Where the towed thing is.</summary>
        internal Vector3 TowedPosition =>
            TryGetTowBeam(out TowBeamModule tow, out _, out _) ? tow.TowedPosition : transform.position;

        /// <summary>Fires Tow Beam at the nearest thing in range it could take.</summary>
        /// <returns>False if it has no Tow Beam ready, or nothing is in range.</returns>
        internal bool TryStartTow()
        {
            if (!TryGetTowBeam(out TowBeamModule tow, out SlotType type, out int index) || tow.IsTowing
                || !modules.IsSlotUsable(type, index) || !tow.TryFindTarget(out IDragTarget target))
            {
                return false;
            }

            Vector3 direction = target.Body.position - transform.position;
            direction.y = 0f;
            return modules.TryActivateSlot(type, index, new ModuleAim(direction));
        }

        /// <summary>At its loot destination: what it tows is taken for good.</summary>
        internal void StealTowed()
        {
            if (TryGetTowBeam(out TowBeamModule tow, out _, out _))
            {
                tow.StealTowed();
            }
        }

        private void Awake()
        {
            modules = GetComponent<EntityModules>();
        }

        private bool TryGetTowBeam(out TowBeamModule tow, out SlotType type, out int index)
        {
            tow = null;
            type = default;
            index = -1;
            if (modules.Entity == null)
            {
                return false;
            }

            int slots = modules.Entity.Slots.SlotsPerType;
            foreach (SlotType slotType in ActivatedSlotTypes)
            {
                for (int i = 0; i < slots; i++)
                {
                    if (modules.GetSlotted(slotType, i) is TowBeamModule found)
                    {
                        tow = found;
                        type = slotType;
                        index = i;
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
