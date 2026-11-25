using TMPro;
using UnityEngine;
using Zenthium.World;

namespace Zenthium.Sandbox
{
    /// <summary>
    /// Shows how many Valuables are lost, for sandbox scenes. The mission's "Lost 2 / Fail at 5" counter comes with the
    /// Valuables goal in Milestone 9. Test tool only.
    /// </summary>
    public sealed class LostValuablesLabel : MonoBehaviour
    {
        [SerializeField] private ValuableService service;
        [SerializeField] private TMP_Text label;

        private void OnEnable()
        {
            service.OnLostChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            if (service != null)
            {
                service.OnLostChanged -= Refresh;
            }
        }

        private void Refresh()
        {
            label.SetText("Valuables lost: {0}", service.LostCount);
        }
    }
}
