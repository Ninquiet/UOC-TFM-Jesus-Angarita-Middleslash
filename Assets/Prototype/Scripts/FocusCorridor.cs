using UnityEngine;

namespace Aberracion
{
    public sealed class FocusCorridor : MonoBehaviour
    {
        public FocusState focus;
        public Camera viewCamera;
        [Range(0, 1)] public float opacity = .98f;
        [Range(.06f, .4f)] public float halfWidthAtFullFocus = .14f;
        [Range(.02f, .3f)] public float endPaddingAtFullFocus = .12f;

        [Range(0, .15f)] public float cornerRadius = .06f;
        [Range(.005f, .2f)] public float edgeFeather = .065f;

        [Min(.01f)] public float targetBlendDuration = .55f;

        public bool Visible { get; set; } = true;

        static readonly int MaskColor = Shader.PropertyToID("_Color");
        static readonly int CorridorFrame = Shader.PropertyToID("_CorridorFrame");
        static readonly int CorridorShape = Shader.PropertyToID("_CorridorShape");

        readonly Vector3[] corners = new Vector3[4];
        Material mask;
        Mesh overlay;
        int meshWidth;
        int meshHeight;
        float displayedClosure;
        Vector3 displayedTarget;

        void Awake()
        {
            var shader = Resources.Load<Shader>("FocusCorridorMask");
            mask = new Material(shader);
            overlay = new Mesh { name = "Focus corridor overlay" };
            overlay.vertices = corners;
            overlay.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        }

        void OnDestroy()
        {
            Destroy(mask);
            Destroy(overlay);
        }

        void LateUpdate()
        {
            var target = focus.Target;
            float desiredClosure = target ? focus.Progress : 0;
            float deltaTime = Time.deltaTime;
            float duration = Mathf.Max(.01f, targetBlendDuration);
            if (target)
            {
                float blend = 1 - Mathf.Exp(-4 * deltaTime / duration);
                displayedTarget = displayedClosure > 0
                    ? Vector3.Lerp(displayedTarget, target.transform.position, blend)
                    : target.transform.position;
            }

            displayedClosure = Mathf.MoveTowards(displayedClosure, desiredClosure, deltaTime / duration);
        }

        void OnDisable() => displayedClosure = 0;

        void OnGUI()
        {
            if (Event.current.type != EventType.Repaint || !Visible || displayedClosure <= 0)
                return;

            Vector3 player = viewCamera.WorldToScreenPoint(focus.transform.position + Vector3.up);
            Vector3 target = viewCamera.WorldToScreenPoint(displayedTarget);
            if (player.z <= 0 || target.z <= 0)
                return;

            Vector2 playerPoint = new Vector2(player.x, player.y);
            Vector2 targetPoint = new Vector2(target.x, target.y);
            DrawCorridor(playerPoint, targetPoint, displayedClosure);
        }

        void DrawCorridor(Vector2 player, Vector2 target, float closure)
        {
            Vector2 center = (player + target) * .5f;
            Vector2 line = target - player;

            Vector2 axis = line.sqrMagnitude > .01f ? line.normalized : Vector2.right;
            Vector2 normal = new Vector2(-axis.y, axis.x);
            Vector2 screenHalfSize = new Vector2(Screen.width, Screen.height) * .5f;
            Vector2 offsetFromScreenCenter = center - screenHalfSize;
            float horizontalExtent = ProjectScreenExtent(screenHalfSize, offsetFromScreenCenter, axis);
            float verticalExtent = ProjectScreenExtent(screenHalfSize, offsetFromScreenCenter, normal);

            float endPadding = Screen.height * endPaddingAtFullFocus;
            float narrowLength = Mathf.Min(horizontalExtent, line.magnitude * .5f + endPadding);
            float narrowWidth = Mathf.Min(verticalExtent, Screen.height * halfWidthAtFullFocus);
            float closing = 1 - (1 - closure) * (1 - closure);
            float halfLength = Mathf.Lerp(horizontalExtent, narrowLength, closing);
            float halfWidth = Mathf.Lerp(verticalExtent, narrowWidth, closing);

            UpdateOverlaySize();

            float radius = Mathf.Min(Screen.height * cornerRadius, Mathf.Min(halfLength, halfWidth));
            float feather = Mathf.Max(1, Screen.height * edgeFeather);
            float alpha = opacity * Mathf.Lerp(closure, 1, closure * closure);
            mask.SetColor(MaskColor, new Color(0, 0, 0, alpha));
            mask.SetVector(CorridorFrame, new Vector4(center.x, center.y, axis.x, axis.y));
            mask.SetVector(CorridorShape, new Vector4(halfLength, halfWidth, radius, feather));

            int previousDepth = GUI.depth;
            GUI.depth = 20;
            GL.PushMatrix();
            try
            {
                GL.LoadPixelMatrix(0, Screen.width, 0, Screen.height);
                if (mask.SetPass(0))
                    Graphics.DrawMeshNow(overlay, Matrix4x4.identity);
            }
            finally
            {
                GL.PopMatrix();
                GUI.depth = previousDepth;
            }
        }

        void UpdateOverlaySize()
        {
            if (meshWidth == Screen.width && meshHeight == Screen.height)
                return;

            meshWidth = Screen.width;
            meshHeight = Screen.height;
            corners[1] = new Vector3(meshWidth, 0, 0);
            corners[2] = new Vector3(meshWidth, meshHeight, 0);
            corners[3] = new Vector3(0, meshHeight, 0);
            overlay.vertices = corners;
        }

        static float ProjectScreenExtent(Vector2 halfSize, Vector2 offset, Vector2 axis)
        {
            return Mathf.Abs(Vector2.Dot(offset, axis))
                + halfSize.x * Mathf.Abs(axis.x)
                + halfSize.y * Mathf.Abs(axis.y) + 1;
        }

    }
}
