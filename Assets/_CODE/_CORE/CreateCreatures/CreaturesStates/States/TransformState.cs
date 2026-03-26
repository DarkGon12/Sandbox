using MessagePipe;
using UnityEngine;
using Zenject;
using System;

namespace Creatures.Navigation.States
{
    [CreateAssetMenu(fileName = "creatureState", menuName = "Creature/State/TransformState")]
    public class TransformState : CreatureState
    {
        [Inject] private ISubscriber<CreatureTransformCommandEvent> _transformEventSub;

        private Transform _thisCreature;
        private Camera _camera;

        private IDisposable _disposable;

        public override void Init(DiContainer container)
        {
            _camera = Camera.main;
            _thisCreature = StateView.transform;

            var bag = DisposableBag.CreateBuilder();
            _transformEventSub.Subscribe(CompleteCommand).AddTo(bag);
            _disposable = bag.Build();

            StateView.EnableColliders(false);
        }

        private void CompleteCommand(CreatureTransformCommandEvent evt)
        {
            Vector3 euler = _thisCreature.localEulerAngles;

            if (evt.CommandIndex == 0)
            {
                Debug.Log("I Get command 0 DROP");
                StateView.EnableColliders(true);
                IsFinished = true;
                StateView.SetState(StateView.GetBeforeState);
            }
            if (evt.CommandIndex == 1)
            {
                euler.y -= 45f;
                _thisCreature.localEulerAngles = euler;
            }
            if (evt.CommandIndex == 2)
            {
                euler.y += 45f;
                _thisCreature.localEulerAngles = euler;
            }

            if (evt.CommandIndex == 3)
            {
                if (_thisCreature.localScale.y < 20f)
                    _thisCreature.localScale += new Vector3(0.2f, 0.2f, 0.2f);
            }
            if (evt.CommandIndex == 4)
            {
                if (_thisCreature.localScale.y > 0.1f)
                    _thisCreature.localScale -= new Vector3(0.2f, 0.2f, 0.2f);
            }
        }

        public override void Run()
        {
            if (IsFinished) return;

            Ray ray = _camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, Mathf.Infinity, ~0, QueryTriggerInteraction.Ignore))
                _thisCreature.transform.position = hit.point;
        }
    }
}