using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenthium.World;

namespace Zenthium.Sandbox
{
    /// <summary>A sandbox button that switches the Mothership Strike on and off, and shows the time to the next one.</summary>
    public sealed class MothershipToggle : MonoBehaviour
    {
        [SerializeField] private MothershipStrike strike;
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text label;

        private int shownSeconds = -1;
        private bool shownOn;

        private void OnEnable()
        {
            button.onClick.AddListener(Toggle);
        }

        private void OnDisable()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(Toggle);
            }
        }

        private void Update()
        {
            int seconds = Mathf.CeilToInt(strike.SecondsToNextStrike);
            if (seconds == shownSeconds && strike.IsOn == shownOn)
            {
                return;
            }

            shownSeconds = seconds;
            shownOn = strike.IsOn;
            if (shownOn)
            {
                label.SetText("Mothership: on ({0} s)", seconds);
            }
            else
            {
                label.SetText("Mothership: off");
            }
        }

        private void Toggle()
        {
            strike.SetOn(!strike.IsOn);
        }
    }
}
