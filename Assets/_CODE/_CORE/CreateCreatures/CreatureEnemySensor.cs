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
        public bool HasPlayerTarget { get; private set; }
        public CreatureState AttackState => _attackState;

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
            SetEnemy(enemy, true);
        }

        public void SetEnemyWithoutResponse(CreatureInformation enemy)
        {
            SetEnemy(enemy, false);
        }

        private void SetEnemy(CreatureInformation enemy, bool aggroEnemyBack)
        {
            if (enemy == null)
            {
                ClearTargets();
                return;
            }

            AggroCreature(enemy, aggroEnemyBack);
        }

        public void SetPlayerTarget()
        {
            CurrentEnemy = null;
            HasPlayerTarget = true;
        }

        public void ClearTargets()
        {
            CurrentEnemy = null;
            HasPlayerTarget = false;
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
                AggroCreature(enemy, true);
        }

        public void TryAggroAll()
        {
            _agryAll = true;
            HasPlayerTarget = false;

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

            AggroCreature(enemy, true);
        }

        private void AggroCreature(CreatureInformation enemy, bool aggroEnemyBack)
        {
            if (enemy == null) return;
            if (enemy == _ownerInfo) return;

            bool targetChanged = CurrentEnemy != enemy || HasPlayerTarget;
            HasPlayerTarget = false;
            CurrentEnemy = enemy;

            if (targetChanged)
                SetOwnAttackState();

            if (!aggroEnemyBack)
                return;

            CreatureEnemySensor enemySensor = enemy.GetComponent<CreatureEnemySensor>();
            CreatureStateView enemyView = enemy.GetComponent<CreatureStateView>();

            if (enemySensor != null)
            {
                bool enemyTargetChanged = enemySensor.CurrentEnemy != _ownerInfo || enemySensor.HasPlayerTarget;
                enemySensor.HasPlayerTarget = false;
                enemySensor.CurrentEnemy = _ownerInfo;

                if (enemyTargetChanged)
                    SetAttackState(enemyView, enemySensor, _attackState);
            }
            else
            {
                SetAttackState(enemyView, enemySensor, _attackState);
            }
        }

        private void SetOwnAttackState()
        {
            SetAttackState(_ownerView, this, _attackState);
        }

        private static void SetAttackState(
            CreatureStateView stateView,
            CreatureEnemySensor sensor,
            CreatureState fallbackState)
        {
            if (stateView == null)
                return;

            CreatureState state = sensor != null && sensor.AttackState != null
                ? sensor.AttackState
                : fallbackState;

            if (state != null)
                stateView.SetState(state);
        }
    }
}
