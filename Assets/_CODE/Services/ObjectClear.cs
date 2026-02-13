using UnityEngine;

public class ObjectClear : MonoBehaviour
{
    [SerializeField] private float _destroyTime = 1f;

    private void Start()
    {
        Destroy(gameObject, _destroyTime);
    }
}