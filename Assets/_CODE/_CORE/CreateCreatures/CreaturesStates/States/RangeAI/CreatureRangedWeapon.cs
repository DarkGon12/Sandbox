using Sandbox.Player;
using UnityEngine;

namespace Creatures.Navigation
{
    public class CreatureRangedWeapon : MonoBehaviour
    {
        private static readonly ParticleSystem[] EmptyMuzzleFlashSystems = new ParticleSystem[0];

        [SerializeField] private Transform _muzzlePoint;
        [SerializeField] private GameObject _projectilePrefab;
        [SerializeField] private bool _useDirectDamageWhenProjectileIsMissing = true;
        
        [Header("Shot Visuals")]
        [SerializeField] private ParticleSystem _muzzleFlash;
        [SerializeField] private int _muzzleFlashEmitCount = 8;
        [SerializeField] private GameObject _muzzleFlashPrefab;
        [SerializeField] private float _muzzleFlashPrefabLifetime = 0.6f;

        [Header("Accuracy")]
        [SerializeField, Range(0f, 1f)] private float _accuracy = 0.72f;
        [SerializeField] private float _accurateShotSpreadAngle = 1.2f;
        [SerializeField] private float _missShotSpreadAngle = 8f;
        [SerializeField] private float _projectileSpawnForwardOffset = 0.12f;
        [SerializeField] private float _targetAimHeight = 1.2f;

        private ParticleSystem[] _muzzleFlashSystems;

        public bool HasMuzzlePoint => _muzzlePoint != null;
        public Vector3 MuzzlePosition => _muzzlePoint != null ? _muzzlePoint.position : transform.position + Vector3.up * 1.2f;

        private void Awake()
        {
            CacheMuzzleFlashSystems();
        }

        public void FireAt(CreatureInformation attacker, GameObject target, Vector3? overrideFirePosition = null)
        {
            if (attacker == null || target == null)
                return;

            int damage = attacker.Damage();
            Vector3 spawnPosition = overrideFirePosition ?? MuzzlePosition;
            Vector3 shotDirection = GetShotDirection(spawnPosition, target, out bool intendedHit);
            Quaternion rotation = Quaternion.LookRotation(shotDirection);

            PlayMuzzleFlash(spawnPosition, rotation);

            if (_projectilePrefab != null)
            {
                Vector3 projectileSpawnPosition = spawnPosition + shotDirection * Mathf.Max(0f, _projectileSpawnForwardOffset);
                GameObject projectile = Instantiate(_projectilePrefab, projectileSpawnPosition, rotation);

                bool projectileInitialized = TryInitProjectile(projectile, attacker, target, damage, shotDirection);
                if (!projectileInitialized && _useDirectDamageWhenProjectileIsMissing && intendedHit)
                    ApplyDirectDamage(attacker, target, damage);

                return;
            }

            if (!_useDirectDamageWhenProjectileIsMissing)
                return;

            if (intendedHit)
                ApplyDirectDamage(attacker, target, damage);
        }

        private Vector3 GetShotDirection(Vector3 spawnPosition, GameObject target, out bool intendedHit)
        {
            intendedHit = Random.value <= _accuracy;

            Vector3 aimPoint = target.transform.position + Vector3.up * Mathf.Max(0f, _targetAimHeight);
            Vector3 direction = aimPoint - spawnPosition;
            if (direction.sqrMagnitude <= 0.001f)
                direction = transform.forward;

            direction.Normalize();

            float spreadAngle = intendedHit ? _accurateShotSpreadAngle : _missShotSpreadAngle;
            return ApplySpread(direction, Mathf.Max(0f, spreadAngle));
        }

        private Vector3 ApplySpread(Vector3 direction, float maxAngle)
        {
            if (maxAngle <= 0.001f)
                return direction;

            Quaternion lookRotation = Quaternion.LookRotation(direction);
            Vector2 spread = Random.insideUnitCircle * maxAngle;
            Vector3 spreadDirection = lookRotation * Quaternion.Euler(-spread.y, spread.x, 0f) * Vector3.forward;
            return spreadDirection.sqrMagnitude > 0.001f ? spreadDirection.normalized : direction;
        }

        private bool TryInitProjectile(
            GameObject projectile,
            CreatureInformation attacker,
            GameObject target,
            int damage,
            Vector3 shotDirection)
        {
            if (projectile == null)
                return false;

            if (projectile.TryGetComponent(out CreatureRangedProjectile rangedProjectile))
            {
                rangedProjectile.Init(attacker, target, damage, shotDirection);
                return true;
            }

            MonoBehaviour[] behaviours = projectile.GetComponents<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is ICreatureProjectile projectileLogic)
                {
                    projectileLogic.Init(attacker, target, damage);
                    return true;
                }
            }

            return false;
        }

        private void PlayMuzzleFlash(Vector3 spawnPosition, Quaternion rotation)
        {
            if (_muzzleFlash != null)
            {
                if (_muzzleFlashSystems == null || _muzzleFlashSystems.Length == 0)
                    CacheMuzzleFlashSystems();

                for (int i = 0; i < _muzzleFlashSystems.Length; i++)
                {
                    ParticleSystem system = _muzzleFlashSystems[i];
                    if (system == null)
                        continue;

                    system.gameObject.SetActive(true);
                    system.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                    system.Play(false);

                    if (_muzzleFlashEmitCount > 0)
                        system.Emit(_muzzleFlashEmitCount);
                }
            }

            if (_muzzleFlashPrefab == null)
                return;

            GameObject muzzleFlash = Instantiate(_muzzleFlashPrefab, spawnPosition, rotation);
            Destroy(muzzleFlash, Mathf.Max(0.05f, _muzzleFlashPrefabLifetime));
        }

        private void CacheMuzzleFlashSystems()
        {
            if (_muzzleFlash == null)
            {
                _muzzleFlashSystems = EmptyMuzzleFlashSystems;
                return;
            }

            _muzzleFlashSystems = _muzzleFlash.GetComponentsInChildren<ParticleSystem>(true);
        }

        private static void ApplyDirectDamage(CreatureInformation attacker, GameObject target, int damage)
        {
            if (target.TryGetComponent(out CreatureInformation targetInfo))
            {
                DamageCreatureTarget(attacker, targetInfo, damage);
                return;
            }

            CreatureInformation parentTargetInfo = target.GetComponentInParent<CreatureInformation>();
            if (parentTargetInfo != null)
            {
                DamageCreatureTarget(attacker, parentTargetInfo, damage);
                return;
            }

            if (target.TryGetComponent(out PlayerInformation playerInfo))
            {
                playerInfo.TakeDamage(damage);
                return;
            }

            PlayerInformation parentPlayerInfo = target.GetComponentInParent<PlayerInformation>();
            if (parentPlayerInfo != null)
                parentPlayerInfo.TakeDamage(damage);
        }

        private static void DamageCreatureTarget(CreatureInformation attacker, CreatureInformation target, int damage)
        {
            target.TakeDamage(damage, attacker);

            if (attacker == null || target.Health <= 0)
                return;

            if (!target.IsEnemyTeam(attacker.Team))
                return;

            CreatureEnemySensor sensor = target.GetComponent<CreatureEnemySensor>();
            if (sensor != null)
                sensor.SetEnemyWithoutResponse(attacker);
        }
    }
}
