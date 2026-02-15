using UnityEngine;
using UnityEngine.UI;

public class CarEnterModel : MonoBehaviour
{
    [SerializeField] private GameObject _carControllUI;
    [SerializeField] private GameObject[] _playerControllUI;
    [SerializeField] private CarControlButton _handBrakeButton;
    [SerializeField] private CarControlButton _throttleButton;
    [SerializeField] private CarControlButton _brakeButton;
    [SerializeField] private FloatingJoystick _moveJoystick;
    [Space(20)]
    [SerializeField] private Button _enterButton;
    [SerializeField] private Button _exitButton;

    private Rigidbody _rb;
    private CarController _selectedCar;
    private Camera _cam;

    private void Start()
    {
        _cam = Camera.main;
        _rb = GetComponent<Rigidbody>();

        _enterButton.onClick.AddListener(EnterCarButton);
        _exitButton.onClick.AddListener(ExitCarButton);
    }

    private void Update()
    {
        Ray ray = _cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 10, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.gameObject.tag == "Car")
            {
                _enterButton.gameObject.SetActive(true);
            }
            else
            {
                _enterButton.gameObject.SetActive(false);
            }
        }
    }

    public void EnterCarButton()
    {
        Ray ray = _cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 10, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.gameObject.tag == "Car")
            {
                _selectedCar = hit.collider.GetComponent<CarController>();
                
                _enterButton.gameObject.SetActive(false);
                _exitButton.gameObject.SetActive(true);

                _selectedCar.InitCar(_carControllUI, _playerControllUI, 
                    _handBrakeButton, _throttleButton, _brakeButton, _moveJoystick, transform);
            }
        }
    }

    public void ExitCarButton()
    {
        if (_selectedCar != null)
        {
            _selectedCar.SeatInCar(false, transform);
            _enterButton.gameObject.SetActive(true);
            _exitButton.gameObject.SetActive(false);

            if (_rb != null)
            {
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
                _rb.rotation = Quaternion.identity;
            }

            transform.rotation = Quaternion.identity;
        }
    }
}