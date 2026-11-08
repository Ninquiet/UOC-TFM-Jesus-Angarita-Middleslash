using UnityEngine;

namespace Aberracion
{
    public sealed class TargetHitVisual : MonoBehaviour
    {
        public DamageableActor actor;
        public Renderer body;
        public FocusState focus;

        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        MaterialPropertyBlock properties;
        Vector3 restingScale;
        Color restingColor;
        float flash;

        void Awake()
        {
            properties = new MaterialPropertyBlock();
            restingScale = body.transform.localScale;
            restingColor = body.sharedMaterial.color;
        }

        void OnEnable()
        {
            actor.Damaged += Flash;
            actor.Restored += ClearFlash;
        }

        void OnDisable()
        {
            actor.Damaged -= Flash;
            actor.Restored -= ClearFlash;
            ClearFlash();
        }

        void Flash(float damage) => flash = 1f;

        void ClearFlash()
        {
            flash = 0;
            body.transform.localScale = restingScale;
        }

        void LateUpdate()
        {
            flash = Mathf.MoveTowards(flash, 0, Time.deltaTime * 5);
            float focusAmount = focus && focus.Target == actor ? focus.Progress : 0;
            Color tint = actor.IsAlive
                ? Color.Lerp(restingColor, new Color(1, .65f, .15f), focusAmount)
                : new Color(.15f, .16f, .19f);
            body.GetPropertyBlock(properties);
            properties.SetColor(BaseColor, Color.Lerp(tint, Color.white, flash));
            body.SetPropertyBlock(properties);
            body.transform.localScale = restingScale * (1 + flash * .07f);
        }
    }
}
