using UnityEngine;
using Zenthium.Core;
using Zenthium.Data;

namespace Zenthium.Combat
{
    /// <summary>
    /// Owns the scene's damage pipeline and friendly fire rules.
    /// </summary>
    public sealed class CombatRulesBehaviour : MonoBehaviour
    {
        [SerializeField] private GameDatabase database;

        [Tooltip("Dev option: enemy attacks can hurt other enemies.")]
        [SerializeField] private bool enemyFriendlyFire = true;

        /// <summary>Null until Awake.</summary>
        public DamagePipeline Pipeline { get; private set; }

        /// <summary>Null until Awake.</summary>
        public FriendlyFireRules FriendlyFire { get; private set; }

        private void Awake()
        {
            if (database == null)
            {
                Debug.LogError($"{name}: {nameof(CombatRulesBehaviour)} needs a Game Database.", this);
                enabled = false;
                return;
            }

            FriendlyFire = new FriendlyFireRules { IsEnemyFriendlyFireEnabled = enemyFriendlyFire };
            Pipeline = new DamagePipeline(database.FormulaConstants, FriendlyFire, new UnityRandomSource());
        }

        private void OnValidate()
        {
            // Lets the dev toggle be flipped in the Inspector during Play mode.
            if (FriendlyFire != null)
            {
                FriendlyFire.IsEnemyFriendlyFireEnabled = enemyFriendlyFire;
            }
        }
    }
}
