using UnityEngine;

namespace Aberracion
{
    internal sealed class StrikeState : CharacterActionState
    {
        public override CharacterActionPhase Phase => CharacterActionPhase.Strike;
        public override float Duration(CharacterActionStateMachine actor) => Mathf.Max(.01f, actor.CurrentMove.strike);

        Vector3 direction;
        bool hitResolved;

        public override void Enter(CharacterActionStateMachine actor)
        {
            direction = actor.Facing.forward;
            hitResolved = false;
        }

        public override CharacterActionState Tick(
            CharacterActionStateMachine actor, CharacterActionInput input, float previousTime, float deltaTime)
        {
            float duration = Duration(actor);
            float previousProgress = Mathf.Clamp01(previousTime / duration);
            float step = actor.CurrentMove.advance *
                (Mathf.SmoothStep(0, 1, actor.PhaseProgress) - Mathf.SmoothStep(0, 1, previousProgress));
            actor.QueueAttack(input);
            actor.Motor.MoveBy(direction * step, deltaTime);

            bool finished = actor.Elapsed >= duration;
            if (!hitResolved && actor.PhaseProgress >= .75f)
            {
                var move = actor.CurrentMove;
                hitResolved = actor.Attack.ResolveImpact(move.damageScale, move.arc, move.hitsMultipleTargets, finished) != null;
            }

            if (!finished)
                return null;
            return actor.HasFollowUp ? (CharacterActionState)actor.Link : actor.Recovery;
        }
    }
}
