using UnityEngine;

public class AddForceOnTrigger : MonoBehaviour
{
    [Header("Force")]
    [SerializeField] private float _power = 10f;
    [SerializeField] private Vector3 _direction = Vector3.up;
    [SerializeField] private ForceMode _forceMode = ForceMode.Impulse;

    [Header("Rotation")]
    [SerializeField] private GameObject _rotator;
    [SerializeField] private float _rotatorSpeed = 180f; 
    [SerializeField] private Vector3 _rotationAxis = Vector3.forward;

    private void Update()
    {
        if (_rotator == null) return;

        _rotator.transform.Rotate(
            _rotationAxis,
            _rotatorSpeed * Time.deltaTime,
            Space.Self
        );
    }

    private void OnTriggerStay(Collider other)
    {
        if (!other.attachedRigidbody) return;

        Rigidbody rb = other.attachedRigidbody;
        rb.AddForce(_direction.normalized * _power, _forceMode);
    }
}