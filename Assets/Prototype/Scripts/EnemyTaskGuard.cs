using UnityEngine;

namespace Aberracion
{
    public sealed class EnemyTaskGuard : MonoBehaviour
    {
        public DamageableActor actor;
        public EnemyTaskBrain brain;
        [Range(0, 1)] public float frontProtection = .9f;

        void OnEnable() => actor.HitAllowed = AllowsHit;
        void OnDisable() => actor.HitAllowed = null;

        bool AllowsHit(Vector3 source)
        {
            var phase = brain.Phase;
            if (phase == EnemyTaskPhase.Windup || phase == EnemyTaskPhase.Punching)
                return false;
            if (phase != EnemyTaskPhase.Exhausted)
                return true;

            Vector3 direction = source - transform.position;
            direction.y = 0;
            return Vector3.Dot(transform.forward, direction.normalized) < frontProtection;
        }
    }
}
