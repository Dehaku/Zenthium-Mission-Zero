using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenthium.World;

namespace Zenthium.Sandbox
{
    /// <summary>
    /// A sandbox button that shows or hides the fog of war and what it hides, to compare. The fog keeps tracking what
    /// was explored either way. Test tool only.
    /// </summary>
    public sealed class FogToggle : MonoBehaviour
    {
        [SerializeField] private FogOfWarView view;
        [SerializeField] private FogHiding hiding;
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text label;

        private void OnEnable()
        {
            button.onClick.AddListener(Toggle);
            Show();
        }

        private void OnDisable()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(Toggle);
            }
        }

        private void Toggle()
        {
            bool isOn = !view.enabled;
            view.enabled = isOn;
            hiding.enabled = isOn;
            Show();
        }

        private void Show()
        {
            label.text = view.enabled ? "Fog: on" : "Fog: off";
        }
    }
}
