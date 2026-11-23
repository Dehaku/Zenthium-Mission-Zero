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
    /// plus the combat debug panel. Saves the Player and TrainingDummy prefabs first, then places them and wires
    /// their scene references.
    /// Sandbox scenes stay out of the build's scene list.
    /// </summary>
    internal static class PlayerSandboxSceneCreator
    {
        // No underscore in the label: Unity reads "_" plus a letter in a menu path as a shortcut key.
        private const string MenuPath = "Zenthium/Setup/Create Player Sandbox Scene";
        private const string ScenePath = "Assets/_Project/Scenes/Sandbox_Player.unity";
        private const string ProjectFolder = "Assets/_Project";
        private const string PrefabFolderName = "Prefabs";
        private const string PrefabFolder = ProjectFolder + "/" + PrefabFolderName;
        private const string PlayerPrefabPath = PrefabFolder + "/Player.prefab";
        private const string DummyPrefabPath = PrefabFolder + "/TrainingDummy.prefab";

        private const float CameraHeight = 30f;

        // Unity's Plane primitive is 10 m across at scale 1.
        private const float PlanePrimitiveSize = 10f;
        private const float RoomSize = 30f;
        private const float WallHeight = 2f;
        private const float WallThickness = 1f;
        private const float PillarSize = 2f;

        // The Capsule primitive is 2 m tall, so its center sits 1 m above the ground.
        private const float CapsuleHalfHeight = 1f;

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

        [MenuItem(MenuPath)]
        private static void CreateSandboxPlayer()
        {
            if (TMP_Settings.instance == null || TMP_Settings.defaultFontAsset == null)
            {
                EditorUtility.DisplayDialog("TextMesh Pro resources missing",
                    "Import them first: Window > TextMeshPro > Import TMP Essential Resources.", "OK");
                return;
            }

            if (!TryLoadSandboxData(out _, out _, out _))
            {
                EditorUtility.DisplayDialog("Game data missing",
                    "Run Zenthium > Data > Import Design CSVs first.", "OK");
                return;
            }

            bool anyExists = File.Exists(ScenePath) || File.Exists(PlayerPrefabPath) || File.Exists(DummyPrefabPath);
            if (anyExists && !EditorUtility.DisplayDialog("Sandbox_Player already exists",
                    $"Replace {ScenePath}, {PlayerPrefabPath} and {DummyPrefabPath} with fresh ones?", "Replace", "Cancel"))
            {
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Load the data again: opening a scene in Single mode unloads unused assets, so objects loaded
            // before it are dead and would be saved as empty references.
            if (!TryLoadSandboxData(out GameDatabase database, out EntityData grey, out HeroData rushHour))
            {
                Debug.LogError($"Couldn't reload the game data after opening a new scene; {ScenePath} wasn't written.");
                return;
            }

            if (!AssetDatabase.IsValidFolder(PrefabFolder))
            {
                AssetDatabase.CreateFolder(ProjectFolder, PrefabFolderName);
            }

            GameObject playerPrefab = SaveAsPrefab(BuildPlayer(database, rushHour), PlayerPrefabPath);
            GameObject dummyPrefab = SaveAsPrefab(BuildDummy(database, grey), DummyPrefabPath);

            Camera camera = SetUpCamera(scene);
            CreateRoom();

            var clock = new GameObject("Game Clock").AddComponent<GameClockDriver>();
            var combatRules = new GameObject("Combat Rules").AddComponent<CombatRulesBehaviour>();
            Wire(combatRules, so => SetReference(so, CombatSandboxSceneCreator.CombatRulesFields.Database, database));

            GameObject player = PlacePlayer(playerPrefab, clock, combatRules, camera);
            Wire(camera.GetComponent<OverheadCameraRig>(),
                so => SetReference(so, CameraRigFields.Target, player.transform));

            EntityBehaviour firstDummy = null;
            foreach (Vector3 position in DummyPositions)
            {
                EntityBehaviour dummy = PlaceEntity(dummyPrefab, position, clock);
                if (firstDummy == null)
                {
                    firstDummy = dummy;
                }
            }

            CombatSandboxSceneCreator.CreateEventSystem();
            CombatSandboxSceneCreator.CreateDebugPanel(
                database, combatRules, firstDummy, player.GetComponent<EntityBehaviour>());

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"Created {ScenePath}, {PlayerPrefabPath} and {DummyPrefabPath}.");
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
            Wire(player.AddComponent<KickAction>(), so => SetReference(so, ActionFields.Database, database));
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

        private static GameObject BuildEntityBody(
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
            return body;
        }

        private static GameObject SaveAsPrefab(GameObject built, string path)
        {
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(built, path);
            Object.DestroyImmediate(built);
            return prefab;
        }

        /// <summary>Places the player and sets the references a prefab can't hold: scene objects.</summary>
        private static GameObject PlacePlayer(
            GameObject prefab, GameClockDriver clock, CombatRulesBehaviour combatRules, Camera camera)
        {
            EntityBehaviour player = PlaceEntity(prefab, PlayerPosition, clock);
            Wire(player.GetComponent<KickAction>(), so => SetReference(so, ActionFields.CombatRules, combatRules));
            Wire(player.GetComponent<PlayerController>(), so => SetReference(so, ActionFields.CombatRules, combatRules));
            Wire(player.GetComponent<PlayerAim>(), so => SetReference(so, AimFields.AimCamera, camera));
            return player.gameObject;
        }

        private static EntityBehaviour PlaceEntity(GameObject prefab, Vector3 position, GameClockDriver clock)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.position = position;
            var entity = instance.GetComponent<EntityBehaviour>();
            Wire(entity, so => SetReference(so, CombatSandboxSceneCreator.EntityFields.Clock, clock));
            AddDebugLabel(entity);
            return entity;
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
