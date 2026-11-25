using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Zenthium.Modules;
using Zenthium.Player;
using Zenthium.Sandbox;
using static Zenthium.Editor.SerializedWiring;

namespace Zenthium.Editor
{
    /// <summary>
    /// Adds the sandbox HUD (bottom left), a Reset scene button (bottom right) and the milestone test checklist
    /// (center, toggled with Caps Lock) to a sandbox scene. Each milestone updates the checklist text file at <see cref="ChecklistPath"/>.
    /// </summary>
    internal static class SandboxHudSetup
    {
        internal const string ChecklistPath = "Assets/_Project/Sandbox/MilestoneChecklist.txt";

        private static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);
        // Layout at the 1920 x 1080 reference: a 500-wide column on each side (debug panel and HUD on the left,
        // hero select and MoreInfo on the right) and the checklist in the middle, so nothing overlaps.
        private static readonly Vector2 HudSize = new Vector2(500f, 470f);
        private static readonly Vector2 ChecklistSize = new Vector2(860f, 800f);
        private const float HudTextSize = 15f;
        private const float ChecklistTextSize = 17f;
        private const float Padding = 12f;
        private static readonly Color PanelColor = new Color(0f, 0f, 0f, 0.7f);

        // The reset button sits in the bottom right corner, under the right-hand column.
        private const string ResetButtonLabel = "Reset scene";
        private static readonly Vector2 ResetButtonSize = new Vector2(180f, 44f);
        private const float ResetButtonTextSize = 20f;
        private static readonly Color ResetButtonColor = new Color(0.6f, 0.15f, 0.1f, 0.9f);

        // Serialized field names, which SerializedObject needs as strings. Keep them in sync with the components.
        private static class HudFields
        {
            public const string Controller = "controller";
            public const string Modules = "modules";
            public const string Label = "label";
        }

        private static class ResetFields
        {
            public const string Button = "button";
        }

        private static class ChecklistFields
        {
            public const string Checklist = "checklist";
            public const string Panel = "panel";
            public const string Label = "label";
        }

        internal static void Create(PlayerController player)
        {
            // The raycaster lets the reset button take clicks; the panels and text below don't catch any.
            var canvasObject = new GameObject("Sandbox HUD Canvas", typeof(Canvas), typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;

            // Shrink: the whole reference layout always fits on screen, whatever the Game view's aspect.
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Shrink;

            GameObject hudPanel = CreatePanel(canvasObject.transform, "HUD Panel", Vector2.zero, HudSize);
            TMP_Text hudText = CreateText(hudPanel.transform, HudTextSize);
            Wire(canvasObject.AddComponent<SandboxHud>(), so =>
            {
                SetReference(so, HudFields.Controller, player);
                SetReference(so, HudFields.Modules, player.GetComponent<EntityModules>());
                SetReference(so, HudFields.Label, hudText);
            });

            CreateResetButton(canvasObject.transform);

            var checklist = AssetDatabase.LoadAssetAtPath<TextAsset>(ChecklistPath);
            if (checklist == null)
            {
                Debug.LogWarning($"No test checklist at {ChecklistPath}; the scene has no checklist overlay.");
                return;
            }

            GameObject checklistPanel = CreatePanel(canvasObject.transform, "Checklist Panel",
                new Vector2(0.5f, 0.5f), ChecklistSize);
            TMP_Text checklistText = CreateText(checklistPanel.transform, ChecklistTextSize);

            // The view sits on the canvas, not the panel, since it hides and shows the panel.
            Wire(canvasObject.AddComponent<SandboxChecklistView>(), so =>
            {
                SetReference(so, ChecklistFields.Checklist, checklist);
                SetReference(so, ChecklistFields.Panel, checklistPanel);
                SetReference(so, ChecklistFields.Label, checklistText);
            });
        }

        /// <summary>A "Reset scene" button in the bottom right corner, which reloads the scene in Play mode.</summary>
        private static void CreateResetButton(Transform canvas)
        {
            var buttonObject = new GameObject("Reset Scene Button", typeof(RectTransform), typeof(Image),
                typeof(Button));
            var rect = (RectTransform)buttonObject.transform;
            rect.SetParent(canvas, false);
            rect.anchorMin = Vector2.right;
            rect.anchorMax = Vector2.right;
            rect.pivot = Vector2.right;
            rect.anchoredPosition = new Vector2(-Padding, Padding);
            rect.sizeDelta = ResetButtonSize;
            buttonObject.GetComponent<Image>().color = ResetButtonColor;

            var labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            var labelRect = (RectTransform)labelObject.transform;
            labelRect.SetParent(rect, false);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            var label = labelObject.GetComponent<TextMeshProUGUI>();
            label.font = TMP_Settings.defaultFontAsset;
            label.fontSize = ResetButtonTextSize;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.raycastTarget = false;
            label.text = ResetButtonLabel;

            Wire(buttonObject.AddComponent<SandboxSceneReset>(),
                so => SetReference(so, ResetFields.Button, buttonObject.GetComponent<Button>()));
        }

        /// <param name="anchor">The screen point the panel hangs from, which is also its pivot.</param>
        private static GameObject CreatePanel(Transform canvas, string panelName, Vector2 anchor, Vector2 size)
        {
            var panel = new GameObject(panelName, typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)panel.transform;
            rect.SetParent(canvas, false);
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.sizeDelta = size;
            panel.GetComponent<Image>().color = PanelColor;

            // Text only: the overlay mustn't catch clicks meant for the panels under it.
            panel.GetComponent<Image>().raycastTarget = false;
            return panel;
        }

        private static TMP_Text CreateText(Transform panel, float fontSize)
        {
            var textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            var rect = (RectTransform)textObject.transform;
            rect.SetParent(panel, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(Padding, Padding);
            rect.offsetMax = new Vector2(-Padding, -Padding);
            var text = textObject.GetComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.enableAutoSizing = true;
            text.fontSizeMax = fontSize;
            text.fontSizeMin = fontSize * 0.5f;
            text.alignment = TextAlignmentOptions.TopLeft;
            text.color = Color.white;
            text.raycastTarget = false;
            text.text = string.Empty;
            return text;
        }
    }
}
