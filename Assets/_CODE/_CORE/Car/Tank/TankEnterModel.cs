using Sandbox.Tank;
using UnityEngine;
using UnityEngine.UI;

namespace Sandbox.Tank
{
    public class TankEnterModel : MonoBehaviour
    {

        [SerializeField] private GameObject _tankControllUI;
        [SerializeField] private GameObject[] _playerControllUI;
        [SerializeField] private FloatingJoystick _rotateJoy;
        [SerializeField] private FixedJoystick _turretJoy;

        [SerializeField] private CarControlButton _gasButton;
        [SerializeField] private CarControlButton _reversButton;
        [SerializeField] private CarControlButton _fireButton;
        [Space(20)]
        [SerializeField] private Button _enterButton;
        [SerializeField] private Button _exitButton;

        private Rigidbody _rb;
        private TankController _selectedTank;
        private Camera _cam;

        private void Start()
        {
            _cam = Camera.main;
            _rb = GetComponent<Rigidbody>();

            _enterButton.onClick.AddListener(EnterTankButton);
            _exitButton.onClick.AddListener(ExitTankButton);
        }

        private void Update()
        {
            Ray ray = _cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 10, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.gameObject.tag == "Tank")
                {
                    _enterButton.gameObject.SetActive(true);
                }
                else
                {
                    _enterButton.gameObject.SetActive(false);
                }
            }
        }

        public void EnterTankButton()
        {
            Ray ray = _cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 10, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.gameObject.tag == "Tank")
                {
                    _selectedTank = hit.collider.GetComponent<TankController>();

                    _enterButton.gameObject.SetActive(false);
                    _exitButton.gameObject.SetActive(true);

                    _selectedTank.InitTank(_tankControllUI, _playerControllUI, transform, _rotateJoy, _turretJoy, _gasButton, _reversButton, _fireButton);
                }
            }
        }

        public void ExitTankButton()
        {
            if (_selectedTank != null)
            {
                _selectedTank.SeatInTank(false, transform);

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
}