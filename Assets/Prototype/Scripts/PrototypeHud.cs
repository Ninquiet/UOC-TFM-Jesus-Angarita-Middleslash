using UnityEngine;

namespace Aberracion
{
    public sealed class PrototypeHud : MonoBehaviour
    {
        public FocusState focus;

        void OnGUI()
        {
            if (!focus.IsHyper)
                return;

            float scale = Mathf.Clamp(Screen.width / 1280f, .75f, 2);
            Matrix4x4 previousMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));

            GUI.Label(new Rect(Screen.width / scale / 2 - 90, 35, 250, 30),
                focus.IsExiting ? "RECUPERANDO PERSPECTIVA" : "HIPERFOCO");
            GUI.matrix = previousMatrix;
        }
    }
}

