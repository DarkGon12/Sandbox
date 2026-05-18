using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Sandbox.Tank
{
    [RequireComponent(typeof(Rigidbody))]
    public class TankController : MonoBehaviour
    {
        [Header("Wheels")]
        [SerializeField] private WheelCollider _flWheel;
        [SerializeField] private WheelCollider _frWheel;
        [SerializeField] private WheelCollider _rlWheel;
        [SerializeField] private WheelCollider _rrWheel;

        [Header("Movement")]
        [SerializeField] private float _motorTorque = 3500f;
        [SerializeField] private float _brakeTorque = 5000f;
        [SerializeField] private float _maxSpeed = 6f;

        [Header("Input")]
        [SerializeField] private Joystick _joystick;          // ТОЛЬКО X
        [SerializeField] private CarControlButton _gasButton;
        [SerializeField] private CarControlButton _reverseButton;

        [SerializeField] private Transform _exitPoint;
        [SerializeField] private GameObject _tankCam;

        private GameObject[] _playerControllUI;
        private GameObject _tankUI;

        private Camera _mainCam;
        private Transform _player;
        private bool _playerSeat = false;
        private bool _tankInit = false;
        private Rigidbody _rigidbody;
        private TankAttackModule _tankModule;

        private const float TURN_DEADZONE = 0.15f;

        private void Awake()
        {
            _tankModule = GetComponent<TankAttackModule>();
            _rigidbody = GetComponent<Rigidbody>();
            _rigidbody.centerOfMass = new Vector3(0f, -0.6f, 0f);

            _mainCam = Camera.main;
        }

        public void InitTank(GameObject tankUI, GameObject[] playerUI, Transform player, 
           Joystick tankJoy, Joystick turrentJoy, CarControlButton gas, CarControlButton revers, CarControlButton fire)
        {
            _joystick = tankJoy;
            _gasButton = gas;
            _reverseButton = revers;

            _tankModule.InitModule(fire, turrentJoy);

            _tankUI = tankUI;
            _playerControllUI = playerUI;
            _player = player;

            _tankInit = true;

            SeatInTank(true, player);
        }

        public void SeatInTank(bool isEnter, Transform player)
        {
            _tankModule.enabled = isEnter; 
            _tankUI.SetActive(isEnter);
            for (int i = 0; i < _playerControllUI.Length; i++)
                _playerControllUI[i].SetActive(!isEnter);

            _player = player;
            _player.gameObject.SetActive(!isEnter);
            _mainCam.gameObject.SetActive(!isEnter);
            _tankCam.gameObject.SetActive(isEnter);

            if (isEnter)
            {
                _player.SetParent(_exitPoint);
                _player.localPosition = Vector3.zero;
            }
            else
            {
                _player.SetParent(null);
                _player.position = _exitPoint.position;
            }

            _playerSeat = isEnter;
        }

        private void FixedUpdate()
        {
            if (!_tankInit) return;
            if (!_playerSeat) return;

            float turn = _joystick.Direction.x;
            float forwardInput = GetForwardInput();

            if (Mathf.Abs(turn) < TURN_DEADZONE)
                turn = 0f;

            if (forwardInput == 0f && turn != 0f)
            {
                if (turn < 0f)
                {
                    SetLeftTrack(-1f);
                    SetRightTrack(1f);
                }
                else
                {
                    SetLeftTrack(1f);
                    SetRightTrack(-1f);
                }
            }

            else
            {
                float left = forwardInput;
                float right = forwardInput;

                if (turn < 0f)
                {
                    left -= Mathf.Abs(turn);
                }
                else if (turn > 0f)
                {
                    right -= Mathf.Abs(turn);
                }

                SetLeftTrack(left);
                SetRightTrack(right);
            }

            LimitSpeed();
        }

        private float GetForwardInput()
        {
            if (_gasButton.Pressed)
                return 1f;

            if (_reverseButton.Pressed)
                return -1f;

            return 0f;
        }

        private void SetLeftTrack(float input)
        {
            input = Mathf.Clamp(input, -1f, 1f);

            ApplyWheel(_flWheel, input);
            ApplyWheel(_rlWheel, input);
        }

        private void SetRightTrack(float input)
        {
            input = Mathf.Clamp(input, -1f, 1f);

            ApplyWheel(_frWheel, input);
            ApplyWheel(_rrWheel, input);
        }

        private void ApplyWheel(WheelCollider wheel, float input)
        {
            bool active = Mathf.Abs(input) > 0.01f;

            wheel.motorTorque = active ? input * _motorTorque : 0f;
            wheel.brakeTorque = active ? 0f : _brakeTorque;
        }

        private void LimitSpeed()
        {
            Vector3 v = _rigidbody.linearVelocity;
            Vector3 flat = new Vector3(v.x, 0f, v.z);

            if (flat.magnitude > _maxSpeed)
            {
                Vector3 limited = flat.normalized * _maxSpeed;
                _rigidbody.linearVelocity = new Vector3(limited.x, v.y, limited.z);
            }
        }

        private void OnDestroy()
        {
            if (_player == null)
                return;
            if (!_playerSeat)
                return;

            SeatInTank(false, _player);
        }
    }
}
