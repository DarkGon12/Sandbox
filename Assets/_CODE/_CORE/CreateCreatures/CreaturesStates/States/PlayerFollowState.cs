using UnityEngine;
using Zenject;

namespace Creatures.Navigation.States
{
    [CreateAssetMenu(fileName = "creatureState", menuName = "Creature/State/FollowState")]
    public class PlayerFollowState : CreatureState
    {
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

            float distance = Vector3.Distance(_player.position, StateView.transform.position);

            if (distance > 4f)
            {
                StateView.GetAgent.isStopped = false;
                MoveAnimation(true);

                StateView.MoveToPoint(_player);
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