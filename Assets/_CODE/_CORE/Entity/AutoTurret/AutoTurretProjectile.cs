using Creatures;
using Creatures.Navigation;
using Sandbox.Weapon;
using UnityEngine;

public class AutoTurretProjectile : MonoBehaviour
{
    [SerializeField] private float _lifeTime = 4f;

    private int _damage;
    private float _speed;

    private bool _isSpent;
    private Collider _projectileCollider;
    private Rigidbody _rb;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    public void Init(Transform firePoint,int damage, float speed)
    {
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;

        _damage = damage;
        _speed = speed;

        _projectileCollider = GetComponent<Collider>();
        _rb.AddForce(firePoint.forward * _speed, ForceMode.Impulse);

        Destroy(gameObject, _lifeTime);
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
        }

        SpendProjectile();
    }

    private void SpendProjectile()
    {
        if (_projectileCollider != null)
            _projectileCollider.enabled = false;

        Destroy(gameObject);
    }
}