using UnityEngine;
using Zenject;

namespace Creatures.Navigation.States
{
    [CreateAssetMenu(fileName = "creatureState", menuName = "Creature/State/AttackPlayerAllState")]
    public class AttackPlayerCommandState : CreatureState
    {
        [SerializeField] private bool _useRangedAttackState = true;
        [SerializeField] private CreatureState _attackPlayerState;

        public override void Init(DiContainer container)
        {
            IsFinished = true;

            CreatureState state = GetAttackPlayerState();
            if (state != null)
                StateView.SetState(state);
        }

        public override void Run()
        {
            if (IsFinished) return;
        }

        private CreatureState GetAttackPlayerState()
        {
            if (!_useRangedAttackState)
                return _attackPlayerState;

            if (StateView == null || StateView.GetComponent<CreatureRangedWeapon>() == null)
                return _attackPlayerState;

            CreatureEnemySensor sensor = StateView.GetSensor;
            if (sensor == null || sensor.AttackState == null)
                return _attackPlayerState;

            sensor.SetPlayerTarget();
            return sensor.AttackState;
        }
    }
}
