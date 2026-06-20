using UnityEngine;

namespace Creatures.Navigation
{
    public interface ICreatureProjectile
    {
        void Init(CreatureInformation attacker, GameObject target, int damage);
    }
}
