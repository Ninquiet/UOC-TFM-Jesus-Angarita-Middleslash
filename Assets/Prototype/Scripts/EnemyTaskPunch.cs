using UnityEngine;

namespace Aberracion
{
    public sealed class EnemyTaskPunch : MonoBehaviour
    {
        public DamageableActor target;
        public EnemyTaskAnimation animationDriver;
        public EnemyFistVolume leftFist;
        public EnemyFistVolume rightFist;
        [Min(0)] public float damage = 18f;
        [Min(0)] public float turnSpeed = 240f;
        [Range(.05f, .95f)] public float impactFraction = .47f;

        readonly Collider[] contacts = new Collider[32];

        public bool HasAttackableTarget
        {
            get
            {
                if (!target || !target.IsAlive)
                    return false;

                return !target.TryGetComponent<PlayerCombatVitals>(out var vitals) || !vitals.IsInvulnerable;
            }
        }
        public float ImpactFraction => impactFraction;

        public bool TargetInRange(float range) =>
            HasAttackableTarget && TargetOffset().sqrMagnitude <= range * range;

        public void TurnTowardTarget(float deltaTime)
        {
            if (!HasAttackableTarget || deltaTime <= 0)
                return;

            Vector3 direction = TargetOffset();
            if (direction.sqrMagnitude > .001f)
            {
                Quaternion heading = Quaternion.LookRotation(direction, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, heading, turnSpeed * deltaTime);
            }
        }

        public void ResolveImpact(int punchIndex)
        {
            if (!HasAttackableTarget)
                return;

            bool left = punchIndex % 2 == 0;
            EnemyFistVolume fist = left ? leftFist : rightFist;
            if (!fist.IsWired || !animationDriver.SamplePunchImpact(left, impactFraction))
                return;

            int count = Physics.OverlapBoxNonAlloc(fist.Position, fist.HalfExtents, contacts,
                fist.Rotation, Physics.AllLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                if (contacts[i].GetComponentInParent<DamageableActor>() != target)
                    continue;

                var vitals = target.GetComponent<PlayerCombatVitals>();
                if (vitals)
                    vitals.ReceivePunch(damage, transform.position);
                else
                    target.ApplyDamage(damage);
                return;
            }
        }

        internal Vector3 TargetOffset()
        {
            Vector3 offset = target.transform.position - transform.position;
            offset.y = 0;
            return offset;
        }
    }
}
