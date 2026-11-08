using UnityEngine;

namespace Aberracion
{
    internal sealed class CapsuleTravel
    {
        readonly CapsuleCollider capsule;
        readonly RaycastHit[] obstacles = new RaycastHit[24];

        public CapsuleTravel(CapsuleCollider capsule) => this.capsule = capsule;

        public float Limit(Vector3 origin, Vector3 direction, float distance)
        {
            Transform body = capsule.transform;
            Vector3 scale = body.lossyScale;
            float radius = capsule.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
            float axis = Mathf.Max(0, capsule.height * Mathf.Abs(scale.y) * .5f - radius);
            Vector3 center = origin + body.TransformVector(capsule.center);
            int count = Physics.CapsuleCastNonAlloc(center - Vector3.up * axis, center + Vector3.up * axis,
                radius * .97f, direction, obstacles, distance + .02f, Physics.AllLayers, QueryTriggerInteraction.Ignore);

            float allowed = distance;
            for (int i = 0; i < count; i++)
            {
                var obstacle = obstacles[i].collider;
                if (obstacle && !obstacle.transform.IsChildOf(body) && obstacles[i].normal.y < .65f)
                    allowed = Mathf.Min(allowed, Mathf.Max(0, obstacles[i].distance - .02f));
            }
            return allowed;
        }
    }
}
