using System;
using UnityEngine;

namespace FinalCharacterController
{
    [DefaultExecutionOrder(-1)]
    public class PlayerController : MonoBehaviour
    {
        #region Class Variables
        [Header("Components")]
        [SerializeField] private CharacterController _characterController;
        [SerializeField] private Camera _playerCamera;
        public float RotationMisMatch { get; private set; } = 0f;
        public bool IsRotatingToTarget { get; private set; } = false;
        
        [Header("Base Movement")]
        public float walkAcceleration = 0.15f;
        public float walkSpeed = 3f;
        public float runAcceleration = 0.25f;
        public float runSpeed = 6f;
        public float sprintAcceleration = 0.5f;
        public float sprintSpeed = 9f;
        public float inAirAcceleration = 0.15f;
        public float drag = 0.1f;
        public float gravity = 25f;
        public float jumpSpeed = 1.0f;
        public float movingThreshold = 0.01f;

        [Header("Animation")] 
        public float playerModelRotationSpeed = 10f;
        public float rotateToTargetTime = 0.25f;
        
        [Header("Camera Settings")]
        public float lookSenseH = 0.1f;
        public float lookSenseV = 0.1f;
        public float lookLimitV = 89f;
        
        [Header("Environment Details")]
        [SerializeField] private LayerMask _groundLayers;
        
        private PlayerLocomotionInput _playerLocomotionInput;
        private PlayerState _playerState;
        
        private Vector2 _cameraRotation = Vector2.zero;
        private Vector2 _playerTargetRotation = Vector2.zero;

        private bool _jumpedLastFrame = false;
        private bool _isRotatingClockwise = false;
        private float _rotatingToTargetTimer = 0f;
        private float _verticalVelocity = 0f;
        private float _antiBump;
        private float _stepOffSet;

        private PlayerMovementState _lastMovementState  = PlayerMovementState.Falling;
        #endregion
        
        #region Startup
        private void Awake()
        {
            _playerLocomotionInput = GetComponent<PlayerLocomotionInput>();
            _playerState = GetComponent<PlayerState>();
            _antiBump = sprintSpeed;
            _stepOffSet = _characterController.stepOffset;
        }
        #endregion

        #region Update Logic
        private void Update()
        {
            UpdateMovementState();
            HandleVerticalMovement();
            HandleLateralMovement();
        }
        
        private void HandleVerticalMovement()
        {
            bool isGrounded = _playerState.IsGroundedState();
            _verticalVelocity -= gravity * Time.deltaTime;
            
            if(isGrounded && _verticalVelocity < 0f)
            {
                _verticalVelocity = -_antiBump;
            }

            if (_playerLocomotionInput.JumpPressed && isGrounded)
            {
                _verticalVelocity += Mathf.Sqrt(jumpSpeed * 3f * gravity);
                _jumpedLastFrame = true;
            }

            if (_playerState.IsStateGroundedState(_lastMovementState) && !isGrounded)
            {
                _verticalVelocity += _antiBump;
            }
 
        }
        
        private void HandleLateralMovement()
        {
            //quick reference for current state
            bool isSprinting = _playerState.CurrentPlayerMovementState == PlayerMovementState.Sprinting;
            bool isGrounded = _playerState.IsGroundedState();
            bool isWalking = _playerState.CurrentPlayerMovementState == PlayerMovementState.Walking;
            
            //state dependent acceleration and speed
            float lateralAcceleration = !isGrounded ? inAirAcceleration : isWalking ? walkAcceleration : isSprinting ? sprintAcceleration : runAcceleration;
            float clampLateralMagnitude = !isGrounded ? sprintSpeed : isWalking ? walkSpeed : isSprinting ? sprintSpeed : runSpeed;
            
            
            Vector3 cameraForwardXZ = new Vector3(_playerCamera.transform.forward.x, 0, _playerCamera.transform.forward.z).normalized;
            Vector3 cameraRightXZ = new Vector3(_playerCamera.transform.right.x, 0, _playerCamera.transform.right.z).normalized;
            Vector3 movementDirection = cameraForwardXZ * _playerLocomotionInput.MovementInput.y + cameraRightXZ * _playerLocomotionInput.MovementInput.x;
            
            Vector3 movementDelta = movementDirection * lateralAcceleration;
            Vector3 newVelocity = _characterController.velocity + movementDelta;
            newVelocity.y += _verticalVelocity;
            newVelocity = !isGrounded ? HandleSteepWalls(newVelocity) : newVelocity;
            
            // Add drag to player
            Vector3 currentDrag = newVelocity.normalized * drag * Time.deltaTime;
            newVelocity = newVelocity.magnitude > drag * Time.deltaTime ? newVelocity - currentDrag : Vector3.zero;
            newVelocity = Vector3.ClampMagnitude(new Vector3(newVelocity.x,0f, newVelocity.z) ,clampLateralMagnitude );
            newVelocity.y += _verticalVelocity;
            
            
            // Move character once per frame
            _characterController.Move(newVelocity * Time.deltaTime);
        }
        
        private void UpdateMovementState()
        {
            _lastMovementState = _playerState.CurrentPlayerMovementState;
            bool canRun = CanRun();
            bool isMovementInput = _playerLocomotionInput.MovementInput != Vector2.zero; // order
            bool isMovingLaterally = IsMovingLaterally();                                // matter   
            bool isSprinting = _playerLocomotionInput.SprintToggleOn && isMovingLaterally; // order 
            bool isWalking = (!canRun && isMovingLaterally) || _playerLocomotionInput.WalkToggleOn; // matters
            bool isGrounded = IsGrounded();
            
            PlayerMovementState lateralState = isWalking ? PlayerMovementState.Walking : 
                isSprinting ? PlayerMovementState.Sprinting :
                isMovingLaterally || isMovementInput
                ? PlayerMovementState.Running
                : PlayerMovementState.Idling;
            
            _playerState.setPlayerMovementState(lateralState);
            
            // Control Airborne state
            if ((!isGrounded || _jumpedLastFrame) && _characterController.velocity.y >= 0f)
            {
                _playerState.setPlayerMovementState(PlayerMovementState.Jumping);
                _jumpedLastFrame = false;
                _characterController.stepOffset = 0f;
            }
            else if ((!isGrounded || _jumpedLastFrame) && _characterController.velocity.y < 0f)
            {
                _playerState.setPlayerMovementState(PlayerMovementState.Falling);
                _jumpedLastFrame = false;
                _characterController.stepOffset = 0f;
            }
            else
            {
                _characterController.stepOffset = _stepOffSet;
            }
        }

        private Vector3 HandleSteepWalls(Vector3 velocity)
        {
            Vector3 normal = CharacterControllerUtils.GetNormalWithSphereCast(_characterController, _groundLayers);
            float angle = Vector3.Angle(normal ,Vector3.up);
            bool validAngle = angle <= _characterController.slopeLimit;
            if (!validAngle)
            {
                velocity = Vector3.ProjectOnPlane(velocity, normal);
            }
            return velocity;
        }
        
        #endregion

        #region LateUpdate Logic
        private void LateUpdate()
        {
           UpdateCameraRotation();
        }

        private void UpdateCameraRotation()
        {
            _cameraRotation.x += lookSenseH * _playerLocomotionInput.LookInput.x;
            _cameraRotation.y = Mathf.Clamp(_cameraRotation.y - lookSenseV * _playerLocomotionInput.LookInput.y, -lookLimitV, lookLimitV);
            
            _playerTargetRotation.x += transform.eulerAngles.x + lookSenseH * _playerLocomotionInput.LookInput.x;
            
            float rotationTolerance = 90f;
            bool isIdling = _playerState.CurrentPlayerMovementState == PlayerMovementState.Idling;
            IsRotatingToTarget = _rotatingToTargetTimer > 0f;

            // rotate if we are not idling
            if (!isIdling)
            {
                RotatePlayerToTarget();
            }
            //If rotation mismatch is not within tolerance, or rotate to target is active , Rotate
            else if (Mathf.Abs(RotationMisMatch) > rotationTolerance || IsRotatingToTarget)
            {
                UpdateIdlingRotation(rotationTolerance);
            }
            
            _playerCamera.transform.rotation = Quaternion.Euler(_cameraRotation.y, _cameraRotation.x, 0f);

            //Get Angle between camera and player
            Vector3 camForwardProjectedXZ = new Vector3(_playerCamera.transform.forward.x, 0f, _playerCamera.transform.forward.z).normalized;
            Vector3 crossProduct = Vector3.Cross(transform.forward, camForwardProjectedXZ);
            float sign = Mathf.Sign(Vector3.Dot(crossProduct, transform.up));
            RotationMisMatch = sign * Vector3.Angle(transform.forward, camForwardProjectedXZ);
        }

        private void UpdateIdlingRotation(float rotationTolerance)
        {
            // Initiating new rotation direction
            if (Mathf.Abs(RotationMisMatch) > rotationTolerance)
            {
                _rotatingToTargetTimer = rotateToTargetTime;
                _isRotatingClockwise = RotationMisMatch > rotationTolerance;
            }
            _rotatingToTargetTimer -= Time.deltaTime;

            if(_isRotatingClockwise && RotationMisMatch > 0f || !_isRotatingClockwise && RotationMisMatch < 0f)
            {
                RotatePlayerToTarget();
            }
        }

        private void RotatePlayerToTarget()
        {
            Quaternion targetRotationX = Quaternion.Euler(0f, _playerTargetRotation.x, 0f);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotationX,
                playerModelRotationSpeed * Time.deltaTime);   
        }

        #endregion

        #region State Checks
        
        private bool IsMovingLaterally()
        {
            Vector3 lateralVelocity = new Vector3(_characterController.velocity.x,0f, _characterController.velocity.z);
            return lateralVelocity.magnitude > movingThreshold;
        }
        
        private bool IsGrounded()
        {
            bool grounded = _playerState.IsGroundedState() ? IsGroundedWhileGrounded() : IsGroundedWhileAirborne(); 
            return grounded;
        }

        private bool IsGroundedWhileGrounded()
        {
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - _characterController.radius, transform.position.z);
            bool grounded = Physics.CheckSphere(spherePosition, _characterController.radius, _groundLayers,
                QueryTriggerInteraction.Ignore);
            return grounded;
        }

        private bool IsGroundedWhileAirborne()
        {
            Vector3 normal = CharacterControllerUtils.GetNormalWithSphereCast(_characterController, _groundLayers);
            float angle = Vector3.Angle(normal ,Vector3.up);
            bool validAngle = angle <= _characterController.slopeLimit;
            
            return _characterController.isGrounded && validAngle;
        }
        
        private bool CanRun()
        {
            // this means player is moving diagonally at an angle of 45 degrees or forward, if so we can run
            return _playerLocomotionInput.MovementInput.y >= Mathf.Abs(_playerLocomotionInput.MovementInput.x);
        }
        
        #endregion
 
    }
}
