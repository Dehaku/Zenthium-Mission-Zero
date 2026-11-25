using TMPro;
using UnityEngine;
using Zenthium.World;

namespace Zenthium.Sandbox
{
    /// <summary>
    /// An in-world label over a building for sandbox scenes: its name and fire state, with the seconds it has burned.
    /// Test tool only.
    /// </summary>
    public sealed class BuildingDebugLabel : MonoBehaviour
    {
        [SerializeField] private Building building;
        [SerializeField] private TMP_Text label;
        [SerializeField] private string title;

        private int shownSeconds = -1;
        private BuildingFireState shownState;

        private void OnEnable()
        {
            Refresh();
        }

        private void LateUpdate()
        {
            int seconds = Mathf.FloorToInt(building.SecondsBurning);
            if (building.FireState != shownState || seconds != shownSeconds)
            {
                Refresh();
            }
        }

        private void Refresh()
        {
            shownState = building.FireState;
            shownSeconds = Mathf.FloorToInt(building.SecondsBurning);
            switch (shownState)
            {
                case BuildingFireState.Burning:
                    label.text = title + "\nBurning " + shownSeconds + " s";
                    break;
                case BuildingFireState.BurnedOut:
                    label.text = title + "\nBurned out";
                    break;
                default:
                    label.text = title;
                    break;
            }
        }
    }
}
