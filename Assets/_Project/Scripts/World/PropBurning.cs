using System;
using System.Collections.Generic;
using UnityEngine;
using Zenthium.Combat;
using Zenthium.Data;
using Zenthium.Modules;
using Zenthium.Statuses;

namespace Zenthium.World
{
    /// <summary>
    /// Burn on a prop (world.md, Fire; statuses.md, Burn): props aren't entities, so their Burns run here. Each Burn
    /// runs on its own, lasts the prop multiplier times longer at the same damage per tick, and ignores Armor and
    /// Damage Reduction, so a burning vehicle reaches 0 HP fast and explodes. While dragged the prop takes nothing, but
    /// its Burns keep running. <c>Ignites</c> hits and burning buildings set it on fire; <c>Extinguish</c> hits put it
    /// out.
    /// </summary>
    [RequireComponent(typeof(Structure))]
    public sealed class PropBurning : MonoBehaviour
    {
        private readonly List<DamageOverTime> burns = new List<DamageOverTime>();
        private Structure structure;

        /// <summary>Raised when it catches fire or goes out.</summary>
        public event Action OnBurningChanged;

        public bool IsBurning => burns.Count > 0;

        /// <summary>Gives it a Burn, stretched by the prop multiplier.</summary>
        /// <param name="damage">The Burn's total damage on an entity.</param>
        /// <param name="seconds">The Burn's length on an entity.</param>
        public void Ignite(float damage, float seconds)
        {
            ModuleWorld world = structure.World;
            if (world == null || structure.IsBroken || !(seconds > 0f))
            {
                return;
            }

            float tickSeconds = world.Database.ModuleConstants.DamageOverTimeTickSeconds;
            float multiplier = world.Database.FireConstants.PropBurnDurationMultiplier;
            if (!(tickSeconds > 0f) || !(multiplier > 0f))
            {
                return;
            }

            bool wasBurning = IsBurning;
            burns.Add(new DamageOverTime(damage * multiplier, seconds * multiplier, tickSeconds));
            if (!wasBurning)
            {
                OnBurningChanged?.Invoke();
            }
        }

        public void Extinguish()
        {
            if (burns.Count == 0)
            {
                return;
            }

            burns.Clear();
            OnBurningChanged?.Invoke();
        }

        private void Awake()
        {
            structure = GetComponent<Structure>();
        }

        private void OnEnable()
        {
            structure.OnHit += ReactToHit;
            structure.OnBroken += Extinguish;
        }

        private void OnDisable()
        {
            structure.OnHit -= ReactToHit;
            structure.OnBroken -= Extinguish;
        }

        private void Update()
        {
            ModuleWorld world = structure.World;
            if (burns.Count == 0 || world == null)
            {
                return;
            }

            float realDelta = Mathf.Min(Time.unscaledDeltaTime, Time.maximumDeltaTime);
            float gameDelta = world.Clock.Clock.GameDelta(realDelta, false);
            float damage = 0f;
            for (int i = burns.Count - 1; i >= 0; i--)
            {
                damage += burns[i].DamagePerTick * burns[i].Advance(gameDelta);
                if (burns[i].IsFinished)
                {
                    burns.RemoveAt(i);
                }
            }

            if (burns.Count == 0)
            {
                OnBurningChanged?.Invoke();
            }

            // Dealt once, after the loop: the damage can break the prop, which puts it out.
            if (damage > 0f)
            {
                var tick = new Hit(null, damage, DamageType.Physical, isDamageOverTime: true);
                structure.TakeHit(new StructureHit(tick, StructureHitKind.Direct));
            }
        }

        private void ReactToHit(StructureHit hit)
        {
            if (hit.Extinguishes)
            {
                Extinguish();
            }
            else if (hit.Ignites && structure.World != null)
            {
                FireConstants fire = structure.World.Database.FireConstants;
                Ignite(fire.BurnDamage, fire.BurnSeconds);
            }
        }
    }
}
