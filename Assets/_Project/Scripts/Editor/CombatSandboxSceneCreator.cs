using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Zenthium.Combat;
using Zenthium.Data;
using Zenthium.Editor.DataImport;
using Zenthium.Entities;
using Zenthium.Player;
using Zenthium.Sandbox;
using Zenthium.Timing;
using static Zenthium.Editor.SerializedWiring;

namespace Zenthium.Editor
{
    /// <summary>
    /// Builds Sandbox_Combat: a Grey goon dummy, a Rush Hour attacker and the combat debug panel, all wired.
    /// Sandbox scenes stay out of the build's scene list.
    /// </summary>
    internal static class CombatSandboxSceneCreator
    {
        // No underscore in the label: Unity reads "_" plus a letter in a menu path as a shortcut key.
        private const string MenuPath = "Zenthium/Setup/Create Combat Sandbox Scene";
        private const string ScenePath = "Assets/_Project/Scenes/Sandbox_Combat.unity";

        private const float CameraHeight = 30f;
        private const float GroundScale = 3f;
        private const float CapsuleHalfHeight = 1f;
        private const float EntitySpacing = 3f;
        private static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);
        private const float PanelWidth = 640f;
        private const int PanelPadding = 12;
        private const float Spacing = 6f;
        private const float ControlHeight = 36f;
        private const float TextSize = 20f;

        // Unity's built-in uGUI sprites, the ones GameObject > UI uses.
        private const string StandardSprite = "UI/Skin/UISprite.psd";
        private const string BackgroundSprite = "UI/Skin/Background.psd";
        private const string InputFieldSprite = "UI/Skin/InputFieldBackground.psd";
        private const string KnobSprite = "UI/Skin/Knob.psd";
        private const string CheckmarkSprite = "UI/Skin/Checkmark.psd";
        private const string DropdownSprite = "UI/Skin/DropdownArrow.psd";
        private const string MaskSprite = "UI/Skin/UIMask.psd";

        // Serialized field names, which SerializedObject needs as strings. Keep them in sync with the components.
        internal static class EntityFields
        {
            public const string Database = "database";
            public const string Clock = "clock";
            public const string Hero = "hero";
            public const string FactionEntity = "factionEntity";
            public const string Team = "team";
        }

        internal static class CombatRulesFields
        {
            public const string Database = "database";
        }

        private static class PanelFields
        {
            public const string Database = "database";
            public const string CombatRules = "combatRules";
            public const string Dummy = "dummy";
            public const string Attacker = "attacker";
            public const string Readout = "readout";
            public const string Log = "log";
            public const string AmountInput = "amountInput";
            public const string DamageTypeDropdown = "damageTypeDropdown";
            public const string DamageOverTimeToggle = "damageOverTimeToggle";
            public const string FromAttackerToggle = "fromAttackerToggle";
            public const string HitButton = "hitButton";
            public const string KickButton = "kickButton";
            public const string HealButton = "healButton";
            public const string StatDropdown = "statDropdown";
            public const string StatInput = "statInput";
            public const string SetBaseStatButton = "setBaseStatButton";
            public const string AddBoostButton = "addBoostButton";
            public const string ReductionInput = "reductionInput";
            public const string AddReductionButton = "addReductionButton";
            public const string ClearModifiersButton = "clearModifiersButton";
            public const string ResetButton = "resetButton";
        }

        [MenuItem(MenuPath)]
        private static void CreateSandboxCombat()
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

            if (File.Exists(ScenePath) && !EditorUtility.DisplayDialog("Sandbox_Combat already exists",
                    $"Replace {ScenePath} with a fresh one?", "Replace", "Cancel"))
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

            SetUpCamera(scene);
            CreateGround();

            var clock = new GameObject("Game Clock").AddComponent<GameClockDriver>();
            var combatRules = new GameObject("Combat Rules").AddComponent<CombatRulesBehaviour>();
            Wire(combatRules, so => SetReference(so, CombatRulesFields.Database, database));

            EntityBehaviour dummy = CreateEntity("Dummy (Grey)", new Vector3(EntitySpacing, CapsuleHalfHeight, 0f),
                database, clock, null, grey, Team.Enemy);
            EntityBehaviour attacker = CreateEntity("Attacker (Rush Hour)",
                new Vector3(-EntitySpacing, CapsuleHalfHeight, 0f), database, clock, rushHour, null, Team.Player);

            CreateEventSystem();
            CreateDebugPanel(database, combatRules, dummy, attacker);

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"Created {ScenePath}.");
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

        private static void SetUpCamera(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.TryGetComponent(out Camera _))
                {
                    root.transform.SetPositionAndRotation(Vector3.up * CameraHeight, Quaternion.Euler(90f, 0f, 0f));
                    root.AddComponent<OverheadCameraRig>();
                    return;
                }
            }
        }

        private static void CreateGround()
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(GroundScale, 1f, GroundScale);
        }

        private static EntityBehaviour CreateEntity(string objectName, Vector3 position, GameDatabase database,
            GameClockDriver clock, HeroData hero, EntityData factionEntity, Team team)
        {
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = objectName;
            body.transform.position = position;
            var entity = body.AddComponent<EntityBehaviour>();
            Wire(entity, so =>
            {
                SetReference(so, EntityFields.Database, database);
                SetReference(so, EntityFields.Clock, clock);
                SetReference(so, EntityFields.Hero, hero);
                SetReference(so, EntityFields.FactionEntity, factionEntity);
                Find(so, EntityFields.Team).enumValueIndex = (int)team;
            });
            return entity;
        }

        internal static void CreateEventSystem()
        {
            // The Input System's module: the legacy StandaloneInputModule throws, since only the Input System is on.
            var eventSystem = new GameObject("EventSystem", typeof(EventSystem));
            eventSystem.AddComponent<InputSystemUIInputModule>();
        }

        internal static void CreateDebugPanel(GameDatabase database, CombatRulesBehaviour combatRules,
            EntityBehaviour dummy, EntityBehaviour attacker)
        {
            var canvasObject = new GameObject("Debug Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;

            var panelObject = new GameObject("Combat Debug Panel", typeof(RectTransform), typeof(Image),
                typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            var panelRect = (RectTransform)panelObject.transform;
            panelRect.SetParent(canvasObject.transform, false);
            panelRect.anchorMin = Vector2.up;
            panelRect.anchorMax = Vector2.up;
            panelRect.pivot = Vector2.up;
            panelRect.sizeDelta = new Vector2(PanelWidth, 0f);
            panelObject.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);
            var layout = panelObject.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(PanelPadding, PanelPadding, PanelPadding, PanelPadding);
            layout.spacing = Spacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            panelObject.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Transform panel = panelObject.transform;
            TMP_DefaultControls.Resources tmpResources = TmpResources();
            DefaultControls.Resources uiResources = UiResources();

            TMP_Text readout = CreateText(panel, tmpResources, "Readout");

            Transform damageRow = CreateRow(panel, "Damage Row");
            TMP_InputField amountInput = CreateInput(damageRow, tmpResources, "Amount", "100");
            TMP_Dropdown damageTypeDropdown = CreateDropdown(damageRow, tmpResources);
            Toggle damageOverTimeToggle = CreateToggle(damageRow, uiResources, "DoT", false);
            Toggle fromAttackerToggle = CreateToggle(damageRow, uiResources, "From attacker", true);

            Transform actionRow = CreateRow(panel, "Action Row");
            Button hitButton = CreateButton(actionRow, tmpResources, "Hit");
            Button kickButton = CreateButton(actionRow, tmpResources, "Kick");
            Button healButton = CreateButton(actionRow, tmpResources, "Heal");

            Transform statRow = CreateRow(panel, "Stat Row");
            TMP_Dropdown statDropdown = CreateDropdown(statRow, tmpResources);
            TMP_InputField statInput = CreateInput(statRow, tmpResources, "Value or %", "20");
            Button setBaseStatButton = CreateButton(statRow, tmpResources, "Set base");
            Button addBoostButton = CreateButton(statRow, tmpResources, "Add boost %");

            Transform defenseRow = CreateRow(panel, "Defense Row");
            TMP_InputField reductionInput = CreateInput(defenseRow, tmpResources, "Reduction %", "50");
            Button addReductionButton = CreateButton(defenseRow, tmpResources, "Add DR %");
            Button clearModifiersButton = CreateButton(defenseRow, tmpResources, "Clear mods");
            Button resetButton = CreateButton(defenseRow, tmpResources, "Reset dummy");

            TMP_Text log = CreateText(panel, tmpResources, "Log");

            var debugPanel = panelObject.AddComponent<CombatDebugPanel>();
            Wire(debugPanel, so =>
            {
                SetReference(so, PanelFields.Database, database);
                SetReference(so, PanelFields.CombatRules, combatRules);
                SetReference(so, PanelFields.Dummy, dummy);
                SetReference(so, PanelFields.Attacker, attacker);
                SetReference(so, PanelFields.Readout, readout);
                SetReference(so, PanelFields.Log, log);
                SetReference(so, PanelFields.AmountInput, amountInput);
                SetReference(so, PanelFields.DamageTypeDropdown, damageTypeDropdown);
                SetReference(so, PanelFields.DamageOverTimeToggle, damageOverTimeToggle);
                SetReference(so, PanelFields.FromAttackerToggle, fromAttackerToggle);
                SetReference(so, PanelFields.HitButton, hitButton);
                SetReference(so, PanelFields.KickButton, kickButton);
                SetReference(so, PanelFields.HealButton, healButton);
                SetReference(so, PanelFields.StatDropdown, statDropdown);
                SetReference(so, PanelFields.StatInput, statInput);
                SetReference(so, PanelFields.SetBaseStatButton, setBaseStatButton);
                SetReference(so, PanelFields.AddBoostButton, addBoostButton);
                SetReference(so, PanelFields.ReductionInput, reductionInput);
                SetReference(so, PanelFields.AddReductionButton, addReductionButton);
                SetReference(so, PanelFields.ClearModifiersButton, clearModifiersButton);
                SetReference(so, PanelFields.ResetButton, resetButton);
            });
        }

        private static Transform CreateRow(Transform parent, string rowName)
        {
            var row = new GameObject(rowName, typeof(RectTransform), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(parent, false);
            var layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = Spacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return row.transform;
        }

        private static TMP_Text CreateText(Transform parent, TMP_DefaultControls.Resources resources, string objectName)
        {
            GameObject textObject = TMP_DefaultControls.CreateText(resources);
            textObject.name = objectName;
            textObject.transform.SetParent(parent, false);
            var text = textObject.GetComponent<TMP_Text>();
            text.fontSize = TextSize;
            text.text = string.Empty;
            return text;
        }

        private static TMP_InputField CreateInput(
            Transform parent, TMP_DefaultControls.Resources resources, string placeholder, string value)
        {
            GameObject inputObject = TMP_DefaultControls.CreateInputField(resources);
            inputObject.name = placeholder + " Input";
            AddToRow(inputObject, parent);
            var input = inputObject.GetComponent<TMP_InputField>();
            input.contentType = TMP_InputField.ContentType.DecimalNumber;
            input.text = value;
            if (input.placeholder is TMP_Text placeholderText)
            {
                placeholderText.text = placeholder;
            }

            return input;
        }

        private static TMP_Dropdown CreateDropdown(Transform parent, TMP_DefaultControls.Resources resources)
        {
            GameObject dropdownObject = TMP_DefaultControls.CreateDropdown(resources);
            AddToRow(dropdownObject, parent);

            // The panel fills the options from the enums at runtime.
            return dropdownObject.GetComponent<TMP_Dropdown>();
        }

        private static Button CreateButton(Transform parent, TMP_DefaultControls.Resources resources, string label)
        {
            GameObject buttonObject = TMP_DefaultControls.CreateButton(resources);
            buttonObject.name = label + " Button";
            AddToRow(buttonObject, parent);
            buttonObject.GetComponentInChildren<TMP_Text>().text = label;
            return buttonObject.GetComponent<Button>();
        }

        private static Toggle CreateToggle(Transform parent, DefaultControls.Resources resources, string label, bool isOn)
        {
            GameObject toggleObject = DefaultControls.CreateToggle(resources);
            toggleObject.name = label + " Toggle";
            AddToRow(toggleObject, parent);

            // DefaultControls labels use the legacy Text component; swap in TextMeshPro like the rest of the UI.
            var legacyLabel = toggleObject.GetComponentInChildren<Text>();
            GameObject labelObject = legacyLabel.gameObject;
            Object.DestroyImmediate(legacyLabel);
            var tmpLabel = labelObject.AddComponent<TextMeshProUGUI>();
            tmpLabel.text = label;
            tmpLabel.fontSize = TextSize;
            tmpLabel.alignment = TextAlignmentOptions.MidlineLeft;

            var toggle = toggleObject.GetComponent<Toggle>();
            toggle.isOn = isOn;
            return toggle;
        }

        private static void AddToRow(GameObject control, Transform row)
        {
            control.transform.SetParent(row, false);
            var element = control.AddComponent<LayoutElement>();
            element.preferredHeight = ControlHeight;
            element.flexibleWidth = 1f;
        }

        private static TMP_DefaultControls.Resources TmpResources()
        {
            return new TMP_DefaultControls.Resources
            {
                standard = BuiltinSprite(StandardSprite),
                background = BuiltinSprite(BackgroundSprite),
                inputField = BuiltinSprite(InputFieldSprite),
                knob = BuiltinSprite(KnobSprite),
                checkmark = BuiltinSprite(CheckmarkSprite),
                dropdown = BuiltinSprite(DropdownSprite),
                mask = BuiltinSprite(MaskSprite),
            };
        }

        private static DefaultControls.Resources UiResources()
        {
            return new DefaultControls.Resources
            {
                standard = BuiltinSprite(StandardSprite),
                background = BuiltinSprite(BackgroundSprite),
                inputField = BuiltinSprite(InputFieldSprite),
                knob = BuiltinSprite(KnobSprite),
                checkmark = BuiltinSprite(CheckmarkSprite),
                dropdown = BuiltinSprite(DropdownSprite),
                mask = BuiltinSprite(MaskSprite),
            };
        }

        private static Sprite BuiltinSprite(string path)
        {
            return AssetDatabase.GetBuiltinExtraResource<Sprite>(path);
        }
    }
}
