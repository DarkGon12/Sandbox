using Creatures;
using Creatures.Navigation;
using Services.ObjectPool;
using System.Collections;
using UnityEngine;

namespace Sandbox.Weapon
{
    [RequireComponent(typeof(Rigidbody))]
    public class WeaponBullet : MonoBehaviour
    {
        [Header("Blood FX Pool")]
        [SerializeField] private string _bloodFxPoolTag = "BloodFx";
        [SerializeField] private GameObject _bloodFxPrefab;
        [SerializeField] private int _bloodFxPoolSize = 20;

        [SerializeField] private CreatureState _attackPlayerState;

        private static bool _bloodPoolCreated;

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
            if (collision.transform.root.CompareTag("Creature"))
            {
                collision.transform.root
                    .GetComponent<CreatureInformation>()
                    .TakeDamage(_damage);

                collision.transform.root
                    .GetComponent<CreatureEffect>()
                    ?.PlayHitEffect(
                        collision.contacts[0].point,
                        collision.contacts[0].normal
                    );
                collision.transform.root
                    .GetComponent<CreatureStateView>()
                    .SetState(_attackPlayerState);
            }

            ReturnToPool();
        }

        private void ReturnToPool()
        {
            StopAllCoroutines();
            ObjectPoolManager.Return(_poolTag, gameObject);
        }
    }
}
