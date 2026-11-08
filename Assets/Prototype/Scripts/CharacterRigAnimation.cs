using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Aberracion
{
    public sealed class CharacterRigAnimation : MonoBehaviour
    {
        // CutRecovery and SweepRecovery share their first fifth with the combo link.
        const float ComboBranchProgress = .2f;

        [Serializable]
        public struct AttackClips
        {
            public AnimationClip windup;
            public AnimationClip strike;
            public AnimationClip recovery;
        }

        public CharacterActionStateMachine actions;
        public CharacterMotor motor;
        public Animator animator;
        public AnimationClip defensiveIdle;
        public AnimationClip defensiveWalk;
        public AnimationClip defensiveRun;
        public AnimationClip offensiveIdle;
        public AnimationClip offensiveWalk;
        public AnimationClip drawWindup;
        public AnimationClip sheathe;
        public AnimationClip sheatheWalk;
        public AnimationClip defensiveFall;
        public AnimationClip defensiveRise;
        public AnimationClip offensiveFall;
        public AnimationClip offensiveRise;
        public AttackClips[] attacks;

        [Min(.01f)] public float transitionDuration = .06f;
        [Min(.1f)] public float defensiveCycleDistance = 2.25f;
        [Min(.1f)] public float offensiveCycleDistance = 1.55f;

        readonly AnimationClipPlayable[] inputs = new AnimationClipPlayable[2];
        PlayableGraph graph;
        AnimationMixerPlayable mixer;
        AnimationClip activeClip;
        int activeSlot;
        float timeInClip;
        float blend;
        Renderer[] blinkRenderers;
        bool[] originalVisibility;

        void OnEnable()
        {
            LoadMissingClips();
            blinkRenderers = GetComponentsInChildren<Renderer>();
            originalVisibility = Array.ConvertAll(blinkRenderers, renderer => renderer.forceRenderingOff);
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            graph = PlayableGraph.Create("Character rig");
            graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            mixer = AnimationMixerPlayable.Create(graph, 2);
            var output = AnimationPlayableOutput.Create(graph, "Character pose", animator);
            output.SetSourcePlayable(mixer);
            graph.Play();
        }

        void LoadMissingClips()
        {
            if (defensiveRun && sheatheWalk && defensiveFall && defensiveRise && offensiveFall && offensiveRise)
                return;

            var clips = Resources.LoadAll<AnimationClip>("MiddleSlash");
            AnimationClip FindClip(string name) => Array.Find(clips, clip =>
                clip.name == name || clip.name.EndsWith("|" + name, StringComparison.Ordinal));

            if (!defensiveRun) defensiveRun = FindClip("DefensiveRun");
            if (!sheatheWalk) sheatheWalk = FindClip("SheatheWalk");
            if (!defensiveFall) defensiveFall = FindClip("DefensiveFall");
            if (!defensiveRise) defensiveRise = FindClip("DefensiveRise");
            if (!offensiveFall) offensiveFall = FindClip("OffensiveFall");
            if (!offensiveRise) offensiveRise = FindClip("OffensiveRise");
        }

        void OnDisable()
        {
            SetBlink(false);
            if (graph.IsValid())
                graph.Destroy();

            inputs[0] = default;
            inputs[1] = default;
            activeClip = null;
            activeSlot = 0;
        }

        void LateUpdate()
        {
            SetBlink(actions.IsGetUpProtected && Mathf.Repeat(actions.PhaseProgress * 8, 1) < .5f);
            float speed = motor.PlanarSpeed;
            AnimationClip clip = SelectClip(speed);
            if (!clip)
                return;

            if (clip != activeClip)
                ChangeClip(clip);

            if (actions.Phase == CharacterActionPhase.Sheathed || actions.Phase == CharacterActionPhase.Drawn)
                timeInClip = Mathf.Repeat(timeInClip + Time.deltaTime * LocomotionRate(speed), clip.length);
            else
                timeInClip = ActionClipProgress() * clip.length;

            inputs[activeSlot].SetTime(timeInClip);
            blend = Mathf.MoveTowards(blend, 1, Time.deltaTime / transitionDuration);
            mixer.SetInputWeight(activeSlot, blend);
            mixer.SetInputWeight(1 - activeSlot, 1 - blend);
            graph.Evaluate(0);
        }

        float ActionClipProgress()
        {
            float progress = actions.PhaseProgress;
            if (actions.LinkDelay <= 0)
                return progress;

            if (actions.Phase == CharacterActionPhase.AttackLink)
                return progress * ComboBranchProgress;
            if (actions.Phase == CharacterActionPhase.Recovery)
                return Mathf.Lerp(ComboBranchProgress, 1, progress);
            return progress;
        }

        void SetBlink(bool hidden)
        {
            if (blinkRenderers == null)
                return;

            for (int i = 0; i < blinkRenderers.Length; i++)
                if (blinkRenderers[i])
                    blinkRenderers[i].forceRenderingOff = originalVisibility[i] || hidden;
        }

        AnimationClip SelectClip(float speed)
        {
            var phase = actions.Phase;
            if (phase == CharacterActionPhase.Sheathed)
                return speed > .12f ? (defensiveRun ? defensiveRun : defensiveWalk) : defensiveIdle;
            if (phase == CharacterActionPhase.Drawn)
                return speed > .12f ? offensiveWalk : offensiveIdle;
            if (phase == CharacterActionPhase.Knockdown)
                return actions.RecoverWithWeaponDrawn ? offensiveFall : defensiveFall;
            if (phase == CharacterActionPhase.GetUp)
                return actions.RecoverWithWeaponDrawn ? offensiveRise : defensiveRise;
            if (phase == CharacterActionPhase.Sheathing)
                return speed > .12f && sheatheWalk ? sheatheWalk : sheathe;
            if (phase == CharacterActionPhase.Windup && actions.IsDrawAttack)
                return drawWindup;

            if (attacks == null || actions.MoveIndex >= attacks.Length)
                return offensiveIdle;

            var move = attacks[actions.MoveIndex];
            switch (phase)
            {
                case CharacterActionPhase.Windup: return move.windup;
                case CharacterActionPhase.Strike: return move.strike;
                default: return move.recovery;
            }
        }

        float LocomotionRate(float speed)
        {
            if (activeClip == defensiveRun || activeClip == defensiveWalk)
                return speed * activeClip.length / defensiveCycleDistance;
            if (activeClip == offensiveWalk)
                return speed * activeClip.length / offensiveCycleDistance;

            return 1;
        }

        void ChangeClip(AnimationClip clip)
        {
            blend = activeClip ? 0 : 1;
            activeSlot = 1 - activeSlot;
            if (inputs[activeSlot].IsValid())
            {
                graph.Disconnect(mixer, activeSlot);
                graph.DestroyPlayable(inputs[activeSlot]);
            }

            var input = AnimationClipPlayable.Create(graph, clip);
            input.SetSpeed(0);
            input.SetApplyFootIK(false);
            graph.Connect(input, 0, mixer, activeSlot);
            inputs[activeSlot] = input;
            activeClip = clip;
            timeInClip = 0;
        }
    }
}

