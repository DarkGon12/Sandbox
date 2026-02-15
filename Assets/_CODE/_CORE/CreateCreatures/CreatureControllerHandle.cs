using Creatures.Navigation;
using MessagePipe;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using Zenject;

namespace Creatures.Control
{
    public class CreatureControllerHandle : MonoBehaviour
    {
        public FloatingJoystick Joystick => _joystick;
        public CarControlButton AttackButton => _attackButton;
        public Button ExitButton => _exitControlButton;

        [Header("Detection")]
        [SerializeField] private float _rayDistance = 3f;
        [SerializeField] private LayerMask _creatureMask;

        [Header("UI")]
        [SerializeField] private Button _enterControlButton;
        [SerializeField] private Button _exitControlButton;
        [SerializeField] private FloatingJoystick _joystick;
        [SerializeField] private CarControlButton _attackButton;
        [SerializeField] private List<GameObject> _playerUI;

        [SerializeField] private GameObject _playerRoot;

        [Inject] private IPublisher<ChangePlayerTagEvent> _changePlayerTagEventPub;

        private bool _canControll = true;
        private CreatureInformation _currentCreature;

        private void OnEnable()
        {
            _enterControlButton.onClick.AddListener(TakeControl);
            _exitControlButton.onClick.RemoveAllListeners();
            _exitControlButton.gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            _enterControlButton.onClick.RemoveAllListeners();
            _exitControlButton.onClick.RemoveAllListeners();
        }

        private void Update()
        {
            if (!_canControll) return;
            TryDetectCreature();
        }

        private void TryDetectCreature()
        {
            if (!_enterControlButton.gameObject.activeSelf)
                _enterControlButton.gameObject.SetActive(false);

            _currentCreature = null;

            if (!Physics.Raycast(
                Camera.main.transform.position,
                Camera.main.transform.forward,
                out RaycastHit hit,
                _rayDistance,
                _creatureMask,
                QueryTriggerInteraction.Ignore))
                return;

            Transform root = hit.transform.root;
            if (!root.CompareTag("Creature"))
                return;

            _currentCreature = root.GetComponent<CreatureInformation>();
            if (_currentCreature == null)
                return;

            _enterControlButton.gameObject.SetActive(true);
        }

        public void TakeControl()
        {
            if (_currentCreature == null)
                return;

            GameObject creatureGO = _currentCreature.gameObject;

            creatureGO.GetComponent<NavMeshAgent>().enabled = false;
            creatureGO.GetComponent<CreatureStateView>().enabled = false;
            creatureGO.GetComponent<CreatureEnemySensor>().enabled = false;

            var controller = creatureGO.GetComponent<CreaturesController>();
            controller.enabled = true;
            controller.EnableControl(this);

            Camera cam = _currentCreature.GetCamera;
            cam.gameObject.SetActive(true);

            creatureGO.tag = "Player";
            _changePlayerTagEventPub.Publish(new()
            {
                PlayerTarget = creatureGO
            });

            // UI
            _enterControlButton.gameObject.SetActive(false);
            _exitControlButton.gameObject.SetActive(true);

            foreach (var ui in _playerUI)
                ui.SetActive(false);

            _playerRoot.SetActive(false);
            _canControll = false;
        }

        public void ReleaseControl(CreatureInformation creature)
        {
            GameObject go = creature.gameObject;

            go.tag = "Creature";
            _changePlayerTagEventPub.Publish(new()
            {
                PlayerTarget = gameObject
            });

            go.GetComponent<NavMeshAgent>().enabled = true;
            go.GetComponent<CreatureStateView>().enabled = true;
            go.GetComponent<CreatureEnemySensor>().enabled = true;
            go.GetComponent<CreaturesController>().enabled = false;

            creature.GetCamera.gameObject.SetActive(false);

            _exitControlButton.gameObject.SetActive(false);

            foreach (var ui in _playerUI)
                ui.SetActive(true);

            _playerRoot.SetActive(true);
            _canControll = true;
        }
    }
}
