using UnityEngine;

namespace Aberracion
{
    public sealed class SwordSwingVisual : MonoBehaviour
    {
        public CharacterActionStateMachine actions;
        public Transform pivot;

        [SerializeField] Vector3 backPosition = new Vector3(.42f, .43f, -.48f);
        [SerializeField] Vector3 shoulderPath = new Vector3(.92f, 1.55f, -.38f);
        [SerializeField] Vector3 offensiveAngles = new Vector3(-25, -150, 0);

        static readonly Quaternion BackRotation = Quaternion.LookRotation(new Vector3(-.48f, .88f, 0));
        const float FinisherGuardArrival = .8f;

        Vector3 handPosition;
        Pose windupStart;
        CharacterActionPhase previousPhase;
        bool hasRenderedPose;

        Pose BackPose => new Pose(backPosition, BackRotation);
        Pose GuardPose => new Pose(handPosition, Quaternion.Euler(offensiveAngles));

        void Awake() => handPosition = pivot.localPosition;

        void LateUpdate()
        {
            var phase = actions.Phase;
            if (phase == CharacterActionPhase.Windup && previousPhase != phase)
                windupStart = CaptureWindupStart();

            Pose pose = GetPose(phase, actions.PhaseProgress);
            pivot.SetLocalPositionAndRotation(pose.position, pose.rotation);
            previousPhase = phase;
            hasRenderedPose = true;
        }

        Pose CaptureWindupStart()
        {
            if (actions.IsDrawAttack)
                return BackPose;
            if (!hasRenderedPose || (previousPhase == CharacterActionPhase.Recovery && actions.MoveIndex == 0))
                return GuardPose;

            return new Pose(pivot.localPosition, pivot.localRotation);
        }

        Pose GetPose(CharacterActionPhase phase, float progress)
        {
            var move = actions.CurrentMove;
            switch (phase)
            {
                case CharacterActionPhase.Sheathed:
                    return BackPose;
                case CharacterActionPhase.Windup:
                    return Blend(windupStart, HandPose(move.raisedAngles), progress, actions.IsDrawAttack);
                case CharacterActionPhase.Strike:
                    return HandPose(Vector3.Lerp(move.raisedAngles, move.impactAngles, Mathf.SmoothStep(0, 1, progress)));
                case CharacterActionPhase.AttackLink:
                    return HandPose(move.impactAngles);
                case CharacterActionPhase.Recovery:
                    return RecoveryPose(move, progress);
                case CharacterActionPhase.Sheathing:
                    return Blend(GuardPose, BackPose, progress, true);
                default:
                    return GuardPose;
            }
        }

        Pose HandPose(Vector3 angles) => new Pose(handPosition, Quaternion.Euler(angles));

        Pose RecoveryPose(GreatswordMove move, float progress)
        {
            float guardArrival = actions.HasFollowUp ? 1f : FinisherGuardArrival;
            float settle = Mathf.InverseLerp(0, guardArrival, progress);
            return Blend(HandPose(move.impactAngles), GuardPose, settle);
        }

        Pose Blend(Pose from, Pose to, float progress, bool passShoulder = false)
        {
            float t = Mathf.SmoothStep(0, 1, progress);
            Vector3 position = passShoulder
                ? Vector3.Lerp(Vector3.Lerp(from.position, shoulderPath, t),
                    Vector3.Lerp(shoulderPath, to.position, t), t)
                : Vector3.Lerp(from.position, to.position, t);

            return new Pose(position, Quaternion.Slerp(from.rotation, to.rotation, t));
        }
    }
}
