using TMPro;
using UnityEngine;
using Zenthium.Core.Controls;

namespace Zenthium.Sandbox
{
    /// <summary>
    /// Shows the current milestone's test checklist (a text file Claude updates each milestone) over the sandbox.
    /// Caps Lock (the Sandbox map's ToggleChecklist) shows and hides it. Test tool only.
    /// </summary>
    public sealed class SandboxChecklistView : MonoBehaviour
    {
        [SerializeField] private TextAsset checklist;
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text label;
        [SerializeField] private bool startVisible = true;

        private InputSystem_Actions actions;

        private void Awake()
        {
            if (checklist == null || panel == null || label == null)
            {
                Debug.LogError($"{name}: {nameof(SandboxChecklistView)} is missing references.", this);
                enabled = false;
                return;
            }

            actions = new InputSystem_Actions();
            label.text = checklist.text;
            panel.SetActive(startVisible);
        }

        private void OnEnable()
        {
            if (actions != null)
            {
                actions.Sandbox.Enable();
            }
        }

        private void OnDisable()
        {
            if (actions != null)
            {
                actions.Sandbox.Disable();
            }
        }

        private void OnDestroy()
        {
            if (actions != null)
            {
                actions.Dispose();
            }
        }

        private void Update()
        {
            if (actions.Sandbox.ToggleChecklist.WasPressedThisFrame())
            {
                panel.SetActive(!panel.activeSelf);
            }
        }
    }
}
