using UnityEngine;
using UnityEngine.EventSystems;

namespace Sandbox.MapEditor
{
    [DisallowMultipleComponent]
    public class MapEditorCameraController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Joystick _moveJoystick;
        [SerializeField] private CarControlButton _upButton;
        [SerializeField] private CarControlButton _downButton;
        [SerializeField] private CarControlButton _speedBoostButton;
        [SerializeField] private Transform _controlledCamera;

        [Header("Move")]
        [SerializeField] private float _moveSpeed = 10f;
        [SerializeField] private float _boostMultiplier = 2.5f;
        [SerializeField] private float _verticalSpeed = 8f;
        [SerializeField] private bool _moveForwardByCameraPitch = true;

        [Header("Look")]
        [SerializeField] private float _touchSensitivity = 0.15f;
        [SerializeField] private float _mouseSensitivity = 2.5f;
        [SerializeField] private float _minPitch = -89f;
        [SerializeField] private float _maxPitch = 89f;
        [SerializeField] private bool _ignoreTouchesOverUI = true;

        private Transform Target => _controlledCamera != null ? _controlledCamera : transform;

        private float _yaw;
        private float _pitch;
        private int _lookFingerId = -1;
        private Vector2 _lastTouchPosition;

        private void Awake()
        {
            Vector3 rotation = Target.rotation.eulerAngles;
            _yaw = rotation.y;
            _pitch = NormalizeAngle(rotation.x);
        }

        private void OnEnable()
        {
            _lookFingerId = -1;
        }

        private void Update()
        {
            HandleTouchLook();
            HandleEditorLook();
            HandleMovement();
        }

        private void HandleMovement()
        {
            Vector2 input = _moveJoystick != null ? _moveJoystick.Direction : Vector2.zero;
            Vector3 forward = _moveForwardByCameraPitch ? Target.forward : Vector3.ProjectOnPlane(Target.forward, Vector3.up).normalized;
            Vector3 right = Vector3.ProjectOnPlane(Target.right, Vector3.up).normalized;

            if (forward.sqrMagnitude < 0.001f)
                forward = Vector3.ProjectOnPlane(Target.up, Vector3.up).normalized;

            Vector3 move = forward * input.y + right * input.x;
            float vertical = 0f;

            if (_upButton != null && _upButton.Pressed)
                vertical += 1f;

            if (_downButton != null && _downButton.Pressed)
                vertical -= 1f;

            move += Vector3.up * vertical * (_verticalSpeed / Mathf.Max(_moveSpeed, 0.001f));

            Vector3 keyboardMove = GetKeyboardMove();
            if (keyboardMove.sqrMagnitude > 0f)
                move += keyboardMove;

            if (move.sqrMagnitude > 1f)
                move.Normalize();

            float speed = _moveSpeed;

            if ((_speedBoostButton != null && _speedBoostButton.Pressed) || IsKeyboardBoostPressed())
                speed *= _boostMultiplier;

            Target.position += move * speed * Time.deltaTime;
        }

        private void HandleTouchLook()
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);

                if (touch.position.x < Screen.width * 0.5f)
                    continue;

                if (IsControlButtonTouch(touch.fingerId))
                    continue;

                if (_ignoreTouchesOverUI && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                    continue;

                ProcessLookTouch(touch);
            }
        }

        private void ProcessLookTouch(Touch touch)
        {
            if (touch.phase == TouchPhase.Began)
            {
                _lookFingerId = touch.fingerId;
                _lastTouchPosition = touch.position;
            }

            if (touch.fingerId != _lookFingerId)
                return;

            if (touch.phase == TouchPhase.Moved)
            {
                Vector2 delta = touch.position - _lastTouchPosition;
                _lastTouchPosition = touch.position;
                ApplyLook(delta * _touchSensitivity);
            }

            if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                _lookFingerId = -1;
            }
        }

        private void ApplyLook(Vector2 delta)
        {
            _yaw += delta.x;
            _pitch -= delta.y;
            _pitch = Mathf.Clamp(_pitch, _minPitch, _maxPitch);

            Target.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        }

        private bool IsControlButtonTouch(int pointerId)
        {
            return (_upButton != null && _upButton.PointerId == pointerId)
                || (_downButton != null && _downButton.PointerId == pointerId)
                || (_speedBoostButton != null && _speedBoostButton.PointerId == pointerId);
        }

        private static float NormalizeAngle(float angle)
        {
            return angle > 180f ? angle - 360f : angle;
        }

        private Vector3 GetKeyboardMove()
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            Vector3 move = Vector3.zero;

            if (Input.GetKey(KeyCode.W))
                move += Target.forward;
            if (Input.GetKey(KeyCode.S))
                move -= Target.forward;
            if (Input.GetKey(KeyCode.D))
                move += Vector3.ProjectOnPlane(Target.right, Vector3.up).normalized;
            if (Input.GetKey(KeyCode.A))
                move -= Vector3.ProjectOnPlane(Target.right, Vector3.up).normalized;
            if (Input.GetKey(KeyCode.E))
                move += Vector3.up;
            if (Input.GetKey(KeyCode.Q))
                move -= Vector3.up;

            return move;
#else
            return Vector3.zero;
#endif
        }

        private bool IsKeyboardBoostPressed()
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
#else
            return false;
#endif
        }

        private void HandleEditorLook()
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            if (!Input.GetMouseButton(1))
                return;

            Vector2 delta = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
            ApplyLook(delta * _mouseSensitivity);
#endif
        }
    }
}
