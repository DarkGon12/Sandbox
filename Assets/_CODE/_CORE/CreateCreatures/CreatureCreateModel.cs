using System.Collections.Generic;
using UnityEngine;
using MessagePipe;
using Zenject;
using System;
using UnityEngine.UI;
using TMPro;
using Sandbox.Player;
using Creatures.Navigation;


namespace Creatures
{
    public class CreatureCreateModel : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _creatureTextInToolGun;
        [SerializeField] private Image _creatureIconInToolGun;
        [SerializeField] private Button _shootToolGun;
        [SerializeField] private WeaponHolderMotion _motion;

        [SerializeField] private GameObject _agrroButton;
        [SerializeField] private GameObject _agrroPlayerButton;

        [SerializeField] private List<GameObject> _creatureCollection = new List<GameObject>();

        [Inject] private ISubscriber<CreatureDeadEvent> _creatureDeadEventSub;
        [Inject] private ISubscriber<CreateCreatureEvent> _createCreatureEventSub;
        [Inject] private DiContainer _container;

        private CreaturesData _selectedCreature;
        private Camera _camera;
        private IDisposable _disposable;

        private void Start()
        {
            _camera = Camera.main;

            var bag = DisposableBag.CreateBuilder();
            _createCreatureEventSub.Subscribe(InstantiateCreatureEvent).AddTo(bag);
            _creatureDeadEventSub.Subscribe(CreatureDeadEvent).AddTo(bag);
            bag.Add(_disposable);
            _disposable = bag.Build();
        }

        private void OnEnable()
        {
            _shootToolGun.onClick.AddListener(ShootToolGun);
        }

        private void OnDisable()
        {
            _shootToolGun.onClick.RemoveListener(ShootToolGun);
        }

        private void InstantiateCreatureEvent(CreateCreatureEvent evt)
        {
            _selectedCreature = evt.CreaturesData;

            _creatureTextInToolGun.text = _selectedCreature.GetUIInfo.CreatureName;
            _creatureIconInToolGun.sprite = _selectedCreature.GetUIInfo.CreatureIcon;
        }

        public void ShootToolGun()
        {
            Ray ray = _camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            GameObject creature;
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, Mathf.Infinity, ~0, QueryTriggerInteraction.Ignore))
                creature = _container.InstantiatePrefab(_selectedCreature.GetObject, hit.point, Quaternion.identity, null);
            else
                creature = _container.InstantiatePrefab(_selectedCreature.GetObject);

            if (creature != null && _selectedCreature.GetCreatureType == CreaturesType.Creatures)
                creature.GetComponent<CreatureInformation>().SetData(_selectedCreature);

            _creatureCollection.Add(creature);
            UpdateInvisibleInterface();
            _motion.PlayShoot();
        }

        public void CreatureDeadEvent(CreatureDeadEvent evt)
        {
            _creatureCollection.RemoveAll(x => x.Equals(evt.Creature));

            _creatureCollection.RemoveAll(x => x == null);
            UpdateInvisibleInterface();
        }

        public void UpdateInvisibleInterface()
        {
            int creatureCounter = 0;

            for(int i = 0; i < _creatureCollection.Count; i++)
            {
                if(_creatureCollection[i].gameObject.tag == "Creature") 
                    creatureCounter++;
            }

            if(creatureCounter > 0)
                _agrroPlayerButton.SetActive(true);
            else
                _agrroPlayerButton.SetActive(false);

            if (creatureCounter > 1) 
                _agrroButton.SetActive(true);
            else
                _agrroButton.SetActive(false);
        }

        private void OnDestroy() => _disposable?.Dispose();
    }
}