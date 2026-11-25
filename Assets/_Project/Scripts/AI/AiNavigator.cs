using UnityEngine;
using UnityEngine.AI;
using Zenthium.Movement;
using Zenthium.World;

namespace Zenthium.AI
{
    /// <summary>
    /// Walks an AI entity along NavMesh paths by steering its <see cref="EntityMotor"/>, so knockback, dashes and
    /// statuses keep working as they do for the player. There's no NavMeshAgent. Paths go through doors, which
    /// aren't carved (world.md, Doors); a locked door in the way is reported in <see cref="BlockingDoor"/> for the AI
    /// to attack. Burning buildings carve their floors out (Milestone 5b), so paths go around them, and an entity
    /// standing on a carved-out floor walks to the nearest walkable point first.
    /// </summary>
    [RequireComponent(typeof(EntityMotor))]
    public sealed class AiNavigator : MonoBehaviour
    {
        private const int MaxCorners = 32;

        [Tooltip("Real seconds between path updates while moving.")]
        [SerializeField, Min(0.05f)] private float repathSeconds = 0.5f;

        [Tooltip("Meters the destination must move before the path is worked out again at once.")]
        [SerializeField, Min(0.1f)] private float repathDistance = 1f;

        [Tooltip("Meters to look for the NavMesh around a point, so a point on a carved-out floor (a burning building) "
            + "finds the nearest walkable one.")]
        [SerializeField, Min(0.5f)] private float sampleRadius = 20f;

        [Tooltip("Meters from a path corner that count as reaching it.")]
        [SerializeField, Min(0.05f)] private float cornerReach = 0.3f;

        [Tooltip("Meters ahead it looks for a locked door in its way.")]
        [SerializeField, Min(0.1f)] private float doorProbeDistance = 1.2f;

        private readonly Vector3[] corners = new Vector3[MaxCorners];
        private EntityMotor motor;
        private NavMeshPath path;
        private Vector3 destination;
        private Vector3 endPoint;
        private float speedFraction;
        private float untilRepath;
        private int cornerCount;
        private int cornerIndex;
        private bool hasDestination;

        /// <summary>A locked, unbroken door it's walking into; null if none.</summary>
        public Door BlockingDoor { get; private set; }

        /// <summary>No destination, or at the end of its path.</summary>
        public bool HasArrived { get; private set; } = true;

        /// <param name="target">Where to go. Moving it less than the repath distance keeps the current path.</param>
        /// <param name="fraction">The fraction of its move speed, such as half speed on patrol.</param>
        public void MoveTo(Vector3 target, float fraction)
        {
            bool isNew = !hasDestination || FlatDistance(target, destination) > repathDistance;
            destination = target;
            speedFraction = fraction;
            hasDestination = true;
            if (isNew)
            {
                HasArrived = false;
                Repath();
            }
        }

        public void Stop()
        {
            hasDestination = false;
            HasArrived = true;
            cornerCount = 0;
            BlockingDoor = null;
            if (motor != null)
            {
                motor.SetMoveInput(Vector2.zero);
            }
        }

        public bool IsNear(Vector3 point, float distance)
        {
            return FlatDistance(transform.position, point) <= distance;
        }

        private void Awake()
        {
            motor = GetComponent<EntityMotor>();
            path = new NavMeshPath();
        }

        private void OnDisable()
        {
            Stop();
        }

        private void Update()
        {
            if (!hasDestination)
            {
                return;
            }

            untilRepath -= Time.unscaledDeltaTime;
            if (untilRepath <= 0f)
            {
                Repath();
            }

            Vector3 position = transform.position;
            while (cornerIndex < cornerCount && FlatDistance(position, corners[cornerIndex]) <= cornerReach)
            {
                cornerIndex++;
            }

            Vector3 next = cornerIndex < cornerCount ? corners[cornerIndex] : endPoint;
            Vector3 direction = next - position;
            direction.y = 0f;
            HasArrived = cornerIndex >= cornerCount && direction.sqrMagnitude <= cornerReach * cornerReach;
            if (HasArrived)
            {
                motor.SetMoveInput(Vector2.zero);
                BlockingDoor = null;
                return;
            }

            direction.Normalize();
            motor.SetMoveInput(new Vector2(direction.x, direction.z) * speedFraction);
            BlockingDoor = FindLockedDoor(position, direction);
        }

        private void Repath()
        {
            untilRepath = repathSeconds;
            cornerIndex = 0;
            cornerCount = 0;
            endPoint = destination;
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit start, sampleRadius, NavMesh.AllAreas)
                && NavMesh.SamplePosition(destination, out NavMeshHit end, sampleRadius, NavMesh.AllAreas)
                && NavMesh.CalculatePath(start.position, end.position, NavMesh.AllAreas, path)
                && path.status != NavMeshPathStatus.PathInvalid)
            {
                // The path starts at the sampled start, which is beside it when it stands on carved-out floor, so
                // the first corner leads it out.
                cornerCount = path.GetCornersNonAlloc(corners);
                endPoint = cornerCount > 0 ? corners[cornerCount - 1] : end.position;
            }

            // With no path (no NavMesh nearby) it walks straight at the destination.
        }

        private Door FindLockedDoor(Vector3 position, Vector3 direction)
        {
            if (!Physics.Raycast(position, direction, out RaycastHit hit, doorProbeDistance, Physics.AllLayers,
                    QueryTriggerInteraction.Ignore))
            {
                return null;
            }

            Door door = hit.collider.GetComponentInParent<Door>();
            return door != null && door.IsLocked && door.Panel != null && !door.Panel.IsBroken ? door : null;
        }

        private static float FlatDistance(Vector3 a, Vector3 b)
        {
            float x = a.x - b.x;
            float z = a.z - b.z;
            return Mathf.Sqrt(x * x + z * z);
        }
    }
}
