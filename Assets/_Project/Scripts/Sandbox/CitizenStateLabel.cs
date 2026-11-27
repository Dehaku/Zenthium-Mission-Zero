using TMPro;
using UnityEngine;
using Zenthium.AI;

namespace Zenthium.Sandbox
{
    /// <summary>A small in-world label with a citizen's kind and whether it's fleeing. Test tool only.</summary>
    public sealed class CitizenStateLabel : MonoBehaviour
    {
        [SerializeField] private CitizenBrain citizen;
        [SerializeField] private TMP_Text label;

        private CitizenKind shownKind;
        private bool shownFleeing;
        private bool hasShown;

        private void LateUpdate()
        {
            if (hasShown && citizen.Kind == shownKind && citizen.IsFleeing == shownFleeing)
            {
                return;
            }

            hasShown = true;
            shownKind = citizen.Kind;
            shownFleeing = citizen.IsFleeing;
            label.SetText(shownFleeing ? $"{shownKind} (fleeing)" : shownKind.ToString());
        }
    }
}
