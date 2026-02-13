using Creatures.Navigation;
using System.Collections.Generic;
using UnityEngine;

namespace Sandbox.Tank
{
    public class Projectile : MonoBehaviour
    {
        [SerializeField] private float _lifeTime = 5f;

        [Header("Explosion")]
        [SerializeField] private float _explosionRadius = 5f;
        [SerializeField] private float _explosionForce = 4000f;
        [SerializeField] private float _upwardsModifier = 0.5f;
        [SerializeField] private LayerMask _affectedLayers;

        [Header("Damage")]
        [SerializeField] private int _damage = 200;

        [Header("FX")]
        [SerializeField] private GameObject _explosionEffect;

        private bool _exploded;

        private void Start()
        {
            Destroy(gameObject, _lifeTime);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (_exploded)
                return;

            _exploded = true;

            Vector3 explosionPoint = collision.contacts[0].point;

            Explode(explosionPoint);
        }

        private void Explode(Vector3 position)
        {
            Collider[] colliders = Physics.OverlapSphere(
                position,
                _explosionRadius,
                _affectedLayers,
                QueryTriggerInteraction.Ignore
            );

            HashSet<CreatureInformation> damagedCreatures = new HashSet<CreatureInformation>();

            foreach (Collider col in colliders)
            {
                CreatureInformation info = col.GetComponentInParent<CreatureInformation>();
                if (info != null && !damagedCreatures.Contains(info))
                {
                    damagedCreatures.Add(info);
                    info.TakeDamage(_damage);
                }

                Rigidbody rb = col.attachedRigidbody;

                if (rb == null || rb.isKinematic)
                    continue;

                rb.AddExplosionForce(
                    _explosionForce,
                    position,
                    _explosionRadius,
                    _upwardsModifier,
                    ForceMode.Impulse
                );
            }

            if (_explosionEffect != null)
            {
                GameObject fx = Instantiate(
                    _explosionEffect,
                    position,
                    Quaternion.identity
                );

                Destroy(fx, 5f);
            }

            Destroy(gameObject);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, _explosionRadius);
        }
#endif
    }
}
