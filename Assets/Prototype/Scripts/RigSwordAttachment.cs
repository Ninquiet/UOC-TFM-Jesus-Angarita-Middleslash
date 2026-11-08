using UnityEngine;

namespace Aberracion
{
    [DefaultExecutionOrder(100)]
    public sealed class RigSwordAttachment : MonoBehaviour
    {
        public Transform weapon;
        public Transform grip;
        public Transform bladeDirection;
        public Transform bladeUp;

        void LateUpdate()
        {
            Vector3 forward = bladeDirection.position - grip.position;
            Vector3 up = bladeUp.position - grip.position;
            if (forward.sqrMagnitude > .000001f && up.sqrMagnitude > .000001f)
                weapon.SetPositionAndRotation(grip.position, Quaternion.LookRotation(forward, up));
        }
    }
}
