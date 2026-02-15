using Sandbox.Player;
using Services.ObjectPool;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Sandbox.Weapon
{
    public class WeaponStandart : MonoBehaviour
    {
        [SerializeField] private Transform _firePoint;
        [SerializeField] private CarControlButton _fireButton;
        [SerializeField] private WeaponData _weaponData;
        [SerializeField] private ParticleSystem _muzzleParticle;
        [SerializeField] private AudioClip _shootClip;

        private int _currentAmmo;
        private bool _isReloading;
        private bool _isFiring;

        private float _nextFireTime;

        private AudioSource _audioSource;
        private WeaponHolderMotion _weaponMotion;

        private EventTrigger _eventTrigger;
        private EventTrigger.Entry _downEntry;
        private EventTrigger.Entry _upEntry;

        private Coroutine _reloadCoroutine;

        private void Start()
        {
            _audioSource = GetComponent<AudioSource>();
            _weaponMotion = transform.parent.GetComponent<WeaponHolderMotion>();

            _currentAmmo = _weaponData.MagazineSize;

            ObjectPoolManager.CreatePool(
                _weaponData.BulletPoolTag,
                _weaponData.BulletPrefab,
                _weaponData.PoolSize);
        }

        private void OnEnable()
        {
            SetupButton();
        }

        private void OnDisable()
        {
            RemoveButtonListeners();

            if (_reloadCoroutine != null)
            {
                StopCoroutine(_reloadCoroutine);
                _reloadCoroutine = null;
            }

            _isReloading = false;
        }

        private void Update()
        {
            if (!_isFiring || _isReloading)
                return;

            if (Time.time < _nextFireTime)
                return;

            if (_currentAmmo <= 0)
            {
                if (_reloadCoroutine == null)
                    _reloadCoroutine = StartCoroutine(Reload());

                return;
            }

            Fire();
            _nextFireTime = Time.time + _weaponData.FireRate;
        }

        private void SetupButton()
        {
            if (_eventTrigger == null)
                _eventTrigger = _fireButton.GetComponent<EventTrigger>()
                                ?? _fireButton.gameObject.AddComponent<EventTrigger>();

            _eventTrigger.triggers.Clear();

            _downEntry = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerDown
            };
            _downEntry.callback.AddListener((_) => StartFiring());

            _upEntry = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerUp
            };
            _upEntry.callback.AddListener((_) => StopFiring());

            _eventTrigger.triggers.Add(_downEntry);
            _eventTrigger.triggers.Add(_upEntry);
        }

        private void RemoveButtonListeners()
        {
            if (_eventTrigger == null) return;
            _eventTrigger.triggers.Clear();
        }

        private void StartFiring()
        {
            if (_isReloading)
                return;

            _isFiring = true;

        }

        private void StopFiring()
        {
            _isFiring = false;

        }

        private void Fire()
        {
            if (_firePoint == null)
            {
                Debug.LogError("FirePoint is NULL!");
                return;
            }

            _currentAmmo--;

            _weaponMotion?.PlayShoot();
            _audioSource.PlayOneShot(_shootClip);

            GameObject bulletObj = ObjectPoolManager.Get(_weaponData.BulletPoolTag);

            if (bulletObj == null)
            {
                Debug.LogError("Bullet from pool is NULL!");
                return;
            }

            bulletObj.transform.SetParent(null);

            bulletObj.transform.position = _firePoint.position + _firePoint.forward * 0.2f;
            bulletObj.transform.rotation = _firePoint.rotation;

            if (!bulletObj.activeInHierarchy)
                bulletObj.SetActive(true);
            _muzzleParticle.Play();

            WeaponBullet bullet = bulletObj.GetComponent<WeaponBullet>();
            bullet.Init(
                _weaponData.BulletPoolTag,
                _weaponData.BulletLifeTime,
                _weaponData.BulletDamage);

            Rigidbody rb = bulletObj.GetComponent<Rigidbody>();


            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;

                rb.position = _firePoint.position + _firePoint.forward * 0.2f;
                rb.rotation = _firePoint.rotation;

                rb.AddForce(_firePoint.forward * _weaponData.BulletForce, ForceMode.Impulse);
            }
            else
            {
                Debug.LogError("Bullet has no Rigidbody!");
            }
        }

        private IEnumerator Reload()
        {
            _isReloading = true;

            yield return new WaitForSeconds(_weaponData.ReloadTime);

            _currentAmmo = _weaponData.MagazineSize;
            _isReloading = false;
            _reloadCoroutine = null;
        }
    }
}