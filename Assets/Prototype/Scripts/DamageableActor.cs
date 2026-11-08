using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Aberracion
{
    public enum CombatTeam { Player, Enemy }

    public sealed class DamageableActor : MonoBehaviour
    {
        [FormerlySerializedAs("enemyId")] public string actorId = "enemy";
        public CombatTeam team = CombatTeam.Enemy;
        [Min(1)] public float maxHealth = 1800f;

        public float Health { get; private set; }
        public bool IsAlive => Health > 0;
        public Func<Vector3, bool> HitAllowed { private get; set; }
        public bool CanBeHitFrom(Vector3 source) => IsAlive && (HitAllowed?.Invoke(source) ?? true);
        public event Action<float> Damaged;
        public event Action Restored;
        public event Action HitBlocked;

        internal void NotifyHitBlocked() => HitBlocked?.Invoke();

        void Awake() => Health = maxHealth;

        public float ApplyDamage(float amount)
        {
            float applied = Mathf.Clamp(amount, 0, Health);
            if (applied <= 0)
                return 0;

            Health -= applied;
            Damaged?.Invoke(applied);
            return applied;
        }

        public void RestoreHealth()
        {
            Health = maxHealth;
            Restored?.Invoke();
        }
    }
}
