using System.Collections.Generic;
using UnityEngine;
using Zenthium.Data;
using Zenthium.Entities;
using Zenthium.Movement;

namespace Zenthium.Modules
{
    /// <summary>
    /// A stand-in brain for summons until Milestone 6's AI replaces it: chase the nearest opponent in range and use
    /// the first active slot in reach; with none, follow the summoner. It walks straight, with no pathfinding.
    /// </summary>
    [RequireComponent(typeof(EntityModules), typeof(EntityMotor))]
    public sealed class PlaceholderSummonBrain : MonoBehaviour
    {
        [Tooltip("Meters within which it chases an opponent.")]
        [SerializeField, Min(0f)] private float chaseRange = 15f;

        [Tooltip("Meters it lets the summoner get away before following.")]
        [SerializeField, Min(0f)] private float followDistance = 10f;

        [Tooltip("Real seconds between target choices.")]
        [SerializeField, Min(0.02f)] private float thinkSeconds = 0.25f;

        private readonly List<EntityBehaviour> nearby = new List<EntityBehaviour>();
        private EntityModules modules;
        private EntityMotor motor;
        private EntityBehaviour target;
        private float untilThink;

        private void Awake()
        {
            modules = GetComponent<EntityModules>();
            motor = GetComponent<EntityMotor>();
        }

        private void OnDisable()
        {
            motor.SetMoveInput(Vector2.zero);
        }

        private void Update()
        {
            Entity self = modules.Entity;
            if (self == null || self.IsIncapacitated || modules.World == null)
            {
                motor.SetMoveInput(Vector2.zero);
                return;
            }

            untilThink -= Time.unscaledDeltaTime;
            if (untilThink <= 0f)
            {
                untilThink = thinkSeconds;
                target = FindTarget(self);
            }

            if (target != null && target.Entity != null && !target.Entity.IsIncapacitated)
            {
                Fight(target);
            }
            else
            {
                Follow(self);
            }
        }

        private void Fight(EntityBehaviour opponent)
        {
            Vector3 toTarget = opponent.transform.position - transform.position;
            toTarget.y = 0f;
            var aim = new ModuleAim(toTarget, toTarget.magnitude);
            modules.SetAim(aim);
            ModuleBehaviour attack = modules.GetSlotted(SlotType.Active, 0);
            float reach = attack != null && attack.Data.RangeTier.IsSet ? attack.Data.RangeTier.Value : 0f;
            if (toTarget.magnitude <= reach)
            {
                motor.SetMoveInput(Vector2.zero);
                modules.TryActivateSlot(SlotType.Active, 0, aim);
            }
            else
            {
                Walk(toTarget);
            }
        }

        private void Follow(Entity self)
        {
            if (!modules.World.TryGetBehaviour(self.Summoner, out EntityBehaviour summoner))
            {
                motor.SetMoveInput(Vector2.zero);
                return;
            }

            Vector3 toSummoner = summoner.transform.position - transform.position;
            toSummoner.y = 0f;
            if (toSummoner.magnitude > followDistance)
            {
                Walk(toSummoner);
            }
            else
            {
                motor.SetMoveInput(Vector2.zero);
            }
        }

        private void Walk(Vector3 direction)
        {
            direction.Normalize();
            motor.SetMoveInput(new Vector2(direction.x, direction.z));
        }

        private EntityBehaviour FindTarget(Entity self)
        {
            modules.World.CollectEntities(transform.position, chaseRange, nearby);
            EntityBehaviour nearest = null;
            float nearestDistance = float.PositiveInfinity;
            foreach (EntityBehaviour candidate in nearby)
            {
                if (!Opponents.Are(self, candidate.Entity) || candidate.Entity.IsIncapacitated)
                {
                    continue;
                }

                float distance = (candidate.transform.position - transform.position).sqrMagnitude;
                if (distance < nearestDistance)
                {
                    nearest = candidate;
                    nearestDistance = distance;
                }
            }

            return nearest;
        }
    }
}
