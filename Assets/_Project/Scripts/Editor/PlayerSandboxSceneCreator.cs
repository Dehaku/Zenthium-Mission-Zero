using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Zenthium.Combat;
using Zenthium.Data;
using Zenthium.Editor.DataImport;
using Zenthium.Entities;
using Zenthium.Modules;
using Zenthium.Movement;
using Zenthium.Player;
using Zenthium.Sandbox;
using Zenthium.Timing;
using static Zenthium.Editor.SerializedWiring;

namespace Zenthium.Editor
{
    /// <summary>
    /// Builds Sandbox_Player: a walled room with a Rush Hour player and Grey training dummies, each with a debug label,
    /// plus the combat debug panel. Saves the Player, TrainingDummy and Projectile prefabs first, then places them and
    /// wires their scene references. <see cref="ModulesSandboxSceneCreator"/> builds on the same room.
    /// Sandbox scenes stay out of the build's scene list.
    /// </summary>
    internal static class PlayerSandboxSceneCreator
    {
        // No underscore in the label: Unity reads "_" plus a letter in a menu path as a shortcut key.
        private const string MenuPath = "Zenthium/Setup/Create Player Sandbox Scene";
        private const string ScenePath = "Assets/_Project/Scenes/Sandbox_Player.unity";
        private const string ProjectFolder = "Assets/_Project";
        private const string PrefabFolderName = "Prefabs";
        internal const string PrefabFolder = ProjectFolder + "/" + PrefabFolderName;
        private const string PlayerPrefabPath = PrefabFolder + "/Player.prefab";
        private const string DummyPrefabPath = PrefabFolder + "/TrainingDummy.prefab";
        private const string ProjectilePrefabPath = PrefabFolder + "/Projectile.prefab";

        private const float CameraHeight = 30f;

        // Unity's Plane primitive is 10 m across at scale 1.
        private const float PlanePrimitiveSize = 10f;
        private const float RoomSize = 30f;
        private const float WallHeight = 2f;
        private const float WallThickness = 1f;
        private const float PillarSize = 2f;

        // The Capsule primitive is 2 m tall, so its center sits 1 m above the ground.
        internal const float CapsuleHalfHeight = 1f;

        private const float ProjectileSize = 0.4f;

        // The aim arrow floats just above the capsule's top, so the overhead camera sees it.
        private static readonly Vector3 AimPivotPosition = new Vector3(0f, 1.05f, 0f);
        private static readonly Vector3 AimArrowPosition = new Vector3(0f, 0f, 0.9f);
        private static readonly Vector3 AimArrowScale = new Vector3(0.25f, 0.1f, 0.8f);

        // Debug labels: a small world-space canvas lying flat above each entity, offset toward screen-up (+Z)
        // so it sits above the capsule as the overhead camera sees it.
        private const float LabelCanvasScale = 0.01f;
        private const float LabelMinFontSize = 12f;
        private const float LabelMaxFontSize = 24f;
        private static readonly Vector3 LabelPosition = new Vector3(0f, 1.1f, 1.4f);
        private static readonly Quaternion LabelFacingCamera = Quaternion.Euler(90f, 0f, 0f);
        private static readonly Vector2 LabelSize = new Vector2(160f, 90f);
        private static readonly Color LabelBackground = new Color(0f, 0f, 0f, 0.6f);

        private static readonly Vector3 PlayerPosition = new Vector3(-5f, CapsuleHalfHeight, 0f);
        private static readonly Vector3[] DummyPositions =
        {
            new Vector3(0f, CapsuleHalfHeight, 0f),
            new Vector3(0f, CapsuleHalfHeight, 5f),
            new Vector3(0f, CapsuleHalfHeight, -5f),
        };

        private static readonly Vector3[] PillarPositions =
        {
            new Vector3(-8f, PillarSize * 0.5f, 8f),
            new Vector3(8f, PillarSize * 0.5f, -8f),
        };

        // Serialized field names, which SerializedObject needs as strings. Keep them in sync with the components.
        private static class ActionFields
        {
            public const string Database = "database";
            public const string CombatRules = "combatRules";
        }

        private static class ModulesFields
        {
            public const string World = "world";
        }

        private static class WorldFields
        {
            public const string Database = "database";
            public const string CombatRules = "combatRules";
            public const string Clock = "clock";
            public const string ProjectilePrefab = "projectilePrefab";
        }

        private static class AimFields
        {
            public const string AimCamera = "aimCamera";
        }

        private static class AimViewFields
        {
            public const string Aim = "aim";
            public const string Pivot = "pivot";
        }

        private static class CameraRigFields
        {
            public const string Target = "target";
        }

        private static class LabelFields
        {
            public const string Target = "target";
            public const string Label = "label";
        }

        /// <summary>What <see cref="TryBuildRoom"/> made, for commands that add to it.</summary>
        internal sealed class Room
        {
            public Scene Scene;
            public GameDatabase Database;
            public GameClockDriver Clock;
            public CombatRulesBehaviour CombatRules;
            public ModuleWorld World;
            public EntityBehaviour Player;
            public CombatDebugPanel DebugPanel;
            public GameObject DummyPrefab;
            public readonly List<EntityBehaviour> Dummies = new List<EntityBehaviour>();
        }

        [MenuItem(MenuPath)]
        private static void CreateSandboxPlayer()
        {
            if (TryBuildRoom(ScenePath, out Room room))
            {
                EditorSceneManager.SaveScene(room.Scene, ScenePath);
                Debug.Log($"Created {ScenePath}, {PlayerPrefabPath}, {DummyPrefabPath} and {ProjectilePrefabPath}.");
            }
        }

        /// <summary>
        /// Asks before replacing anything, then opens a new scene with the room, the rebuilt prefabs, the scene
        /// services, the player, 3 dummies and the combat debug panel. The caller saves the scene.
        /// </summary>
        internal static bool TryBuildRoom(string scenePath, out Room room)
        {
            room = null;
            if (TMP_Settings.instance == null || TMP_Settings.defaultFontAsset == null)
            {
                EditorUtility.DisplayDialog("TextMesh Pro resources missing",
                    "Import them first: Window > TextMeshPro > Import TMP Essential Resources.", "OK");
                return false;
            }

            if (!TryLoadSandboxData(out _, out _, out _))
            {
                EditorUtility.DisplayDialog("Game data missing",
                    "Run Zenthium > Data > Import Design CSVs first.", "OK");
                return false;
            }

            bool anyExists = File.Exists(scenePath) || File.Exists(PlayerPrefabPath) || File.Exists(DummyPrefabPath)
                || File.Exists(ProjectilePrefabPath);
            if (anyExists && !EditorUtility.DisplayDialog($"{Path.GetFileNameWithoutExtension(scenePath)} already exists",
                    $"Replace {scenePath}, the Player, TrainingDummy and Projectile prefabs and the module prefabs "
                    + "with fresh ones? Other sandbox scenes use the same prefabs.", "Replace", "Cancel"))
            {
                return false;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return false;
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Load the data again: opening a scene in Single mode unloads unused assets, so objects loaded
            // before it are dead and would be saved as empty references.
            if (!TryLoadSandboxData(out GameDatabase database, out EntityData grey, out HeroData rushHour))
            {
                Debug.LogError($"Couldn't reload the game data after opening a new scene; {scenePath} wasn't written.");
                return false;
            }

            if (!AssetDatabase.IsValidFolder(PrefabFolder))
            {
                AssetDatabase.CreateFolder(ProjectFolder, PrefabFolderName);
            }

            // Built in a local: lambdas can't capture the out parameter.
            var built = new Room { Scene = scene, Database = database };
            GameObject playerPrefab = SaveAsPrefab(BuildPlayer(database, rushHour), PlayerPrefabPath);
            built.DummyPrefab = SaveAsPrefab(BuildDummy(database, grey), DummyPrefabPath);
            GameObject projectilePrefab = SaveAsPrefab(BuildProjectile(), ProjectilePrefabPath);

            Camera camera = SetUpCamera(scene);
            CreateRoom();

            built.Clock = new GameObject("Game Clock").AddComponent<GameClockDriver>();
            built.CombatRules = new GameObject("Combat Rules").AddComponent<CombatRulesBehaviour>();
            Wire(built.CombatRules,
                so => SetReference(so, CombatSandboxSceneCreator.CombatRulesFields.Database, database));

            built.World = new GameObject("Module World").AddComponent<ModuleWorld>();
            Wire(built.World, so =>
            {
                SetReference(so, WorldFields.Database, database);
                SetReference(so, WorldFields.CombatRules, built.CombatRules);
                SetReference(so, WorldFields.Clock, built.Clock);
                SetReference(so, WorldFields.ProjectilePrefab, projectilePrefab.GetComponent<Projectile>());
            });
            ModulePrefabBuilder.BuildAndWire(built.World, database);

            built.Player = PlacePlayer(playerPrefab, built, camera);
            Wire(camera.GetComponent<OverheadCameraRig>(),
                so => SetReference(so, CameraRigFields.Target, built.Player.transform));

            foreach (Vector3 position in DummyPositions)
            {
                built.Dummies.Add(PlaceEntity(built.DummyPrefab, position, built));
            }

            CombatSandboxSceneCreator.CreateEventSystem();
            built.DebugPanel = CombatSandboxSceneCreator.CreateDebugPanel(
                database, built.CombatRules, built.Dummies[0], built.Player);
            SandboxHudSetup.Create(built.Player.GetComponent<PlayerController>());
            room = built;
            return true;
        }

        /// <summary>Places an entity and sets the references a prefab can't hold: the scene's clock and Module World.</summary>
        internal static EntityBehaviour PlaceEntity(GameObject prefab, Vector3 position, Room room)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.position = position;
            var entity = instance.GetComponent<EntityBehaviour>();
            Wire(entity, so => SetReference(so, CombatSandboxSceneCreator.EntityFields.Clock, room.Clock));
            Wire(instance.GetComponent<EntityModules>(), so => SetReference(so, ModulesFields.World, room.World));
            AddDebugLabel(entity);
            return entity;
        }

        private static bool TryLoadSandboxData(out GameDatabase database, out EntityData grey, out HeroData rushHour)
        {
            grey = null;
            rushHour = null;
            database = AssetDatabase.LoadAssetAtPath<GameDatabase>(GeneratedAssetWriter.DatabasePath);
            return database != null
                && database.Entities.TryGet(DataIds.Entities.Grey, out grey)
                && database.Heroes.TryGet(DataIds.Heroes.RushHour, out rushHour);
        }

        private static GameObject BuildPlayer(GameDatabase database, HeroData rushHour)
        {
            GameObject player = BuildEntityBody("Player", database, rushHour, null, Team.Player);
            Wire(player.AddComponent<DragAction>(), so => SetReference(so, ActionFields.Database, database));
            var aim = player.AddComponent<PlayerAim>();
            Wire(player.AddComponent<PlayerController>(), so => SetReference(so, ActionFields.Database, database));

            var pivot = new GameObject("Aim Pivot").transform;
            pivot.SetParent(player.transform, false);
            pivot.localPosition = AimPivotPosition;
            GameObject arrow = GameObject.CreatePrimitive(PrimitiveType.Cube);
            arrow.name = "Aim Arrow";

            // A collider here would join the player's rigidbody and widen its body.
            Object.DestroyImmediate(arrow.GetComponent<BoxCollider>());
            arrow.transform.SetParent(pivot, false);
            arrow.transform.localPosition = AimArrowPosition;
            arrow.transform.localScale = AimArrowScale;

            Wire(player.AddComponent<AimIndicatorView>(), so =>
            {
                SetReference(so, AimViewFields.Aim, aim);
                SetReference(so, AimViewFields.Pivot, pivot);
            });
            return player;
        }

        private static GameObject BuildDummy(GameDatabase database, EntityData grey)
        {
            GameObject dummy = BuildEntityBody("Training Dummy", database, null, grey, Team.Enemy);
            dummy.AddComponent<EntityDragTarget>();
            return dummy;
        }

        internal static GameObject BuildEntityBody(
            string objectName, GameDatabase database, HeroData hero, EntityData factionEntity, Team team)
        {
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = objectName;

            // CLAUDE.md: gameplay is on XZ, so rigidbodies freeze Y position and all rotation. A free Y rotation let
            // bumps spin the body and its children.
            var physicsBody = body.AddComponent<Rigidbody>();
            physicsBody.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
            physicsBody.interpolation = RigidbodyInterpolation.Interpolate;
            physicsBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            var entity = body.AddComponent<EntityBehaviour>();
            Wire(entity, so =>
            {
                SetReference(so, CombatSandboxSceneCreator.EntityFields.Database, database);
                SetReference(so, CombatSandboxSceneCreator.EntityFields.Hero, hero);
                SetReference(so, CombatSandboxSceneCreator.EntityFields.FactionEntity, factionEntity);
                Find(so, CombatSandboxSceneCreator.EntityFields.Team).enumValueIndex = (int)team;
            });
            body.AddComponent<EntityMotor>();
            body.AddComponent<EntityModules>();
            return body;
        }

        private static GameObject BuildProjectile()
        {
            GameObject projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectile.name = "Projectile";

            // Projectiles find hits by sweeping; a collider would get in the way of knockback and other sweeps.
            Object.DestroyImmediate(projectile.GetComponent<SphereCollider>());
            projectile.transform.localScale = Vector3.one * ProjectileSize;
            projectile.AddComponent<Projectile>();
            return projectile;
        }

        internal static GameObject SaveAsPrefab(GameObject built, string path)
        {
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(built, path);
            Object.DestroyImmediate(built);
            return prefab;
        }

        /// <summary>Places the player and sets the references a prefab can't hold: scene objects.</summary>
        private static EntityBehaviour PlacePlayer(GameObject prefab, Room room, Camera camera)
        {
            EntityBehaviour player = PlaceEntity(prefab, PlayerPosition, room);
            Wire(player.GetComponent<PlayerController>(),
                so => SetReference(so, ActionFields.CombatRules, room.CombatRules));
            Wire(player.GetComponent<PlayerAim>(), so => SetReference(so, AimFields.AimCamera, camera));
            return player;
        }

        /// <summary>
        /// Adds the label to the scene instance, not the prefab, so the prefabs carry no sandbox-only parts.
        /// </summary>
        private static void AddDebugLabel(EntityBehaviour entity)
        {
            var canvasObject = new GameObject("Debug Label", typeof(RectTransform), typeof(Canvas), typeof(Image));
            var canvasRect = (RectTransform)canvasObject.transform;
            canvasRect.SetParent(entity.transform, false);
            canvasRect.localPosition = LabelPosition;
            canvasRect.localRotation = LabelFacingCamera;
            canvasRect.localScale = Vector3.one * LabelCanvasScale;
            canvasRect.sizeDelta = LabelSize;
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            canvasObject.GetComponent<Image>().color = LabelBackground;

            var textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            var textRect = (RectTransform)textObject.transform;
            textRect.SetParent(canvasRect, false);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            var text = textObject.GetComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.enableAutoSizing = true;
            text.fontSizeMin = LabelMinFontSize;
            text.fontSizeMax = LabelMaxFontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.text = string.Empty;

            Wire(canvasObject.AddComponent<EntityDebugLabel>(), so =>
            {
                SetReference(so, LabelFields.Target, entity);
                SetReference(so, LabelFields.Label, text);
            });
        }

        private static Camera SetUpCamera(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.TryGetComponent(out Camera camera))
                {
                    root.transform.SetPositionAndRotation(Vector3.up * CameraHeight, Quaternion.Euler(90f, 0f, 0f));
                    root.AddComponent<OverheadCameraRig>();
                    return camera;
                }
            }

            throw new System.InvalidOperationException("The new scene has no camera.");
        }

        private static void CreateRoom()
        {
            var room = new GameObject("Room").transform;

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.SetParent(room, false);
            float groundScale = RoomSize / PlanePrimitiveSize;
            ground.transform.localScale = new Vector3(groundScale, 1f, groundScale);

            float edge = (RoomSize + WallThickness) * 0.5f;
            float wallY = WallHeight * 0.5f;
            float longSide = RoomSize + 2f * WallThickness;
            CreateBlock(room, "Wall North", new Vector3(0f, wallY, edge), new Vector3(longSide, WallHeight, WallThickness));
            CreateBlock(room, "Wall South", new Vector3(0f, wallY, -edge), new Vector3(longSide, WallHeight, WallThickness));
            CreateBlock(room, "Wall East", new Vector3(edge, wallY, 0f), new Vector3(WallThickness, WallHeight, RoomSize));
            CreateBlock(room, "Wall West", new Vector3(-edge, wallY, 0f), new Vector3(WallThickness, WallHeight, RoomSize));

            for (int i = 0; i < PillarPositions.Length; i++)
            {
                CreateBlock(room, $"Pillar {i + 1}", PillarPositions[i], Vector3.one * PillarSize);
            }
        }

        private static void CreateBlock(Transform parent, string objectName, Vector3 position, Vector3 size)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = objectName;
            block.transform.SetParent(parent, false);
            block.transform.localPosition = position;
            block.transform.localScale = size;
        }
    }
}
