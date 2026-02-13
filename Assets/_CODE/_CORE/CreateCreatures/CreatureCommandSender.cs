using UnityEngine.UI;
using MessagePipe;
using UnityEngine;
using Zenject;

namespace Creatures.Navigation
{
    public class CreatureCommandSender : MonoBehaviour
    {
        [SerializeField] private bool _anyCommand = false;
        [SerializeField] private CreatureState _command;

        [Inject] private IPublisher<CreatureCommandEvent> _creatureCommandEventPub;

        private Camera _camera;
        private Button _button;

        private void Start()
        {
            _button = GetComponent<Button>();
            _camera = Camera.main;
            _button.onClick.AddListener(SendCommand);
        }

        private void SendCommand()
        {
            if(_anyCommand)
            {
                _creatureCommandEventPub.Publish(new()
                {
                    CommandState = _command,
                    TargetCreature = null,
                    AnyCommand = _anyCommand
                });
                return;
            }

            Ray ray = _camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            GameObject creature = null;
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, Mathf.Infinity, ~0, QueryTriggerInteraction.Ignore))
            {
                Transform root = hit.collider.transform.root;

                if (root.CompareTag("Creature"))
                {
                    creature = root.gameObject;
                }
            }

            if (creature == null) return;

            _creatureCommandEventPub.Publish(new()
            {
                CommandState = _command,
                TargetCreature = creature,
                AnyCommand = _anyCommand
            });
        }

    }
}