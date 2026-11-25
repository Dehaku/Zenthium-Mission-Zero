using UnityEngine;
using Zenthium.AI;

namespace Zenthium.Sandbox
{
    /// <summary>
    /// Placeholder awareness visuals for an AI entity (stats.md, Detection; enemies.md, Becoming aware): the bar
    /// filling over it while it's unaware, the comic "!" when it notices, and the grey alert circle during its wait.
    /// Test visuals until Milestone 10's HUD.
    /// </summary>
    public sealed class AwarenessView : MonoBehaviour
    {
        [SerializeField] private AiBrain brain;

        [Tooltip("Shown while the bar has something in it.")]
        [SerializeField] private GameObject bar;

        [Tooltip("The bar's fill, scaled along X from its left end.")]
        [SerializeField] private Transform barFill;

        [SerializeField] private GameObject exclaim;

        [Tooltip("A flat disc of diameter 1, scaled to the alert radius while it's shown.")]
        [SerializeField] private Transform alertCircle;

        [Tooltip("Real seconds the \"!\" stays up.")]
        [SerializeField, Min(0f)] private float exclaimSeconds = 1f;

        private Vector3 fillScale;
        private Vector3 fillPosition;
        private float exclaimLeft;

        private void Awake()
        {
            fillScale = barFill.localScale;
            fillPosition = barFill.localPosition;
        }

        private void OnEnable()
        {
            brain.OnNoticed += ShowExclaim;
        }

        private void OnDisable()
        {
            if (brain != null)
            {
                brain.OnNoticed -= ShowExclaim;
            }
        }

        private void LateUpdate()
        {
            float fill = brain.AwarenessBar;
            bool isFilling = brain.AwarenessLevel == AwarenessLevel.Unaware && fill > 0f;
            bar.SetActive(isFilling);
            if (isFilling)
            {
                barFill.localScale = new Vector3(fillScale.x * fill, fillScale.y, fillScale.z);
                barFill.localPosition = fillPosition - new Vector3(fillScale.x * (1f - fill) * 0.5f, 0f, 0f);
            }

            exclaimLeft -= Time.unscaledDeltaTime;
            exclaim.SetActive(exclaimLeft > 0f);

            bool isAlerting = brain.AwarenessLevel == AwarenessLevel.Noticing;
            alertCircle.gameObject.SetActive(isAlerting);
            if (isAlerting)
            {
                float diameter = brain.AlertRadius * 2f;
                alertCircle.localScale = new Vector3(diameter, alertCircle.localScale.y, diameter);
            }
        }

        private void ShowExclaim()
        {
            exclaimLeft = exclaimSeconds;
        }
    }
}
