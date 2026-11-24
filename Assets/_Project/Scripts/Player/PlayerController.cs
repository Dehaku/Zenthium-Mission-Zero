using UnityEngine;
using UnityEngine.InputSystem;
using Zenthium.Combat;
using Zenthium.Core.Controls;
using Zenthium.Data;
using Zenthium.Entities;
using Zenthium.Modules;
using Zenthium.Movement;

namespace Zenthium.Player
{
    /// <summary>
    /// Turns the player's input (controls.md) into the shared actions: move, aim, the Sprint toggle, Kick, Drag,
    /// Power 1-4 (active slots), Gear 1-4 (gear actives), the friendly fire hold and the MoreInfo hold. The actions
    /// themselves are the same ones any entity uses.
    /// </summary>
    [RequireComponent(typeof(EntityBehaviour), typeof(EntityMotor), typeof(PlayerAim))]
    [RequireComponent(typeof(EntityModules), typeof(DragAction))]
    public sealed class PlayerController : MonoBehaviour
    {
        // controls.md: four Power buttons and four Gear buttons, one per slot.
        private const int SlotButtonCount = 4;

        [SerializeField] private GameDatabase database;
        [SerializeField] private CombatRulesBehaviour combatRules;

        private readonly InputAction[] powerButtons = new InputAction[SlotButtonCount];
        private readonly InputAction[] gearButtons = new InputAction[SlotButtonCount];

        private InputSystem_Actions actions;
        private EntityBehaviour entityBehaviour;
        private EntityMotor motor;
        private PlayerAim aim;
        private EntityModules modules;
        private DragAction drag;
        private SprintToggle sprint;
        private float sprintBonus;

        /// <summary>True while the MoreInfo button is held, so the module info shows.</summary>
        public bool IsMoreInfoHeld { get; private set; }

        public bool IsSprintOn => sprint != null && sprint.IsOn;

        private void Awake()
        {
            entityBehaviour = GetComponent<EntityBehaviour>();
            motor = GetComponent<EntityMotor>();
            aim = GetComponent<PlayerAim>();
            modules = GetComponent<EntityModules>();
            drag = GetComponent<DragAction>();
            actions = new InputSystem_Actions();

            InputSystem_Actions.PlayerActions player = actions.Player;
            powerButtons[0] = player.Power1;
            powerButtons[1] = player.Power2;
            powerButtons[2] = player.Power3;
            powerButtons[3] = player.Power4;
            gearButtons[0] = player.Gear1;
            gearButtons[1] = player.Gear2;
            gearButtons[2] = player.Gear3;
            gearButtons[3] = player.Gear4;

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
            IsMoreInfoHeld = false;
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
            modules.SetAim(CurrentAim());
            SetFriendlyFireHeld(player.FriendlyFire.IsPressed());
            IsMoreInfoHeld = player.MoreInfo.IsPressed();

            if (player.Sprint.WasPressedThisFrame() && sprint != null)
            {
                sprint.Toggle();
            }

            if (player.Kick.WasPressedThisFrame())
            {
                modules.TryKick(CurrentAim());
            }

            if (player.Drag.WasPressedThisFrame())
            {
                drag.Use();
            }

            for (int i = 0; i < SlotButtonCount; i++)
            {
                if (powerButtons[i].WasPressedThisFrame())
                {
                    modules.TryActivateSlot(SlotType.Active, i, CurrentAim());
                }

                if (gearButtons[i].WasPressedThisFrame())
                {
                    modules.TryActivateSlot(SlotType.Gear, i, CurrentAim());
                }
            }
        }

        private ModuleAim CurrentAim()
        {
            return new ModuleAim(aim.Direction, aim.PointDistance);
        }

        private void Bind(Entity entity)
        {
            // A reset entity starts with Sprint off. Sprinter's bigger bonus comes from the new entity's slots.
            sprint = new SprintToggle(entity.MoveSpeed, SprintToggle.BonusFor(sprintBonus, entity.Slots));
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
