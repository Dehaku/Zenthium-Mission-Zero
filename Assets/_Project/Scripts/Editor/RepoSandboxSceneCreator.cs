using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Zenthium.AI;
using Zenthium.Data;
using Zenthium.Entities;
using Zenthium.Modules;
using Zenthium.Sandbox;
using Zenthium.World;
using static Zenthium.Editor.SerializedWiring;

namespace Zenthium.Editor
{
    /// <summary>
    /// Builds Sandbox_Repo (Milestone 7) on the shared 60 m room: a Galactic Repo Agency squad (GleebPeep, two
    /// Greys, a Tractor Drone, a Shield Bearer, an Auditor and a Notary) patrolling the north and a Tow Saucer by the
    /// prop row, built from entities.csv and factions.csv
    /// with no test kits; two crop circles; a downed, a Scared and a Stubborn citizen; a stash of Valuables and the
    /// lost counter; the map's bounds; a spawner for Call for Backup; the hero select and MoreInfo panels; and a baked
    /// NavMesh, and the Mothership Strike with its on/off button. Every Repo entity is built for rank B, so all of
    /// GleebPeep's modules are unlocked.
    /// </summary>
    internal static class RepoSandboxSceneCreator
    {
        // No underscore in the label: Unity reads "_" plus a letter in a menu path as a shortcut key.
        private const string MenuPath = "Zenthium/Setup/Create Repo Agency Sandbox Scene";
        private const string ScenePath = "Assets/_Project/Scenes/Sandbox_Repo.unity";
        private const string NavMeshPath = "Assets/_Project/Scenes/Sandbox_Repo_NavMesh.asset";
        private const string AiEntityPrefabPath = PlayerSandboxSceneCreator.PrefabFolder + "/AiEntity.prefab";

        private const float RoomSize = 60f;
        private const float Height = PlayerSandboxSceneCreator.CapsuleHalfHeight;

        // Rank B: GleebPeep's Call for Backup unlocks there (Factions/GalacticRepoAgency.md).
        private const int SandboxDifficultyLevel = 2;

        private static readonly Vector3[] SquadSockets =
        {
            new Vector3(0f, 0f, 22f),
            new Vector3(10f, 0f, 24f),
            new Vector3(-8f, 0f, 20f),
        };

        private static readonly Vector3 LeaderPosition = new Vector3(4f, Height, 24f);
        private static readonly Vector3[] GreyPositions = { new Vector3(0f, Height, 22f), new Vector3(2f, Height, 20f) };
        private static readonly Vector3 DronePosition = new Vector3(-4f, Height, 22f);
        private static readonly Vector3 ShieldBearerPosition = new Vector3(6f, Height, 21f);

        // Milestone 7b. The Auditor and Notary patrol with the squad; the Tow Saucer waits within Tow Beam's 15 m of
        // the prop row's bench, south of the start.
        private static readonly Vector3 AuditorPosition = new Vector3(-6f, Height, 24f);
        private static readonly Vector3 NotaryPosition = new Vector3(8f, Height, 23f);
        private static readonly Vector3 TowSaucerPosition = new Vector3(-6f, Height, -8f);

        private static readonly Vector2 MothershipButtonPosition = new Vector2(-12f, 68f);
        private static readonly Color MothershipButtonColor = new Color(0.6f, 0.15f, 0.15f, 0.9f);
        private static readonly Vector2 CanvasReferenceResolution = new Vector2(1920f, 1080f);

        private static readonly Vector3[] CropCirclePositions =
        {
            new Vector3(20f, 0f, 12f),
            new Vector3(-20f, 0f, 15f),
        };

        // Within the squad's unaware spotting range, so the Greys go for them at the start.
        private static readonly Vector3 DownedCitizenPosition = new Vector3(-8f, Height, 12f);
        private static readonly Vector3 StubbornCitizenPosition = new Vector3(14f, Height, 14f);
        private static readonly Vector3 ScaredCitizenPosition = new Vector3(8f, Height, 4f);
        private const int DownedCitizenValuables = 1;
        private const int ScaredCitizenValuables = 2;

        private static readonly Vector3 StashPosition = new Vector3(-14f, 0.3f, 24f);
        private const int StashBags = 3;

        // The disc lies just above the ground, so it doesn't flicker against it.
        private const float CircleHeight = 0.02f;
        private const float CircleThickness = 0.01f;
        private static readonly Color CropCircleColor = new Color(0.3f, 1f, 0.4f, 0.35f);

        // Serialized field names, which SerializedObject needs as strings. Keep them in sync with the components.
        private static class Fields
        {
            public const string DifficultyLevel = "difficultyLevel";
            public const string DragDatabase = "database";
            public const string MapBoundsSize = "size";
            public const string CropCircleWorld = "world";
            public const string CropCircleFaction = "faction";
            public const string SpawnerWorld = "world";
            public const string SpawnerMapBounds = "mapBounds";
            public const string SpawnerValuables = "valuables";
            public const string SpawnerPrefab = "prefab";
            public const string WorldSpawner = "entitySpawner";
            public const string StrikeWorld = "world";
            public const string StrikeFaction = "faction";
            public const string ToggleStrike = "strike";
            public const string ToggleButton = "button";
            public const string ToggleLabel = "label";
        }

        [MenuItem(MenuPath)]
        private static void CreateSandboxRepo()
        {
            if (!PlayerSandboxSceneCreator.TryBuildRoom(ScenePath, out PlayerSandboxSceneCreator.Room room, RoomSize))
            {
                return;
            }

            GameDatabase database = room.Database;
            if (!database.Factions.TryGet(DataIds.Factions.GalacticRepoAgency, out FactionData repo)
                || !database.Entities.TryGet(DataIds.Entities.GleebPeep, out EntityData gleebPeep)
                || !database.Entities.TryGet(DataIds.Entities.Grey, out EntityData grey)
                || !database.Entities.TryGet(DataIds.Entities.TractorDrone, out EntityData drone)
                || !database.Entities.TryGet(DataIds.Entities.ShieldBearer, out EntityData shieldBearer)
                || !database.Entities.TryGet(DataIds.Entities.Auditor, out EntityData auditor)
                || !database.Entities.TryGet(DataIds.Entities.Notary, out EntityData notary)
                || !database.Entities.TryGet(DataIds.Entities.TowSaucer, out EntityData towSaucer)
                || !database.Modules.TryGet(DataIds.Modules.Drag, out ModuleData drag)
                || !CropCircleRules.TryCreate(repo, out CropCircleRules cropCircles, out _))
            {
                Debug.LogError($"The game data is missing the Repo Agency, one of its entities, Drag or its crop circle "
                    + $"values; {ScenePath} wasn't written.");
                return;
            }

            // Matches the hero select's slots dropdown, which starts at all slots.
            Wire(room.Player, so => Find(so, CombatSandboxSceneCreator.EntityFields.UnlockedSlots).intValue =
                database.ModuleConstants.SlotsPerType);

            // The Repo squad replaces the room's dummies.
            foreach (EntityBehaviour dummy in room.Dummies)
            {
                Object.DestroyImmediate(dummy.gameObject);
            }

            room.Dummies.Clear();

            // The room's walls stand on its edge, so goons escape within the bounds' margin of them.
            var mapBounds = new GameObject("Map Bounds").AddComponent<MapBounds>();
            Wire(mapBounds, so => Find(so, Fields.MapBoundsSize).vector2Value = new Vector2(RoomSize, RoomSize));

            for (int i = 0; i < CropCirclePositions.Length; i++)
            {
                CreateCropCircle(room, repo, cropCircles.Radius, $"Crop Circle {i + 1}", CropCirclePositions[i]);
            }

            CreateSpawner(room, mapBounds, grey);

            AiSquad squad = AiSandboxSceneCreator.CreateSquad(room, "Repo Squad", SquadSockets);
            EntityBehaviour leader = PlaceRepoEntity(room, "GleebPeep (Leader)", LeaderPosition, gleebPeep, drag,
                squad, mapBounds);
            for (int i = 0; i < GreyPositions.Length; i++)
            {
                PlaceRepoEntity(room, $"Grey {i + 1}", GreyPositions[i], grey, drag, squad, mapBounds);
            }

            PlaceRepoEntity(room, "Tractor Drone", DronePosition, drone, drag, squad, mapBounds);
            PlaceRepoEntity(room, "Shield Bearer", ShieldBearerPosition, shieldBearer, drag, squad, mapBounds);
            PlaceRepoEntity(room, "Auditor", AuditorPosition, auditor, drag, squad, mapBounds);
            PlaceRepoEntity(room, "Notary", NotaryPosition, notary, drag, squad, mapBounds);
            PlaceRepoEntity(room, "Tow Saucer", TowSaucerPosition, towSaucer, drag, null, mapBounds);

            WorldSandboxSceneCreator.BuildPropRow(room);
            CreateMothership(room, repo);

            AiSandboxSceneCreator.PlaceCitizen(room, $"Downed Citizen ({DownedCitizenValuables} Valuable)",
                DownedCitizenPosition, CitizenKind.Incapacitated, DownedCitizenValuables);
            AiSandboxSceneCreator.PlaceCitizen(room, "Stubborn Citizen", StubbornCitizenPosition, CitizenKind.Stubborn, 0);
            AiSandboxSceneCreator.PlaceCitizen(room, $"Scared Citizen ({ScaredCitizenValuables} Valuables)",
                ScaredCitizenPosition, CitizenKind.Scared, ScaredCitizenValuables);
            AiSandboxSceneCreator.PlaceBags(room, "Stash", StashPosition, StashBags);
            WorldSandboxSceneCreator.CreateLostCounter(room.Valuables);

            CombatSandboxSceneCreator.BindDebugPanel(room.DebugPanel, room.Player, leader);
            ModulesSandboxSceneCreator.CreateModulesCanvas(room);
            WorldSandboxSceneCreator.BakeNavMesh(room.Geometry, NavMeshPath);

            EditorSceneManager.SaveScene(room.Scene, ScenePath);
            Debug.Log($"Created {ScenePath}, {NavMeshPath} and {AiEntityPrefabPath}.");
        }

        /// <summary>
        /// A training dummy turned into a Repo entity from its data, at the sandbox's rank, with the AI. One whose list
        /// has Drag also drags citizens.
        /// </summary>
        private static EntityBehaviour PlaceRepoEntity(PlayerSandboxSceneCreator.Room room, string entityName,
            Vector3 position, EntityData data, ModuleData drag, AiSquad squad, MapBounds mapBounds)
        {
            EntityBehaviour entity = PlayerSandboxSceneCreator.PlaceEntity(room.DummyPrefab, position, room);
            entity.name = entityName;
            Wire(entity, so =>
            {
                SetReference(so, CombatSandboxSceneCreator.EntityFields.FactionEntity, data);
                Find(so, Fields.DifficultyLevel).intValue = SandboxDifficultyLevel;
            });

            if (data.Lists(drag))
            {
                AddCitizenDragging(entity.gameObject, room.Database);
            }

            AddRepoParts(entity.gameObject);
            AiSandboxSceneCreator.AddGoonAi(entity.gameObject, squad, mapBounds);
            return entity;
        }

        /// <summary>Towing and crashing: each does nothing on an entity without Tow Beam or crash values.</summary>
        private static void AddRepoParts(GameObject body)
        {
            body.AddComponent<AiTower>();
            body.AddComponent<CrashOnDefeat>();
        }

        /// <summary>The Mothership Strike, off, and the button that switches it on, above the Reset scene button.</summary>
        private static void CreateMothership(PlayerSandboxSceneCreator.Room room, FactionData repo)
        {
            var strike = new GameObject("Mothership Strike").AddComponent<MothershipStrike>();
            Wire(strike, so =>
            {
                SetReference(so, Fields.StrikeWorld, room.World);
                SetReference(so, Fields.StrikeFaction, repo);
            });

            var canvasObject = new GameObject("Repo Sandbox Canvas", typeof(Canvas), typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = CanvasReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Shrink;

            Button button = SandboxHudSetup.CreateButton(canvasObject.transform, "Mothership Button",
                "Mothership: off", MothershipButtonPosition, MothershipButtonColor, out TMP_Text label);
            Wire(button.gameObject.AddComponent<MothershipToggle>(), so =>
            {
                SetReference(so, Fields.ToggleStrike, strike);
                SetReference(so, Fields.ToggleButton, button);
                SetReference(so, Fields.ToggleLabel, label);
            });
        }

        private static void AddCitizenDragging(GameObject body, GameDatabase database)
        {
            Wire(body.AddComponent<DragAction>(), so => SetReference(so, Fields.DragDatabase, database));
            body.AddComponent<AiCitizenSnatcher>();
        }

        /// <summary>A crop circle and its see-through disc, sized from the faction's radius.</summary>
        private static void CreateCropCircle(PlayerSandboxSceneCreator.Room room, FactionData repo, float radius,
            string circleName, Vector3 position)
        {
            var circle = new GameObject(circleName);
            circle.transform.position = position;
            Wire(circle.AddComponent<CropCircle>(), so =>
            {
                SetReference(so, Fields.CropCircleWorld, room.World);
                SetReference(so, Fields.CropCircleFaction, repo);
            });

            // A cylinder primitive is 1 m across and 2 m tall, so it scales by the diameter and half the thickness.
            AiSandboxSceneCreator.Visual(PrimitiveType.Cylinder, "Disc", circle.transform,
                new Vector3(0f, CircleHeight, 0f), new Vector3(radius * 2f, CircleThickness * 0.5f, radius * 2f),
                AiSandboxSceneCreator.TransparentMaterial("CropCircle", CropCircleColor));
        }

        /// <summary>
        /// The AI body prefab Call for Backup spawns (it drags citizens only when its data lists Drag), and the
        /// scene's spawner, which the Module World uses.
        /// </summary>
        private static void CreateSpawner(PlayerSandboxSceneCreator.Room room, MapBounds mapBounds, EntityData grey)
        {
            GameObject body = PlayerSandboxSceneCreator.BuildEntityBody("AI Entity", room.Database, null, grey,
                Team.Enemy);
            body.AddComponent<EntityDragTarget>();
            AddCitizenDragging(body, room.Database);
            AddRepoParts(body);
            AiSandboxSceneCreator.AddGoonAi(body, null, null);
            GameObject prefab = PlayerSandboxSceneCreator.SaveAsPrefab(body, AiEntityPrefabPath);

            var spawner = new GameObject("Faction Entity Spawner").AddComponent<FactionEntitySpawner>();
            Wire(spawner, so =>
            {
                SetReference(so, Fields.SpawnerWorld, room.World);
                SetReference(so, Fields.SpawnerMapBounds, mapBounds);
                SetReference(so, Fields.SpawnerValuables, room.Valuables);
                SetReference(so, Fields.SpawnerPrefab, prefab.GetComponent<EntityBehaviour>());
            });
            Wire(room.World, so => SetReference(so, Fields.WorldSpawner, spawner));
        }
    }
}
