using UnityEngine;

namespace Sandbox.Weapon
{
    [CreateAssetMenu(fileName = "WeaponData", menuName = "Sandbox/Weapon/CreateWeaponData")]
    public class WeaponData : ScriptableObject
    {
        [Header("Bullet")]
        public int BulletDamage = 10;
        public string BulletPoolTag;
        public GameObject BulletPrefab;
        public int PoolSize = 20;
        public float BulletForce = 1500f;
        public float BulletLifeTime = 5f;

        [Header("Fire")]
        public float FireRate = 0.15f;
        public bool IsAutomatic = true;

        [Header("Ammo")]
        public int MagazineSize = 30;
        public float ReloadTime = 2f;
    }
}
