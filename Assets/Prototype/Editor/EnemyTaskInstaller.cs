using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Aberracion.Editor
{
    public static class EnemyTaskInstaller
    {
        const string Folder = "Assets/Prototype/EnemyTask";
        const string ModelPath = Folder + "/EnemyTask.fbx";

        public static void Attach(PrototypePlayer player, SessionShortcuts session, Collider arenaFloor)
        {
            var playerHealth = GetOrAdd<DamageableActor>(player.gameObject);
            playerHealth.actorId = "player";
            playerHealth.team = CombatTeam.Player;
            playerHealth.maxHealth = 100;

            var vitals = GetOrAdd<PlayerCombatVitals>(player.gameObject);
            vitals.actor = playerHealth;
            vitals.actions = player.GetComponent<CharacterActionStateMachine>();
            Undo.RecordObject(session, "Connect player health");
            session.playerHealth = playerHealth;

            foreach (var enemy in session.enemies)
                AttachEnemy(enemy, playerHealth, arenaFloor);
        }

        static void AttachEnemy(DamageableActor enemy, DamageableActor player, Collider arenaFloor)
        {
            GetOrAdd<EnemyKnockback>(enemy.gameObject);
            if (enemy.TryGetComponent(out EnemyTaskBrain existingBrain))
            {
                Undo.RecordObject(existingBrain, "Connect enemy patrol floor");
                existingBrain.patrolFloor = arenaFloor;
                return;
            }

            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            var clips = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<AnimationClip>()
                .Where(clip => !clip.name.StartsWith("__preview__", StringComparison.Ordinal)).ToArray();
            AnimationClip Clip(string name) => clips.Single(clip => clip.name == name ||
                clip.name.EndsWith("|" + name, StringComparison.Ordinal));

            var visual = new GameObject("Enemy Task Visual");
            Undo.RegisterCreatedObjectUndo(visual, "Add Enemy Task visual");
            Undo.SetTransformParent(visual.transform, enemy.transform, "Parent Enemy Task visual");
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model, enemy.gameObject.scene);
            Undo.RegisterCreatedObjectUndo(instance, "Add Enemy Task rig");
            Undo.SetTransformParent(instance.transform, visual.transform, "Parent Enemy Task rig");
            instance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            FitVisual(visual.transform, model, Clip("TaskIdle"), Clip("TaskPunchLeft"));

            var renderer = instance.GetComponentInChildren<SkinnedMeshRenderer>();
            Undo.RecordObject(renderer, "Assign Enemy Task material");
            renderer.sharedMaterial = LoadMaterial();
            PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            var original = enemy.GetComponent<MeshRenderer>();
            Undo.RecordObject(original, "Hide enemy placeholder");
            original.enabled = false;

            var targetVisual = enemy.GetComponent<TargetHitVisual>();
            Undo.RecordObject(targetVisual, "Connect Enemy Task hit feedback");
            targetVisual.body = renderer;

            var punch = Undo.AddComponent<EnemyTaskPunch>(enemy.gameObject);
            punch.target = player;
            var brain = Undo.AddComponent<EnemyTaskBrain>(enemy.gameObject);
            brain.actor = enemy;
            brain.punch = punch;
            brain.patrolFloor = arenaFloor;
            var guard = Undo.AddComponent<EnemyTaskGuard>(enemy.gameObject);
            guard.actor = enemy;
            guard.brain = brain;

            var animation = Undo.AddComponent<EnemyTaskAnimation>(enemy.gameObject);
            animation.brain = brain;
            animation.animator = instance.GetComponent<Animator>();
            animation.idle = Clip("TaskIdle");
            animation.jump = Clip("TaskJump");
            animation.windup = Clip("TaskWindup");
            animation.punchLeft = Clip("TaskPunchLeft");
            animation.punchRight = Clip("TaskPunchRight");
            animation.exhausted = Clip("TaskExhausted");
            animation.stagger = Clip("TaskStagger");
            animation.recover = Clip("TaskRecover");
            animation.defeated = Clip("TaskDefeated");
            WirePunchVolumes(punch, animation);
        }

        public static void WirePunchVolumes(EnemyTaskPunch punch, EnemyTaskAnimation animation)
        {
            Undo.RecordObject(punch, "Connect animated fist volumes");
            var bones = animation.animator.GetComponentsInChildren<Transform>();
            Transform Marker(string name) => bones.Single(bone => bone.name == name);
            EnemyFistVolume Fist(string side) => new EnemyFistVolume
            {
                center = Marker("HitCenter." + side),
                extentX = Marker("HitX." + side),
                extentY = Marker("HitY." + side),
                extentZ = Marker("HitZ." + side)
            };

            punch.animationDriver = animation;
            punch.leftFist = Fist("L");
            punch.rightFist = Fist("R");
        }

        static void FitVisual(Transform visual, GameObject model, AnimationClip idle, AnimationClip punch)
        {
            var preview = EditorSceneManager.NewPreviewScene();
            try
            {
                var sample = (GameObject)PrefabUtility.InstantiatePrefab(model, preview);
                idle.SampleAnimation(sample, 0);
                Bounds bounds = sample.GetComponentInChildren<SkinnedMeshRenderer>().bounds;
                punch.SampleAnimation(sample, punch.length * .47f);
                var bones = sample.GetComponentsInChildren<Transform>();
                var fist = bones.Single(bone => bone.name == "Fist.L");
                var body = bones.Single(bone => bone.name == "Body");
                visual.localRotation = fist.position.z < body.position.z
                    ? Quaternion.Euler(0, 180, 0)
                    : Quaternion.identity;
                float scale = 1.85f / Mathf.Max(.01f, bounds.size.y);
                visual.localScale = Vector3.one * scale;
                visual.localPosition = new Vector3(0, -.9f - bounds.min.y * scale, 0);
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(preview);
            }
        }

        static Material LoadMaterial()
        {
            string path = Folder + "/EnemyTask.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material)
                return material;

            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "/EnemyTask_Albedo.png"));
            material.SetFloat("_Smoothness", .25f);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        static T GetOrAdd<T>(GameObject owner) where T : Component
        {
            var existing = owner.GetComponent<T>();
            if (existing)
            {
                Undo.RecordObject(existing, "Configure " + typeof(T).Name);
                return existing;
            }
            return Undo.AddComponent<T>(owner);
        }
    }
}
