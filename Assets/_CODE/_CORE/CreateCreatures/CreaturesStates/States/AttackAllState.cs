using UnityEngine;
using Zenject;

namespace Creatures.Navigation.States
{
    [CreateAssetMenu(fileName = "creatureState", menuName = "Creature/State/AttackAllState")]
    public class AttackAllState : CreatureState
    {
        public override void Init(DiContainer container)
        {
            IsFinished = true;

            StateView.GetSensor.TryAggroAll();
        }

        public override void Run()
        {
            if (IsFinished) return;
        }
    }
}