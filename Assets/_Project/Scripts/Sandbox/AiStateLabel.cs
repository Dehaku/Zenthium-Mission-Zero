using TMPro;
using UnityEngine;
using Zenthium.AI;
using Zenthium.Entities;

namespace Zenthium.Sandbox
{
    /// <summary>
    /// A small in-world label with an AI entity's state, and the team of whoever it's attacking. Test tool only.
    /// </summary>
    public sealed class AiStateLabel : MonoBehaviour
    {
        [SerializeField] private AiBrain brain;
        [SerializeField] private TMP_Text label;

        private AiState shownState;
        private EntityBehaviour shownTarget;
        private bool hasShown;

        private void LateUpdate()
        {
            if (hasShown && brain.State == shownState && brain.Target == shownTarget)
            {
                return;
            }

            hasShown = true;
            shownState = brain.State;
            shownTarget = brain.Target;
            Entity target = shownTarget != null ? shownTarget.Entity : null;
            label.SetText(target != null ? $"{shownState} > {target.Team}" : shownState.ToString());
        }
    }
}
