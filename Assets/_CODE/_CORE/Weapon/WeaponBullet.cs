using UnityEngine;
using Services.ObjectPool;
using System.Collections;
using Creatures.Navigation;

namespace Sandbox.Weapon
{
    [RequireComponent(typeof(Rigidbody))]
    public class WeaponBullet : MonoBehaviour
    {
        private Rigidbody _rb;
        private string _poolTag;
        private float _lifeTime;
        private int _damage;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();

            _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        public void Init(string poolTag, float lifeTime, int damage)
        {
            _damage = damage;
            _poolTag = poolTag;
            _lifeTime = lifeTime;

            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;

            StopAllCoroutines();
            StartCoroutine(LifeRoutine());
        }

        private IEnumerator LifeRoutine()
        {
            yield return new WaitForSeconds(_lifeTime);
            ReturnToPool();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.transform.root.tag == "Creature")
                collision.transform.root.GetComponent<CreatureInformation>().TakeDamage(_damage);

            ReturnToPool();
        }

        private void ReturnToPool()
        {
            StopAllCoroutines();
            ObjectPoolManager.Return(_poolTag, gameObject);
        }
    }
}
