using UnityEngine;

namespace Aberracion
{
    public readonly struct CharacterActionInput
    {
        public readonly Vector3 Movement;
        public readonly Vector3 Aim;
        public readonly bool AttackPressed;
        public readonly bool SheathePressed;

        public CharacterActionInput(Vector3 movement, Vector3 aim, bool attackPressed, bool sheathePressed)
        {
            Movement = movement;
            Aim = aim;
            AttackPressed = attackPressed;
            SheathePressed = sheathePressed;
        }
    }
}
