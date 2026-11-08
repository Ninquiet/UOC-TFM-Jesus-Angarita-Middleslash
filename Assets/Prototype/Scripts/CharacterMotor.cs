using UnityEngine;

namespace Aberracion
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class CharacterMotor : MonoBehaviour
    {
        [Min(0)] public float speed = 5f;
        [Min(0)] public float groundPressure = 9.8f;

        CharacterController controller;

        public float PlanarSpeed => Vector3.ProjectOnPlane(controller.velocity, Vector3.up).magnitude;

        void Awake() => controller = GetComponent<CharacterController>();

        public void Move(Vector3 worldDirection, float deltaTime, float speedFactor = 1f)
        {
            worldDirection.y = 0;
            Vector3 velocity = Vector3.ClampMagnitude(worldDirection, 1f) * speed * speedFactor;
            MoveBy(velocity * deltaTime, deltaTime);
        }

        public void MoveBy(Vector3 displacement, float deltaTime)
        {
            displacement.y = -groundPressure * deltaTime;
            controller.Move(displacement);
        }

        public void Face(Vector3 worldDirection)
        {
            worldDirection.y = 0;
            if (worldDirection.sqrMagnitude > .01f)
                transform.rotation = Quaternion.LookRotation(worldDirection);
        }
    }
}


