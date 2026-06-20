using Sandbox.Player;
using UnityEngine;

namespace Creatures.Navigation
{
    [DisallowMultipleComponent]
    public class CreatureRangedProjectile : MonoBehaviour, ICreatureProjectile
    {
        private static readonly int DefaultHitMask = Physics.DefaultRaycastLayers
                                                     & ~(1 << 5)
                                                     & ~(1 << 8);
        private static readonly RaycastHit[] Hits = new RaycastHit[16];

        [SerializeField] private float _speed = 55f;
        [SerializeField] private float _lifeTime = 2.5f;
        [SerializeField] private float _hitRadius = 0.12f;
        [SerializeField] private LayerMask _hitMask;
        [SerializeField] private Vector3 _visualRotationOffset = new Vector3(0f, 180f, 0f);

        private CreatureInformation _attacker;
        private int _damage;
        private float _endTime;
        private Vector3 _direction;
        private bool _initialized;

        public void Init(CreatureInformation attacker, GameObject target, int damage)
        {
            Init(attacker, target, damage, transform.forward);
        }

        public void Init(CreatureInformation attacker, GameObject target, int damage, Vector3 direction)
        {
            _attacker = attacker;
            _damage = damage;
            _direction = direction.sqrMagnitude > 0.001f ? direction.normalized : transform.forward;
            _endTime = Time.time + Mathf.Max(0.05f, _lifeTime);
            _initialized = true;

            transform.rotation = Quaternion.LookRotation(_direction) * Quaternion.Euler(_visualRotationOffset);
        }

        private void Update()
        {
            if (!_initialized)
                return;

            if (Time.time >= _endTime)
            {
                Destroy(gameObject);
                return;
            }

            float distance = Mathf.Max(0f, _speed) * Time.deltaTime;
            Vector3 origin = transform.position;

            if (TryHit(origin, distance, out RaycastHit hit))
            {
                ApplyDamage(hit.collider);
                Destroy(gameObject);
                return;
            }

            transform.position = origin + _direction * distance;
        }

        private bool TryHit(Vector3 origin, float distance, out RaycastHit closestHit)
        {
            closestHit = default;

            if (distance <= 0f)
                return false;

            int mask = _hitMask.value != 0 ? _hitMask.value : DefaultHitMask;
            int hitCount = Physics.SphereCastNonAlloc(
                origin,
                Mathf.Max(0.01f, _hitRadius),
                _direction,
                Hits,
                distance,
                mask,
                QueryTriggerInteraction.Ignore);

            float closestDistance = float.MaxValue;
            bool hasHit = false;
            Transform attackerRoot = _attacker != null ? _attacker.transform : null;

            for (int i = 0; i < hitCount; i++)
            {
                Collider hitCollider = Hits[i].collider;
                if (hitCollider == null)
                    continue;

                Transform hitTransform = hitCollider.transform;
                if (hitTransform == transform || hitTransform.IsChildOf(transform))
                    continue;

                if (attackerRoot != null && (hitTransform == attackerRoot || hitTransform.IsChildOf(attackerRoot)))
                    continue;

                if (Hits[i].distance >= closestDistance)
                    continue;

                closestDistance = Hits[i].distance;
                closestHit = Hits[i];
                hasHit = true;
            }

            return hasHit;
        }

        private void ApplyDamage(Collider hitCollider)
        {
            if (hitCollider == null)
                return;

            PlayerInformation player = hitCollider.GetComponentInParent<PlayerInformation>();
            if (player != null)
            {
                player.TakeDamage(_damage);
                return;
            }

            CreatureInformation creature = hitCollider.GetComponentInParent<CreatureInformation>();
            if (creature != null && creature != _attacker)
            {
                creature.TakeDamage(_damage, _attacker);
                TryAggroSurvivingCreature(creature);
            }
        }

        private void TryAggroSurvivingCreature(CreatureInformation creature)
        {
            if (creature == null || _attacker == null || creature.Health <= 0)
                return;

            if (!creature.IsEnemyTeam(_attacker.Team))
                return;

            CreatureEnemySensor sensor = creature.GetComponent<CreatureEnemySensor>();
            if (sensor != null)
                sensor.SetEnemyWithoutResponse(_attacker);
        }
    }
}
