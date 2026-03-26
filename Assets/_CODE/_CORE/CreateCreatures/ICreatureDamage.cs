using Creatures.Navigation;
using UnityEngine;

public interface ICreatureDamage
{
    public virtual void TakeDamage(int damage, CreatureInformation attacker = null)
    {

    }
}