using UnityEngine;

namespace Aberracion
{
    public sealed class FocusState : MonoBehaviour
    {
        [Header("Experimental tuning")]
        [Min(.01f)] public float progressPerHit = .11f;
        [Min(0)] public float decayDelay = 3.4f;
        [Min(0)] public float decaySpeed = .3f;
        [Range(0, 1)] public float entryThreshold = .7f;
        [Min(1)] public int minimumHits = 7;
        [Range(0, 1)] public float entryChance = .45f;
        [Min(1)] public float damageMultiplier = 2.2f;
        [Min(.1f)] public float exitDuration = 2.5f;
        public SessionTelemetry telemetry;

        public DamageableActor Target => IsHyper ? hyperTarget : buildupTarget;
        public float Progress => IsHyper ? HyperStrength : buildupProgress;
        public float HyperStrength { get; private set; }
        public bool IsHyper { get; private set; }
        public bool IsExiting { get; private set; }

        DamageableActor hyperTarget;
        DamageableActor buildupTarget;
        float buildupProgress;
        int hits;
        float idleTime;
        bool hyperfocusQueued;

        void Update() => Tick(Time.deltaTime);

        public float DamageFor(DamageableActor target)
        {
            return IsHyper && target == hyperTarget
                ? Mathf.Lerp(1, damageMultiplier, HyperStrength)
                : 1;
        }

        public void Hit(DamageableActor target, float randomSample)
        {
            if (IsHyper && !IsExiting && target == hyperTarget)
            {
                idleTime = 0;
                return;
            }

            DamageableActor previousTarget = buildupTarget ? buildupTarget : Target;
            if (IsHyper && !IsExiting)
                BeginExit();

            if (target != buildupTarget)
            {
                if (target != previousTarget)
                    telemetry?.Record("objetivo_cambio", target.actorId,
                        (previousTarget ? previousTarget.actorId : "none") + (IsHyper ? ":hyper" : ":normal"));

                ClearBuildup();
                buildupTarget = target;
            }

            idleTime = 0;
            hits++;
            buildupProgress = Mathf.Clamp01(buildupProgress + progressPerHit);
            if (MeetsEntryConditions && randomSample < entryChance)
                hyperfocusQueued = true;

            TryEnterHyperfocus();
        }

        public void Tick(float deltaTime)
        {
            float previousIdleTime = idleTime;
            idleTime += deltaTime;

            if (IsHyper && !IsExiting)
            {
                if (!hyperTarget || !hyperTarget.IsAlive)
                    BeginExit();
                else if (idleTime >= decayDelay)
                {
                    BeginExit();
                    deltaTime = Mathf.Min(deltaTime, idleTime - decayDelay);
                }
                else
                    return;
            }

            UpdateBuildup(Mathf.Min(deltaTime, Mathf.Max(0, idleTime - Mathf.Max(previousIdleTime, decayDelay))));
            if (IsExiting)
                FadeHyperfocus(deltaTime);
        }

        bool MeetsEntryConditions => buildupProgress >= entryThreshold && hits >= minimumHits;

        void TryEnterHyperfocus()
        {
            if (IsHyper || !hyperfocusQueued || !buildupTarget || !buildupTarget.IsAlive)
                return;

            hyperTarget = buildupTarget;
            IsHyper = true;
            HyperStrength = 1;
            idleTime = 0;
            hyperfocusQueued = false;
            telemetry?.Record("hiperfoco_inicio", hyperTarget.actorId);
        }

        void BeginExit()
        {
            IsExiting = true;
            ClearBuildup();
        }

        void UpdateBuildup(float decayTime)
        {
            if (!buildupTarget || !buildupTarget.IsAlive)
            {
                ClearBuildup();
                return;
            }

            buildupProgress = Mathf.MoveTowards(buildupProgress, 0, decaySpeed * decayTime);
            if (buildupProgress == 0)
                hits = 0;
            if (!MeetsEntryConditions)
                hyperfocusQueued = false;
        }

        void FadeHyperfocus(float deltaTime)
        {
            HyperStrength = Mathf.MoveTowards(HyperStrength, 0, deltaTime / Mathf.Max(.1f, exitDuration));
            if (HyperStrength > 0)
                return;

            EndHyperfocus("gradual_exit");
            TryEnterHyperfocus();
        }

        public void ResetState()
        {
            EndHyperfocus("reset");
            ClearBuildup();
            idleTime = 0;
        }

        void EndHyperfocus(string reason)
        {
            if (IsHyper)
                telemetry?.Record("hiperfoco_fin", hyperTarget ? hyperTarget.actorId : "", reason);

            IsHyper = false;
            IsExiting = false;
            HyperStrength = 0;
            hyperTarget = null;
        }

        void ClearBuildup()
        {
            buildupTarget = null;
            buildupProgress = 0;
            hits = 0;
            hyperfocusQueued = false;
        }

        void OnDisable() => ResetState();
    }
}
