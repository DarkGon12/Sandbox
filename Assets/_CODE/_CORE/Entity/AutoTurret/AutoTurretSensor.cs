using Creatures.Navigation;
using UnityEngine;

public class AutoTurretSensor : MonoBehaviour
{
    [Header("Links")]
    [SerializeField] private Transform _turretGun;
    [SerializeField] private Transform _muzzlePoint;
    [SerializeField] private ParticleSystem _muzzleFlash;
    [SerializeField] private AutoTurretProjectile _projectilePrefab;

    [Header("Target Search")]
    [SerializeField] private string _targetTag = "Creatures";
    [SerializeField] private float _searchRadius = 20f;
    [SerializeField] private float _viewAngle = 90f;
    [SerializeField] private LayerMask _searchMask = ~0;
    [SerializeField] private LayerMask _obstacleMask = ~0;
    [SerializeField] private float _scanInterval = 0.15f;

    [Header("Rotation")]
    [SerializeField] private float _idleRotateSpeed = 40f;
    [SerializeField] private float _aimRotateSpeed = 120f;
    [SerializeField] private float _maxPitchUp = 40f;
    [SerializeField] private float _maxPitchDown = 15f;

    [Header("Fire")]
    [SerializeField] private float _fireRate = 0.25f;
    [SerializeField] private float _projectileSpeed = 30f;
    [SerializeField] private float _fireAngleTolerance = 6f;

    [SerializeField] private AudioClip _fireClip;

    private AutoTurretInformation _turretInfo;
    private CreatureInformation _currentTargetInfo;
    private Transform _currentTargetBone;

    private float _nextScanTime;
    private float _nextFireTime;

    private AudioSource _audioSource;

    private void Awake()
    {
        _turretInfo = GetComponentInParent<AutoTurretInformation>();
        _audioSource = GetComponent<AudioSource>();
    }

    private void Update()
    {
        if (_turretGun == null)
            return;

        if (Time.time >= _nextScanTime)
        {
            _nextScanTime = Time.time + _scanInterval;
            SearchTarget();
        }

        if (_currentTargetBone == null || _currentTargetInfo == null || !IsTargetValid(_currentTargetInfo, _currentTargetBone))
        {
            ClearTarget();
            IdleRotate();
            return;
        }

        AimAtTarget();

        if (CanShoot())
            Shoot();
    }

    private void IdleRotate()
    {
        // ѕосто€нное вращение вокруг своей оси Y
        _turretGun.Rotate(0f, _idleRotateSpeed * Time.deltaTime, 0f, Space.Self);
    }

    private void SearchTarget()
    {
        Collider[] hits = Physics.OverlapSphere(
            _turretGun.position,
            _searchRadius,
            _searchMask,
            QueryTriggerInteraction.Collide);

        CreatureInformation bestTargetInfo = null;
        Transform bestTargetBone = null;
        float bestSqrDistance = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider col = hits[i];
            if (col == null)
                continue;

            Transform hitTransform = col.transform;

            if (!hitTransform.CompareTag(_targetTag))
                continue;

            CreatureInformation creatureInfo = hitTransform.GetComponentInParent<CreatureInformation>();
            if (creatureInfo == null)
                continue;

            if (_turretInfo != null && !_turretInfo.IsEnemy(creatureInfo))
                continue;

            if (!IsInsideViewCone(hitTransform.position))
                continue;

            if (!HasLineOfSight(hitTransform.position, creatureInfo.transform))
                continue;

            float sqrDistance = (hitTransform.position - _turretGun.position).sqrMagnitude;
            if (sqrDistance < bestSqrDistance)
            {
                bestSqrDistance = sqrDistance;
                bestTargetInfo = creatureInfo;
                bestTargetBone = hitTransform;
            }
        }

        _currentTargetInfo = bestTargetInfo;
        _currentTargetBone = bestTargetBone;
    }

    private bool IsTargetValid(CreatureInformation info, Transform bone)
    {
        if (info == null || bone == null)
            return false;

        if (!bone.CompareTag(_targetTag))
            return false;

        if (_turretInfo != null && !_turretInfo.IsEnemy(info))
            return false;

        Vector3 toTarget = bone.position - _turretGun.position;
        if (toTarget.sqrMagnitude > _searchRadius * _searchRadius)
            return false;

        if (!IsInsideViewCone(bone.position))
            return false;

        if (!HasLineOfSight(bone.position, info.transform))
            return false;

        return true;
    }

    private bool IsInsideViewCone(Vector3 targetPoint)
    {
        Vector3 toTarget = targetPoint - _turretGun.position;
        float distance = toTarget.magnitude;

        if (distance <= 0.001f)
            return true;

        float angle = Vector3.Angle(_turretGun.forward, toTarget.normalized);
        return angle <= _viewAngle * 0.5f;
    }

    private bool HasLineOfSight(Vector3 targetPoint, Transform targetRoot)
    {
        Vector3 origin = _muzzlePoint != null ? _muzzlePoint.position : _turretGun.position;
        Vector3 dir = targetPoint - origin;
        float distance = dir.magnitude;

        if (distance <= 0.001f)
            return true;

        dir /= distance;

        if (Physics.Raycast(origin, dir, out RaycastHit hit, distance, _obstacleMask, QueryTriggerInteraction.Ignore))
        {
            if (!hit.transform.IsChildOf(targetRoot))
                return false;
        }

        return true;
    }

    private void AimAtTarget()
    {
        Vector3 targetPoint = _currentTargetBone.position;
        Vector3 origin = _turretGun.position;
        Vector3 toTarget = targetPoint - origin;

        if (toTarget.sqrMagnitude <= 0.0001f)
            return;

        Quaternion desiredWorldRotation = Quaternion.LookRotation(toTarget.normalized, Vector3.up);

        Vector3 desiredEuler = NormalizeEuler(desiredWorldRotation.eulerAngles);
        Vector3 currentEuler = NormalizeEuler(_turretGun.rotation.eulerAngles);

        float desiredPitch = desiredEuler.x;
        desiredPitch = ClampPitch(desiredPitch);

        float nextYaw = Mathf.MoveTowardsAngle(currentEuler.y, desiredEuler.y, _aimRotateSpeed * Time.deltaTime);
        float nextPitch = Mathf.MoveTowardsAngle(currentEuler.x, desiredPitch, _aimRotateSpeed * Time.deltaTime);

        _turretGun.rotation = Quaternion.Euler(nextPitch, nextYaw, 0f);
    }

    private float ClampPitch(float pitch)
    {
        if (pitch > 180f)
            pitch -= 360f;

        return Mathf.Clamp(pitch, -_maxPitchDown, _maxPitchUp);
    }

    private Vector3 NormalizeEuler(Vector3 euler)
    {
        if (euler.x > 180f) euler.x -= 360f;
        if (euler.y > 180f) euler.y -= 360f;
        if (euler.z > 180f) euler.z -= 360f;
        return euler;
    }

    private bool CanShoot()
    {
        if (_currentTargetBone == null)
            return false;

        if (Time.time < _nextFireTime)
            return false;

        Vector3 origin = _muzzlePoint != null ? _muzzlePoint.position : _turretGun.position;
        Vector3 dirToTarget = (_currentTargetBone.position - origin).normalized;

        Transform fireTransform = _muzzlePoint != null ? _muzzlePoint : _turretGun;
        float angle = Vector3.Angle(fireTransform.forward, dirToTarget);

        return angle <= _fireAngleTolerance;
    }

    private void Shoot()
    {
        _nextFireTime = Time.time + _fireRate;

        if (_muzzleFlash != null)
            _muzzleFlash.Play();

        if (_projectilePrefab == null || _muzzlePoint == null || _turretInfo == null)
            return;

        AutoTurretProjectile projectile = Instantiate(
            _projectilePrefab,
            _muzzlePoint.position,
            _muzzlePoint.rotation);

        _audioSource.PlayOneShot(_fireClip);

        projectile.Init(_muzzlePoint, _turretInfo.Damage, _projectileSpeed);
    }

    private void ClearTarget()
    {
        _currentTargetInfo = null;
        _currentTargetBone = null;
    }

    private void OnDrawGizmosSelected()
    {
        if (_turretGun == null)
            return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(_turretGun.position, _searchRadius);

        Vector3 left = Quaternion.AngleAxis(-_viewAngle * 0.5f, Vector3.up) * _turretGun.forward;
        Vector3 right = Quaternion.AngleAxis(_viewAngle * 0.5f, Vector3.up) * _turretGun.forward;

        Gizmos.DrawRay(_turretGun.position, left * _searchRadius);
        Gizmos.DrawRay(_turretGun.position, right * _searchRadius);

        if (_currentTargetBone != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(
                _muzzlePoint != null ? _muzzlePoint.position : _turretGun.position,
                _currentTargetBone.position);
        }
    }
}