using System;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FinalCharacterController
{
    [DefaultExecutionOrder(-2)]
    public class PlayerActionsInput : MonoBehaviour, PlayerControls.IPlayerActionMapActions
    {
        #region Class Variables 
        private PlayerLocomotionInput _playerLocomotionInput;
        private PlayerState _playerState;
        public bool AttackPressed { get; private set; }
        public bool GatherPressed { get; private set; }
 
        #endregion
        
        #region Startup

        private void Awake()
        {
            _playerLocomotionInput = GetComponent<PlayerLocomotionInput>();
            _playerState = GetComponent<PlayerState>();
        }
        
        private void OnEnable()
        {
            if (PlayerInputManager.Instance?.PlayerControls == null)
            {
                Debug.LogError("Player controls not initialized. cannot enable");
                return;
            }
            PlayerInputManager.Instance.PlayerControls.PlayerActionMap.Enable();
            PlayerInputManager.Instance.PlayerControls.PlayerActionMap.SetCallbacks(this);
        }

        private void OnDisable()
        {
            if (PlayerInputManager.Instance?.PlayerControls == null)
            {
                Debug.LogError("Player controls not initialized. cannot disable");
                return;
            }
            
            PlayerInputManager.Instance.PlayerControls.PlayerActionMap.Disable();
            PlayerInputManager.Instance.PlayerControls.PlayerActionMap.RemoveCallbacks(this);
        }
        #endregion

        #region update

        private void Update()
        {
            if (_playerLocomotionInput.MovementInput != Vector2.zero ||
                _playerState.CurrentPlayerMovementState == PlayerMovementState.Jumping ||
                _playerState.CurrentPlayerMovementState == PlayerMovementState.Falling )                                          
            {
                GatherPressed = false;
            }
        }


        private void SetGatherPressedFalse()
        {
            GatherPressed = false;
        }

        private void SetAttackPressedFalse()
        {
            AttackPressed = false;
        }

        #endregion
        
        #region InputCallbacks
        public void OnAttack(InputAction.CallbackContext context)
        {
            if (!context.performed)
                return;
            AttackPressed = true;
        }

        public void OnGather(InputAction.CallbackContext context)
        {
            if (!context.performed)
                return;
            GatherPressed = true;
        }
        #endregion
    }
}
