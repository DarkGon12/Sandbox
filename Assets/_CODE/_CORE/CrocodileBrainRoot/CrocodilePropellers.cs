using UnityEngine;

public class CrocodilePropellers : MonoBehaviour
{
    private enum RotateAxis
    {
        X,
        Y,
        Z
    }

    [SerializeField] private float _speed = 90f;
    [SerializeField] private RotateAxis _axis = RotateAxis.Y;

    private Transform _cachedTransform;
    private Vector3 _rotationAxis;

    private void Awake()
    {
        _cachedTransform = transform;
        UpdateAxis();
    }

    private void Update()
    {
        _cachedTransform.Rotate(_rotationAxis, _speed * Time.deltaTime, Space.Self);
    }

    private void OnValidate()
    {
        UpdateAxis();
    }

    private void UpdateAxis()
    {
        switch (_axis)
        {
            case RotateAxis.X:
                _rotationAxis = Vector3.right;
                break;

            case RotateAxis.Y:
                _rotationAxis = Vector3.up;
                break;

            case RotateAxis.Z:
                _rotationAxis = Vector3.forward;
                break;
        }
    }
}