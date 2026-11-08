using UnityEngine;

namespace Aberracion
{
    internal sealed class RecoveryState : CharacterActionState
    {
        public override CharacterActionPhase Phase => CharacterActionPhase.Recovery;
        public override float Duration(CharacterActionStateMachine actor) => Mathf.Max(.01f, actor.CurrentMove.recovery);

        public override CharacterActionState Tick(
            CharacterActionStateMachine actor, CharacterActionInput input, float previousTime, float deltaTime)
        {
            actor.Motor.Move(Vector3.zero, deltaTime);
            if (actor.Elapsed < Duration(actor))
            {
                actor.QueueAttack(input);
                if (!actor.AttackQueued)
                    return null;

                actor.Attack.NotifyFinished();
                return actor.ContinueAttack();
            }

            actor.Attack.NotifyFinished();
            return actor.Drawn;
        }
    }
}
