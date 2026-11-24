using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenthium.Data;
using Zenthium.Entities;
using Zenthium.Modules;

namespace Zenthium.Sandbox
{
    /// <summary>
    /// Sandbox hero select: picks the player's hero (starting heroes first), how many slots of each type are unlocked
    /// (counted from the first; it starts at all), and a test module put into the last slot of its type (to try
    /// modules outside the kit, such as Time Stop). Apply rebuilds the player. Test tool only.
    /// </summary>
    public sealed class HeroSelectPanel : MonoBehaviour
    {
        private const string NoTestModule = "(no test module)";

        [SerializeField] private GameDatabase database;
        [SerializeField] private EntityBehaviour player;
        [SerializeField] private TMP_Dropdown heroDropdown;
        [SerializeField] private TMP_Dropdown unlockedSlotsDropdown;
        [SerializeField] private TMP_Dropdown testModuleDropdown;
        [SerializeField] private Button applyButton;

        private readonly List<HeroData> heroes = new List<HeroData>();
        private readonly List<ModuleData> testModules = new List<ModuleData>();

        private void Awake()
        {
            if (database == null || player == null || heroDropdown == null || unlockedSlotsDropdown == null
                || testModuleDropdown == null || applyButton == null)
            {
                Debug.LogError($"{name}: {nameof(HeroSelectPanel)} is missing references.", this);
                enabled = false;
                return;
            }

            FillHeroes();
            FillUnlockedSlots();
            FillTestModules();
        }

        private void OnEnable()
        {
            applyButton.onClick.AddListener(Apply);
        }

        private void OnDisable()
        {
            applyButton.onClick.RemoveListener(Apply);
        }

        private void Apply()
        {
            if (heroes.Count == 0)
            {
                return;
            }

            int test = testModuleDropdown.value;
            // Option i unlocks i + 1 slots of each type.
            int unlocked = unlockedSlotsDropdown.value + 1;
            player.SetHero(heroes[heroDropdown.value], unlocked, test > 0 ? testModules[test - 1] : null);
        }

        private void FillHeroes()
        {
            foreach (HeroData hero in database.Heroes.Entries)
            {
                if (hero.IsStarter)
                {
                    heroes.Add(hero);
                }
            }

            foreach (HeroData hero in database.Heroes.Entries)
            {
                if (!hero.IsStarter)
                {
                    heroes.Add(hero);
                }
            }

            var options = new List<string>(heroes.Count);
            foreach (HeroData hero in heroes)
            {
                options.Add(hero.IsStarter ? hero.Id : hero.Id + " (most modules not built yet)");
            }

            heroDropdown.ClearOptions();
            heroDropdown.AddOptions(options);
        }

        private void FillUnlockedSlots()
        {
            int slotsPerType = database.ModuleConstants.SlotsPerType;
            var options = new List<string>(slotsPerType);
            for (int count = 1; count <= slotsPerType; count++)
            {
                string option = count.ToString(CultureInfo.InvariantCulture);
                options.Add(count == database.ModuleConstants.PresetSlotsUnlocked ? option + " (preset start)" : option);
            }

            unlockedSlotsDropdown.ClearOptions();
            unlockedSlotsDropdown.AddOptions(options);
            unlockedSlotsDropdown.SetValueWithoutNotify(slotsPerType - 1);
        }

        /// <summary>Slottable modules that have a behavior, so the test slot always does something.</summary>
        private void FillTestModules()
        {
            var options = new List<string> { NoTestModule };
            foreach (ModuleData module in database.Modules.Entries)
            {
                if (module.TryGetSlotType(out _) && ModuleCatalog.TryCreate(module, database, out _, out _))
                {
                    testModules.Add(module);
                    options.Add(module.Id);
                }
            }

            testModuleDropdown.ClearOptions();
            testModuleDropdown.AddOptions(options);
        }
    }
}
