using MessagePipe;
using System;
using System.Collections.Generic;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.AI;
using Zenject;

namespace Creatures.Navigation
{
    public class CreatureStateView : MonoBehaviour
    {
        [SerializeField] private CreatureState _beforeState;
        [SerializeField] private CreatureState _startState;
        [SerializeField] private CreatureState _currentState;

        [SerializeField] private GameObject _ragdollDead;
        [SerializeField] private List<Collider> _colliders = new List<Collider>();

        [Inject] private ISubscriber<ChangePlayerTagEvent> _changePlayerTagEventSub;
        [Inject] private ISubscriber<CreatureDeadEvent> _creatureDeadEventSub;
        [Inject] private ISubscriber<CreatureCommandEvent> _commadEventSub;
        [Inject] private DiContainer _container;

        private CreatureEnemySensor _creatureSensor;
        private CreatureInformation _creatureInfo;

        private BoxCollider _boxCollider;

        private GameObject _playerTarget;
        private NavMeshAgent _agent;
        private Animator _anim;

        private IDisposable _disposable;

        public CreatureState GetBeforeState => _beforeState;
        public CreatureEnemySensor GetSensor => _creatureSensor;
        public CreatureInformation GetInfo => _creatureInfo;
        public GameObject GetPlayer() => _playerTarget;
        public Animator GetAnimator => _anim;
        public NavMeshAgent GetAgent => _agent;


        private void Awake()
        {
            _playerTarget = GameObject.FindGameObjectWithTag("Player");

            _boxCollider = GetComponent<BoxCollider>();

            var bag = DisposableBag.CreateBuilder();
            _changePlayerTagEventSub.Subscribe(UpdateTagEvent).AddTo(bag);
            _creatureDeadEventSub.Subscribe(CreatureDeadEvent).AddTo(bag);
            _commadEventSub.Subscribe(InitCommand).AddTo(bag);
            bag.Add(_disposable);
            _disposable = bag.Build();

            _creatureSensor = GetComponent<CreatureEnemySensor>();
            _creatureInfo = GetComponent<CreatureInformation>();

            _agent = GetComponent<NavMeshAgent>();
            _anim = GetComponent<Animator>();

            SetState(_startState);
        }

        public void UpdateTagEvent(ChangePlayerTagEvent evt)
        {
          //  _playerTarget = evt.PlayerTarget;
        }

        public void EnableColliders(bool state)
        {
            _boxCollider.enabled = state;

            for (int i = 0; i < _colliders.Count; i++)
                _colliders[i].enabled = state;
        }

        public void SetState(CreatureState state)
        {
            if (_currentState != null && _beforeState.Equals(_currentState))
                _beforeState = _currentState;

            _currentState = Instantiate(state);
            _container.Inject(_currentState);

            _currentState.StateView = this;
            _currentState.Init(_container);
        }

        private void Update()
        {
            if (!_currentState.IsFinished)
                _currentState.Run();
            else
                SetState(_startState);
        }

        public void MoveToPoint(Transform point)
        {
            _agent.destination = point.position;
        }

        private void InitCommand(CreatureCommandEvent evt)
        {
            if (evt.AnyCommand)
            {
                SetState(evt.CommandState);
                return;
            }

            if (evt.TargetCreature == null) return;

            if (evt.TargetCreature.Equals(gameObject))
            {
                SetState(evt.CommandState);
            }
        }

        public void CreatureDeadEvent(CreatureDeadEvent evt)
        {
            if (evt.UpdateList == false && evt.Creature.Equals(gameObject))
            {
                Instantiate(_ragdollDead, transform.position, Quaternion.identity);
                Destroy(gameObject);
            }
        }

        private void OnDestroy() => _disposable?.Dispose();
    }
}