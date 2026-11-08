namespace Aberracion
{
    internal sealed class SheathingState : CharacterActionState
    {
        public override CharacterActionPhase Phase => CharacterActionPhase.Sheathing;
        public override float Duration(CharacterActionStateMachine actor) => actor.sheathingDuration;

        public override CharacterActionState Tick(
            CharacterActionStateMachine actor, CharacterActionInput input, float previousTime, float deltaTime)
        {
            actor.Move(input.Movement, deltaTime, actor.drawnSpeedFactor / 3f);
            return actor.Elapsed >= Duration(actor) ? actor.Sheathed : null;
        }
    }
}
