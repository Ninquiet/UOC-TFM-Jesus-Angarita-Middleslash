using UnityEngine;

namespace Aberracion
{
    [RequireComponent(typeof(Camera))]
    [DefaultExecutionOrder(110)]
    public sealed class IsometricCamera : MonoBehaviour
    {
        public Transform target;
        public Vector3 offset = new Vector3(9, 12, -9);
        [Min(0)] public float smoothing = 8;
        [Min(0)] public float focusPadding = .55f;

        Camera view;
        FocusState focus;
        float normalSize;
        ActorBounds playerBounds;
        ActorBounds enemyBounds;
        DamageableActor framedEnemy;

        void Awake()
        {
            view = GetComponent<Camera>();
            normalSize = view.orthographicSize;
            if (target)
            {
                focus = target.GetComponent<FocusState>();
                playerBounds = new ActorBounds(target);
            }
        }

        void LateUpdate()
        {
            if (!target)
                return;

            Quaternion rotation = Quaternion.LookRotation(-offset);
            float blend = 1 - Mathf.Exp(-smoothing * Time.deltaTime);
            Vector3 anchor = target.position;
            float desiredSize = normalSize;
            float safeSize = 0;

            DamageableActor enemy = focus && view.orthographic ? focus.Target : null;
            float strength = enemy ? Mathf.Clamp01(focus.Progress) : 0;
            if (strength > 0)
            {
                if (framedEnemy != enemy)
                {
                    framedEnemy = enemy;
                    enemyBounds = new ActorBounds(enemy.transform);
                }

                Vector3 right = rotation * Vector3.right;
                Vector3 up = rotation * Vector3.up;
                Rect pair = ProjectPair(playerBounds.Read(), enemyBounds.Read(), right, up);
                float aspect = Mathf.Max(.01f, view.aspect);
                float tightSize = RequiredSize(pair, pair.center, aspect);
                desiredSize = Mathf.Lerp(normalSize, tightSize, strength);

                Vector2 playerAnchor = Project(anchor, right, up);
                Vector2 center = Vector2.Lerp(playerAnchor, pair.center, strength);
                Vector2 shift = center - playerAnchor;
                anchor += right * shift.x + up * shift.y;
                Vector3 nextPosition = Vector3.Lerp(transform.position, anchor + offset, blend);
                safeSize = RequiredSize(pair, Project(nextPosition, right, up), aspect);
            }

            transform.SetPositionAndRotation(Vector3.Lerp(transform.position, anchor + offset, blend), rotation);
            if (view.orthographic)
                view.orthographicSize = Mathf.Max(safeSize, Mathf.Lerp(view.orthographicSize, desiredSize, blend));
        }

        float RequiredSize(Rect pair, Vector2 center, float aspect)
        {
            float halfWidth = Mathf.Max(pair.xMax - center.x, center.x - pair.xMin);
            float halfHeight = Mathf.Max(pair.yMax - center.y, center.y - pair.yMin);
            return Mathf.Max(.01f, (halfWidth + focusPadding) / aspect, halfHeight + focusPadding);
        }

        static Rect ProjectPair(Bounds first, Bounds second, Vector3 right, Vector3 up)
        {
            Rect a = ProjectBounds(first, right, up);
            Rect b = ProjectBounds(second, right, up);
            return Rect.MinMaxRect(Mathf.Min(a.xMin, b.xMin), Mathf.Min(a.yMin, b.yMin),
                Mathf.Max(a.xMax, b.xMax), Mathf.Max(a.yMax, b.yMax));
        }

        static Rect ProjectBounds(Bounds bounds, Vector3 right, Vector3 up)
        {
            Vector2 center = Project(bounds.center, right, up);
            Vector2 extent = new Vector2(ProjectExtent(bounds.extents, right), ProjectExtent(bounds.extents, up));
            return Rect.MinMaxRect(center.x - extent.x, center.y - extent.y, center.x + extent.x, center.y + extent.y);
        }

        static Vector2 Project(Vector3 point, Vector3 right, Vector3 up) =>
            new Vector2(Vector3.Dot(point, right), Vector3.Dot(point, up));

        static float ProjectExtent(Vector3 extent, Vector3 axis) =>
            extent.x * Mathf.Abs(axis.x) + extent.y * Mathf.Abs(axis.y) + extent.z * Mathf.Abs(axis.z);

        sealed class ActorBounds
        {
            readonly Transform root;
            readonly Collider body;
            readonly SkinnedMeshRenderer[] skins;

            public ActorBounds(Transform root)
            {
                this.root = root;
                body = root.GetComponent<Collider>();
                skins = root.GetComponentsInChildren<SkinnedMeshRenderer>();
            }

            public Bounds Read()
            {
                Bounds bounds = body && body.enabled ? body.bounds : new Bounds(root.position, Vector3.one);
                foreach (var skin in skins)
                {
                    if (skin && skin.enabled && skin.gameObject.activeInHierarchy)
                        bounds.Encapsulate(skin.bounds);
                }
                return bounds;
            }
        }
    }
}
