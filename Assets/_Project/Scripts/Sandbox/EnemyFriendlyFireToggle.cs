using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenthium.Combat;

namespace Zenthium.Sandbox
{
    /// <summary>
    /// The dev toggle for enemy friendly fire (enemies.md, Other rules): a button that switches whether enemy attacks
    /// can hurt other enemies. Test tool until the dev options menu exists.
    /// </summary>
    public sealed class EnemyFriendlyFireToggle : MonoBehaviour
    {
        [SerializeField] private CombatRulesBehaviour combatRules;
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text label;

        private void OnEnable()
        {
            button.onClick.AddListener(Toggle);
        }

        private void OnDisable()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(Toggle);
            }
        }

        private void Start()
        {
            // The rules are created in the combat rules' Awake, so read them once everything is awake.
            ShowState();
        }

        private void Toggle()
        {
            FriendlyFireRules rules = combatRules.FriendlyFire;
            if (rules != null)
            {
                rules.IsEnemyFriendlyFireEnabled = !rules.IsEnemyFriendlyFireEnabled;
                ShowState();
            }
        }

        private void ShowState()
        {
            FriendlyFireRules rules = combatRules.FriendlyFire;
            if (rules != null)
            {
                label.SetText(rules.IsEnemyFriendlyFireEnabled ? "Enemy FF: on" : "Enemy FF: off");
            }
        }
    }
}
