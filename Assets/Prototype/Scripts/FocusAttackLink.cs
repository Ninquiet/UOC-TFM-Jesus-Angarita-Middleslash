using UnityEngine;

namespace Aberracion
{
    public sealed class FocusAttackLink : MonoBehaviour
    {
        public MeleeAttack attack;
        public FocusState focus;

        void OnEnable()
        {
            attack.DamageMultiplier = focus.DamageFor;
            attack.PreferredTarget = GetFocusTarget;
            attack.PrimaryHit += AddFocus;
        }

        void OnDisable()
        {
            attack.DamageMultiplier = null;
            attack.PreferredTarget = null;
            attack.PrimaryHit -= AddFocus;
        }

        DamageableActor GetFocusTarget() => focus.Target;

        void AddFocus(DamageableActor target) => focus.Hit(target, Random.value);
    }
}
