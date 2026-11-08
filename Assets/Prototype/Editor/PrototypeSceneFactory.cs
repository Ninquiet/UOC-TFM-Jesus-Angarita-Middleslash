using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Aberracion.Editor
{
    public static class PrototypeSceneFactory
    {
        [MenuItem("TFM/Create PEC2 prototype scene")]
        public static void Create()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var ground = LoadMaterial("Ground", new Color(.10f, .15f, .19f));
            var stone = LoadMaterial("Stone", new Color(.21f, .30f, .35f));
            var heroMaterial = LoadMaterial("Hero", new Color(.18f, .7f, .73f));
            var sword = LoadMaterial("Sword", new Color(.78f, .87f, .93f));
            var enemyMaterial = LoadMaterial("Enemy", new Color(.45f, .24f, .32f));
            var environment = CreateGroup("Environment");
            var actors = CreateGroup("Actors");
            var lighting = CreateGroup("Lighting");
            var cameras = CreateGroup("Cameras");
            var systems = CreateGroup("Systems");
            var arenaFloor = CreateArena(ground, stone, environment, lighting);

            var session = CreateGroup("Session", systems).gameObject;
            var telemetry = session.AddComponent<SessionTelemetry>();
            var hero = CreateGroup("Aberracion", actors).gameObject;
            hero.transform.position = new Vector3(0, .05f, -3);
            var controller = hero.AddComponent<CharacterController>();
            controller.height = 2;
            controller.center = Vector3.up;
            controller.radius = .4f;

            CreateDecoration("Body", PrimitiveType.Capsule, Vector3.up, new Vector3(.8f, 1, .8f), heroMaterial, hero.transform);
            var pivot = new GameObject("Sword pivot").transform;
            pivot.SetParent(hero.transform, false);
            pivot.localPosition = new Vector3(.3f, 1.1f, 0);
            CreateDecoration("Greatsword", PrimitiveType.Cube, new Vector3(0, 0, 1.35f), new Vector3(.4f, .16f, 2.5f), sword, pivot);
            CreateDecoration("Guard", PrimitiveType.Cube, new Vector3(0, 0, .25f), new Vector3(.85f, .23f, .2f), stone, pivot);

            var focus = hero.AddComponent<FocusState>();
            focus.telemetry = telemetry;
            var player = hero.AddComponent<PrototypePlayer>();
            player.viewCamera = CreateCamera(hero.transform, cameras);
            player.controls = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");

            var shortcuts = session.AddComponent<SessionShortcuts>();
            shortcuts.focus = focus;
            shortcuts.telemetry = telemetry;
            var enemies = CreateGroup("Enemies", actors);
            shortcuts.enemies = new[]
            {
                CreateEnemy(1, new Vector3(-8, 1, 7), enemyMaterial, enemies),
                CreateEnemy(2, new Vector3(7, 1, 8), enemyMaterial, enemies),
                CreateEnemy(3, new Vector3(9, 1, -8), enemyMaterial, enemies)
            };

            PrototypeSceneWiring.Connect(player, shortcuts, pivot);
            MiddleSlashRigInstaller.Attach(hero);
            EnemyTaskInstaller.Attach(player, shortcuts, arenaFloor);
            session.AddComponent<PrototypeSmokeTest>();
            EditorSceneManager.SaveScene(scene, PrototypeBuild.ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(PrototypeBuild.ScenePath, true) };
            AssetDatabase.SaveAssets();
        }

        static Collider CreateArena(Material ground, Material stone, Transform environment, Transform lighting)
        {
            var floor = CreateGroup("Floor", environment);
            var boundaries = CreateGroup("Boundaries", environment);
            var arenaFloor = CreateShape("Ground", PrimitiveType.Cube, new Vector3(0, -.3f, 0),
                new Vector3(24, .6f, 24), ground, floor).GetComponent<Collider>();
            for (int side = -1; side <= 1; side += 2)
            {
                CreateShape(side < 0 ? "West" : "East", PrimitiveType.Cube, new Vector3(side * 12, .5f, 0), new Vector3(.5f, 1, 24), stone, boundaries);
                CreateShape(side < 0 ? "South" : "North", PrimitiveType.Cube, new Vector3(0, .5f, side * 12), new Vector3(24, 1, .5f), stone, boundaries);
            }

            for (int x = -8; x <= 8; x += 4)
                for (int z = -8; z <= 8; z += 4)
                    CreateShape($"Tile X{x} Z{z}", PrimitiveType.Cube, new Vector3(x, .008f, z), new Vector3(3.8f, .015f, 3.8f), stone, floor);

            var light = CreateGroup("Key light", lighting).gameObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.8f;
            light.shadows = LightShadows.Soft;
            light.transform.rotation = Quaternion.Euler(50, -35, 0);
            RenderSettings.ambientLight = new Color(.38f, .43f, .52f);
            return arenaFloor;
        }

        static Camera CreateCamera(Transform hero, Transform parent)
        {
            var camera = CreateGroup("Isometric Camera", parent).gameObject.AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.orthographic = true;
            camera.orthographicSize = 9;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.025f, .035f, .06f);
            camera.farClipPlane = 80;
            camera.gameObject.AddComponent<AudioListener>();

            var follow = camera.gameObject.AddComponent<IsometricCamera>();
            follow.target = hero;
            camera.transform.position = hero.position + follow.offset;
            camera.transform.rotation = Quaternion.LookRotation(-follow.offset);
            return camera;
        }

        static DamageableActor CreateEnemy(int index, Vector3 position, Material material, Transform parent)
        {
            var enemy = CreateShape($"Resistant {index}", PrimitiveType.Capsule,
                position, new Vector3(1.2f, 1, 1.2f), material, parent).AddComponent<DamageableActor>();
            enemy.actorId = $"enemy-{index}";
            return enemy;
        }

        static Transform CreateGroup(string name, Transform parent = null)
        {
            var group = new GameObject(name).transform;
            group.SetParent(parent, false);
            return group;
        }

        static void CreateDecoration(string name, PrimitiveType primitive, Vector3 position, Vector3 scale, Material material, Transform parent)
        {
            var decoration = CreateShape(name, primitive, position, scale, material, parent);
            Object.DestroyImmediate(decoration.GetComponent<Collider>());
        }

        static GameObject CreateShape(string name, PrimitiveType primitive, Vector3 position, Vector3 scale, Material material, Transform parent = null)
        {
            var shape = GameObject.CreatePrimitive(primitive);
            shape.name = name;
            shape.transform.SetParent(parent, false);
            shape.transform.localPosition = position;
            shape.transform.localScale = scale;
            shape.GetComponent<Renderer>().sharedMaterial = material;
            return shape;
        }

        static Material LoadMaterial(string name, Color color)
        {
            string path = $"Assets/Prototype/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material)
                return material;

            material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = color };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }
    }
}


