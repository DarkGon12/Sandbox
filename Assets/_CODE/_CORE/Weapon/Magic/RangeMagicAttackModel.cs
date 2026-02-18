using Creatures;
using Creatures.Navigation;
using System.Collections.Generic;
using UnityEngine;

namespace Sandbox.Weapon
{
    public class RangeMagicAttackModel : MonoBehaviour
    {
        [Header("Damage")]
        [SerializeField] private int _damagePerTick = 5;
        [SerializeField] private float _damageInterval = 0.25f;

        [Header("Cone Settings")]
        [SerializeField] private float _maxDistance = 3f;
        [SerializeField] private float _coneAngle = 45f;
        [SerializeField] private float _radius = 1.5f;

        [Header("Refs")]
        [SerializeField] private Transform _origin;
        [SerializeField] private LayerMask _creatureMask;
        [SerializeField] private HandMagicModel _magicModel;

        private float _nextDamageTime;

        private void Update()
        {
            if (!_magicModel.IsAttacking)
                return;

            if (Time.time < _nextDamageTime)
                return;

            DealConeDamage();
            _nextDamageTime = Time.time + _damageInterval;
        }

        // ================= DAMAGE =================

        private void DealConeDamage()
        {
            Collider[] hits = Physics.OverlapSphere(
                _origin.position,
                _maxDistance,
                _creatureMask,
                QueryTriggerInteraction.Ignore);

            HashSet<CreatureInformation> damaged = new HashSet<CreatureInformation>();

            foreach (var hit in hits)
            {
                Transform root = hit.transform.root;

                if (!root.CompareTag("Creature"))
                    continue;

                var creature = root.GetComponent<CreatureInformation>();
                if (creature == null)
                    continue;

                if (!IsInCone(creature.transform))
                    continue;

                if (damaged.Add(creature))
                {
                    creature.TakeDamage(_damagePerTick);
                }
            }
        }

        private bool IsInCone(Transform target)
        {
            Vector3 dirToTarget =
                (target.position - _origin.position).normalized;

            float angle =
                Vector3.Angle(_origin.forward, dirToTarget);

            if (angle > _coneAngle)
                return false;

            float distance =
                Vector3.Distance(_origin.position, target.position);

            return distance <= _maxDistance;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (_origin == null)
                return;

            Gizmos.color = Color.red;

            Gizmos.DrawWireSphere(_origin.position, _maxDistance);

            Vector3 left = Quaternion.Euler(0, -_coneAngle, 0) * _origin.forward;
            Vector3 right = Quaternion.Euler(0, _coneAngle, 0) * _origin.forward;

            Gizmos.DrawLine(_origin.position, _origin.position + left * _maxDistance);
            Gizmos.DrawLine(_origin.position, _origin.position + right * _maxDistance);
        }
#endif
    }
}
