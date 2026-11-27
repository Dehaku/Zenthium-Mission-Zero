using TMPro;
using UnityEngine;
using Zenthium.AI;
using Zenthium.Entities;

namespace Zenthium.Sandbox
{
    /// <summary>
    /// A small in-world label with an AI entity's state: the team of whoever it's attacking, its pickup progress
    /// while looting, and whether it panicked. Test tool only.
    /// </summary>
    public sealed class AiStateLabel : MonoBehaviour
    {
        private const int ProgressSteps = 10;

        [SerializeField] private AiBrain brain;
        [SerializeField] private TMP_Text label;

        [Tooltip("Optional: shows its pickup progress while looting.")]
        [SerializeField] private AiLooter looter;

        private AiState shownState;
        private EntityBehaviour shownTarget;
        private int shownProgress;
        private bool shownPanic;
        private bool hasShown;

        private void LateUpdate()
        {
            // In steps, so the text is only rebuilt a few times per pickup.
            int progress = looter != null ? Mathf.FloorToInt(looter.PickupProgress * ProgressSteps) : 0;
            if (hasShown && brain.State == shownState && brain.Target == shownTarget && progress == shownProgress
                && brain.IsPanicking == shownPanic)
            {
                return;
            }

            hasShown = true;
            shownState = brain.State;
            shownTarget = brain.Target;
            shownProgress = progress;
            shownPanic = brain.IsPanicking;
            Entity target = shownTarget != null ? shownTarget.Entity : null;
            if (shownPanic)
            {
                label.SetText($"{shownState} (panic)");
            }
            else if (target != null)
            {
                label.SetText($"{shownState} > {target.Team}");
            }
            else if (shownProgress > 0)
            {
                label.SetText($"{shownState} {shownProgress * ProgressSteps}%");
            }
            else
            {
                label.SetText(shownState.ToString());
            }
        }
    }
}
