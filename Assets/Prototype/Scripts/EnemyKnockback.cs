using UnityEngine;

namespace Aberracion
{
    [RequireComponent(typeof(CapsuleCollider))]
    public sealed class EnemyKnockback : MonoBehaviour
    {
        [Min(0)] public float distance = .35f;
        [Min(.01f)] public float duration = .16f;

        CapsuleTravel travel;
        DamageableActor actor;
        Vector3 startingPosition;
        Vector3 direction;
        float remaining;

        void Awake()
        {
            travel = new CapsuleTravel(GetComponent<CapsuleCollider>());
            actor = GetComponent<DamageableActor>();
            startingPosition = transform.localPosition;
        }

        void OnEnable() => actor.Restored += ResetPosition;
        void OnDisable() => actor.Restored -= ResetPosition;

        void ResetPosition()
        {
            remaining = 0;
            transform.localPosition = startingPosition;
        }

        public void PushFrom(Vector3 source)
        {
            Vector3 offset = transform.position - source;
            offset.y = 0;
            if (offset.sqrMagnitude < .001f)
                return;

            direction = offset.normalized;
            remaining = distance;
        }

        void LateUpdate()
        {
            if (remaining <= 0)
                return;

            float step = Mathf.Min(remaining, distance * Time.deltaTime / Mathf.Max(.01f, duration));
            float allowed = travel.Limit(transform.position, direction, step);

            transform.position += direction * allowed;
            remaining = allowed < step ? 0 : remaining - step;
        }

    }
}
