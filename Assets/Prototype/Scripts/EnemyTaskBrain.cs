using UnityEngine;

namespace Aberracion
{
    public enum EnemyTaskPhase { Idle, Windup, Punching, Exhausted, Staggered, Recovering, Defeated, Jumping, Patrolling }

    [RequireComponent(typeof(CapsuleCollider))]
    public sealed class EnemyTaskBrain : MonoBehaviour
    {
        public DamageableActor actor;
        public EnemyTaskPunch punch;
        public Collider patrolFloor;
        [Min(0)] public float patrolRadius = 5f;
        [Min(0)] public float activationRange = 10f;
        [Min(0)] public float punchStartRange = 4.65f;
        [Min(.01f)] public float jumpInterval = 3f;
        [Range(0, 4)] public float jumpDistance = 4f;
        [Min(.01f)] public float jumpDuration = .72f;
        [Min(0)] public float jumpHeight = .85f;
        [Min(1)] public int punchesPerBurst = 4;
        [Min(.01f)] public float windupDuration = .45f;
        [Min(.01f)] public float punchDuration = .56f;
        [Min(.01f)] public float exhaustedDuration = 4.25f;
        [Min(.01f)] public float recoveryAfterHit = 3f;
        [Min(.01f)] public float recoveryDuration = .58f;

        public EnemyTaskPhase Phase { get; private set; }
        public int PunchIndex { get; private set; }
        public float Elapsed { get; private set; }

        [Range(0, .45f)] public float punchAimFraction = .30f;

        bool impactResolved;
        EnemyTaskJump jump;
        EnemyPatrolRoute patrol;
        float jumpCooldown;
        bool patrolJump;

        float PunchRange => Mathf.Min(punchStartRange, activationRange);

        void Awake()
        {
            jump = new EnemyTaskJump(GetComponent<CapsuleCollider>());
            patrol = new EnemyPatrolRoute(transform.position);
            jumpCooldown = jumpInterval;
        }

        void OnEnable()
        {
            actor.Damaged += OnHit;
            actor.Restored += ResetBurst;
        }

        void OnDisable()
        {
            actor.Damaged -= OnHit;
            actor.Restored -= ResetBurst;
            if (Phase == EnemyTaskPhase.Jumping)
                ResetBurst();
        }

        void Update() => Tick(Time.deltaTime);

        public void Tick(float deltaTime)
        {
            Elapsed += deltaTime;
            jumpCooldown = Mathf.Max(0, jumpCooldown - deltaTime);
            switch (Phase)
            {
                case EnemyTaskPhase.Idle:
                case EnemyTaskPhase.Patrolling:
                    ChooseNextAction();
                    break;
                case EnemyTaskPhase.Jumping:
                    AdvanceJump(deltaTime);
                    break;
                case EnemyTaskPhase.Windup:
                    if (!punch.TargetInRange(activationRange))
                    {
                        Enter(EnemyTaskPhase.Idle);
                        break;
                    }
                    punch.TurnTowardTarget(deltaTime);
                    if (Elapsed >= windupDuration)
                        StartPunch();
                    break;
                case EnemyTaskPhase.Punching:
                    AdvancePunch(deltaTime);
                    break;
                case EnemyTaskPhase.Exhausted:
                    if (Elapsed >= exhaustedDuration)
                        Enter(EnemyTaskPhase.Recovering);
                    break;
                case EnemyTaskPhase.Staggered:
                    if (Elapsed >= recoveryAfterHit)
                        Enter(EnemyTaskPhase.Recovering);
                    break;
                case EnemyTaskPhase.Recovering:
                    if (Elapsed >= recoveryDuration)
                        Enter(EnemyTaskPhase.Idle);
                    break;
            }
        }

        void ChooseNextAction()
        {
            if (punch.TargetInRange(PunchRange))
            {
                patrol.Clear();
                PunchIndex = 0;
                Enter(EnemyTaskPhase.Windup);
                return;
            }

            bool pursuing = punch.TargetInRange(activationRange);
            var waitingPhase = pursuing ? EnemyTaskPhase.Idle : EnemyTaskPhase.Patrolling;
            if (Phase != waitingPhase)
                Enter(waitingPhase);
            if (jumpCooldown > 0 || jumpDistance <= 0)
                return;

            if (pursuing)
            {
                patrol.Clear();
                Vector3 offset = punch.TargetOffset();
                float distance = Mathf.Clamp(offset.magnitude - PunchRange * .9f, 0, jumpDistance);
                StartJump(offset.normalized * distance, false);
            }
            else if (patrolRadius > 0)
            {
                Vector3 offset = patrol.OffsetFrom(transform.position, patrolRadius, patrolFloor);
                StartJump(Vector3.ClampMagnitude(offset, jumpDistance), true);
            }
        }

        void AdvancePunch(float deltaTime)
        {
            float aimTimeRemaining = punchDuration * punchAimFraction - (Elapsed - deltaTime);
            punch.TurnTowardTarget(Mathf.Min(deltaTime, aimTimeRemaining));

            if (!impactResolved && Elapsed >= punchDuration * punch.ImpactFraction)
            {
                impactResolved = true;
                if (punch.TargetInRange(activationRange))
                    punch.ResolveImpact(PunchIndex);
            }

            if (Elapsed < punchDuration)
                return;

            PunchIndex++;
            if (PunchIndex < punchesPerBurst && punch.TargetInRange(activationRange))
                StartPunch();
            else
                Enter(EnemyTaskPhase.Exhausted);
        }

        void StartJump(Vector3 displacement, bool isPatrolJump)
        {
            if (displacement.sqrMagnitude < .001f)
            {
                patrol.Clear();
                return;
            }
            jump.Begin(displacement);
            patrolJump = isPatrolJump;
            jumpCooldown = jumpInterval;
            Enter(EnemyTaskPhase.Jumping);
        }

        void AdvanceJump(float deltaTime)
        {
            float duration = Mathf.Max(.01f, jumpDuration);
            jump.TurnTowardHeading(punch.turnSpeed * deltaTime);
            jump.Advance((Elapsed - deltaTime) / duration, Elapsed / duration, jumpHeight);
            if (Elapsed >= jumpDuration)
            {
                if (patrolJump && jump.WasBlocked)
                    patrol.Clear();
                Enter(patrolJump ? EnemyTaskPhase.Patrolling : EnemyTaskPhase.Idle);
            }
        }

        void StartPunch()
        {
            impactResolved = false;
            Enter(EnemyTaskPhase.Punching);
        }

        void OnHit(float damage) => Enter(actor.IsAlive ? EnemyTaskPhase.Staggered : EnemyTaskPhase.Defeated);

        void ResetBurst()
        {
            PunchIndex = 0;
            patrol.Clear();
            jumpCooldown = jumpInterval;
            Enter(EnemyTaskPhase.Idle);
        }

        void Enter(EnemyTaskPhase phase)
        {
            if (Phase == EnemyTaskPhase.Jumping)
                jump.Land();
            Phase = phase;
            Elapsed = 0;
        }

        void OnDrawGizmosSelected()
        {
            const int segments = 48;
            Vector3 center = transform.position;
            if (TryGetComponent(out CapsuleCollider capsule))
                center.y = capsule.bounds.min.y + .03f;
            Gizmos.color = new Color(1f, .65f, .2f);
            Vector3 previous = center + Vector3.right * activationRange;
            for (int i = 1; i <= segments; i++)
            {
                float angle = i * 2 * Mathf.PI / segments;
                Vector3 next = center + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * activationRange;
                Gizmos.DrawLine(previous, next);
                previous = next;
            }
        }
    }
}
