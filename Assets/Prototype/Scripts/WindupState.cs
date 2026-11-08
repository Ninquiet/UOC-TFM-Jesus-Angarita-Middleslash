using UnityEngine;

namespace Aberracion
{
    internal sealed class WindupState : CharacterActionState
    {
        public override CharacterActionPhase Phase => CharacterActionPhase.Windup;
        public override float Duration(CharacterActionStateMachine actor) => actor.WindupDuration;

        Vector3 initialDirection;

        public override void Enter(CharacterActionStateMachine actor)
        {
            initialDirection = actor.Facing.forward;
        }

        public override CharacterActionState Tick(
            CharacterActionStateMachine actor, CharacterActionInput input, float previousTime, float deltaTime)
        {
            actor.Motor.Move(Vector3.zero, deltaTime);
            Vector3 aim = Vector3.ProjectOnPlane(input.Aim, Vector3.up);
            if (actor.Elapsed <= actor.aimCorrectionWindow && aim.sqrMagnitude > .01f)
            {
                float angle = Vector3.SignedAngle(initialDirection, aim, Vector3.up);
                float correction = Mathf.Clamp(angle, -actor.maxAimCorrection, actor.maxAimCorrection);
                actor.Motor.Face(Quaternion.AngleAxis(correction, Vector3.up) * initialDirection);
            }

            return actor.Elapsed >= Duration(actor) ? actor.Strike : null;
        }
    }
}
