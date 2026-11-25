using UnityEngine;
using Zenthium.Data;
using Zenthium.Entities;
using Zenthium.Modules;

namespace Zenthium.Sandbox
{
    /// <summary>
    /// Makes a dummy attack a target every few seconds: Kick when the target is within Kick's range, otherwise its
    /// first active slot. So projectiles, knockback, Time Stop and statuses can be tested on a fixed target, without
    /// the AI choosing its own. Its timer runs on the dummy's game time, so
    /// Time Slow and Time Stop affect it. Test tool only.
    /// </summary>
    [RequireComponent(typeof(EntityModules))]
    public sealed class SandboxAutoCaster : MonoBehaviour
    {
        [SerializeField] private EntityBehaviour target;

        [Tooltip("Game seconds between attempts. The module's own cooldown still applies.")]
        [SerializeField, Min(0.1f)] private float intervalSeconds = 2f;

        private EntityModules modules;
        private EntityBehaviour self;
        private float untilNext;

        private void Awake()
        {
            modules = GetComponent<EntityModules>();
            self = GetComponent<EntityBehaviour>();
            if (target == null)
            {
                Debug.LogError($"{name}: {nameof(SandboxAutoCaster)} needs a target.", this);
                enabled = false;
            }
        }

        private void Update()
        {
            Entity entity = modules.Entity;
            if (entity == null || self.ClockDriver == null || !target.isActiveAndEnabled)
            {
                return;
            }

            float realDelta = Mathf.Min(Time.unscaledDeltaTime, Time.maximumDeltaTime);
            untilNext -= self.ClockDriver.Clock.GameDelta(realDelta, entity.IsExemptFromTimePowers);
            if (untilNext > 0f)
            {
                return;
            }

            untilNext = intervalSeconds;
            Vector3 aim = target.transform.position - transform.position;
            aim.y = 0f;
            ModuleBehaviour kick = modules.Kick;
            if (kick != null && aim.magnitude <= kick.Data.RangeTier.Value)
            {
                modules.TryKick(new ModuleAim(aim, aim.magnitude));
            }
            else
            {
                modules.TryActivateSlot(SlotType.Active, 0, new ModuleAim(aim, aim.magnitude));
            }
        }
    }
}
