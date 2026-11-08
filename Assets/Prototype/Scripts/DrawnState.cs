namespace Aberracion
{
    internal sealed class DrawnState : CharacterActionState
    {
        public override CharacterActionPhase Phase => CharacterActionPhase.Drawn;

        public override CharacterActionState Tick(
            CharacterActionStateMachine actor, CharacterActionInput input, float previousTime, float deltaTime)
        {
            if (input.AttackPressed)
                return actor.StartAttack(input.Aim);
            if (input.SheathePressed)
                return actor.Sheathing;

            actor.Move(input.Movement, deltaTime, actor.drawnSpeedFactor);
            return null;
        }
    }
}
