namespace Aberracion
{
    internal sealed class SheathedState : CharacterActionState
    {
        public override CharacterActionPhase Phase => CharacterActionPhase.Sheathed;

        public override CharacterActionState Tick(
            CharacterActionStateMachine actor, CharacterActionInput input, float previousTime, float deltaTime)
        {
            if (input.AttackPressed)
                return actor.StartAttack(input.Aim);

            actor.Move(input.Movement, deltaTime, actor.sheathedSpeedFactor);
            return null;
        }
    }
}
