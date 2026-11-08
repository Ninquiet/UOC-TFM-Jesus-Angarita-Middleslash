using UnityEngine;
using UnityEngine.InputSystem;

namespace Aberracion
{
    public sealed class SessionShortcuts : MonoBehaviour
    {
        public FocusState focus;
        public DamageableActor playerHealth;
        public DamageableActor[] enemies;
        public SessionTelemetry telemetry;
        public FocusCorridor corridor;
        public CombatAudio combatAudio;

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (keyboard.rKey.wasPressedThisFrame)
            {
                focus.ResetState();
                if (playerHealth)
                    playerHealth.RestoreHealth();
                foreach (var enemy in enemies)
                    enemy.RestoreHealth();
            }

            if (keyboard.f2Key.wasPressedThisFrame)
                telemetry.recording = !telemetry.recording;
            if (keyboard.f3Key.wasPressedThisFrame)
                corridor.Visible = !corridor.Visible;
            if (keyboard.f4Key.wasPressedThisFrame)
                combatAudio.Muted = !combatAudio.Muted;
            if (keyboard.escapeKey.wasPressedThisFrame)
                Application.Quit();
        }
    }
}
