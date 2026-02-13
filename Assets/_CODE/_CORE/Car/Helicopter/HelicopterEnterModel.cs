using Sandbox.Helicopter;
using UnityEngine;
using UnityEngine.UI;

namespace Sandbox.Helicoper
{
    public class HelicopterEnterModel : MonoBehaviour
    {
        [SerializeField] private GameObject _helicopterControllUI;
        [SerializeField] private GameObject[] _playerControllUI;
        [SerializeField] private FixedJoystick _helicopterJoy;

        [SerializeField] private CarControlButton _liftButton;
        [SerializeField] private CarControlButton _landButton;

        [SerializeField] private CarControlButton _yawLeftButton;
        [SerializeField] private CarControlButton _yawRightButton;

        [Space(20)]
        [SerializeField] private Button _enterButton;
        [SerializeField] private Button _exitButton;
        private GameObject _tankUI;
        private HelicopterMovement _selectedHellicopter;
        private Camera _cam;

        private void Start()
        {
            _cam = Camera.main;

            _enterButton.onClick.AddListener(EnterHellicopterButton);
            _exitButton.onClick.AddListener(ExitHelicopterButton);
        }

        private void Update()
        {
            Ray ray = _cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 10, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.gameObject.tag == "Helicopter")
                {
                    _enterButton.gameObject.SetActive(true);
                }
                else
                {
                    _enterButton.gameObject.SetActive(false);
                }
            }
        }

        public void EnterHellicopterButton()
        {
            Ray ray = _cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 10, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.gameObject.tag == "Helicopter")
                {
                    _selectedHellicopter = hit.collider.GetComponent<HelicopterMovement>();

                    _enterButton.gameObject.SetActive(false);
                    _exitButton.gameObject.SetActive(true);

                    _selectedHellicopter.InitHelicopter(_helicopterJoy, _liftButton, _landButton, _yawLeftButton, _yawRightButton,
                    transform, _helicopterControllUI, _playerControllUI);
                }
            }
        }

        public void ExitHelicopterButton()
        {
            if (_selectedHellicopter != null)
            {
                _selectedHellicopter.SeatInHelicopter(false, transform);

                _enterButton.gameObject.SetActive(true);
                _exitButton.gameObject.SetActive(false);
            }
        }
    }
}