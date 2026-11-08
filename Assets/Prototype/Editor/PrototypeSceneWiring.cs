using UnityEngine;

namespace Aberracion.Editor
{
    public static class PrototypeSceneWiring
    {
        public static void Connect(PrototypePlayer player, SessionShortcuts shortcuts, Transform swordPivot)
        {
            FocusState focus = shortcuts.focus;
            var actions = GetOrAdd<CharacterActionStateMachine>(player.gameObject);
            var attack = player.GetComponent<MeleeAttack>();
            attack.targetTeam = CombatTeam.Enemy;

            var swing = GetOrAdd<SwordSwingVisual>(player.gameObject);
            swing.actions = actions;
            swing.pivot = swordPivot;

            var focusAttack = GetOrAdd<FocusAttackLink>(player.gameObject);
            focusAttack.attack = attack;
            focusAttack.focus = focus;
            shortcuts.telemetry.attack = attack;

            var session = shortcuts.gameObject;
            shortcuts.corridor = GetOrAdd<FocusCorridor>(session);
            shortcuts.corridor.focus = focus;
            shortcuts.corridor.viewCamera = player.viewCamera;

            shortcuts.combatAudio = GetOrAdd<CombatAudio>(session);
            shortcuts.combatAudio.attack = attack;
            shortcuts.combatAudio.focus = focus;
            session.GetComponent<AudioSource>().playOnAwake = false;

            var hud = GetOrAdd<PrototypeHud>(session);
            hud.focus = focus;

            foreach (var enemy in shortcuts.enemies)
            {
                enemy.team = CombatTeam.Enemy;
                var hitVisual = GetOrAdd<TargetHitVisual>(enemy.gameObject);
                hitVisual.actor = enemy;
                hitVisual.body = enemy.GetComponent<Renderer>();
                hitVisual.focus = focus;

                var numbers = GetOrAdd<FloatingDamageNumbers>(enemy.gameObject);
                numbers.actor = enemy;
                numbers.viewCamera = player.viewCamera;
            }
        }

        static T GetOrAdd<T>(GameObject owner) where T : Component
        {
            return owner.TryGetComponent(out T existing) ? existing : owner.AddComponent<T>();
        }
    }
}



