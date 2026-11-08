using UnityEngine;
using UnityEngine.InputSystem;

namespace Aberracion
{
    [RequireComponent(typeof(CharacterActionStateMachine))]
    public sealed class PrototypePlayer : MonoBehaviour
    {
        public InputActionAsset controls;
        public Camera viewCamera;

        public bool AcceptInput { get; set; } = true;

        InputActionAsset ownedControls;
        InputAction moveAction;
        InputAction attackAction;
        InputAction sheatheAction;
        CharacterActionStateMachine actions;

        void Awake()
        {
            actions = GetComponent<CharacterActionStateMachine>();
            ownedControls = Instantiate(controls);
            moveAction = ownedControls.FindAction("Player/Move", true);
            attackAction = ownedControls.FindAction("Player/Attack", true);
            sheatheAction = ownedControls.FindAction("Player/Sheathe", true);
        }

        void OnEnable() => moveAction.actionMap.Enable();
        void OnDisable() => moveAction.actionMap.Disable();
        void OnDestroy() => Destroy(ownedControls);

        void Update()
        {
            if (!AcceptInput)
                return;

            Vector2 input = moveAction.ReadValue<Vector2>();
            Vector3 right = Vector3.ProjectOnPlane(viewCamera.transform.right, Vector3.up).normalized;
            Vector3 forward = Vector3.Cross(right, Vector3.up);
            Vector3 direction = right * input.x + forward * input.y;
            var command = new CharacterActionInput(direction, ReadAim(),
                attackAction.WasPressedThisFrame(), sheatheAction.WasPressedThisFrame());
            actions.Tick(command, Time.deltaTime);
        }

        Vector3 ReadAim()
        {
            if (Mouse.current != null)
            {
                Ray ray = viewCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
                if (new Plane(Vector3.up, Vector3.zero).Raycast(ray, out float distance))
                    return ray.GetPoint(distance) - transform.position;
            }

            return transform.forward;
        }
    }
}
