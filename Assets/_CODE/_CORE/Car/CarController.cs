using Unity.Scenes;
using UnityEngine;

public class CarController : MonoBehaviour
{
    [SerializeField] private Camera _carCam;
    [SerializeField] private Transform _exitPoint;

    private GameObject _carControllUI;
    private GameObject[] _playerControllUI;
    private CarControlButton _handBrakeButton;
    private CarControlButton _throttleButton;
    private CarControlButton _brakeButton;
    private FloatingJoystick _moveJoystick;

    private SCC_RigidStabilizer _rbStabilizer;
    private SCC_Drivetrain _driveTrain;
    private SCC_AntiRoll _antiRoll;
    private SCC_Audio _audio;

    private SCC_InputProcessor _inputProcessor;
    private SCC_Inputs _inputs;

    private Camera _mainCam;
    private Transform _player;

    private bool _carInit;
    private bool _inCar;

    private void Awake()
    {
        _carInit = false;
        _mainCam = Camera.main;

        _rbStabilizer = GetComponent<SCC_RigidStabilizer>();
        _driveTrain = GetComponent<SCC_Drivetrain>();
        _antiRoll = GetComponent<SCC_AntiRoll>();
        _audio = GetComponent<SCC_Audio>();

        _inputProcessor = GetComponent<SCC_InputProcessor>();
        _inputs = new SCC_Inputs();
    }

    public void InitCar(GameObject carUI, GameObject[] controllUI, CarControlButton handBrake, 
        CarControlButton throttle, CarControlButton brake, FloatingJoystick joystic, Transform player)
    {
        _carControllUI = carUI;
        _playerControllUI = controllUI;
        _handBrakeButton = handBrake;
        _throttleButton = throttle;
        _brakeButton = brake;
        _moveJoystick = joystic;
        _player = player;

        _carInit = true;

        SeatInCar(true, _player);
    }

    public void SeatInCar(bool isEnter, Transform player)
    {
        _carControllUI.SetActive(isEnter);
        for (int i = 0; i < _playerControllUI.Length; i++)
            _playerControllUI[i].SetActive(!isEnter);

        _player = player;
        _player.gameObject.SetActive(!isEnter);
        _mainCam.gameObject.SetActive(!isEnter);
        _carCam.gameObject.SetActive(isEnter);

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

        _inputProcessor.enabled = isEnter;
        _rbStabilizer.enabled = isEnter;
        _driveTrain.enabled = isEnter;
        _antiRoll.enabled = isEnter;
        _audio.enabled = isEnter;
        _inCar = isEnter;
    }

    private void Update()
    {
        if (!_carInit) return;

        float horizontal = _moveJoystick.Direction.x;

        // Газ
        _inputs.throttleInput = _throttleButton.Pressed ? 1f : 0f;

        // Тормоз / задний ход
        _inputs.brakeInput = _brakeButton.Pressed ? 1f : 0f;

        // Руль
        _inputs.steerInput = horizontal;

        // Ручник
        _inputs.handbrakeInput = _handBrakeButton.Pressed ? 1f : 0f;

        _inputProcessor.OverrideInputs(_inputs);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.transform.root.tag == "Creature")
        {
            CreatureRagdollHelper ragdoll = collision.transform.root.GetComponent<CreatureRagdollHelper>();
            ragdoll.EnableRagdoll();
            ragdoll.DisableRagdoll();
        }
    }
    private void OnDestroy()
    {
        if (_player == null)
            return;
        if (!_inCar)
            return;

        SeatInCar(false, _player);
    }
}