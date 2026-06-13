using UnityEngine;

public class MattressImpactRelay : MonoBehaviour
{
    [SerializeField] private MattressImpactEffects _rootImpactEffects;

    private void Awake()
    {
        if (_rootImpactEffects == null)
            _rootImpactEffects = GetComponentInParent<MattressImpactEffects>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (_rootImpactEffects == null)
            return;

        _rootImpactEffects.ProcessCollision(collision);
    }
}