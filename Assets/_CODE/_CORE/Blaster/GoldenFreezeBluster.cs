using System.Collections;
using Creatures.Control;
using Creatures.Navigation;
using UnityEngine;
using UnityEngine.AI;

public class GoldenFreezeBluster : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private CarControlButton _fireButton;
    [SerializeField] private bool _automaticFire = false;

    [Header("Raycast")]
    [SerializeField] private Transform _firePoint;
    [SerializeField] private Camera _camera;
    [SerializeField] private float _range = 60f;
    [SerializeField] private LayerMask _hitMask = ~0;
    [SerializeField] private float _fireCooldown = 0.35f;

    [Header("Golden Statue")]
    [SerializeField] private Texture _goldTexture;
    [SerializeField] private float _statueLifeTime = 30f;
    [SerializeField] private float _settleTimeout = 4f;
    [SerializeField] private float _settledSpeed = 0.08f;
    [SerializeField] private float _impactForce = 4f;

    [Header("Laser Visual")]
    [SerializeField] private LineRenderer _laserLine;
    [SerializeField] private float _laserVisibleTime = 0.08f;

    private float _nextFireTime;
    private Coroutine _laserRoutine;

    private void Awake()
    {
        if (_camera == null)
            _camera = Camera.main;

        if (_laserLine != null)
            _laserLine.enabled = false;
    }

    private void OnEnable()
    {
        if (_camera == null)
            _camera = Camera.main;

        EnsureFireButton();

        if (_fireButton != null)
            _fireButton.OnDown += TryShoot;
    }

    private void OnDisable()
    {
        if (_fireButton != null)
            _fireButton.OnDown -= TryShoot;

        if (_laserRoutine != null)
        {
            StopCoroutine(_laserRoutine);
            _laserRoutine = null;
        }

        if (_laserLine != null)
            _laserLine.enabled = false;
    }

    private void Update()
    {
        if (_automaticFire && _fireButton != null && _fireButton.Pressed)
            TryShoot();

#if UNITY_EDITOR || UNITY_STANDALONE
        if (_fireButton == null && Input.GetMouseButtonDown(0))
            TryShoot();
#endif
    }

    private void TryShoot()
    {
        if (Time.time < _nextFireTime)
            return;

        _nextFireTime = Time.time + Mathf.Max(0.01f, _fireCooldown);
        Shoot();
    }

    private void Shoot()
    {
        Ray ray = GetAimRay();
        Vector3 laserStart = _firePoint != null ? _firePoint.position : ray.origin;
        Vector3 laserEnd = ray.origin + ray.direction * _range;

        if (Physics.Raycast(ray, out RaycastHit hit, _range, _hitMask, QueryTriggerInteraction.Ignore))
        {
            laserEnd = hit.point;

            GameObject creatureRoot = GetCreatureRoot(hit.collider);
            if (creatureRoot != null)
                PetrifyCreature(creatureRoot, ray.direction, hit.point);
        }

        ShowLaser(laserStart, laserEnd);
    }

    private Ray GetAimRay()
    {
        if (_camera != null)
            return _camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        Transform origin = _firePoint != null ? _firePoint : transform;
        return new Ray(origin.position, origin.forward);
    }

    private GameObject GetCreatureRoot(Collider hitCollider)
    {
        if (hitCollider == null)
            return null;

        CreatureInformation creatureInfo = hitCollider.GetComponentInParent<CreatureInformation>();
        if (creatureInfo != null)
            return creatureInfo.gameObject;

        Transform current = hitCollider.transform;
        while (current != null)
        {
            if (current.tag == "Creature" || current.tag == "Creatures")
                return current.gameObject;

            current = current.parent;
        }

        return null;
    }

    private void PetrifyCreature(GameObject creatureRoot, Vector3 hitDirection, Vector3 hitPoint)
    {
        GoldenPetrifiedCreature petrified = creatureRoot.GetComponent<GoldenPetrifiedCreature>();
        if (petrified == null)
            petrified = creatureRoot.AddComponent<GoldenPetrifiedCreature>();

        petrified.Petrify(
            _goldTexture,
            Mathf.Max(0.1f, _statueLifeTime),
            Mathf.Max(0.1f, _settleTimeout),
            Mathf.Max(0f, _settledSpeed),
            hitDirection,
            hitPoint,
            Mathf.Max(0f, _impactForce));
    }

    private void ShowLaser(Vector3 start, Vector3 end)
    {
        if (_laserLine == null)
            return;

        if (_laserRoutine != null)
            StopCoroutine(_laserRoutine);

        _laserRoutine = StartCoroutine(LaserRoutine(start, end));
    }

    private IEnumerator LaserRoutine(Vector3 start, Vector3 end)
    {
        _laserLine.positionCount = 2;
        _laserLine.SetPosition(0, start);
        _laserLine.SetPosition(1, end);
        _laserLine.enabled = true;

        yield return new WaitForSeconds(_laserVisibleTime);

        _laserLine.enabled = false;
        _laserRoutine = null;
    }

    private void EnsureFireButton()
    {
        if (_fireButton != null)
            return;

        CarControlButton[] buttons = FindObjectsByType<CarControlButton>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < buttons.Length; i++)
        {
            if (!buttons[i].gameObject.activeInHierarchy)
                continue;

            string buttonName = buttons[i].name.ToLowerInvariant();
            if (buttonName.Contains("shoot") ||
                buttonName.Contains("fire") ||
                buttonName.Contains("attack") ||
                buttonName.Contains("toolgun"))
            {
                _fireButton = buttons[i];
                return;
            }
        }

        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i].gameObject.activeInHierarchy)
            {
                _fireButton = buttons[i];
                return;
            }
        }
    }
}

public class GoldenPetrifiedCreature : MonoBehaviour
{
    private Coroutine _petrifyRoutine;
    private bool _petrified;

    public void Petrify(
        Texture goldTexture,
        float statueLifeTime,
        float settleTimeout,
        float settledSpeed,
        Vector3 hitDirection,
        Vector3 hitPoint,
        float impactForce)
    {
        if (_petrified)
            return;

        _petrified = true;

        ApplyGoldTexture(goldTexture);
        StopCreatureLogic();
        DropCreature(hitDirection, hitPoint, impactForce);

        if (_petrifyRoutine != null)
            StopCoroutine(_petrifyRoutine);

        _petrifyRoutine = StartCoroutine(PetrifyRoutine(statueLifeTime, settleTimeout, settledSpeed));
    }

    private void ApplyGoldTexture(Texture goldTexture)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Material[] materials = renderers[i].materials;
            for (int j = 0; j < materials.Length; j++)
            {
                Material material = materials[j];
                if (material == null)
                    continue;

                if (goldTexture != null)
                {
                    material.mainTexture = goldTexture;

                    if (material.HasProperty("_BaseMap"))
                        material.SetTexture("_BaseMap", goldTexture);
                }

                Color goldColor = new Color(1f, 0.74f, 0.16f, 1f);
                material.color = goldColor;

                if (material.HasProperty("_BaseColor"))
                    material.SetColor("_BaseColor", goldColor);

                if (material.HasProperty("_Metallic"))
                    material.SetFloat("_Metallic", 1f);

                if (material.HasProperty("_Smoothness"))
                    material.SetFloat("_Smoothness", 0.75f);
            }
        }
    }

    private void StopCreatureLogic()
    {
        CreatureStateView stateView = GetComponent<CreatureStateView>();
        if (stateView != null)
            stateView.enabled = false;

        CreatureEnemySensor enemySensor = GetComponent<CreatureEnemySensor>();
        if (enemySensor != null)
            enemySensor.enabled = false;

        CreatureRangedWeapon rangedWeapon = GetComponent<CreatureRangedWeapon>();
        if (rangedWeapon != null)
            rangedWeapon.enabled = false;

        CreatureRangedCombatContext rangedContext = GetComponent<CreatureRangedCombatContext>();
        if (rangedContext != null)
            rangedContext.enabled = false;

        CreaturesController controller = GetComponent<CreaturesController>();
        if (controller != null)
            controller.enabled = false;

        NavMeshAgent agent = GetComponent<NavMeshAgent>();
        if (agent != null && agent.enabled)
            agent.enabled = false;

        Animator[] animators = GetComponentsInChildren<Animator>(true);
        for (int i = 0; i < animators.Length; i++)
            animators[i].enabled = false;
    }

    private void DropCreature(Vector3 hitDirection, Vector3 hitPoint, float impactForce)
    {
        CreatureRagdollHelper ragdollHelper = GetComponent<CreatureRagdollHelper>();
        if (ragdollHelper != null)
            ragdollHelper.EnableRagdoll();

        Rigidbody[] bodies = GetComponentsInChildren<Rigidbody>(true);
        Vector3 forceDirection = hitDirection.sqrMagnitude > 0.001f
            ? hitDirection.normalized
            : transform.forward;

        for (int i = 0; i < bodies.Length; i++)
        {
            Rigidbody body = bodies[i];
            body.isKinematic = false;
            body.useGravity = true;

            if (impactForce > 0f)
                body.AddForceAtPosition(forceDirection * impactForce, hitPoint, ForceMode.Impulse);
        }
    }

    private IEnumerator PetrifyRoutine(float statueLifeTime, float settleTimeout, float settledSpeed)
    {
        yield return WaitForSettle(settleTimeout, settledSpeed);
        FreezeRigidbodies();

        yield return new WaitForSeconds(statueLifeTime);
        Destroy(gameObject);
    }

    private IEnumerator WaitForSettle(float settleTimeout, float settledSpeed)
    {
        Rigidbody[] bodies = GetComponentsInChildren<Rigidbody>(true);
        float timeoutAt = Time.time + settleTimeout;
        float settledSpeedSqr = settledSpeed * settledSpeed;

        while (Time.time < timeoutAt)
        {
            bool settled = true;
            for (int i = 0; i < bodies.Length; i++)
            {
                Rigidbody body = bodies[i];
                if (body == null)
                    continue;

                if (body.linearVelocity.sqrMagnitude > settledSpeedSqr ||
                    body.angularVelocity.sqrMagnitude > settledSpeedSqr)
                {
                    settled = false;
                    break;
                }
            }

            if (settled)
                break;

            yield return null;
        }
    }

    private void FreezeRigidbodies()
    {
        Rigidbody[] bodies = GetComponentsInChildren<Rigidbody>(true);
        for (int i = 0; i < bodies.Length; i++)
        {
            Rigidbody body = bodies[i];
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.constraints = RigidbodyConstraints.FreezeAll;
        }
    }
}
