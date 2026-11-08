using UnityEngine;

namespace Aberracion
{
    internal sealed class EnemyTaskJump
    {
        readonly Transform body;
        readonly CapsuleTravel travel;
        Vector3 offset;
        float groundY;

        public bool WasBlocked { get; private set; }

        public EnemyTaskJump(CapsuleCollider capsule)
        {
            body = capsule.transform;
            travel = new CapsuleTravel(capsule);
        }

        public void Begin(Vector3 displacement)
        {
            offset = displacement;
            groundY = body.position.y;
            WasBlocked = false;
        }

        public void Advance(float previousTime, float currentTime, float height)
        {
            // Match the anticipation and landing keys in TaskJump.
            float before = Mathf.InverseLerp(.2f, .8f, previousTime);
            float progress = Mathf.InverseLerp(.2f, .8f, currentTime);
            Vector3 ground = body.position;
            ground.y = groundY;
            Vector3 step = offset * (progress - before);
            if (step.sqrMagnitude > 0)
            {
                float distance = travel.Limit(ground, step.normalized, step.magnitude);
                WasBlocked |= distance < step.magnitude - .001f;
                ground += step.normalized * distance;
            }
            body.position = ground + Vector3.up * (4 * height * progress * (1 - progress));
        }

        public void TurnTowardHeading(float degrees)
        {
            Quaternion heading = Quaternion.LookRotation(offset, Vector3.up);
            body.rotation = Quaternion.RotateTowards(body.rotation, heading, degrees);
        }

        public void Land()
        {
            Vector3 position = body.position;
            position.y = groundY;
            body.position = position;
        }
    }
}
