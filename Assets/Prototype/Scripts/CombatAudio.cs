using UnityEngine;

namespace Aberracion
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class CombatAudio : MonoBehaviour
    {
        public MeleeAttack attack;
        public FocusState focus;
        [Range(0, 1)] public float volume = .35f;
        public bool Muted { get; set; }

        AudioSource source;
        AudioClip impact;
        AudioClip hyperfocus;
        bool wasHyper;

        void Awake()
        {
            source = GetComponent<AudioSource>();
            impact = CreateTone(110, .12f);
            hyperfocus = CreateTone(650, .45f);
        }

        void OnEnable()
        {
            wasHyper = focus.IsHyper;
            attack.Hit += PlayImpact;
        }

        void OnDisable() => attack.Hit -= PlayImpact;

        void Update()
        {
            if (focus.IsHyper && !wasHyper && !Muted)
            {
                source.pitch = 1;
                source.PlayOneShot(hyperfocus, volume);
            }
            wasHyper = focus.IsHyper;
        }

        void PlayImpact(DamageableActor target, float damage)
        {
            if (Muted)
                return;

            source.pitch = 1 + focus.Progress * .4f;
            source.PlayOneShot(impact, volume);
        }

        static AudioClip CreateTone(float frequency, float seconds)
        {
            const int sampleRate = 22050;
            int count = Mathf.CeilToInt(sampleRate * seconds);
            float[] samples = new float[count];
            for (int i = 0; i < count; i++)
                samples[i] = Mathf.Sin(2 * Mathf.PI * frequency * i / sampleRate) * Mathf.Exp(-7f * i / count) * .5f;

            AudioClip clip = AudioClip.Create("Prototype tone", count, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        void OnDestroy()
        {
            Destroy(impact);
            Destroy(hyperfocus);
        }
    }
}
