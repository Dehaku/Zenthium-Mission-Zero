using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Zenthium.Data;
using Zenthium.Entities;
using Zenthium.Missions;
using Zenthium.Modules;
using Zenthium.Sandbox;
using Zenthium.World;
using static Zenthium.Editor.SerializedWiring;

namespace Zenthium.Editor
{
    /// <summary>
    /// Builds the Mission scene (Milestone 9): the map sandbox's services and player, plus the mission director, the
    /// enemy and citizen spawners (saving the citizen body prefab), and the debug mission launcher with its hero
    /// picker. Until HQ exists (Milestone 11), the scene keeps the sandboxes' debug parts: the checklist, the HUD and
    /// the launcher.
    /// </summary>
    internal static class MissionSceneCreator
    {
        private const string MenuPath = "Zenthium/Setup/Create Mission Scene";
        private const string ScenePath = "Assets/_Project/Scenes/Mission.unity";
        private const string CitizenPrefabPath = PlayerSandboxSceneCreator.PrefabFolder + "/Citizen.prefab";

        private static class Fields
        {
            public const string World = "world";
            public const string Valuables = "valuables";
            public const string Prefab = "prefab";
            public const string CitizenEntity = "citizenEntity";
            public const string Spawner = "spawner";
            public const string Assembler = "assembler";
            public const string Registry = "registry";
            public const string Player = "player";
            public const string Enemies = "enemies";
            public const string Citizens = "citizens";
        }

        private static class LauncherFields
        {
            public const string Database = "database";
            public const string Director = "director";
            public const string Library = "library";
            public const string Factions = "factions";
            public const string LauncherRoot = "launcherRoot";
            public const string RunningRoot = "runningRoot";
            public const string TypeDropdown = "typeDropdown";
            public const string FactionDropdown = "factionDropdown";
            public const string LocationDropdown = "locationDropdown";
            public const string RankDropdown = "rankDropdown";
            public const string SizeDropdown = "sizeDropdown";
            public const string LeaderDropdown = "leaderDropdown";
            public const string HeroDropdown = "heroDropdown";
            public const string SlotsDropdown = "slotsDropdown";
            public const string SeedInput = "seedInput";
            public const string LaunchButton = "launchButton";
            public const string NewSeedButton = "newSeedButton";
            public const string AbandonButton = "abandonButton";
            public const string Status = "status";
            public const string MissionText = "missionText";
        }

        [MenuItem(MenuPath)]
        private static void CreateMissionScene()
        {
            if (!PlayerSandboxSceneCreator.TryBuildRoom(ScenePath, out PlayerSandboxSceneCreator.Room room))
            {
                return;
            }

            GameDatabase database = room.Database;
            if (!database.Factions.TryGet(DataIds.Factions.GalacticRepoAgency, out FactionData repo)
                || !database.Entities.TryGet(DataIds.Entities.Grey, out EntityData grey)
                || database.SpawnConstants.Budget(MapSize.Small) <= 0f)
            {
                Debug.LogError($"The game data is missing the Repo Agency, Grey or the mission constants; {ScenePath} "
                    + "wasn't written. Re-import the design CSVs.");
                return;
            }

            MapSandboxSceneCreator.MapServices map = MapSandboxSceneCreator.CreateMapServices(room, grey);
            CitizenSpawner citizens = CreateCitizenSpawner(room, grey);

            var enemies = new GameObject("Mission Enemy Spawner").AddComponent<MissionEnemySpawner>();
            Wire(enemies, so =>
            {
                SetReference(so, Fields.Spawner, map.Spawner);
                SetReference(so, Fields.World, room.World);
            });

            var director = new GameObject("Mission Director").AddComponent<MissionDirector>();
            Wire(director, so =>
            {
                SetReference(so, Fields.World, room.World);
                SetReference(so, Fields.Assembler, map.Assembler);
                SetReference(so, Fields.Registry, map.Registry);
                SetReference(so, Fields.Player, room.Player);
                SetReference(so, Fields.Enemies, enemies);
                SetReference(so, Fields.Citizens, citizens);
                SetReference(so, Fields.Valuables, room.Valuables);
            });

            CreateLauncher(room, director, map, repo);
            EditorSceneManager.SaveScene(room.Scene, ScenePath);
            Debug.Log($"Created {ScenePath} and {CitizenPrefabPath}. "
                + MapSandboxSceneCreator.LibraryReport(map.Library));
        }

        /// <summary>
        /// The citizen body prefab and its spawner. entities.csv has no citizen row, so citizens are built from the
        /// Grey's data like the sandboxes' citizens, on the Civilian team.
        /// </summary>
        private static CitizenSpawner CreateCitizenSpawner(PlayerSandboxSceneCreator.Room room, EntityData grey)
        {
            GameObject body = PlayerSandboxSceneCreator.BuildEntityBody("Citizen", room.Database, null, grey,
                Team.Civilian);
            body.AddComponent<EntityDragTarget>();
            AiSandboxSceneCreator.AddCitizenParts(body);
            PlayerSandboxSceneCreator.AddCarrierView(body.GetComponent<ValuableCarrier>());
            GameObject prefab = PlayerSandboxSceneCreator.SaveAsPrefab(body, CitizenPrefabPath);

            var spawner = new GameObject("Citizen Spawner").AddComponent<CitizenSpawner>();
            Wire(spawner, so =>
            {
                SetReference(so, Fields.World, room.World);
                SetReference(so, Fields.Valuables, room.Valuables);
                SetReference(so, Fields.Prefab, prefab.GetComponent<EntityBehaviour>());
                SetReference(so, Fields.CitizenEntity, grey);
            });
            return spawner;
        }

        /// <summary>The launcher at the top right, and the bar that replaces it while a mission runs.</summary>
        private static void CreateLauncher(PlayerSandboxSceneCreator.Room room, MissionDirector director,
            MapSandboxSceneCreator.MapServices map, FactionData repo)
        {
            Transform canvas = MapSandboxSceneCreator.CreateOverlayCanvas("Mission Launcher Canvas");
            TMP_DefaultControls.Resources tmpResources = CombatSandboxSceneCreator.TmpResources();

            Transform launcher = MapSandboxSceneCreator.CreatePanel(canvas, "Mission Launcher");
            TMP_Text title = CombatSandboxSceneCreator.CreateText(launcher, tmpResources, "Title");
            title.text = "Debug mission launcher";

            Transform missionRow = CombatSandboxSceneCreator.CreateRow(launcher, "Mission Row");
            TMP_Dropdown typeDropdown = CombatSandboxSceneCreator.CreateDropdown(missionRow, tmpResources);
            TMP_Dropdown factionDropdown = CombatSandboxSceneCreator.CreateDropdown(missionRow, tmpResources);

            Transform placeRow = CombatSandboxSceneCreator.CreateRow(launcher, "Place Row");
            TMP_Dropdown locationDropdown = CombatSandboxSceneCreator.CreateDropdown(placeRow, tmpResources);
            TMP_Dropdown sizeDropdown = CombatSandboxSceneCreator.CreateDropdown(placeRow, tmpResources);

            Transform rankRow = CombatSandboxSceneCreator.CreateRow(launcher, "Rank Row");
            TMP_Dropdown rankDropdown = CombatSandboxSceneCreator.CreateDropdown(rankRow, tmpResources);
            TMP_Dropdown leaderDropdown = CombatSandboxSceneCreator.CreateDropdown(rankRow, tmpResources);

            Transform heroRow = CombatSandboxSceneCreator.CreateRow(launcher, "Hero Row");
            TMP_Dropdown heroDropdown = CombatSandboxSceneCreator.CreateDropdown(heroRow, tmpResources);
            TMP_Dropdown slotsDropdown = CombatSandboxSceneCreator.CreateDropdown(heroRow, tmpResources);

            Transform seedRow = CombatSandboxSceneCreator.CreateRow(launcher, "Seed Row");
            TMP_InputField seedInput = CombatSandboxSceneCreator.CreateInput(seedRow, tmpResources, "Seed", "0");
            seedInput.contentType = TMP_InputField.ContentType.IntegerNumber;
            Button newSeedButton = CombatSandboxSceneCreator.CreateButton(seedRow, tmpResources, "New seed");
            Button launchButton = CombatSandboxSceneCreator.CreateButton(seedRow, tmpResources, "Launch");
            TMP_Text status = CombatSandboxSceneCreator.CreateText(launcher, tmpResources, "Status");

            Transform running = MapSandboxSceneCreator.CreatePanel(canvas, "Mission Bar");
            TMP_Text missionText = CombatSandboxSceneCreator.CreateText(running, tmpResources, "Mission");
            Transform barRow = CombatSandboxSceneCreator.CreateRow(running, "Bar Row");
            Button abandonButton = CombatSandboxSceneCreator.CreateButton(barRow, tmpResources, "Abandon");
            MapSandboxSceneCreator.CreateFogButton(barRow, tmpResources, map.FogView, map.FogHiding);
            running.gameObject.SetActive(false);

            // On the canvas, which stays on: the launcher and the bar switch each other on and off.
            Wire(canvas.gameObject.AddComponent<MissionLauncherPanel>(), so =>
            {
                SetReference(so, LauncherFields.Database, room.Database);
                SetReference(so, LauncherFields.Director, director);
                SetReference(so, LauncherFields.Library, map.Library);
                SetList(so, LauncherFields.Factions, new List<Object> { repo });
                SetReference(so, LauncherFields.LauncherRoot, launcher.gameObject);
                SetReference(so, LauncherFields.RunningRoot, running.gameObject);
                SetReference(so, LauncherFields.TypeDropdown, typeDropdown);
                SetReference(so, LauncherFields.FactionDropdown, factionDropdown);
                SetReference(so, LauncherFields.LocationDropdown, locationDropdown);
                SetReference(so, LauncherFields.RankDropdown, rankDropdown);
                SetReference(so, LauncherFields.SizeDropdown, sizeDropdown);
                SetReference(so, LauncherFields.LeaderDropdown, leaderDropdown);
                SetReference(so, LauncherFields.HeroDropdown, heroDropdown);
                SetReference(so, LauncherFields.SlotsDropdown, slotsDropdown);
                SetReference(so, LauncherFields.SeedInput, seedInput);
                SetReference(so, LauncherFields.LaunchButton, launchButton);
                SetReference(so, LauncherFields.NewSeedButton, newSeedButton);
                SetReference(so, LauncherFields.AbandonButton, abandonButton);
                SetReference(so, LauncherFields.Status, status);
                SetReference(so, LauncherFields.MissionText, missionText);
            });
        }

        private static void SetList(SerializedObject serialized, string field, List<Object> values)
        {
            SerializedProperty list = Find(serialized, field);
            list.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
        }
    }
}
