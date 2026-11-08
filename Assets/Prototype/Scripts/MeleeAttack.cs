using System;
using System.Collections.Generic;
using UnityEngine;

namespace Aberracion
{
    public sealed class MeleeAttack : MonoBehaviour
    {
        public CombatTeam targetTeam = CombatTeam.Enemy;
        [Min(.1f)] public float reach = 3f;
        [Range(10, 180)] public float arc = 55f;
        [Min(0)] public float baseDamage = 25f;

        public Func<DamageableActor, float> DamageMultiplier { private get; set; }
        public Func<DamageableActor> PreferredTarget { private get; set; }
        public event Action Started;
        public event Action Finished;
        public event Action<DamageableActor, float> Hit;
        public event Action<DamageableActor> PrimaryHit;

        readonly Collider[] nearbyColliders = new Collider[32];
        readonly List<DamageableActor> targets = new List<DamageableActor>(32);

        readonly List<DamageableActor> blockedTargets = new List<DamageableActor>(8);

        internal void NotifyStarted() => Started?.Invoke();
        internal void NotifyFinished() => Finished?.Invoke();

        public DamageableActor ResolveImpact(float damageScale = 1f, float? attackArc = null, bool hitMultiple = false, bool reportBlocked = true)
        {
            if (!isActiveAndEnabled)
                return null;

            CollectTargets(attackArc ?? arc);
            DamageableActor primary = SelectPrimaryTarget(hitMultiple);
            if (!primary)
            {
                if (reportBlocked)
                {
                    foreach (var blocked in blockedTargets)
                        blocked.NotifyHitBlocked();
                }
                return null;
            }

            float primaryDamage = ApplyHit(primary, damageScale);
            if (hitMultiple)
            {
                foreach (var target in targets)
                {
                    if (target != primary)
                        ApplyHit(target, damageScale);
                }
            }

            if (primaryDamage > 0)
                PrimaryHit?.Invoke(primary);

            return primary;
        }

        void CollectTargets(float attackArc)
        {
            targets.Clear();
            blockedTargets.Clear();
            Physics.SyncTransforms();
            Vector3 origin = transform.position + Vector3.up;
            int count = Physics.OverlapSphereNonAlloc(origin, reach, nearbyColliders, Physics.AllLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var collider = nearbyColliders[i];
                var actor = collider.GetComponentInParent<DamageableActor>();
                if (!actor || actor.transform == transform || actor.team != targetTeam || !actor.IsAlive)
                    continue;

                Vector3 offset = collider.bounds.center - origin;
                float forwardDistance = Mathf.Clamp(Vector3.Dot(offset, transform.forward), 0, reach);
                Vector3 contact = collider.ClosestPoint(origin + transform.forward * forwardDistance) - origin;
                contact.y = 0;
                if (Vector3.Angle(transform.forward, contact) > attackArc * .5f)
                    continue;

                var candidates = actor.CanBeHitFrom(transform.position) ? targets : blockedTargets;
                if (!candidates.Contains(actor))
                    candidates.Add(actor);
            }
        }

        DamageableActor SelectPrimaryTarget(bool hitMultiple)
        {
            DamageableActor preferred = hitMultiple ? PreferredTarget?.Invoke() : null;
            if (preferred && targets.Contains(preferred) && AngleTo(preferred) <= arc * .5f)
                return preferred;

            DamageableActor closest = null;
            DamageableActor closestFront = null;
            float closestDistance = float.PositiveInfinity;
            float closestFrontDistance = float.PositiveInfinity;
            foreach (var target in targets)
            {
                float distance = OffsetTo(target).sqrMagnitude;
                if (distance < closestDistance)
                {
                    closest = target;
                    closestDistance = distance;
                }

                if (hitMultiple && distance < closestFrontDistance && AngleTo(target) <= arc * .5f)
                {
                    closestFront = target;
                    closestFrontDistance = distance;
                }
            }

            return closestFront ? closestFront : closest;
        }

        float ApplyHit(DamageableActor target, float damageScale)
        {
            float multiplier = DamageMultiplier?.Invoke(target) ?? 1f;
            float damage = target.ApplyDamage(baseDamage * damageScale * multiplier);
            if (damage > 0)
            {
                target.GetComponent<EnemyKnockback>()?.PushFrom(transform.position);
                Hit?.Invoke(target, damage);
            }
            return damage;
        }

        float AngleTo(DamageableActor target) => Vector3.Angle(transform.forward, OffsetTo(target));

        Vector3 OffsetTo(DamageableActor target)
        {
            Vector3 offset = target.transform.position - transform.position;
            offset.y = 0;
            return offset;
        }
    }
}
