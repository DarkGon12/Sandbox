using Sandbox.Player;
using UnityEngine;

namespace Creatures.Navigation
{
    public class CreatureRangedWeapon : MonoBehaviour
    {
        [SerializeField] private Transform _muzzlePoint;
        [SerializeField] private GameObject _projectilePrefab;
        [SerializeField] private bool _useDirectDamageWhenProjectileIsMissing = true;

        public Vector3 MuzzlePosition => _muzzlePoint != null ? _muzzlePoint.position : transform.position + Vector3.up * 1.2f;

        public void FireAt(CreatureInformation attacker, GameObject target, Vector3? overrideFirePosition = null)
        {
            if (attacker == null || target == null)
                return;

            int damage = attacker.Damage();

            if (_projectilePrefab != null)
            {
                Vector3 spawnPosition = overrideFirePosition ?? MuzzlePosition;
                Quaternion rotation = Quaternion.identity;

                Vector3 direction = target.transform.position + Vector3.up * 1.2f - spawnPosition;
                if (direction.sqrMagnitude > 0.001f)
                    rotation = Quaternion.LookRotation(direction.normalized);

                GameObject projectile = Instantiate(_projectilePrefab, spawnPosition, rotation);

                MonoBehaviour[] behaviours = projectile.GetComponents<MonoBehaviour>();
                for (int i = 0; i < behaviours.Length; i++)
                {
                    if (behaviours[i] is ICreatureProjectile projectileLogic)
                    {
                        projectileLogic.Init(attacker, target, damage);
                        break;
                    }
                }

                return;
            }

            if (!_useDirectDamageWhenProjectileIsMissing)
                return;

            if (target.TryGetComponent(out CreatureInformation targetInfo))
                targetInfo.TakeDamage(damage, attacker);
            else if (target.TryGetComponent(out PlayerInformation playerInfo))
                playerInfo.TakeDamage(damage);
        }
    }
}
