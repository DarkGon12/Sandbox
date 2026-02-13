using UnityEngine;

namespace Creatures
{
    [CreateAssetMenu(fileName = "Creature", menuName = "Creature/CreateCreature")]
    public class CreaturesData : ScriptableObject
    {
        [SerializeField] private CreaturesType _creatureType;
        [Space(20)]
        [SerializeField] private GameObject _instanceObject; 
        [SerializeField] private int _weaponIndex;
        [SerializeField] private string _creatureName;
        [SerializeField] private string _mapIndex;
        [Space(10)]
        [SerializeField] private CreatureStats _creatureStats;
        [SerializeField] private CreatureUI _creatureUI;

        public string GetMapIndex => _mapIndex;
        public int GetWeaponIndex => _weaponIndex;
        public CreaturesType GetCreatureType => _creatureType;
        public GameObject GetObject => _instanceObject;
        public string GetName => _creatureName;

        public CreatureStats GetStats => _creatureStats;
        public CreatureUI GetUIInfo => _creatureUI;
    }

    [System.Serializable]
    public class CreatureStats
    {
        public float MoveSpeed;
        public int AttackPower;
    }

    [System.Serializable]
    public class CreatureUI
    {
        public Sprite CreatureIcon;
        public string CreaturePrice;
        public string CreatureName;
    }

    public enum CreaturesType
    {
        Creatures,
        Entity,
        Weapon,
        Car,
        Map
    }
}