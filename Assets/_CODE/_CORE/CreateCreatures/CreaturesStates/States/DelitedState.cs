using MessagePipe;
using UnityEngine;
using Zenject;

namespace Creatures.Navigation.States
{
    [CreateAssetMenu(fileName = "creatureState", menuName = "Creature/State/DelitedState")]
    public class DelitedState : CreatureState
    {
        [Inject] private IPublisher<CreatureDeadEvent> _cretureDelitedEventPub;

        public override void Init(DiContainer container)
        {
            IsFinished = true;

            _cretureDelitedEventPub.Publish(new() { Creature = StateView.gameObject, UpdateList = true });
            Destroy(StateView.gameObject);
        }

        public override void Run()
        {
            if (IsFinished) return;
        }
    }
}