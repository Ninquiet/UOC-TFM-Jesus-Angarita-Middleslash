namespace Aberracion
{
    public enum CharacterActionPhase
    {
        Sheathed,
        Drawn,
        Windup,
        Strike,
        Recovery,
        Sheathing,
        Knockdown,
        GetUp,
        AttackLink
    }

    internal abstract class CharacterActionState
    {
        public abstract CharacterActionPhase Phase { get; }
        public virtual float Duration(CharacterActionStateMachine actor) => 0;
        public virtual void Enter(CharacterActionStateMachine actor) { }
        public virtual void Exit(CharacterActionStateMachine actor) { }
        public abstract CharacterActionState Tick(
            CharacterActionStateMachine actor, CharacterActionInput input, float previousTime, float deltaTime);
    }
}
