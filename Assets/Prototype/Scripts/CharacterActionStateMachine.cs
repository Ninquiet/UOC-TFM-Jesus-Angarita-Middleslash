using UnityEngine;

namespace Aberracion
{
    [RequireComponent(typeof(CharacterMotor), typeof(MeleeAttack))]
    public sealed class CharacterActionStateMachine : MonoBehaviour
    {
        [Min(0)] public float sheathedSpeedFactor = 1.4f;
        [Range(0, 1)] public float drawnSpeedFactor = .6f;
        [Min(.01f)] public float sheathingDuration = 1.5f;
        [Min(.01f)] public float drawWindupDuration = .18f;
        [Min(0)] public float aimCorrectionWindow = .20f;
        [Range(0, 90)] public float maxAimCorrection = 20f;

        [Min(0)] public float knockdownDistance = .75f;
        [Min(.01f)] public float fallDuration = .36f;
        [Min(.01f)] public float getUpDuration = 1.5f;

        [SerializeField] GreatswordMove[] combo =
        {
            new GreatswordMove("Vertical Cut", .35f, .12f, .65f, .30f, .25f, 55, .56f, false,
                new Vector3(-105, 0, 0), new Vector3(62, 0, 0)),
            new GreatswordMove("Rising Sweep", .23f, .20f, .72f, .30f, .32f, 150, .72f, true,
                new Vector3(32, -75, 0), new Vector3(-72, 68, 0)),
            new GreatswordMove("Heavy Finisher", .82f, .26f, .92f, 0, .42f, 65, 3.2f, false,
                new Vector3(-115, 0, 0), new Vector3(75, 0, 0))
        };

        internal readonly SheathedState Sheathed = new SheathedState();
        internal readonly DrawnState Drawn = new DrawnState();
        internal readonly WindupState Windup = new WindupState();
        internal readonly StrikeState Strike = new StrikeState();
        internal readonly AttackLinkState Link = new AttackLinkState();
        internal readonly RecoveryState Recovery = new RecoveryState();
        internal readonly SheathingState Sheathing = new SheathingState();

        internal readonly KnockdownState Knockdown = new KnockdownState();
        internal readonly GetUpState GetUp = new GetUpState();

        CharacterActionState current;
        float elapsed;
        int moveIndex;
        bool attackQueued;

        public GreatswordMove CurrentMove => combo != null && moveIndex < combo.Length ? combo[moveIndex] : default;
        public int MoveIndex => moveIndex;
        public bool IsKnockedDown => current == Knockdown || current == GetUp;
        public bool IsGetUpProtected => current == GetUp && elapsed < GetUp.Duration(this) * .5f;
        public bool IsInvulnerable => current == Knockdown || IsGetUpProtected;
        public bool RecoverWithWeaponDrawn { get; private set; }
        public bool IsDrawAttack { get; private set; }
        public float WindupDuration => Mathf.Max(.01f, IsDrawAttack
            ? Mathf.Min(drawWindupDuration, CurrentMove.windup)
            : CurrentMove.windup);
        public bool HasFollowUp => combo != null && moveIndex + 1 < combo.Length;
        internal bool AttackQueued => attackQueued;
        public float LinkDelay => HasFollowUp ? Mathf.Clamp(CurrentMove.linkDelay, 0, CurrentMove.recovery) : 0;

        internal CharacterMotor Motor { get; private set; }
        internal MeleeAttack Attack { get; private set; }
        internal Transform Facing => transform;
        internal float Elapsed => elapsed;

        public CharacterActionPhase Phase => current?.Phase ?? CharacterActionPhase.Sheathed;
        public float PhaseProgress => current != null && current.Duration(this) > 0
            ? Mathf.Clamp01(elapsed / current.Duration(this))
            : 0;

        void Awake()
        {
            Motor = GetComponent<CharacterMotor>();
            Attack = GetComponent<MeleeAttack>();
        }

        void OnEnable() => ChangeState(Sheathed);

        void OnDisable()
        {
            if (current == Windup || current == Strike || current == Link || current == Recovery)
                Attack.NotifyFinished();
        }

        public void Tick(CharacterActionInput input, float deltaTime)
        {
            if (!isActiveAndEnabled)
                return;

            float previousTime = elapsed;
            elapsed += deltaTime;
            var next = current.Tick(this, input, previousTime, deltaTime);
            if (next != null)
                ChangeState(next);
        }

        public void KnockDownFrom(Vector3 source, bool survives)
        {
            if (!isActiveAndEnabled || IsInvulnerable)
                return;

            if (current == Windup || current == Strike || current == Link || current == Recovery)
                Attack.NotifyFinished();

            if (current != GetUp)
                RecoverWithWeaponDrawn = current != Sheathed;
            attackQueued = false;

            Vector3 away = Vector3.ProjectOnPlane(transform.position - source, Vector3.up);
            Vector3 direction = away.sqrMagnitude > .001f ? away.normalized : -transform.forward;
            Motor.Face(-direction);
            Knockdown.Configure(direction, survives);
            ChangeState(Knockdown);
        }

        public void ResetKnockdown()
        {
            if (IsKnockedDown)
                ChangeState(Sheathed);
        }

        internal CharacterActionState StartAttack(Vector3 aim)
        {
            if (combo == null || combo.Length == 0 || !Attack.isActiveAndEnabled)
                return null;

            IsDrawAttack = current == Sheathed;
            moveIndex = 0;
            attackQueued = false;
            Motor.Face(aim);
            Attack.NotifyStarted();
            return Windup;
        }

        internal void QueueAttack(CharacterActionInput input)
        {
            if (!HasFollowUp || !input.AttackPressed || attackQueued)
                return;

            attackQueued = true;
        }

        internal CharacterActionState ContinueAttack()
        {
            moveIndex++;
            IsDrawAttack = false;
            attackQueued = false;
            Attack.NotifyStarted();
            return Windup;
        }

        internal void Move(Vector3 direction, float deltaTime, float speedFactor)
        {
            Motor.Move(direction, deltaTime, speedFactor);
            Motor.Face(direction);
        }

        void ChangeState(CharacterActionState next)
        {
            current?.Exit(this);
            current = next;
            elapsed = 0;
            current.Enter(this);
        }
    }
}

