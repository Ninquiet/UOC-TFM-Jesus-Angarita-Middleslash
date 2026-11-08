using UnityEngine;

namespace Aberracion
{
    internal sealed class EnemyPatrolRoute
    {
        const float ArrivalDistance = .4f;
        const float ArenaInset = 1f;
        readonly Vector3 home;
        Vector3? destination;

        public EnemyPatrolRoute(Vector3 home) => this.home = home;

        public Vector3 OffsetFrom(Vector3 position, float radius, Collider arenaFloor)
        {
            if (!destination.HasValue || PlanarOffset(position, destination.Value).sqrMagnitude <= ArrivalDistance * ArrivalDistance)
            {
                Vector2 sample = Random.insideUnitCircle * radius;
                Vector3 point = home + new Vector3(sample.x, 0, sample.y);
                if (arenaFloor)
                {
                    Bounds bounds = arenaFloor.bounds;
                    point.x = Mathf.Clamp(point.x, bounds.min.x + ArenaInset, bounds.max.x - ArenaInset);
                    point.z = Mathf.Clamp(point.z, bounds.min.z + ArenaInset, bounds.max.z - ArenaInset);
                }
                destination = point;
            }
            return PlanarOffset(position, destination.Value);
        }

        public void Clear() => destination = null;

        static Vector3 PlanarOffset(Vector3 from, Vector3 to) => Vector3.ProjectOnPlane(to - from, Vector3.up);
    }
}
