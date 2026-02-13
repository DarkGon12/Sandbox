using MessagePipe;
using UnityEngine;
using Zenject;

namespace Creatures.Entity
{
    public class CreatureLifeTime : MonoBehaviour
    {
        [Inject] private IPublisher<CreatureDeadEvent> _creatureDeadEventPub;
        [SerializeField] private float _delay;

        private void Start()
        {
            _creatureDeadEventPub.Publish(new() { Creature = gameObject });
            Destroy(gameObject, _delay);
        }
    }
}