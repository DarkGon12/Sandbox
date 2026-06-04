using Creatures.Navigation;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerKick : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button _kickButton;

    [Header("Kick Visual")]
    [SerializeField] private GameObject _leg;

    [Header("Raycast")]
    [SerializeField] private Camera _camera;
    [SerializeField] private float _kickDistance = 3f;
    [SerializeField] private LayerMask _kickMask = ~0;

    [Header("Kick Settings")]
    [SerializeField] private float _kickForce = 12f;
    [SerializeField] private int _damage = 25;

    [Header("Effects")]
    [SerializeField] private GameObject _hitParticlePrefab;

    private Animator _anim;

    private void Awake()
    {
        _anim = GetComponent<Animator>();

        if (_camera == null)
            _camera = Camera.main;

        if (_leg != null)
            _leg.SetActive(false);
    }

    private void OnEnable()
    {
        if (_kickButton != null)
            _kickButton.onClick.AddListener(Kick);
    }

    private void OnDisable()
    {
        if (_kickButton != null)
            _kickButton.onClick.RemoveListener(Kick);

        if (_leg != null)
            _leg.SetActive(false);
    }

    public void Kick()
    {
        if (_leg == null)
            return;

        _leg.SetActive(true);
        _anim.SetTrigger("kick");
    }

    public void DoKickHit()
    {
        if (!TryGetKickHit(out RaycastHit hit))
            return;

        CreatureRagdollHelper ragdollHelper =
            hit.collider.GetComponentInParent<CreatureRagdollHelper>();

        CreatureInformation creatureInformation =
            hit.collider.GetComponentInParent<CreatureInformation>();

        if (ragdollHelper != null)
        {
            ragdollHelper.EnableRagdoll();
            ragdollHelper.DisableRagdoll();
        }

        if (creatureInformation != null)
            creatureInformation.TakeDamage(_damage);

        Rigidbody rigidbody = hit.rigidbody;

        if (rigidbody == null)
            rigidbody = hit.collider.GetComponentInParent<Rigidbody>();

        if (rigidbody != null)
            PushRigidbody(rigidbody, hit.point);
    }

    public void SpawnKickParticle()
    {
        if (_hitParticlePrefab == null)
            return;

        if (!TryGetKickHit(out RaycastHit hit))
            return;

        Instantiate(
            _hitParticlePrefab,
            hit.point,
            Quaternion.LookRotation(hit.normal)
        );
    }

    private bool TryGetKickHit(out RaycastHit hit)
    {
        hit = default;

        if (_camera == null)
            return false;

        Ray ray = _camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        return Physics.Raycast(
            ray,
            out hit,
            _kickDistance,
            _kickMask,
            QueryTriggerInteraction.Ignore
        );
    }

    private void PushRigidbody(Rigidbody rigidbody, Vector3 hitPoint)
    {
        if (_camera == null || rigidbody == null)
            return;

        Vector3 pushDirection = rigidbody.worldCenterOfMass - _camera.transform.position;

        if (pushDirection.sqrMagnitude <= 0.001f)
            pushDirection = _camera.transform.forward;

        pushDirection.Normalize();

        rigidbody.AddForceAtPosition(
            pushDirection * _kickForce,
            hitPoint,
            ForceMode.Impulse
        );
    }
}