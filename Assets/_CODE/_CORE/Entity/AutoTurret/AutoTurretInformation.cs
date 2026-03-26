using Creatures.Navigation;
using MessagePipe;
using UnityEngine;
using Zenject;

public class AutoTurretInformation : MonoBehaviour, ICreatureDamage
{
    [SerializeField] private int _turretHealth = 100;
    [SerializeField] private int _turretDamage = 10;
    [SerializeField] private CreatureTeam _team = CreatureTeam.White;
    [SerializeField] private MeshRenderer _teamCircle;

    [Inject] private IPublisher<CreatureDeadEvent> _creatureDeadEventPub;

    private bool _isDead;

    public int Damage => _turretDamage;
    public CreatureTeam Team => _team;

    public bool IsEnemy(CreatureInformation other)
    {
        if (other == null)
            return false;

        return other.Team != _team;
    }

    public void SetTeam(CreatureTeam team)
    {
        _team = team;

        if (team == CreatureTeam.Green)
            _teamCircle.material.color = Color.green;
        if (team == CreatureTeam.Red)
            _teamCircle.material.color = Color.red;
        if (team == CreatureTeam.White)
            _teamCircle.material.color = Color.white;
    }

    public void TakeDamage(int damage, CreatureInformation attacker = null)
    {
        if (_isDead)
            return;

        _turretHealth -= damage;

        if (_turretHealth <= 0)
        {
            _isDead = true;

            if (attacker != null)
                attacker.IncreaseLevel();

            _creatureDeadEventPub.Publish(new CreatureDeadEvent { Creature = gameObject });
        }
    }
}