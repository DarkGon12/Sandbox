using MessagePipe;
using UnityEngine;
using Zenject;

namespace Creatures.Navigation.States
{
    [CreateAssetMenu(fileName = "creatureState", menuName = "Creature/State/AttackPlayerAllState")]
    public class AttackPlayerCommandState : CreatureState
    {
        [SerializeField] private bool _allEnemyAttack = false;
        [SerializeField] private CreatureState _attackPlayerState;

        [Inject] private IPublisher<CreatureCommandEvent> _creatureCommandEvtPub;

        public override void Init(DiContainer container)
        {
            IsFinished = true;

            _creatureCommandEvtPub.Publish(new() { AnyCommand = true, CommandState = _attackPlayerState });
        }

        public override void Run()
        {
            if (IsFinished) return;
        }
    }
}