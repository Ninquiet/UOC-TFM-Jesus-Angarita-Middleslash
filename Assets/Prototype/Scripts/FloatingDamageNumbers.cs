using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Aberracion
{
    public sealed class FloatingDamageNumbers : MonoBehaviour
    {
        public DamageableActor actor;
        public Camera viewCamera;
        [Min(.1f)] public float lifetime = .85f;
        [Min(0)] public float height = 1.15f;
        [Min(0)] public float rise = .95f;

        readonly List<Popup> popups = new List<Popup>();
        GUIStyle label;

        struct Popup
        {
            public Vector3 Origin;
            public float Started;
            public string Text;
        }

        void OnEnable()
        {
            actor.Damaged += ShowDamage;
            actor.HitBlocked += ShowBlocked;
        }

        void OnDisable()
        {
            actor.Damaged -= ShowDamage;
            actor.HitBlocked -= ShowBlocked;
            popups.Clear();
        }

        void ShowDamage(float damage) => ShowPopup(damage.ToString("0.#", CultureInfo.InvariantCulture));

        void ShowBlocked() => ShowPopup("BLOQUEADO");

        void ShowPopup(string text)
        {
            popups.Add(new Popup
            {
                Origin = transform.position + Vector3.up * height,
                Started = Time.time,
                Text = text
            });
        }

        void Update() => popups.RemoveAll(popup => Time.time - popup.Started >= lifetime);

        void OnGUI()
        {
            if (Event.current.type != EventType.Repaint || popups.Count == 0)
                return;

            label ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            label.fontSize = Mathf.RoundToInt(Mathf.Clamp(Screen.height / 32f, 16f, 32f));
            Color previousColor = GUI.color;

            foreach (var popup in popups)
            {
                float progress = Mathf.Clamp01((Time.time - popup.Started) / lifetime);
                Vector3 screen = viewCamera.WorldToScreenPoint(popup.Origin + Vector3.up * (progress * rise));
                if (screen.z <= 0)
                    continue;

                var rect = new Rect(screen.x - 80, Screen.height - screen.y - 16, 160, 32);
                GUI.color = new Color(0, 0, 0, 1 - progress);
                label.normal.textColor = Color.white;
                GUI.Label(new Rect(rect.x + 1, rect.y + 2, rect.width, rect.height), popup.Text, label);
                GUI.color = new Color(1, .85f, .47f, 1 - progress);
                GUI.Label(rect, popup.Text, label);
            }

            GUI.color = previousColor;
        }
    }
}
