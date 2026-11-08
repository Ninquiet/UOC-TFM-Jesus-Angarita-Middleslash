using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Aberracion
{
    public sealed class EnemyTaskAnimation : MonoBehaviour
    {
        public EnemyTaskBrain brain;
        public Animator animator;
        public AnimationClip idle;
        public AnimationClip jump;
        public AnimationClip windup;
        public AnimationClip punchLeft;
        public AnimationClip punchRight;
        public AnimationClip exhausted;
        public AnimationClip stagger;
        public AnimationClip recover;
        public AnimationClip defeated;

        readonly AnimationClipPlayable[] inputs = new AnimationClipPlayable[2];
        PlayableGraph graph;
        AnimationMixerPlayable mixer;
        AnimationClip activeClip;
        int activeSlot;
        float blend;

        void OnEnable()
        {
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            graph = PlayableGraph.Create("Enemy Task pose");
            graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            mixer = AnimationMixerPlayable.Create(graph, 2);
            AnimationPlayableOutput.Create(graph, "Enemy Task", animator).SetSourcePlayable(mixer);
            graph.Play();
        }

        void OnDisable()
        {
            if (graph.IsValid())
                graph.Destroy();

            inputs[0] = default;
            inputs[1] = default;
            activeClip = null;
        }

        void LateUpdate()
        {
            AnimationClip clip = SelectClip();
            if (clip != activeClip)
                ChangeClip(clip);

            inputs[activeSlot].SetTime(ClipTime(clip));
            blend = Mathf.MoveTowards(blend, 1, Time.deltaTime / .08f);
            mixer.SetInputWeight(activeSlot, blend);
            mixer.SetInputWeight(1 - activeSlot, 1 - blend);
            graph.Evaluate(0);
        }

        public bool SamplePunchImpact(bool left, float fraction)
        {
            if (!graph.IsValid())
                return false;

            AnimationClip clip = left ? punchLeft : punchRight;
            if (!clip)
                return false;
            if (clip != activeClip)
                ChangeClip(clip);

            inputs[activeSlot].SetTime(clip.length * fraction);
            mixer.SetInputWeight(activeSlot, 1);
            mixer.SetInputWeight(1 - activeSlot, 0);
            blend = 1;
            graph.Evaluate(0);
            return true;
        }

        AnimationClip SelectClip()
        {
            return brain.Phase switch
            {
                EnemyTaskPhase.Windup => windup,
                EnemyTaskPhase.Jumping => jump ? jump : idle,
                EnemyTaskPhase.Punching => brain.PunchIndex % 2 == 0 ? punchLeft : punchRight,
                EnemyTaskPhase.Exhausted => exhausted,
                EnemyTaskPhase.Staggered => stagger,
                EnemyTaskPhase.Recovering => recover,
                EnemyTaskPhase.Defeated => defeated,
                _ => idle
            };
        }

        float ClipTime(AnimationClip clip)
        {
            if (brain.Phase == EnemyTaskPhase.Idle || brain.Phase == EnemyTaskPhase.Patrolling)
                return Mathf.Repeat(brain.Elapsed, clip.length);

            float duration = brain.Phase switch
            {
                EnemyTaskPhase.Windup => brain.windupDuration,
                EnemyTaskPhase.Jumping => brain.jumpDuration,
                EnemyTaskPhase.Punching => brain.punchDuration,
                EnemyTaskPhase.Recovering => brain.recoveryDuration,
                _ => clip.length
            };

            return Mathf.Clamp01(brain.Elapsed / Mathf.Max(.01f, duration)) * clip.length;
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
        }
    }
}
