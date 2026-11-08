using UnityEngine;

namespace Aberracion
{
    public sealed class PrototypeHud : MonoBehaviour
    {
        public FocusState focus;
        public PlayerCombatVitals playerVitals;
        public SessionTelemetry telemetry;

        void OnGUI()
        {
            float scale = Mathf.Clamp(Screen.width / 1280f, .75f, 2);
            Matrix4x4 previousMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));

            GUI.Box(new Rect(20, 20, 390, 105), "ABERRACIÓN  /  PEC 2 · 0.1.0");
            GUI.Label(new Rect(35, 49, 360, 24), "WASD / flechas: mover · Ratón: dirección");
            GUI.Label(new Rect(35, 72, 360, 24), "Clic: tajo · Q: guardar espada · R: reiniciar");
            GUI.Label(new Rect(35, 95, 360, 24), "F2: registro · F3: paneles · F4: sonido · Esc: salir");
            if (focus.IsHyper)
                GUI.Label(new Rect(Screen.width / scale / 2 - 90, 35, 250, 30),
                    focus.IsExiting ? "RECUPERANDO PERSPECTIVA" : "HIPERFOCO");

            GUI.Label(new Rect(25, Screen.height / scale - 35, 650, 25),
                "Prototipo experimental · Parámetros provisionales · Registro " + (telemetry.recording ? "activo" : "desactivado"));
            if (playerVitals)
            {
                var health = playerVitals.actor;
                Color previousColor = GUI.color;
                GUI.color = Color.Lerp(Color.white, new Color(1, .28f, .24f), playerVitals.HitPulse);
                string status = health.IsAlive
                    ? $"Vida {Mathf.CeilToInt(health.Health)} / {health.maxHealth:0}"
                    : "DERROTADO · R para reiniciar";
                GUI.Box(new Rect(20, 134, 215, 30), status);
                GUI.color = previousColor;
            }
            GUI.matrix = previousMatrix;
        }
    }
}

