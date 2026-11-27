using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenthium.Core;
using Zenthium.Data;
using Zenthium.Missions;
using Zenthium.World;

namespace Zenthium.Sandbox
{
    /// <summary>
    /// The debug mission launcher (Milestone 9), until HQ starts missions (Milestone 11). Picks the type, faction,
    /// location, rank, map size, Leader and seed, each "Random" rolling it the way a real mission does
    /// (<see cref="MissionRoller"/>), and the hero with how many slots are unlocked. Launch reloads the scene with the
    /// mission pending. While a mission runs it shows the mission and an Abandon button, which goes back to the
    /// launcher. It remembers its choices between missions. Test tool only.
    /// </summary>
    public sealed class MissionLauncherPanel : MonoBehaviour
    {
        private const string RandomOption = "Random";
        private const string ByRankOption = "By rank";
        private const int MaxSeed = 100000;

        private static readonly string[] LeaderOptions = { "Leader: roll", "Leader: yes", "Leader: no" };
        private static int[] rememberedChoices;
        private static string rememberedSeed;

        [SerializeField] private GameDatabase database;
        [SerializeField] private MissionDirector director;
        [SerializeField] private MapBlockLibrary library;

        [Tooltip("The factions missions can be run by. Only factions with built entities belong here.")]
        [SerializeField] private List<FactionData> factions = new List<FactionData>();

        [Tooltip("Shown before a mission starts.")]
        [SerializeField] private GameObject launcherRoot;

        [Tooltip("Shown while a mission runs.")]
        [SerializeField] private GameObject runningRoot;

        [SerializeField] private TMP_Dropdown typeDropdown;
        [SerializeField] private TMP_Dropdown factionDropdown;
        [SerializeField] private TMP_Dropdown locationDropdown;
        [SerializeField] private TMP_Dropdown rankDropdown;
        [SerializeField] private TMP_Dropdown sizeDropdown;
        [SerializeField] private TMP_Dropdown leaderDropdown;
        [SerializeField] private TMP_Dropdown heroDropdown;
        [SerializeField] private TMP_Dropdown slotsDropdown;
        [SerializeField] private TMP_InputField seedInput;
        [SerializeField] private Button launchButton;
        [SerializeField] private Button newSeedButton;
        [SerializeField] private Button abandonButton;
        [SerializeField] private TMP_Text status;
        [SerializeField] private TMP_Text missionText;

        private readonly List<LocationType> locations = new List<LocationType>();
        private readonly List<HeroData> heroes = new List<HeroData>();

        private enum LeaderChoice
        {
            Roll,
            Yes,
            No,
        }

        private TMP_Dropdown[] Dropdowns => new[]
        {
            typeDropdown, factionDropdown, locationDropdown, rankDropdown, sizeDropdown, leaderDropdown,
            heroDropdown, slotsDropdown,
        };

        private void Awake()
        {
            if (database == null || director == null || library == null || launcherRoot == null
                || runningRoot == null || Array.Exists(Dropdowns, dropdown => dropdown == null) || seedInput == null
                || launchButton == null || newSeedButton == null || abandonButton == null || status == null
                || missionText == null)
            {
                Debug.LogError($"{name}: {nameof(MissionLauncherPanel)} is missing references.", this);
                enabled = false;
                return;
            }

            FillOptions();
            RestoreChoices();
        }

        private void OnEnable()
        {
            launchButton.onClick.AddListener(Launch);
            newSeedButton.onClick.AddListener(NewSeed);
            abandonButton.onClick.AddListener(Abandon);
            director.OnMissionStarted += ShowRunning;
        }

        private void OnDisable()
        {
            launchButton.onClick.RemoveListener(Launch);
            newSeedButton.onClick.RemoveListener(NewSeed);
            abandonButton.onClick.RemoveListener(Abandon);
            if (director != null)
            {
                director.OnMissionStarted -= ShowRunning;
            }
        }

        private void Start()
        {
            if (director.IsRunning)
            {
                ShowRunning();
            }
            else
            {
                launcherRoot.SetActive(true);
                runningRoot.SetActive(false);
            }
        }

        private void ShowRunning()
        {
            launcherRoot.SetActive(false);
            runningRoot.SetActive(true);
            missionText.text = director.Setup.ToString();
        }

        private void FillOptions()
        {
            Fill(typeDropdown, WithRandom(Enum.GetNames(typeof(MissionType))));

            var factionNames = new List<string>();
            foreach (FactionData faction in factions)
            {
                factionNames.Add(faction != null ? faction.Id : "(missing)");
            }

            Fill(factionDropdown, WithRandom(factionNames));

            var held = new List<LocationType>();
            foreach (MapBlock block in library.Blocks)
            {
                if (block == null)
                {
                    continue;
                }

                block.CollectLocations(held);
                foreach (LocationType location in held)
                {
                    if (!locations.Contains(location))
                    {
                        locations.Add(location);
                    }
                }
            }

            locations.Sort();
            var locationNames = new List<string>();
            foreach (LocationType location in locations)
            {
                locationNames.Add(location.ToString());
            }

            Fill(locationDropdown, WithRandom(locationNames));
            Fill(rankDropdown, Enum.GetNames(typeof(Rank)));
            var sizes = new List<string> { ByRankOption };
            sizes.AddRange(Enum.GetNames(typeof(MapSize)));
            Fill(sizeDropdown, sizes);
            Fill(leaderDropdown, LeaderOptions);
            FillHeroes();

            int slotsPerType = database.ModuleConstants.SlotsPerType;
            var slots = new List<string>(slotsPerType);
            for (int count = 1; count <= slotsPerType; count++)
            {
                slots.Add($"{count.ToString(CultureInfo.InvariantCulture)} slot(s)");
            }

            Fill(slotsDropdown, slots);
            slotsDropdown.SetValueWithoutNotify(slotsPerType - 1);
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

            var names = new List<string>(heroes.Count);
            foreach (HeroData hero in heroes)
            {
                names.Add(hero.IsStarter ? hero.Id : hero.Id + " (most modules not built yet)");
            }

            Fill(heroDropdown, names);
        }

        private void RestoreChoices()
        {
            TMP_Dropdown[] dropdowns = Dropdowns;
            if (rememberedChoices != null && rememberedChoices.Length == dropdowns.Length)
            {
                for (int i = 0; i < dropdowns.Length; i++)
                {
                    dropdowns[i].SetValueWithoutNotify(
                        Mathf.Clamp(rememberedChoices[i], 0, dropdowns[i].options.Count - 1));
                }
            }

            seedInput.text = rememberedSeed ?? UnityEngine.Random.Range(0, MaxSeed).ToString();
        }

        private void RememberChoices()
        {
            TMP_Dropdown[] dropdowns = Dropdowns;
            rememberedChoices = new int[dropdowns.Length];
            for (int i = 0; i < dropdowns.Length; i++)
            {
                rememberedChoices[i] = dropdowns[i].value;
            }

            rememberedSeed = seedInput.text;
        }

        private void NewSeed()
        {
            seedInput.text = UnityEngine.Random.Range(0, MaxSeed).ToString();
        }

        /// <summary>Rolls what's left on Random, in the real roll's order, then starts the mission.</summary>
        private void Launch()
        {
            if (!int.TryParse(seedInput.text, out int seed))
            {
                seed = UnityEngine.Random.Range(0, MaxSeed);
                seedInput.text = seed.ToString();
            }

            if (heroes.Count == 0 || factions.Count == 0 || locations.Count == 0)
            {
                status.text = "Needs a hero, a faction and a block library with locations.";
                return;
            }

            var random = new SeededRandomSource(seed);
            int level = rankDropdown.value;
            MissionType type = typeDropdown.value > 0 ? (MissionType)(typeDropdown.value - 1)
                : MissionRoller.RollType(random);
            FactionData faction = factionDropdown.value > 0 ? factions[factionDropdown.value - 1]
                : MissionRoller.PickFaction(random, factions, type);
            if (faction == null)
            {
                status.text = "No faction to pick.";
                return;
            }

            LocationType location;
            if (locationDropdown.value > 0)
            {
                location = locations[locationDropdown.value - 1];
            }
            else if (!MissionRoller.TryPickLocation(random, faction, locations, out location))
            {
                status.text = "No location to pick.";
                return;
            }

            MissionConstants constants = database.MissionConstants;
            bool hasLeader;
            switch ((LeaderChoice)leaderDropdown.value)
            {
                case LeaderChoice.Yes:
                    hasLeader = new FactionRoster(faction, database.Entities.Entries, level).Leader != null;
                    break;
                case LeaderChoice.No:
                    hasLeader = false;
                    break;
                default:
                    hasLeader = MissionRoller.RollsLeader(random, faction, database.Entities.Entries, level, constants);
                    break;
            }

            MapSize size = sizeDropdown.value > 0 ? (MapSize)(sizeDropdown.value - 1) : constants.MapSizeFor(level);
            var setup = new MissionSetup(type, faction, location, level, size, hasLeader, seed);

            // Option i unlocks i + 1 slots of each type.
            PendingMission.Set(new MissionLaunch(setup, heroes[heroDropdown.value], slotsDropdown.value + 1));
            RememberChoices();
            MissionSceneLoader.ReloadActive();
        }

        private void Abandon()
        {
            MissionSceneLoader.ReloadActive();
        }

        private static List<string> WithRandom(IEnumerable<string> names)
        {
            var options = new List<string> { RandomOption };
            options.AddRange(names);
            return options;
        }

        private static void Fill(TMP_Dropdown dropdown, IEnumerable<string> names)
        {
            dropdown.ClearOptions();
            dropdown.AddOptions(new List<string>(names));
        }
    }
}
