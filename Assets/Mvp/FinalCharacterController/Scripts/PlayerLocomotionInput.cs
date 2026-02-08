using UnityEngine;
using UnityEngine.InputSystem;

namespace FinalCharacterController
{
    [DefaultExecutionOrder(-2)]
    public class PlayerLocomotionInput : MonoBehaviour, PlayerControls.IPlayerLocomotionMapActions
    {
        [SerializeField] private bool holdToSprint = true;
        
        public bool SprintToggleOn { get; private set; }
        public PlayerControls PlayerControls { get; private set; }
        public Vector2 MovementInput { get; private set; }
        public Vector2 LookInput { get; private set; }
        
        private void Awake()
        {
            PlayerControls = new PlayerControls();
            PlayerControls.PlayerLocomotionMap.Enable();
            PlayerControls.PlayerLocomotionMap.SetCallbacks(this);
        }

        private void OnDisable()
        {
            PlayerControls.PlayerLocomotionMap.Disable();
            PlayerControls.PlayerLocomotionMap.RemoveCallbacks(this);
        }

        public void OnMovement(InputAction.CallbackContext context)
        {
            MovementInput = context.ReadValue<Vector2>();
            print(MovementInput);
        }

        public void OnLook(InputAction.CallbackContext context)
        {
           LookInput = context.ReadValue<Vector2>();
        }

        public void OnToggleSprint(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                SprintToggleOn = holdToSprint || !SprintToggleOn;
            }
            else if (context.canceled)
            {
                SprintToggleOn = !holdToSprint && SprintToggleOn;
            }
        }
    }
}
