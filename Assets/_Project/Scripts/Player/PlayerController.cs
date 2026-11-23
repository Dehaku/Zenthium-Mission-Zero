using UnityEngine;
using Zenthium.Combat;
using Zenthium.Core.Controls;
using Zenthium.Data;
using Zenthium.Entities;
using Zenthium.Modules;
using Zenthium.Movement;

namespace Zenthium.Player
{
    /// <summary>
    /// Turns the player's input (controls.md) into the shared actions: move, aim, the Sprint toggle, Kick, Drag and
    /// the friendly fire hold. The actions themselves are the same ones any entity uses.
    /// </summary>
    [RequireComponent(typeof(EntityBehaviour), typeof(EntityMotor), typeof(PlayerAim))]
    [RequireComponent(typeof(KickAction), typeof(DragAction))]
    public sealed class PlayerController : MonoBehaviour
    {
        [SerializeField] private GameDatabase database;
        [SerializeField] private CombatRulesBehaviour combatRules;

        private InputSystem_Actions actions;
        private EntityBehaviour entityBehaviour;
        private EntityMotor motor;
        private PlayerAim aim;
        private KickAction kick;
        private DragAction drag;
        private SprintToggle sprint;
        private float sprintBonus;

        private void Awake()
        {
            entityBehaviour = GetComponent<EntityBehaviour>();
            motor = GetComponent<EntityMotor>();
            aim = GetComponent<PlayerAim>();
            kick = GetComponent<KickAction>();
            drag = GetComponent<DragAction>();
            actions = new InputSystem_Actions();

            if (database == null || combatRules == null
                || !database.Modules.TryGet(DataIds.Modules.Sprint, out ModuleData sprintData)
                || !SprintToggle.TryGetBonus(sprintData, out sprintBonus))
            {
                Debug.LogError($"{name}: {nameof(PlayerController)} needs Combat Rules and a Game Database with "
                    + "Sprint's speed bonus.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            actions.Player.Enable();

            // The entity may run Awake before or after this, so bind now and on every (re)creation.
            entityBehaviour.OnEntityCreated += Bind;
            if (entityBehaviour.Entity != null)
            {
                Bind(entityBehaviour.Entity);
            }
        }

        private void OnDisable()
        {
            actions.Player.Disable();
            entityBehaviour.OnEntityCreated -= Bind;
            sprint = null;
            motor.SetMoveInput(Vector2.zero);
            SetFriendlyFireHeld(false);
        }

        private void OnDestroy()
        {
            actions.Dispose();
        }

        private void Update()
        {
            if (entityBehaviour.ClockDriver == null || entityBehaviour.ClockDriver.Clock.IsPaused)
            {
                motor.SetMoveInput(Vector2.zero);
                return;
            }

            InputSystem_Actions.PlayerActions player = actions.Player;
            motor.SetMoveInput(player.Move.ReadValue<Vector2>());
            aim.UpdateAim(player.AimPointer.ReadValue<Vector2>(), player.AimStick.ReadValue<Vector2>());
            SetFriendlyFireHeld(player.FriendlyFire.IsPressed());

            if (player.Sprint.WasPressedThisFrame() && sprint != null)
            {
                sprint.Toggle();
            }

            if (player.Kick.WasPressedThisFrame())
            {
                kick.TryKick(aim.Direction);
            }

            if (player.Drag.WasPressedThisFrame())
            {
                drag.Use();
            }
        }

        private void Bind(Entity entity)
        {
            // A reset entity starts with Sprint off.
            sprint = new SprintToggle(entity.MoveSpeed, sprintBonus);
        }

        private void SetFriendlyFireHeld(bool held)
        {
            if (combatRules != null && combatRules.FriendlyFire != null)
            {
                combatRules.FriendlyFire.IsPlayerFriendlyFireHeld = held;
            }
        }
    }
}
