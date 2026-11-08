using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Aberracion.Editor
{
    public static class MiddleSlashRigInstaller
    {
        const string AssetFolder = "Assets/Prototype/Character";
        const string ModelPath = AssetFolder + "/Resources/MiddleSlash.fbx";

        public static void Attach(GameObject hero)
        {
            if (hero.GetComponent<CharacterRigAnimation>())
                return;

            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (!model)
                throw new InvalidOperationException("Import MiddleSlash.fbx before installing the character.");

            var clips = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<AnimationClip>()
                .Where(clip => !clip.name.StartsWith("__preview__", StringComparison.Ordinal)).ToArray();
            AnimationClip Clip(string name) => clips.Single(c => c.name == name || c.name.EndsWith("|" + name, StringComparison.Ordinal));
            var idle = Clip("DefensiveIdle");
            var material = LoadMaterial();

            var visual = new GameObject("MiddleSlash Visual");
            Undo.RegisterCreatedObjectUndo(visual, "Add MiddleSlash rig");
            Undo.SetTransformParent(visual.transform, hero.transform, "Parent character visual");
            visual.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model, hero.scene);
            Undo.RegisterCreatedObjectUndo(instance, "Add skinned character");
            Undo.SetTransformParent(instance.transform, visual.transform, "Parent skinned character");
            instance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

            Transform[] bones = instance.GetComponentsInChildren<Transform>();
            Transform Bone(string name) => bones.Single(t => t.name == name);
            Transform grip = Bone("SwordSocket");
            Transform tip = Bone("SwordTip");
            Transform up = Bone("SwordUp");
            var renderers = instance.GetComponentsInChildren<SkinnedMeshRenderer>();
            if (renderers.Length == 0)
                throw new InvalidOperationException("MiddleSlash has no skinned meshes.");

            foreach (var renderer in renderers)
            {
                Undo.RecordObject(renderer, "Assign MiddleSlash material");
                renderer.sharedMaterials = renderer.sharedMaterials.Select(_ => material).ToArray();
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            }

            Pose stowedSword = AlignVisual(visual.transform, model, idle);

            var animator = instance.GetComponent<Animator>();
            if (!animator)
                animator = Undo.AddComponent<Animator>(instance);
            Undo.RecordObject(animator, "Set animation playback options");
            animator.applyRootMotion = false;
            PrefabUtility.RecordPrefabInstancePropertyModifications(animator);

            var animation = Undo.AddComponent<CharacterRigAnimation>(hero);
            animation.actions = hero.GetComponent<CharacterActionStateMachine>();
            animation.motor = hero.GetComponent<CharacterMotor>();
            animation.animator = animator;
            animation.defensiveIdle = idle;
            animation.defensiveWalk = Clip("DefensiveWalk");
            animation.defensiveRun = Clip("DefensiveRun");
            animation.offensiveIdle = Clip("OffensiveIdle");
            animation.offensiveWalk = Clip("OffensiveWalk");
            animation.drawWindup = Clip("DrawWindup");
            animation.sheathe = Clip("Sheathe");
            animation.sheatheWalk = Clip("SheatheWalk");
            WireKnockdownClips(animation);
            animation.attacks = new[] { Attack("Cut"), Attack("Sweep"), Attack("Finisher") };

            CharacterRigAnimation.AttackClips Attack(string prefix) => new CharacterRigAnimation.AttackClips
            {
                windup = Clip(prefix + "Windup"),
                strike = Clip(prefix + "Strike"),
                recovery = Clip(prefix + "Recovery")
            };

            var originalSword = hero.GetComponent<SwordSwingVisual>();
            var attachment = Undo.AddComponent<RigSwordAttachment>(hero);
            attachment.weapon = originalSword.pivot;
            attachment.grip = grip;
            attachment.bladeDirection = tip;
            attachment.bladeUp = up;

            Undo.RecordObject(originalSword, "Use animated sword attachment");
            originalSword.enabled = false;
            Undo.RecordObject(attachment.weapon, "Fit sword to character");
            attachment.weapon.localScale = Vector3.one * .7f;
            attachment.weapon.SetPositionAndRotation(stowedSword.position, stowedSword.rotation);

            var placeholder = hero.transform.Find("Body");
            if (placeholder)
            {
                Undo.RecordObject(placeholder.gameObject, "Hide prototype body");
                placeholder.gameObject.SetActive(false);
            }
        }

        public static void WireKnockdownClips(CharacterRigAnimation animation)
        {
            var clips = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<AnimationClip>()
                .Where(clip => !clip.name.StartsWith("__preview__", StringComparison.Ordinal));
            AnimationClip Clip(string name) => clips.Single(clip => clip.name == name ||
                clip.name.EndsWith("|" + name, StringComparison.Ordinal));

            Undo.RecordObject(animation, "Connect player knockdown clips");
            animation.defensiveFall = Clip("DefensiveFall");
            animation.defensiveRise = Clip("DefensiveRise");
            animation.offensiveFall = Clip("OffensiveFall");
            animation.offensiveRise = Clip("OffensiveRise");
        }

        public static void RefitInstalledVisual(GameObject hero)
        {
            var animation = hero.GetComponent<CharacterRigAnimation>();
            var sword = hero.GetComponent<RigSwordAttachment>();
            Transform visual = animation.animator.transform.parent;
            Undo.RecordObjects(new UnityEngine.Object[] { visual, sword.weapon }, "Fit MiddleSlash to gameplay scale");
            Pose stowedSword = AlignVisual(visual, AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath), animation.defensiveIdle);
            sword.weapon.SetPositionAndRotation(stowedSword.position, stowedSword.rotation);
        }

        static Pose AlignVisual(Transform visual, GameObject model, AnimationClip idle)
        {
            var preview = EditorSceneManager.NewPreviewScene();
            try
            {
                var sample = (GameObject)PrefabUtility.InstantiatePrefab(model, preview);
                idle.SampleAnimation(sample, 0);
                var bones = sample.GetComponentsInChildren<Transform>();
                Vector3 BonePosition(string name) =>
                    sample.transform.InverseTransformPoint(bones.Single(t => t.name == name).position);

                Vector3 grip = BonePosition("SwordSocket");
                Vector3 tip = BonePosition("SwordTip");
                Vector3 up = BonePosition("SwordUp");
                visual.localRotation = grip.z > BonePosition("Bone").z
                    ? Quaternion.Euler(0, 180, 0)
                    : Quaternion.identity;

                var renderers = sample.GetComponentsInChildren<SkinnedMeshRenderer>();
                Bounds bounds = renderers[0].bounds;
                foreach (var renderer in renderers)
                    bounds.Encapsulate(renderer.bounds);

                float scale = 1.8f / Mathf.Max(.01f, bounds.size.y);
                visual.localScale = Vector3.one * scale;
                var ground = bones.SingleOrDefault(t => t.name == "GroundReference");
                float soleHeight = ground ? sample.transform.InverseTransformPoint(ground.position).y : bounds.min.y;
                visual.localPosition = new Vector3(0, -soleHeight * scale, 0);
                return new Pose(visual.TransformPoint(grip), Quaternion.LookRotation(
                    visual.TransformDirection(tip - grip), visual.TransformDirection(up - grip)));
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(preview);
            }
        }

        static Material LoadMaterial()
        {
            string path = AssetFolder + "/MiddleSlash.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material)
                return material;

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetFolder + "/MiddleSlash_Albedo.png");
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetTexture("_BaseMap", texture);
            material.SetFloat("_Smoothness", .22f);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }
    }
}

