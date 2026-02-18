using UnityEngine;

namespace Creatures.Navigation
{
    [RequireComponent(typeof(Collider))]
    public class CreatureEnemySensor : MonoBehaviour
    {
        [SerializeField] private CreatureState _attackState;

        private CreatureInformation _ownerInfo;
        private CreatureStateView _ownerView;

        public CreatureInformation CurrentEnemy { get; private set; }
        public bool HasEnemy => CurrentEnemy != null;

        private bool _agryAll = false;

        public bool CanAgrryAll => _agryAll;

        private void Awake()
        {
            _ownerView = GetComponent<CreatureStateView>();
            _ownerInfo = GetComponent<CreatureInformation>();
        }

        private void OnTriggerEnter(Collider other)
        {
           // TryAggro(other);
        }

        public void SetEnemy(CreatureInformation enemy)
        {
            CurrentEnemy = enemy;

            if (enemy == null) return;
            if (enemy == _ownerInfo) return;

            enemy.GetComponent<CreatureEnemySensor>().CurrentEnemy = _ownerInfo;
            enemy.GetComponent<CreatureStateView>().SetState(_attackState);

            CurrentEnemy = enemy;
            _ownerView.SetState(_attackState);
        }

        public void ForceRecheck()
        {
            var hits = Physics.OverlapSphere(transform.position, 25f);
            foreach (var hit in hits)
                TryAggro(hit);
        }

        private void TryAggro(Collider other)
        {            
            CreatureInformation enemy = other.GetComponentInParent<CreatureInformation>();
            if (enemy == null) return;
            if (enemy == _ownerInfo) return;

            if (_ownerInfo.IsEnemyTeam(enemy.Team))
            {
                enemy.GetComponent<CreatureEnemySensor>().CurrentEnemy = _ownerInfo;
                enemy.GetComponent<CreatureStateView>().SetState(_attackState);

                CurrentEnemy = enemy;
                _ownerView.SetState(_attackState);
            }
        }

        public void TryAggroAll()
        {
            _agryAll = true;

            var hits = Physics.OverlapSphere(transform.position, 3000f);
            foreach (var hit in hits)
                TryAggro(hit);
                //TryAggroAny(hit);
        }

        private void TryAggroAny(Collider other)
        {
            CreatureInformation enemy = other.GetComponentInParent<CreatureInformation>();
            if (enemy == null) return;
            if (enemy == _ownerInfo) return;

            enemy.GetComponent<CreatureEnemySensor>().CurrentEnemy = _ownerInfo;
            enemy.GetComponent<CreatureStateView>().SetState(_attackState);

            CurrentEnemy = enemy;
            _ownerView.SetState(_attackState);
        }
    }
}