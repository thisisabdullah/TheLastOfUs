using UnityEngine;
using System.Collections;

public class EllieController : MonoBehaviour
{
    [Header("Settings")]
    public float WalkSpeed;
    public float CrouchSpeed;
    public float SprintSpeed;
    public float RotationSpeed;
    public float SpeedChangeRate = 10f;
    public float MouseSensitivity = 100f;
    public float TopClamp = 70.0f;
    public float BottomClamp = -30.0f;
    public float CameraAngleOverride = 0.0f;

    [Header("References")]
    public Transform CinemachineCameraTarget;

    [Header("KeyCodes")]
    public KeyCode PickupKey;
    public KeyCode CrouchKey;
    public KeyCode RollForwardKey;

    private Animator _animator;
    private Vector3 _moveDirection;
    private CharacterController _characterController;

    private int _animIDSpeed;
    private int _animIDStrafe;
    private int _animIDPickup;
    private int _animIDCrouch;
    private int _animIDRollForward;

    private float _currentSpeed;
    private float _currentStrafe;
    private float _cinemachineTargetYaw;
    private float _cinemachineTargetPitch;

    private bool _isPickingUp = false;
    private bool _isCrouching;

    public bool IsCurrentDeviceMouse { get; private set; }

    private void Start()
    {
        _animator = GetComponent<Animator>();
        _characterController = GetComponent<CharacterController>();
        _cinemachineTargetYaw = CinemachineCameraTarget.eulerAngles.y;
        AssignAnimationIDs();
    }

    private void AssignAnimationIDs()
    {
        _animIDSpeed = Animator.StringToHash("Speed");
        _animIDStrafe = Animator.StringToHash("Strafe");
        _animIDPickup = Animator.StringToHash("Pickup");
        _animIDCrouch = Animator.StringToHash("Crouch");
        _animIDRollForward = Animator.StringToHash("RollForward");
    }

    private void Update()
    {
        CameraRotation();
        HandleMovement();
        Pickup();
        Crouch();
    }

    private void CameraRotation()
    {
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        // Adjust yaw and pitch without Time.deltaTime for mouse input
        float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

        _cinemachineTargetYaw += mouseX * MouseSensitivity * deltaTimeMultiplier;
        _cinemachineTargetPitch -= mouseY * MouseSensitivity * deltaTimeMultiplier;

        // Clamp the pitch to avoid extreme up/down camera angles
        _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

        // Apply rotation to the camera target
        Quaternion targetRotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride, _cinemachineTargetYaw, 0.0f);

        // Smoothly rotate the camera target
        CinemachineCameraTarget.rotation = Quaternion.Slerp(CinemachineCameraTarget.rotation, targetRotation, Time.deltaTime * RotationSpeed);
    }


    private static float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360f) angle += 360f;
        if (angle > 360f) angle -= 360f;
        return Mathf.Clamp(angle, min, max);
    }

    private void HandleMovement()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        bool isRunning = Input.GetKey(KeyCode.LeftShift);

        Vector3 inputDirection = new Vector3(horizontal, 0, vertical).normalized;

        if (inputDirection.magnitude >= 0.1f && !_isPickingUp)
        {
            float targetSpeed = _isCrouching ? WalkSpeed : (vertical < 0 ? WalkSpeed : (isRunning ? SprintSpeed : WalkSpeed));
            _moveDirection = Quaternion.Euler(0, CinemachineCameraTarget.eulerAngles.y, 0) * inputDirection;

            // Check if rolling forward
            if (Input.GetKeyDown(RollForwardKey))
            {
                RollForward();
                targetSpeed = WalkSpeed; // Limit speed to walk when rolling
            }

            // Animation speed blending
            float forwardSpeed = vertical > 0 ? targetSpeed : -WalkSpeed;
            float strafeSpeed = horizontal != 0 ? (vertical >= 0 ? horizontal * 2 : horizontal) : 0;

            _currentSpeed = Mathf.Lerp(_currentSpeed, forwardSpeed, Time.deltaTime * SpeedChangeRate);
            _currentStrafe = Mathf.Lerp(_currentStrafe, strafeSpeed, Time.deltaTime * SpeedChangeRate);

            _animator.SetFloat(_animIDSpeed, _currentSpeed);
            _animator.SetFloat(_animIDStrafe, _currentStrafe);

            Quaternion targetRotation = Quaternion.Euler(0, CinemachineCameraTarget.eulerAngles.y, 0);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * RotationSpeed);

            _characterController.Move(targetSpeed * Time.deltaTime * _moveDirection);
        }
        else
        {
            _currentSpeed = Mathf.Lerp(_currentSpeed, 0f, Time.deltaTime * SpeedChangeRate);
            _currentStrafe = Mathf.Lerp(_currentStrafe, 0f, Time.deltaTime * SpeedChangeRate);
            _animator.SetFloat(_animIDSpeed, _currentSpeed);
            _animator.SetFloat(_animIDStrafe, _currentStrafe);
        }
    }

    // Roll forward logic
    private void RollForward()
    {
        _animator.SetTrigger(_animIDRollForward);  // Trigger roll animation
        StartCoroutine(RollMovement());  // Start coroutine to move the player during the roll
    }

    // Coroutine for smooth movement during roll
    private IEnumerator RollMovement()
    {
        float rollDuration = 0.8f; // Duration of the roll animation
        float elapsedTime = 0f;
        float rollDistance = 3f; // Adjust how far you want the player to move

        // Move player forward during the roll animation
        while (elapsedTime < rollDuration)
        {
            // Move the player forward based on the direction they're facing
            Vector3 rollDirection = transform.forward; // Roll in the forward direction
            _characterController.Move((rollDistance / rollDuration) * Time.deltaTime * rollDirection);

            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }


    private void Crouch()
    {
        if (Input.GetKeyDown(CrouchKey))
        {
            _isCrouching = !_isCrouching;  // Toggle crouch state
            _animator.SetBool(_animIDCrouch, _isCrouching);
        }
    }

    private void Pickup()
    {
        if(Input.GetKeyDown(PickupKey) && !_isPickingUp)
        {
            _animator.SetTrigger(_animIDPickup);
            _isPickingUp = true;
        }
    }

    public void HandlePickupFromAnimation() // Called from Animation event
    {
        _isPickingUp = false;
    }
}
