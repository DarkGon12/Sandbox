using UnityEngine;
using System.Collections;

namespace Sandbox.Tank
{
    public class TankAttackModule : MonoBehaviour
    {
        [Header("Turret")]
        [SerializeField] private Transform _turret;        // башня
        [SerializeField] private Transform _gun;           // пушка
        [SerializeField] private float _turretRotateSpeed = 90f;
        [SerializeField] private float _gunRotateSpeed = 60f;

        [Header("Gun Limits")]
        [SerializeField] private float _minGunAngle = -30f;
        [SerializeField] private float _maxGunAngle = 8f;

        [Header("Fire")]
        [SerializeField] private CarControlButton _fireButton;
        [SerializeField] private Transform _firePoint;
        [SerializeField] private GameObject _projectilePrefab;
        [SerializeField] private float _fireForce = 3000f;     // импульс снаряда
        [SerializeField] private float _recoilForce = 1800f;   // импульс отдачи
        [SerializeField] private float _reloadTime = 1.5f;

        [Header("Input")]
        [SerializeField] private Joystick _joystick;

        [Header("References")]
        [SerializeField] private Rigidbody _tankRigidbody;
        [SerializeField] private AudioSource _fireClip;

        private bool _canFire = true;
        private float _currentGunAngle;

        private void Awake()
        {
            _currentGunAngle = _gun.localEulerAngles.x;
            if (_currentGunAngle > 180f)
                _currentGunAngle -= 360f;
        }

        public void InitModule(CarControlButton fireBtn, Joystick turret)
        {
            _joystick = turret;
            _fireButton = fireBtn;
        }

        private void Update()
        {
            HandleTurretRotation();
            HandleFire();
        }

        private void HandleTurretRotation()
        {
            Vector3 input = _joystick.Direction;

            // ===== ВРАЩЕНИЕ БАШНИ (Y) =====
            float turretInput = input.x;
            if (!float.IsNaN(turretInput))
            {
                _turret.Rotate(
                    Vector3.up,
                    turretInput * _turretRotateSpeed * Time.deltaTime,
                    Space.Self
                );
            }

            // ===== ВРАЩЕНИЕ ПУШКИ (X) =====
            float gunInput = input.y;
            if (float.IsNaN(gunInput))
                gunInput = 0f;

            _currentGunAngle -= gunInput * _gunRotateSpeed * Time.deltaTime;
            _currentGunAngle = Mathf.Clamp(_currentGunAngle, _minGunAngle, _maxGunAngle);

            _gun.localRotation = Quaternion.Euler(_currentGunAngle, 0f, 0f);
        }

        private void HandleFire()
        {
            if (!_canFire)
                return;

            if (_fireButton.Pressed)
            {
                StartCoroutine(FireRoutine());
            }
        }

        private IEnumerator FireRoutine()
        {
            _canFire = false;

            Vector3 recoilDirection = -_firePoint.forward;

            _tankRigidbody.AddForceAtPosition(
                recoilDirection * _recoilForce,
                _firePoint.position,
                ForceMode.Impulse
            );

            GameObject projectile = Instantiate(
                _projectilePrefab,
                _firePoint.position,
                _firePoint.rotation
            );

            Rigidbody projectileRb = projectile.GetComponent<Rigidbody>();
            if (projectileRb != null)
            {
                projectileRb.AddForce(
                    _firePoint.forward * _fireForce,
                    ForceMode.Impulse
                );
            }
            _fireClip.Play();

            yield return new WaitForSeconds(_reloadTime);

            _canFire = true;
        }
    }
}
