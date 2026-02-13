using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Sandbox.Helicopter
{
    public class HelicopterMovement : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Rigidbody _rb;
        [SerializeField] private Transform _body;
        [SerializeField] private Transform _mainRotor;
        [SerializeField] private Transform _tailRotor;

        [SerializeField] private Transform _exitPoint;
        [SerializeField] private Camera _heliCam;

        [Header("Input")]
        [SerializeField] private Joystick _joy;
        [SerializeField] private CarControlButton _takeOffButton;
        [SerializeField] private CarControlButton _landButton;
        [SerializeField] private CarControlButton _yawLeftButton;
        [SerializeField] private CarControlButton _yawRightButton;

        [Header("Engine")]
        [SerializeField] private float _enginePower = 1f;
        [SerializeField] private float _engineSmooth = 2f;

        [Header("Lift")]
        [SerializeField] private float _additionalLift = 6f;
        [SerializeField] private float _liftSmooth = 2.5f;
        [SerializeField] private float _maxUpSpeed = 5f;
        [SerializeField] private float _maxDownSpeed = -2f;

        [Header("Movement")]
        [SerializeField] private float _moveForce = 12f;
        [SerializeField] private float _maxHorizontalSpeed = 8f;

        [Header("Tilt")]
        [SerializeField] private float _tiltAngle = 15f;
        [SerializeField] private float _tiltSmooth = 6f;

        [Header("Yaw")]
        [SerializeField] private float _yawTorque = 4f;
        [SerializeField] private float _yawSmooth = 4f;

        [Header("Rotors")]
        [SerializeField] private float _mainRotorSpeed = 900f;
        [SerializeField] private float _tailRotorSpeed = 1400f;

        // -------- runtime --------
        private bool _engineEnabled;
        private float _currentEnginePower;

        private float _targetLift;
        private float _currentLift;

        private float _currentYaw;

        private float _currentTiltX;
        private float _currentTiltZ;
        private float _tiltXVelocity;
        private float _tiltZVelocity;

        private GameObject[] _playerControllUI;
        private GameObject _heliUI;
        private Transform _player;
        private Camera _mainCam;

        // ================= ENGINE =================

        private void Start()
        {
            _mainCam = Camera.main;
        }

        public void InitHelicopter(Joystick joy, CarControlButton takeOff, CarControlButton land, 
            CarControlButton yawLeft, CarControlButton yawRight, Transform player, GameObject heliUI, GameObject[] playerUi)
        {
            _joy = joy;
            _takeOffButton = takeOff;
            _landButton = land;
            _yawLeftButton = yawLeft;
            _yawRightButton = yawRight;

            _heliUI = heliUI;
            _playerControllUI = playerUi;
            _player = player;

            SeatInHelicopter(true, player);
        }

        public void SeatInHelicopter(bool isEnter, Transform player)
        {
            _heliUI.SetActive(isEnter);
            for (int i = 0; i < _playerControllUI.Length; i++)
                _playerControllUI[i].SetActive(!isEnter);

            _player = player;
            _player.gameObject.SetActive(!isEnter);
            _mainCam.gameObject.SetActive(!isEnter);
            _heliCam.gameObject.SetActive(isEnter);

            if (isEnter)
            {
                EngineOn();
                _player.SetParent(_exitPoint);
                _player.localPosition = Vector3.zero;
            }
            else
            {
                EngineOff();
                _player.SetParent(null);
                _player.position = _exitPoint.position;
            }
        }

        [ContextMenu("EngineOn")]
        public void EngineOn()
        {
            _engineEnabled = true;
            _rb.linearDamping = 1.5f;
        }

        [ContextMenu("EngineOff")]
        public void EngineOff()
        {
            _engineEnabled = false;
            _rb.linearDamping = 0f;
        }

        // ================= UNITY =================

        private void Awake()
        {
            // физика НЕ крутит вертолёт по X/Z
            _rb.constraints =
                RigidbodyConstraints.FreezeRotationX |
                RigidbodyConstraints.FreezeRotationZ;
        }

        private void Update()
        {
            RotateRotors();
        }

        private void FixedUpdate()
        {
            UpdateEnginePower();
            if (!_engineEnabled) return;
            HandleLiftInput();

            if (_currentEnginePower > 0.01f)
            {
                ApplyLift();
                ApplyMovement();
                ApplyYaw();
            }

            HandleTilt();
            ClampVelocity();
        }

        // ================= ENGINE POWER =================

        private void UpdateEnginePower()
        {
            float target = _engineEnabled ? _enginePower : 0f;

            _currentEnginePower = Mathf.Lerp(
                _currentEnginePower,
                target,
                Time.fixedDeltaTime * _engineSmooth
            );
        }

        // ================= LIFT =================

        private void HandleLiftInput()
        {
            if (!_engineEnabled)
            {
                _targetLift = 0f;
                return;
            }

            if (_takeOffButton.Pressed)
                _targetLift = _additionalLift;
            else if (_landButton.Pressed)
                _targetLift = -_additionalLift * 0.6f;
            else
                _targetLift = 0f;

            _currentLift = Mathf.Lerp(
                _currentLift,
                _targetLift,
                Time.fixedDeltaTime * _liftSmooth
            );
        }

        private void ApplyLift()
        {
            float gravity = Physics.gravity.magnitude * _rb.mass;
            float lift = (gravity + _currentLift * _rb.mass) * _currentEnginePower;

            _rb.AddForce(Vector3.up * lift, ForceMode.Force);
        }

        // ================= MOVEMENT =================

        private void ApplyMovement()
        {
            Vector2 input = _joy.Direction;
            if (input.sqrMagnitude < 0.001f) return;

            Vector3 force =
                (_body.forward * input.y +
                 _body.right * input.x) *
                _moveForce *
                _currentEnginePower;

            _rb.AddForce(force, ForceMode.Force);
        }

        // ================= YAW =================

        private void ApplyYaw()
        {
            float yawInput = 0f;

            if (_yawLeftButton.Pressed)
                yawInput -= 1f;

            if (_yawRightButton.Pressed)
                yawInput += 1f;

            if (Mathf.Approximately(yawInput, 0f))
                yawInput = _joy.Direction.x;

            float targetYaw = yawInput * _yawTorque;

            _currentYaw = Mathf.Lerp(
                _currentYaw,
                targetYaw,
                Time.fixedDeltaTime * _yawSmooth
            );

            _rb.AddTorque(
                Vector3.up * _currentYaw * _currentEnginePower,
                ForceMode.Force
            );
        }

        // ================= TILT (NO JITTER) =================

        private void HandleTilt()
        {
            Vector2 input = _joy.Direction;

            float targetX = input.y * _tiltAngle;
            float targetZ = -input.x * _tiltAngle;

            _currentTiltX = Mathf.SmoothDampAngle(
                _currentTiltX,
                targetX,
                ref _tiltXVelocity,
                1f / _tiltSmooth,
                Mathf.Infinity,
                Time.fixedDeltaTime
            );

            _currentTiltZ = Mathf.SmoothDampAngle(
                _currentTiltZ,
                targetZ,
                ref _tiltZVelocity,
                1f / _tiltSmooth,
                Mathf.Infinity,
                Time.fixedDeltaTime
            );

            _body.localRotation = Quaternion.Euler(
                _currentTiltX,
                _body.localEulerAngles.y,
                _currentTiltZ
            );
        }

        // ================= ROTORS =================

        private void RotateRotors()
        {
            if (!_engineEnabled && _currentEnginePower < 0.05f) return;

            float speed = Mathf.Lerp(0.2f, 1f, _currentEnginePower);

            _mainRotor.Rotate(Vector3.up, _mainRotorSpeed * speed * Time.deltaTime);
            _tailRotor.Rotate(Vector3.right, _tailRotorSpeed * speed * Time.deltaTime);
        }

        // ================= LIMITS =================

        private void ClampVelocity()
        {
            Vector3 v = _rb.linearVelocity;

            v.y = Mathf.Clamp(v.y, _maxDownSpeed, _maxUpSpeed);

            Vector3 horizontal = new Vector3(v.x, 0f, v.z);
            if (horizontal.magnitude > _maxHorizontalSpeed)
            {
                horizontal = horizontal.normalized * _maxHorizontalSpeed;
                v.x = horizontal.x;
                v.z = horizontal.z;
            }

            _rb.linearVelocity = v;
        }
    }
}
