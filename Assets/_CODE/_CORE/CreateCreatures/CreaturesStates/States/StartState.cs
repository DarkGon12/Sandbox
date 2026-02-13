using UnityEngine;
using Zenject;

namespace Creatures.Navigation.States
{
    [CreateAssetMenu(fileName = "creatureState", menuName = "Creature/State/StartState")]
    public class StartState : CreatureState
    {
        private DiContainer _container;

        public override void Init(DiContainer container)
        {
            _container = container;
            _container.Inject(this);

            StopAllAnimation();

            StateView.GetAgent.isStopped = true;
            StateView.GetAnimator.SetBool("idle", true);
        }

        private void StopAllAnimation()
        {
            StateView.GetAnimator.SetBool("idle", false);
            StateView.GetAnimator.SetBool("walk", false);
        }

        public override void Run()
        {
            if (IsFinished) return;
        }
    }
}