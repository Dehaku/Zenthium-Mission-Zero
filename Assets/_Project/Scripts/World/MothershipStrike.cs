using System.Collections.Generic;
using UnityEngine;
using Zenthium.Combat;
using Zenthium.Data;
using Zenthium.Entities;
using Zenthium.Modules;

namespace Zenthium.World
{
    /// <summary>
    /// The Repo Agency's Mothership Strike (Factions/GalacticRepoAgency.md): while on, every interval of game time an
    /// orbital beam targets the player's position. A red ring counts down where it will land, like Breaching Charge
    /// (its placed charge), then it deals its damage in its radius to everything there, as an environmental blast.
    /// Energy damage (the user's decision, Milestone 7b). Off unless switched on: Milestone 11 switches it on from
    /// rank S.
    /// </summary>
    public sealed class MothershipStrike : MonoBehaviour
    {
        [SerializeField] private ModuleWorld world;

        [Tooltip("The faction whose Mothership this is, for its values in factions.csv.")]
        [SerializeField] private FactionData faction;

        [Tooltip("Off by default: missions switch it on from rank S (Milestone 11).")]
        [SerializeField] private bool isOn;

        private readonly List<EntityBehaviour> everyone = new List<EntityBehaviour>();
        private MothershipRules rules;
        private MothershipTimer timer;

        public bool IsOn => isOn;

        /// <summary>Game seconds until the next strike; 0 while off.</summary>
        public float SecondsToNextStrike => isOn && timer != null ? timer.SecondsLeft : 0f;

        /// <summary>Switching it on starts a full interval before the first strike.</summary>
        public void SetOn(bool on)
        {
            if (on && !isOn && timer != null)
            {
                timer.Restart();
            }

            isOn = on;
        }

        private void Awake()
        {
            if (world == null || !MothershipRules.TryCreate(faction, out rules, out _))
            {
                Debug.LogError($"{name}: {nameof(MothershipStrike)} needs a Module World and a faction with Mothership "
                    + "values in factions.csv.", this);
                enabled = false;
                return;
            }

            timer = new MothershipTimer(rules.IntervalSeconds);
        }

        private void Update()
        {
            if (!isOn)
            {
                return;
            }

            float realDelta = Mathf.Min(Time.unscaledDeltaTime, Time.maximumDeltaTime);
            if (timer.Tick(world.Clock.Clock.GameDelta(realDelta, false)) && TryFindPlayer(out Vector3 target))
            {
                TimedCharge marker = world.SpawnCharge(target);
                if (marker != null)
                {
                    marker.Arm(world, new Hit(null, rules.Damage, DamageType.Energy), rules.Radius,
                        rules.CountdownSeconds);
                }
            }
        }

        private bool TryFindPlayer(out Vector3 position)
        {
            position = default;
            world.CollectAll(everyone);
            foreach (EntityBehaviour body in everyone)
            {
                Entity entity = body.Entity;
                if (entity != null && entity.Team == Team.Player && !entity.IsIncapacitated)
                {
                    position = body.transform.position;
                    return true;
                }
            }

            return false;
        }
    }
}
