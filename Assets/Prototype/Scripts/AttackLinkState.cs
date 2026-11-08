using UnityEngine;

namespace Aberracion
{
    internal sealed class AttackLinkState : CharacterActionState
    {
        public override CharacterActionPhase Phase => CharacterActionPhase.AttackLink;
        public override float Duration(CharacterActionStateMachine actor) => actor.LinkDelay;

        public override CharacterActionState Tick(
            CharacterActionStateMachine actor, CharacterActionInput input, float previousTime, float deltaTime)
        {
            actor.Motor.Move(Vector3.zero, deltaTime);
            actor.QueueAttack(input);
            if (actor.Elapsed < Duration(actor))
                return null;

            if (!actor.AttackQueued)
                return actor.Recovery;

            actor.Attack.NotifyFinished();
            return actor.ContinueAttack();
        }
    }
}
