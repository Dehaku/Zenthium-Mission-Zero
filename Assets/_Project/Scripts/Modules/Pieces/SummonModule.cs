using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using Zenthium.Data;
using Zenthium.Entities;

namespace Zenthium.Modules
{
    /// <summary>
    /// Summons entities that fight on the user's side for <c>summon_lifetime_s</c> (Ink Beast, modules.md), using
    /// <c>summon_attack</c>. Equipped modules' <c>extra_beasts</c> (Living Ink) add more. A new cast replaces the
    /// summons still out.
    /// </summary>
    public sealed class SummonModule : ModuleBehaviour
    {
        public const string LifetimeKey = "summon_lifetime_s";
        public const string AttackKey = "summon_attack";
        public const string ExtraSummonsKey = "extra_beasts";

        // Summons appear around the user, this far from its center.
        private const float SpawnRingRadius = 1.5f;

        private readonly List<EntityBehaviour> summons = new List<EntityBehaviour>();
        private readonly ModuleData[] attack;
        private readonly EntityData summoned;
        private readonly float lifetime;
        private float remaining;

        public SummonModule(ModuleData data, EntityData summoned, ModuleData attack, float lifetime) : base(data)
        {
            this.summoned = summoned;
            this.attack = new[] { attack };
            this.lifetime = lifetime;
        }

        /// <summary>The base count plus every equipped module's <c>extra_beasts</c>.</summary>
        public static int Count(int baseCount, ModuleSlots slots)
        {
            float extra = 0f;
            foreach (SlotType type in (SlotType[])Enum.GetValues(typeof(SlotType)))
            {
                for (int index = 0; index < slots.SlotsPerType; index++)
                {
                    if (slots.IsEquipped(type, index)
                        && slots.Get(type, index).Values.TryGetNumber(ExtraSummonsKey, out float more))
                    {
                        extra += more;
                    }
                }
            }

            return baseCount + Mathf.Max(0, Mathf.RoundToInt(extra));
        }

        public override void DescribeEffects(StringBuilder text)
        {
            text.Append("Summons ").Append(Count(1, Owner.Slots)).Append(' ').Append(summoned.Id).Append(" for ")
                .Append(lifetime.ToString("0.#", CultureInfo.InvariantCulture)).Append(" s, attacking with ")
                .Append(attack[0].Id).Append('\n');
        }

        protected override void Activate(ModuleAim aim)
        {
            Despawn();
            int count = Count(1, Owner.Slots);
            for (int i = 0; i < count; i++)
            {
                float angle = (360f / count) * i;
                Vector3 offset = Quaternion.Euler(0f, angle, 0f) * (aim.Direction.normalized * SpawnRingRadius);
                EntityBehaviour summon = Context.World.SpawnSummon(summoned, Owner, attack, Context.Position + offset);
                if (summon != null)
                {
                    summons.Add(summon);
                }
            }

            remaining = lifetime;
        }

        protected internal override void Tick(float gameDelta)
        {
            if (summons.Count == 0)
            {
                return;
            }

            remaining -= gameDelta;
            if (remaining <= 0f)
            {
                Despawn();
            }
        }

        protected override void OnDisabled()
        {
            Despawn();
        }

        private void Despawn()
        {
            foreach (EntityBehaviour summon in summons)
            {
                if (summon != null)
                {
                    UnityEngine.Object.Destroy(summon.gameObject);
                }
            }

            summons.Clear();
        }
    }
}
