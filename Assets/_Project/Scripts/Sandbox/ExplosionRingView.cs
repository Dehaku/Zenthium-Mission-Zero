using UnityEngine;
using Zenthium.World;

namespace Zenthium.Sandbox
{
    /// <summary>
    /// Placeholder presentation for a vehicle about to explode (world.md, Props): a red ring the size of the blast,
    /// shown from destruction until the explosion. The real art comes in Milestone 16.
    /// </summary>
    public sealed class ExplosionRingView : MonoBehaviour
    {
        [SerializeField] private VehicleExplosion explosion;

        [Tooltip("A flat disc, hidden until the fuse is lit. It's scaled to the blast's diameter.")]
        [SerializeField] private Transform ring;

        private void Awake()
        {
            ring.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            explosion.OnFuseLit += Show;
        }

        private void OnDisable()
        {
            explosion.OnFuseLit -= Show;
        }

        private void Show(float radius)
        {
            // The ring hangs under the vehicle, so undo the vehicle's own scale to get a true radius.
            Vector3 parentScale = ring.parent != null ? ring.parent.lossyScale : Vector3.one;
            Vector3 scale = ring.localScale;
            ring.localScale = new Vector3(radius * 2f / parentScale.x, scale.y, radius * 2f / parentScale.z);
            ring.gameObject.SetActive(true);
        }
    }
}
