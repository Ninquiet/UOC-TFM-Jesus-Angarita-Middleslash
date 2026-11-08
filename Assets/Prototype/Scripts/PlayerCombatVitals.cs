using UnityEngine;

namespace Aberracion
{
    public sealed class PlayerCombatVitals : MonoBehaviour
    {
        public DamageableActor actor;
        public CharacterActionStateMachine actions;
        public bool IsInvulnerable => actions.IsInvulnerable;
        public float HitPulse { get; private set; }

        void OnEnable()
        {
            actor.Damaged += OnHit;
            actor.Restored += OnRestored;
        }

        void OnDisable()
        {
            actor.Damaged -= OnHit;
            actor.Restored -= OnRestored;
        }

        void Update() => HitPulse = Mathf.MoveTowards(HitPulse, 0, Time.deltaTime * 3);

        public void ReceivePunch(float damage, Vector3 source)
        {
            if (IsInvulnerable || actor.ApplyDamage(damage) <= 0)
                return;

            actions.KnockDownFrom(source, actor.IsAlive);
        }

        void OnHit(float damage) => HitPulse = 1;

        void OnRestored()
        {
            HitPulse = 0;
            actions.ResetKnockdown();
        }
    }
}
