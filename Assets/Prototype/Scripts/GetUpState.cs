using UnityEngine;

namespace Aberracion
{
    internal sealed class GetUpState : CharacterActionState
    {
        public override CharacterActionPhase Phase => CharacterActionPhase.GetUp;
        public override float Duration(CharacterActionStateMachine actor) => Mathf.Max(.01f, actor.getUpDuration);

        public override CharacterActionState Tick(
            CharacterActionStateMachine actor, CharacterActionInput input, float previousTime, float deltaTime)
        {
            actor.Motor.Move(Vector3.zero, deltaTime);
            if (actor.Elapsed < Duration(actor))
                return null;

            return actor.RecoverWithWeaponDrawn ? actor.Drawn : (CharacterActionState)actor.Sheathed;
        }
    }
}
