using System.Text;
using TMPro;
using UnityEngine;
using Zenthium.Modules;
using Zenthium.Player;

namespace Zenthium.Sandbox
{
    /// <summary>
    /// Shows the player's module info while MoreInfo is held (modules.md, Module info), as plain text. A stand-in
    /// until Milestone 10's HUD.
    /// </summary>
    public sealed class ModuleInfoView : MonoBehaviour
    {
        [SerializeField] private PlayerController controller;
        [SerializeField] private EntityModules modules;
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text label;

        [Tooltip("Real seconds between text updates while shown, so live numbers don't rebuild every frame.")]
        [SerializeField, Min(0.02f)] private float refreshSeconds = 0.25f;

        private readonly StringBuilder text = new StringBuilder();
        private float untilRefresh;

        private void Awake()
        {
            if (controller == null || modules == null || panel == null || label == null)
            {
                Debug.LogError($"{name}: {nameof(ModuleInfoView)} is missing references.", this);
                enabled = false;
                return;
            }

            panel.SetActive(false);
        }

        private void Update()
        {
            bool show = controller.IsMoreInfoHeld;
            if (panel.activeSelf != show)
            {
                panel.SetActive(show);
                untilRefresh = 0f;
            }

            if (!show)
            {
                return;
            }

            untilRefresh -= Time.unscaledDeltaTime;
            if (untilRefresh <= 0f)
            {
                untilRefresh = refreshSeconds;
                ModuleInfoText.Build(text, modules);
                label.SetText(text);
            }
        }
    }
}
