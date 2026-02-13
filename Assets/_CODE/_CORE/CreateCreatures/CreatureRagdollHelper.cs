using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

public class CreatureRagdollHelper : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator _animator;
    [SerializeField] private NavMeshAgent _agent;
    [SerializeField] private Rigidbody _mainRigidbody;
    [SerializeField] private Transform _hips;

    [Header("Blend Settings")]
    [SerializeField] private float _getUpDelay = 2f;
    [SerializeField] private float _blendDuration = 0.5f;

    [Header("Auto Get Up")]
    [SerializeField] private float _velocityThreshold = 0.1f;
    [SerializeField] private float _angularThreshold = 0.1f;
    [SerializeField] private float _stableTimeRequired = 1f;

    private Rigidbody[] _ragdollBodies;
    private Dictionary<Transform, Quaternion> _storedRotations = new();

    private bool _isRagdoll;

    private void Awake()
    {
        _ragdollBodies = GetComponentsInChildren<Rigidbody>();
        DisableRagdollPhysics();
    }

    #region PUBLIC

    [ContextMenu("Enable")]
    public void EnableRagdoll()
    {
        if (_isRagdoll) return;

        _isRagdoll = true;

        _animator.enabled = false;
        if (_agent) _agent.enabled = false;

        foreach (var rb in _ragdollBodies)
            rb.isKinematic = false;
    }

    [ContextMenu("Disable")]
    public void DisableRagdoll()
    {
        if (!_isRagdoll) return;

        StartCoroutine(WaitUntilStoppedThenGetUp());
    }

    private bool IsRagdollStable()
    {
        foreach (var rb in _ragdollBodies)
        {
            if (rb == _mainRigidbody) continue;

            if (rb.linearVelocity.magnitude > _velocityThreshold)
                return false;

            if (rb.angularVelocity.magnitude > _angularThreshold)
                return false;
        }

        return true;
    }


    #endregion

    #region ROUTINE
    private IEnumerator WaitUntilStoppedThenGetUp()
    {
        float stableTimer = 0f;

        while (true)
        {
            bool isStable = IsRagdollStable();

            if (isStable)
            {
                stableTimer += Time.deltaTime;

                if (stableTimer >= _stableTimeRequired)
                    break;
            }
            else
            {
                stableTimer = 0f;
            }

            yield return null;
        }

        yield return StartCoroutine(GetUpRoutine());
    }

    private IEnumerator GetUpRoutine()
    {
        yield return new WaitForSeconds(_getUpDelay);

        bool isOnBack = IsLyingOnBack();

        AlignRootToHips();

        StoreBoneRotations();

        DisableRagdollPhysics();

        _animator.enabled = true;

        if (isOnBack)
            _animator.Play("GetUpFromBack", 0, 0f);
        else
            _animator.Play("GetUpFromBelly", 0, 0f);

        yield return BlendToAnimation();

        if (_agent) _agent.enabled = true;

        _isRagdoll = false;
    }

    #endregion

    #region BLEND

    private IEnumerator BlendToAnimation()
    {
        float timer = 0f;

        while (timer < _blendDuration)
        {
            float t = timer / _blendDuration;

            foreach (var pair in _storedRotations)
            {
                Transform bone = pair.Key;
                bone.localRotation = Quaternion.Slerp(pair.Value, bone.localRotation, t);
            }

            timer += Time.deltaTime;
            yield return null;
        }
    }

    private void StoreBoneRotations()
    {
        _storedRotations.Clear();

        foreach (var rb in _ragdollBodies)
        {
            if (rb.transform == transform) continue;

            _storedRotations.Add(rb.transform, rb.transform.localRotation);
        }
    }

    #endregion

    #region UTILS

    private void DisableRagdollPhysics()
    {
        foreach (var rb in _ragdollBodies)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    private bool IsLyingOnBack()
    {
        Vector3 hipsUp = _hips.up;
        return Vector3.Dot(hipsUp, Vector3.up) < 0;
    }

    private void AlignRootToHips()
    {
        Vector3 hipsPosition = _hips.position;
        transform.position = hipsPosition;

        Vector3 forward = Vector3.ProjectOnPlane(_hips.forward, Vector3.up);
        transform.rotation = Quaternion.LookRotation(forward);
    }

    #endregion
}
