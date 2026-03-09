using UnityEngine;
using Zenject;

namespace Creatures.Navigation.States
{
    [CreateAssetMenu(fileName = "creatureState", menuName = "Creature/State/FollowState")]
    public class PlayerFollowState : CreatureState
    {
        private Vector3 _lastTargetPos;

        private DiContainer _container;
        private Transform _player;

        public override void Init(DiContainer container)
        {
            _container = container;
            _container.Inject(this);

            _player = StateView.GetPlayer().transform;

            StateView.GetAnimator.SetBool("walk", true);
            StateView.GetAnimator.SetBool("idle", false);
        }

        public override void Run()
        {
            if (IsFinished) return;

            if (Vector3.SqrMagnitude(_player.position - _lastTargetPos) > 1f)
            {
                StateView.GetAgent.isStopped = false;
                MoveAnimation(true);

                StateView.MoveToPoint(_player);
                _lastTargetPos = _player.position;
            }
            else
            {
                StateView.GetAgent.isStopped = true;
                MoveAnimation(false);
            }
        }

        private void MoveAnimation(bool state)
        {
            StateView.GetAnimator.SetBool("idle", !state);
            StateView.GetAnimator.SetBool("walk", state);
        }
    }
}