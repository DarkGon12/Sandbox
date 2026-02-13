using MessagePipe;
using UnityEngine;
using Zenject;
using System;
using Creatures.UI;

namespace Creatures.Navigation
{
    public class CreatureInformation : MonoBehaviour
    {
        [SerializeField] private int _health;
        [SerializeField] private int _level = 1;
        
        [Space(20)]
        [SerializeField] private CreatureTeam _creatureTeam;

        [Inject] private IPublisher<CreatureDeadEvent> _creatureDeadEventPub;

        public event Action<int> OnLevelUp;

        private int _damagePower;
        private bool _isDead;
        private CreaturesData _creatureData;

        public int SetDamage(int damage) => _damagePower = damage;
        public int SetHealth(int health) => _health = health;

        public int Level => _level;
        public CreatureTeam Team => _creatureTeam;

        public int Damage()
        {
            if (_damagePower == 0)
                _damagePower = 10;
            return _damagePower;
        }

        public void SetData(CreaturesData data)
        {
            _creatureData = data;
            _damagePower = data.GetStats.AttackPower;
        }

        public void SetTeam(CreatureTeam team)
        {
            _creatureTeam = team;

            CreatureEnemySensor sensor = GetComponent<CreatureEnemySensor>();
            if (sensor != null)
                sensor.ForceRecheck();
        }

        public bool IsEnemyTeam(CreatureTeam otherTeam)
        {
            return otherTeam != _creatureTeam;
        }

        public void TakeDamage(int damage, CreatureInformation attacker = null)
        {
            if (_isDead)
                return;

            _health -= damage;

            if (_health <= 0)
            {
                _isDead = true;

                if (attacker != null && attacker != this)
                {
                    attacker.IncreaseLevel();
                }

                _creatureDeadEventPub.Publish(new() { Creature = gameObject });
            }
        }

        public void IncreaseLevel()
        {
            _level++;
            _damagePower += 10;
            _health += 100;

            OnLevelUp?.Invoke(_level);
        }

        public void DecreaseLevel()
        {
            _level = Mathf.Max(1, _level - 1);
            OnLevelUp?.Invoke(_level);
        }
    }

    public enum CreatureTeam
    {
        White,
        Red,
        Green
    }
}
