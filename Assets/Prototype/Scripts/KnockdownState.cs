using UnityEngine;

namespace Aberracion
{
    internal sealed class KnockdownState : CharacterActionState
    {
        public override CharacterActionPhase Phase => CharacterActionPhase.Knockdown;
        public override float Duration(CharacterActionStateMachine actor) => Mathf.Max(.01f, actor.fallDuration);

        Vector3 direction;
        bool canGetUp;

        public void Configure(Vector3 pushDirection, bool survives)
        {
            direction = pushDirection;
            canGetUp = survives;
        }

        public override CharacterActionState Tick(
            CharacterActionStateMachine actor, CharacterActionInput input, float previousTime, float deltaTime)
        {
            float before = Mathf.Clamp01(previousTime / Duration(actor));
            float distance = actor.knockdownDistance * (Travel(actor.PhaseProgress) - Travel(before));
            actor.Motor.MoveBy(direction * distance, deltaTime);

            return canGetUp && actor.Elapsed >= Duration(actor) ? actor.GetUp : null;
        }

        static float Travel(float progress)
        {
            float remaining = 1 - progress;
            return 1 - remaining * remaining * remaining;
        }
    }
}
